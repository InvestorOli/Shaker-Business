using Microsoft.EntityFrameworkCore;
using ShakerBusiness.Data;

namespace ShakerBusiness.Services;

public sealed class LeaderboardService(IDbContextFactory<ShakerDbContext> dbFactory)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(30);

    private sealed record Snapshot(DateTime LoadedUtc, IReadOnlyList<PlayerAccount> Accounts);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile Snapshot _snapshot = new(DateTime.MinValue, []);

    public async Task<IReadOnlyList<PlayerAccount>> GetAccountsAsync(bool forceRefresh = false)
    {
        var current = _snapshot;
        if (!forceRefresh && IsFresh(current))
        {
            return current.Accounts;
        }

        await _refreshLock.WaitAsync();
        try
        {
            current = _snapshot;
            if (!forceRefresh && IsFresh(current))
            {
                return current.Accounts;
            }

            await using var db = await dbFactory.CreateDbContextAsync();
            var accounts = await db.PlayerAccounts
                .AsNoTracking()
                .Where(p => !p.IsHidden)
                .Select(p => new PlayerAccount
                {
                    TwitchUserId = p.TwitchUserId,
                    DisplayName = p.DisplayName,
                    ProfileImageUrl = p.ProfileImageUrl,
                    LifetimeEarnings = p.LifetimeEarnings,
                    AngelCount = p.AngelCount,
                    LastOnlineUtc = p.LastOnlineUtc,
                    IsModerator = p.IsModerator,
                    IsDev = p.IsDev,
                    SnakeHighScore = p.SnakeHighScore,
                    TimberHighScore = p.TimberHighScore,
                    FishingXp = p.FishingXp,
                    PrestigeCount = p.PrestigeCount,
                    LastPrestigeUtc = p.LastPrestigeUtc,
                })
                .ToListAsync();

            _snapshot = new Snapshot(DateTime.UtcNow, accounts);
            return accounts;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static bool IsFresh(Snapshot snapshot) => DateTime.UtcNow - snapshot.LoadedUtc < MaxAge;
}
