using System.Numerics;
using Microsoft.EntityFrameworkCore;
using ShakerBusiness.Data;
using ShakerBusiness.Models;

namespace ShakerBusiness.Services;

public sealed record FishingAdminBaitDto(string BaitId, string Name, int Count);

public sealed record FishingAdminQuestDto(string FishId, bool Completed, DateTime BatchUtc, TimeSpan TimeUntilRefresh);

public sealed record FishingAdminInfoDto(long Pearls, int RodLevel, string? ActiveBaitId, long FishingXp, int FishingLevel, int CatchCount, IReadOnlyList<FishingAdminBaitDto> BaitCounts, IReadOnlyList<FishingAdminQuestDto> Quests);

public sealed class AdminToolService(
    IDbContextFactory<ShakerDbContext> dbFactory,
    LiveGameSessionDirectory liveDirectory,
    GameEngineService callerEngine,
    FishingPondService fishingPond,
    GlobalBoostService globalBoost,
    MaintenanceModeService maintenanceMode,
    GameEndService gameEnd)
{
    private static void EnsureAuthorized(GameEngineService engine)
    {
        if (!engine.IsDev)
        {
            throw new UnauthorizedAccessException("AdminToolService requires an authorized Dev session.");
        }
    }

    public async Task<(bool Success, string Message)> StartRevenueBoostAsync(double multiplier, int minutes)
    {
        EnsureAuthorized(callerEngine);

        if (!double.IsFinite(multiplier) || multiplier <= 1 || multiplier > 2.5 || minutes <= 0)
        {
            return (false, "Multiplikator muss über 1 und höchstens 2,5 liegen, die Dauer über 0 Minuten.");
        }

        await globalBoost.StartAsync(multiplier, TimeSpan.FromMinutes(minutes));
        return (true, $"Boost x{multiplier} für {minutes} Minuten gestartet.");
    }

    public async Task<(bool Success, string Message)> StopRevenueBoostAsync()
    {
        EnsureAuthorized(callerEngine);
        await globalBoost.StopAsync();
        return (true, "Boost beendet.");
    }

    public (bool IsActive, double Multiplier, DateTime? ExpiresUtc) GetRevenueBoostStatus()
    {
        EnsureAuthorized(callerEngine);
        return (globalBoost.IsActive, globalBoost.CurrentMultiplier, globalBoost.ExpiresUtc);
    }

    public async Task<(bool Success, string Message)> SetMaintenanceModeAsync(bool enabled)
    {
        EnsureAuthorized(callerEngine);
        await maintenanceMode.SetEnabledAsync(enabled);
        return (true, enabled ? "Wartungsmodus aktiviert." : "Wartungsmodus deaktiviert.");
    }

    public bool GetMaintenanceModeStatus()
    {
        EnsureAuthorized(callerEngine);
        return maintenanceMode.IsEnabled;
    }

    public async Task<(bool Success, string Message)> SetGameEndAsync(bool enabled, DateTime? endsAtUtc)
    {
        EnsureAuthorized(callerEngine);

        if (enabled && endsAtUtc is null)
        {
            return (false, "Bitte Datum und Uhrzeit festlegen, bevor du das Spielende aktivierst.");
        }

        await gameEnd.SetAsync(enabled, endsAtUtc);
        return (true, enabled ? $"Spielende aktiviert: {gameEnd.EndsAtLocalText}." : "Spielende deaktiviert.");
    }

    public (bool Enabled, DateTime? EndsAtUtc) GetGameEndStatus()
    {
        EnsureAuthorized(callerEngine);
        return (gameEnd.IsEnabled, gameEnd.EndsAtUtc);
    }

    public async Task<List<PlayerAccount>> SearchAccountsAsync(string query)
    {
        EnsureAuthorized(callerEngine);
        query = query.Trim();
        if (query.Length == 0)
        {
            return [];
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.PlayerAccounts
            .AsNoTracking()
            .Where(p => p.TwitchUserId == query || p.DisplayName.Contains(query))
            .OrderBy(p => p.DisplayName)
            .Take(20)
            .ToListAsync();
    }

    public async Task<(List<ResetLogEntry> Items, int TotalCount)> GetResetLogsAsync(
        string? userFilter,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize)
    {
        EnsureAuthorized(callerEngine);

        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.ResetLogEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userFilter))
        {
            var trimmed = userFilter.Trim();
            query = query.Where(r => r.TwitchUserId == trimmed || r.DisplayName.Contains(trimmed));
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(r => r.TimestampUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(r => r.TimestampUtc <= toUtc.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.TimestampUtc)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<AdminEditLogEntry>> GetRecentAdminEditsAsync(int take = 100)
    {
        EnsureAuthorized(callerEngine);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.AdminEditLogEntries
            .AsNoTracking()
            .OrderByDescending(r => r.TimestampUtc)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<AdminBusinessEditLogEntry>> GetRecentBusinessEditsAsync(int take = 100)
    {
        EnsureAuthorized(callerEngine);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.AdminBusinessEditLogEntries
            .AsNoTracking()
            .OrderByDescending(r => r.TimestampUtc)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<AdminRoleEditLogEntry>> GetRecentRoleEditsAsync(int take = 100)
    {
        EnsureAuthorized(callerEngine);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.AdminRoleEditLogEntries
            .AsNoTracking()
            .OrderByDescending(r => r.TimestampUtc)
            .Take(take)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> SetModeratorAsync(
        string targetTwitchUserId,
        bool isModerator,
        string adminTwitchUserId,
        string adminDisplayName)
    {
        EnsureAuthorized(callerEngine);

        var wasOnline = await KillSessionIfOnlineAsync(targetTwitchUserId);

        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts.FirstOrDefaultAsync(a => a.TwitchUserId == targetTwitchUserId);
        if (account is null)
        {
            return (false, "Spieler nicht gefunden.");
        }

        var before = account.IsModerator;
        account.IsModerator = isModerator;

        db.AdminRoleEditLogEntries.Add(new AdminRoleEditLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            TargetTwitchUserId = targetTwitchUserId,
            TargetDisplayName = account.DisplayName,
            AdminTwitchUserId = adminTwitchUserId,
            AdminDisplayName = adminDisplayName,
            AppliedLive = wasOnline,
            ModeratorBefore = before,
            ModeratorAfter = isModerator,
        });

        await db.SaveChangesAsync();

        return (true, wasOnline
            ? "Spieler war online: Sitzung wurde beendet, Mod-Status wurde direkt in der Datenbank aktualisiert."
            : "Spieler war offline: Mod-Status wurde direkt in der Datenbank aktualisiert.");
    }

    private async Task<bool> KillSessionIfOnlineAsync(string targetTwitchUserId)
    {
        if (!liveDirectory.TryGet(targetTwitchUserId, out var liveEngine) || liveEngine is null)
        {
            return false;
        }

        await liveEngine.AdminKillSessionAsync();
        return true;
    }

    public bool IsUserOnline(string twitchUserId)
    {
        EnsureAuthorized(callerEngine);
        return liveDirectory.TryGet(twitchUserId, out _);
    }

    public async Task<(bool Success, string Message)> OverwriteStatsAsync(
        string targetTwitchUserId,
        BigInteger cash,
        BigInteger lifetimeEarnings,
        BigInteger angelCount,
        BigInteger earningsAtLastReset,
        double oliModifierPercent,
        int prestigeCount,
        string adminTwitchUserId,
        string adminDisplayName)
    {
        EnsureAuthorized(callerEngine);

        if (!BigNumberFormatter.IsStorable(cash)
            || !BigNumberFormatter.IsStorable(lifetimeEarnings)
            || !BigNumberFormatter.IsStorable(angelCount)
            || !BigNumberFormatter.IsStorable(earningsAtLastReset))
        {
            return (false, $"Zahlenwerte müssen positiv sein und dürfen höchstens {BigNumberFormatter.MaxStoredDigits} Stellen haben.");
        }

        if (prestigeCount < 0 || prestigeCount > GameData.MaxPrestigeCount || !double.IsFinite(oliModifierPercent))
        {
            return (false, $"Prestige-Zähler muss zwischen 0 und {GameData.MaxPrestigeCount} liegen und Oli-Prozent muss eine gültige Zahl sein.");
        }

        var wasOnline = await KillSessionIfOnlineAsync(targetTwitchUserId);

        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts.FirstOrDefaultAsync(a => a.TwitchUserId == targetTwitchUserId);
        if (account is null)
        {
            return (false, "Spieler nicht gefunden.");
        }

        var log = new AdminEditLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            TargetTwitchUserId = targetTwitchUserId,
            TargetDisplayName = account.DisplayName,
            AdminTwitchUserId = adminTwitchUserId,
            AdminDisplayName = adminDisplayName,
            AppliedLive = wasOnline,
            CashBefore = account.Cash.ToString(),
            LifetimeEarningsBefore = account.LifetimeEarnings.ToString(),
            AngelCountBefore = account.AngelCount.ToString(),
            EarningsAtLastResetBefore = account.EarningsAtLastReset.ToString(),
            OliModifierPercentBefore = account.OliModifierPercent,
            PrestigeCountBefore = account.PrestigeCount,
        };

        account.Cash = cash;
        account.LifetimeEarnings = lifetimeEarnings;
        account.AngelCount = angelCount;
        account.EarningsAtLastReset = earningsAtLastReset;
        account.OliModifierPercent = oliModifierPercent;

        if (prestigeCount != account.PrestigeCount)
        {
            account.LastPrestigeUtc = DateTime.UtcNow;
        }

        account.PrestigeCount = prestigeCount;

        log.CashAfter = account.Cash.ToString();
        log.LifetimeEarningsAfter = account.LifetimeEarnings.ToString();
        log.AngelCountAfter = account.AngelCount.ToString();
        log.EarningsAtLastResetAfter = account.EarningsAtLastReset.ToString();
        log.OliModifierPercentAfter = account.OliModifierPercent;
        log.PrestigeCountAfter = account.PrestigeCount;

        db.AdminEditLogEntries.Add(log);
        await db.SaveChangesAsync();

        return (true, wasOnline
            ? "Spieler war online: Sitzung wurde beendet, Datenbank wurde direkt aktualisiert."
            : "Spieler war offline: Datenbank wurde direkt aktualisiert.");
    }

    public async Task<Dictionary<string, int>> GetBusinessLevelsAsync(string twitchUserId)
    {
        EnsureAuthorized(callerEngine);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.PlayerBusinesses
            .AsNoTracking()
            .Where(b => b.TwitchUserId == twitchUserId)
            .ToDictionaryAsync(b => b.BusinessId, b => b.Owned);
    }

    public async Task<(bool Success, string Message)> OverwriteBusinessLevelsAsync(
        string targetTwitchUserId,
        Dictionary<string, int> ownedByBusinessId,
        string adminTwitchUserId,
        string adminDisplayName)
    {
        EnsureAuthorized(callerEngine);

        if (ownedByBusinessId.Keys.Any(id => GameData.Businesses.All(b => b.Id != id)))
        {
            return (false, "Unbekanntes Business in der Eingabe.");
        }

        var wasOnline = await KillSessionIfOnlineAsync(targetTwitchUserId);

        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts
            .Include(a => a.Businesses)
            .FirstOrDefaultAsync(a => a.TwitchUserId == targetTwitchUserId);

        if (account is null)
        {
            return (false, "Spieler nicht gefunden.");
        }

        foreach (var (businessId, owned) in ownedByBusinessId)
        {
            var after = Math.Max(0, owned);
            var row = account.Businesses.FirstOrDefault(b => b.BusinessId == businessId);
            int before;

            if (row is null)
            {
                before = 0;
                row = new PlayerBusiness { TwitchUserId = targetTwitchUserId, BusinessId = businessId, Owned = after };
                db.PlayerBusinesses.Add(row);
            }
            else
            {
                before = row.Owned;
                row.Owned = after;
            }

            if (before == after)
            {
                continue;
            }

            if (after > 0)
            {
                row.HasManager = true;
            }

            db.AdminBusinessEditLogEntries.Add(new AdminBusinessEditLogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                TargetTwitchUserId = targetTwitchUserId,
                TargetDisplayName = account.DisplayName,
                AdminTwitchUserId = adminTwitchUserId,
                AdminDisplayName = adminDisplayName,
                AppliedLive = wasOnline,
                BusinessId = businessId,
                OwnedBefore = before,
                OwnedAfter = after,
            });
        }

        await db.SaveChangesAsync();

        return (true, wasOnline
            ? "Spieler war online: Sitzung wurde beendet, Business-Level wurden direkt in der Datenbank aktualisiert."
            : "Spieler war offline: Business-Level wurden direkt in der Datenbank aktualisiert.");
    }

    public async Task<FishingAdminInfoDto> GetFishingInfoAsync(string twitchUserId)
    {
        EnsureAuthorized(callerEngine);

        await using var db = await dbFactory.CreateDbContextAsync();
        var profile = await db.PlayerFishingProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.TwitchUserId == twitchUserId);
        var account = await db.PlayerAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.TwitchUserId == twitchUserId);
        var catchCount = await db.PlayerFishCatches.CountAsync(c => c.TwitchUserId == twitchUserId);
        var baitEntries = await db.PlayerBaitInventoryEntries.AsNoTracking().Where(b => b.TwitchUserId == twitchUserId).ToListAsync();

        var baitCounts = FishingData.Baits
            .Select(b => new FishingAdminBaitDto(b.Id, b.Name, baitEntries.FirstOrDefault(e => e.BaitId == b.Id)?.Count ?? 0))
            .ToList();

        var questRows = await db.PlayerFishingQuests.AsNoTracking().Where(q => q.TwitchUserId == twitchUserId).OrderBy(q => q.Id).ToListAsync();
        var quests = questRows
            .Select(q =>
            {
                var remaining = FishingData.QuestRefreshInterval - (DateTime.UtcNow - q.BatchUtc);
                return new FishingAdminQuestDto(q.FishId, q.Completed, q.BatchUtc, remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
            })
            .ToList();

        var xp = account?.FishingXp ?? 0;
        return new FishingAdminInfoDto(
            profile?.Pearls ?? 0,
            profile?.RodLevel ?? FishingData.MinRodLevel,
            profile?.ActiveBaitId,
            xp,
            FishingData.LevelForXp(xp),
            catchCount,
            baitCounts,
            quests);
    }

    public async Task<(bool Success, string Message)> OverwriteFishingAsync(
        string targetTwitchUserId,
        long pearls,
        int rodLevel,
        long fishingXp,
        IReadOnlyDictionary<string, int> baitCounts)
    {
        EnsureAuthorized(callerEngine);

        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts.FirstOrDefaultAsync(a => a.TwitchUserId == targetTwitchUserId);
        if (account is null)
        {
            return (false, "Spieler nicht gefunden.");
        }

        var profile = await db.PlayerFishingProfiles.FirstOrDefaultAsync(p => p.TwitchUserId == targetTwitchUserId);
        if (profile is null)
        {
            profile = new PlayerFishingProfile { TwitchUserId = targetTwitchUserId, RodLevel = FishingData.MinRodLevel };
            db.PlayerFishingProfiles.Add(profile);
        }

        profile.Pearls = Math.Max(0, pearls);
        profile.RodLevel = Math.Clamp(rodLevel, FishingData.MinRodLevel, FishingData.MaxRodLevel);
        account.FishingXp = Math.Max(0, fishingXp);

        foreach (var bait in FishingData.Baits)
        {
            var count = Math.Max(0, baitCounts.GetValueOrDefault(bait.Id));
            var entry = await db.PlayerBaitInventoryEntries.FirstOrDefaultAsync(b => b.TwitchUserId == targetTwitchUserId && b.BaitId == bait.Id);
            if (entry is null)
            {
                if (count <= 0)
                {
                    continue;
                }

                db.PlayerBaitInventoryEntries.Add(new PlayerBaitInventoryEntry { TwitchUserId = targetTwitchUserId, BaitId = bait.Id, Count = count });
            }
            else
            {
                entry.Count = count;
            }
        }

        if (profile.ActiveBaitId is not null && Math.Max(0, baitCounts.GetValueOrDefault(profile.ActiveBaitId)) <= 0)
        {
            profile.ActiveBaitId = null;
        }

        await db.SaveChangesAsync();

        return (true, "Angel-Daten wurden aktualisiert.");
    }

    public async Task<(bool Success, string Message)> GiveFishAsync(
        string targetTwitchUserId,
        string fishId,
        int level,
        int quantity)
    {
        EnsureAuthorized(callerEngine);

        var fish = FishingData.Find(fishId);
        if (fish is null)
        {
            return (false, "Ungültiger Fisch.");
        }

        var clampedLevel = Math.Clamp(level, 1, FishingData.MaxFishLevel);
        var clampedQuantity = Math.Clamp(quantity, 1, 1000);

        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts.FirstOrDefaultAsync(a => a.TwitchUserId == targetTwitchUserId);
        if (account is null)
        {
            return (false, "Spieler nicht gefunden.");
        }

        for (var i = 0; i < clampedQuantity; i++)
        {
            db.PlayerFishCatches.Add(new PlayerFishCatch
            {
                TwitchUserId = targetTwitchUserId,
                FishId = fish.Id,
                Level = clampedLevel,
                CaughtUtc = DateTime.UtcNow,
            });
        }

        var discovery = await db.PlayerFishDiscoveries.FirstOrDefaultAsync(d => d.TwitchUserId == targetTwitchUserId && d.FishId == fish.Id);
        if (discovery is null)
        {
            db.PlayerFishDiscoveries.Add(new PlayerFishDiscovery
            {
                TwitchUserId = targetTwitchUserId,
                FishId = fish.Id,
                BestLevel = clampedLevel,
                FirstCaughtUtc = DateTime.UtcNow,
            });
        }
        else if (clampedLevel > discovery.BestLevel)
        {
            discovery.BestLevel = clampedLevel;
        }

        await db.SaveChangesAsync();

        return (true, $"{clampedQuantity}x {fish.Name} (Lv {clampedLevel}) wurde ins Inventar gelegt.");
    }

    public IReadOnlyList<HotspotDto> GetFishingHotspots()
    {
        EnsureAuthorized(callerEngine);
        return fishingPond.GetHotspots();
    }

    public (bool Success, string Message) TriggerFishingHotspot(string spotId, string fishId)
    {
        EnsureAuthorized(callerEngine);
        var success = fishingPond.SetHotspot(spotId, fishId);
        return success
            ? (true, "Hotspot wurde ausgelöst.")
            : (false, "Ungültiger Spot oder Fisch.");
    }
}
