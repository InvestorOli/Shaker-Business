using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ShakerBusiness.Data;

namespace ShakerBusiness.Services;

public sealed record GlobalChatMessage(Guid Id, string TwitchUserId, string DisplayName, string? ProfileImageUrl, bool IsModerator, bool IsDev, bool IsAdminPost, string Text, DateTime SentUtc, bool IsSystemMessage = false, int PrestigeCount = 0);

public sealed class GlobalChatService(IDbContextFactory<ShakerDbContext> dbFactory)
{
    public const int MaxWords = 200;
    public const int MaxChars = 1500;
    public const string AllowedCharsDescription = "a-z A-Z 0-9 ä ö ü Ä Ö Ü ß - . _ @ : + % ? ! , ( ) # und Leerzeichen";

    private const int MaxStoredMessages = 500;
    private static readonly TimeSpan MessageLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan RateLimitInterval = TimeSpan.FromSeconds(2);
    private static readonly Regex AllowedCharsPattern = new(@"^[a-zA-Z0-9äöüÄÖÜß\-._@:+%?!,()# ]+$", RegexOptions.Compiled);

    private static readonly string[] BannedWords =
    [
        "nigger", "nigga", "nazi", "hitler", "schwul", "schwuchtel",
        "hurensohn", "hure", "nutte", "schlampe", "fotze", "wichser", 
        "wichsen", "arschloch", "scheisse", "scheiss", "spast", "spasti", 
        "mongo", "behindert", "kanake", "neger", "judensau", "untermensch",
    ];

    private readonly object _lock = new();
    private readonly List<GlobalChatMessage> _messages = [];
    private readonly ConcurrentDictionary<string, bool> _bannedUserIds = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastMessageAtUtc = new();
    private volatile bool _bannedLoaded;

    public event Action? OnChanged;

    public IReadOnlyList<GlobalChatMessage> GetMessages()
    {
        lock (_lock)
        {
            PruneExpired();
            return _messages.ToList();
        }
    }

    public bool IsBanned(string twitchUserId) => _bannedUserIds.TryGetValue(twitchUserId, out var banned) && banned;

    public async Task EnsureBannedListLoadedAsync()
    {
        if (_bannedLoaded)
        {
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var bannedIds = await db.PlayerAccounts.Where(p => p.IsChatBanned).Select(p => p.TwitchUserId).ToListAsync();
        foreach (var id in bannedIds)
        {
            _bannedUserIds[id] = true;
        }

        _bannedLoaded = true;
    }

    private async Task<bool> IsDevUserAsync(string twitchUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.PlayerAccounts.Where(p => p.TwitchUserId == twitchUserId).Select(p => p.IsDev).FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error)> PostMessageAsync(string? twitchUserId, string displayName, string? profileImageUrl, bool isModerator, bool isDev, bool isAdminPost, string rawText, int prestigeCount = 0)
    {
        if (string.IsNullOrEmpty(twitchUserId))
        {
            return (false, "Nicht angemeldet.");
        }

        await EnsureBannedListLoadedAsync();

        if (IsBanned(twitchUserId))
        {
            return (false, "Du bist vom Chat gesperrt.");
        }

        if (_lastMessageAtUtc.TryGetValue(twitchUserId, out var lastAt) && DateTime.UtcNow - lastAt < RateLimitInterval)
        {
            return (false, "Bitte warte kurz, bevor du die nächste Nachricht schickst.");
        }

        var text = rawText.Trim();
        if (text.Length == 0)
        {
            return (false, "Nachricht ist leer.");
        }

        if (text.Length > MaxChars)
        {
            return (false, $"Nachricht ist zu lang (max. {MaxChars} Zeichen).");
        }

        if (!AllowedCharsPattern.IsMatch(text))
        {
            return (false, $"Nachricht enthält nicht erlaubte Zeichen. Erlaubt: {AllowedCharsDescription}");
        }

        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount > MaxWords)
        {
            return (false, $"Nachricht hat zu viele Wörter (max. {MaxWords}).");
        }

        if (ContainsBannedWord(text))
        {
            return (false, "Nachricht enthält nicht erlaubte Wörter.");
        }

        var message = new GlobalChatMessage(Guid.NewGuid(), twitchUserId, displayName, profileImageUrl, isModerator, isDev, isAdminPost, text, DateTime.UtcNow, PrestigeCount: isAdminPost ? 0 : Math.Max(0, prestigeCount));

        lock (_lock)
        {
            PruneExpired();
            _messages.Add(message);
        }

        _lastMessageAtUtc[twitchUserId] = DateTime.UtcNow;
        OnChanged?.Invoke();
        return (true, null);
    }

    public async Task DeleteMessageAsync(Guid messageId, bool actingIsModerator, bool actingIsDev)
    {
        if (!actingIsModerator && !actingIsDev)
        {
            return;
        }

        GlobalChatMessage? message;
        lock (_lock)
        {
            message = _messages.FirstOrDefault(m => m.Id == messageId);
        }

        if (message is null || (!actingIsDev && await IsDevUserAsync(message.TwitchUserId)))
        {
            return;
        }

        lock (_lock)
        {
            _messages.RemoveAll(m => m.Id == messageId);
        }

        PostSystemMessage($"Eine Nachricht von {message.DisplayName} wurde gelöscht.");
    }

    public async Task SetBannedAsync(string targetTwitchUserId, bool banned, bool actingIsModerator, bool actingIsDev)
    {
        if ((!actingIsModerator && !actingIsDev) || string.IsNullOrEmpty(targetTwitchUserId))
        {
            return;
        }

        if (!actingIsDev && await IsDevUserAsync(targetTwitchUserId))
        {
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts.FindAsync(targetTwitchUserId);
        if (account is null)
        {
            return;
        }

        account.IsChatBanned = banned;
        await db.SaveChangesAsync();

        _bannedUserIds[targetTwitchUserId] = banned;
        PostSystemMessage(banned ? $"{account.DisplayName} wurde vom Chat gebannt." : $"{account.DisplayName} wurde entbannt.");
    }

    private void PostSystemMessage(string text)
    {
        var message = new GlobalChatMessage(Guid.NewGuid(), "", "System", null, false, false, false, text, DateTime.UtcNow, IsSystemMessage: true);

        lock (_lock)
        {
            PruneExpired();
            _messages.Add(message);
        }

        OnChanged?.Invoke();
    }

    private static bool ContainsBannedWord(string text)
    {
        var normalized = text.ToLowerInvariant();
        foreach (var ch in new[] { '-', '.', '_', '@', ':', '+', '%', '?', '!', ',', '(', ')', '#', ' ' })
        {
            normalized = normalized.Replace(ch.ToString(), "");
        }

        foreach (var banned in BannedWords)
        {
            if (normalized.Contains(banned, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void PruneExpired()
    {
        var cutoff = DateTime.UtcNow - MessageLifetime;
        _messages.RemoveAll(m => m.SentUtc < cutoff);

        if (_messages.Count > MaxStoredMessages)
        {
            _messages.RemoveRange(0, _messages.Count - MaxStoredMessages);
        }
    }
}
