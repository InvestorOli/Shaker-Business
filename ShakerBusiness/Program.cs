using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using ShakerBusiness.Components;
using ShakerBusiness.Data;
using ShakerBusiness.Extensions;
using ShakerBusiness.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
{
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(60);
});

builder.Services.AddScoped<GameEngineService>();
builder.Services.AddSingleton<GlobalChatService>();
builder.Services.AddSingleton<PlayerSessionRegistry>();
builder.Services.AddSingleton<LiveGameSessionDirectory>();
builder.Services.AddScoped<AdminToolService>();
builder.Services.AddSingleton<FishingPondService>();
builder.Services.AddSingleton<GlobalBoostService>();
builder.Services.AddSingleton<MaintenanceModeService>();
builder.Services.AddSingleton<LeaderboardService>();
builder.Services.AddSingleton<SlotWalletService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GameEndService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, GameCircuitHandler>();

builder.Services.AddDbContextFactory<ShakerDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("MariaDb"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MariaDb"))));

builder.Services.AddScoped<ShakerDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<ShakerDbContext>>().CreateDbContext());

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ShakerDbContext>()
    .SetApplicationName("ShakerBusiness");

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                               | ForwardedHeaders.XForwardedProto
                               | ForwardedHeaders.XForwardedHost;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
var twitchClientId = builder.Configuration["Twitch:ClientId"];
var twitchClientSecret = builder.Configuration["Twitch:ClientSecret"];
var twitchConfigured = !string.IsNullOrEmpty(twitchClientId) && !string.IsNullOrEmpty(twitchClientSecret);

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    if (twitchConfigured)
    {
        options.DefaultChallengeScheme = "Twitch";
    }
});

authBuilder.AddCookie(options =>
{
    options.Cookie.Name = "Shakercasino.Auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
});

if (twitchConfigured)
{
    authBuilder.AddOAuth("Twitch", options =>
    {
        options.ClientId = twitchClientId!;
        options.ClientSecret = twitchClientSecret!;
        options.CallbackPath = "/signin-twitch";
        options.AuthorizationEndpoint = "https://id.twitch.tv/oauth2/authorize";
        options.TokenEndpoint = "https://id.twitch.tv/oauth2/token";
        options.UserInformationEndpoint = "https://api.twitch.tv/helix/users";
        options.SaveTokens = false;

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "display_name");
        options.ClaimActions.MapJsonKey("urn:twitch:login", "login");
        options.ClaimActions.MapJsonKey("urn:twitch:profileimage", "profile_image_url");

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                request.Headers.Add("Client-Id", context.Options.ClientId);

                using var response = await context.Backchannel.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted);
                var payload = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
                var user = payload.GetProperty("data")[0];

                context.RunClaimActions(user);
            }
        };
    });
}

var app = builder.Build();

await app.EnsureMigrationsAppliedAsync<ShakerDbContext>();
await app.Services.GetRequiredService<GlobalBoostService>().LoadAsync();
await app.Services.GetRequiredService<MaintenanceModeService>().LoadAsync();
await app.Services.GetRequiredService<GameEndService>().LoadAsync();
app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

static string ToLocalPath(string? url) =>
    url is { Length: > 0 } && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\')) ? url : "/";

app.MapGet("/login", (string? returnUrl) => twitchConfigured
    ? Results.Challenge(new AuthenticationProperties { RedirectUri = ToLocalPath(returnUrl), IsPersistent = true }, ["Twitch"])
    : Results.Content("Twitch-Login ist noch nicht konfiguriert (Client-ID/Secret fehlen).", "text/plain"));

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/");
});

static async Task<(IResult? Error, CertificateData? Data)> LoadCertificateAsync(
    HttpContext ctx, GameEndService gameEnd, IDbContextFactory<ShakerDbContext> dbFactory)
{
    if (!gameEnd.HasEnded)
    {
        return (Results.NotFound(), null);
    }

    var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
    {
        return (Results.Unauthorized(), null);
    }

    await using var db = await dbFactory.CreateDbContextAsync();
    var account = await db.PlayerAccounts
        .AsNoTracking()
        .Include(a => a.Businesses)
        .FirstOrDefaultAsync(a => a.TwitchUserId == userId);
    if (account is null || !CertificateService.HasPlayed(account))
    {
        return (Results.Text("Für dich gibt es keine Urkunde, amigo: Wer nie gespielt hat, bekommt auch nichts von Oli.", "text/plain", statusCode: StatusCodes.Status404NotFound), null);
    }

    ctx.Response.Headers.CacheControl = "private, no-store";
    return (null, CertificateService.FromAccount(account, DateTime.UtcNow));
}

app.MapGet("/urkunde", async (HttpContext ctx, GameEndService gameEnd, IDbContextFactory<ShakerDbContext> dbFactory) =>
{
    var (error, data) = await LoadCertificateAsync(ctx, gameEnd, dbFactory);
    return error ?? Results.File(CertificateService.Build(data!), "application/pdf", CertificateService.FileName(data!.PlayerName));
}).RequireAuthorization();

app.MapGet("/urkunde/vorschau", async (HttpContext ctx, GameEndService gameEnd, IDbContextFactory<ShakerDbContext> dbFactory) =>
{
    var (error, data) = await LoadCertificateAsync(ctx, gameEnd, dbFactory);
    if (error is not null)
    {
        return error;
    }

    ctx.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'";
    return Results.Content(CertificateService.BuildPreviewSvg(data!), "image/svg+xml");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
