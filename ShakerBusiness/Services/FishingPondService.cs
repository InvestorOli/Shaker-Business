using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ShakerBusiness.Data;
using ShakerBusiness.Models;

namespace ShakerBusiness.Services;

public sealed class PondPlayerState
{
    public string TwitchUserId { get; init; } = "";
    public string DisplayName { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsCasting { get; set; }
    public double CastX { get; set; }
    public double CastY { get; set; }
    public bool HasBite { get; set; }
    public string? PendingFishId { get; set; }
    public int PendingFishLevel { get; set; }
    public int PendingQteSeed { get; set; }
    public DateTime BiteStartUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public string CastColor { get; set; } = FishingPondService.DefaultCastColor;
}

public sealed record FishInventoryItemDto(string FishId, string Name, FishRarity Rarity, string Color, int Count, int AverageLevel, long TotalValue);

public sealed record BaitInventoryItemDto(string BaitId, string Name, long Price, int Count);

public sealed record FishingProfileDto(long Pearls, int RodLevel, long RodUpgradeCost, string? ActiveBaitId, IReadOnlyList<FishInventoryItemDto> Inventory, IReadOnlyList<BaitInventoryItemDto> BaitInventory, int Level, long XpIntoLevel, long XpSpanForLevel);

public sealed record FishingQuestDto(string FishId, string FishName, FishRarity Rarity, string Color, bool Completed, TimeSpan TimeUntilRefresh);

public sealed record QteInputEventDto(double TMs, bool Holding);

public sealed record HotspotDto(string SpotId, string SpotName, string FishId, string FishName, FishRarity Rarity, string Color, TimeSpan TimeRemaining);

public sealed record FishDexEntryDto(string FishId, string Name, FishRarity Rarity, string Color, bool Discovered, int BestLevel, DateTime? FirstCaughtUtc);

public sealed class FishingPondService : IDisposable
{
    public const string DefaultCastColor = "#e2503f";

    private sealed class HotspotState
    {
        public required string SpotId { get; init; }
        public required string FishId { get; init; }
        public required DateTime ExpiresUtc { get; init; }
    }

    private readonly IDbContextFactory<ShakerDbContext> dbFactory;
    private readonly ConcurrentDictionary<string, PondPlayerState> _players = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _userLocks = new();
    private readonly System.Threading.Timer _cleanupTimer;
    private readonly System.Threading.Timer _hotspotTimer;
    private readonly object _hotspotLock = new();
    private readonly Dictionary<string, HotspotState> _hotspotStates = new();

    public event Action? OnChanged;

    public FishingPondService(IDbContextFactory<ShakerDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
        _cleanupTimer = new System.Threading.Timer(_ => RemoveStalePlayers(), null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
        RollNewHotspots();
        _hotspotTimer = new System.Threading.Timer(_ => RollNewHotspots(), null, FishingData.HotspotRotationInterval, FishingData.HotspotRotationInterval);
    }

    private void RollNewHotspots()
    {
        var maxCount = Math.Min(FishingData.MaxActiveHotspots, FishingData.FishingSpots.Count);
        var count = Random.Shared.Next(1, maxCount + 1);
        var chosenSpots = FishingData.FishingSpots
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList();

        var expiresUtc = DateTime.UtcNow + FishingData.HotspotRotationInterval;
        lock (_hotspotLock)
        {
            _hotspotStates.Clear();
            foreach (var spot in chosenSpots)
            {
                var fish = FishingData.Fish[Random.Shared.Next(FishingData.Fish.Count)];
                _hotspotStates[spot.Id] = new HotspotState
                {
                    SpotId = spot.Id,
                    FishId = fish.Id,
                    ExpiresUtc = expiresUtc,
                };
            }
        }

        OnChanged?.Invoke();
    }

    public bool SetHotspot(string spotId, string fishId)
    {
        var spot = FishingData.FishingSpots.FirstOrDefault(s => s.Id == spotId);
        var fish = FishingData.Find(fishId);
        if (spot is null || fish is null)
        {
            return false;
        }

        lock (_hotspotLock)
        {
            _hotspotStates[spot.Id] = new HotspotState
            {
                SpotId = spot.Id,
                FishId = fish.Id,
                ExpiresUtc = DateTime.UtcNow + FishingData.HotspotRotationInterval,
            };
        }

        OnChanged?.Invoke();
        return true;
    }

    public IReadOnlyList<HotspotDto> GetHotspots()
    {
        List<HotspotState> states;
        lock (_hotspotLock)
        {
            states = _hotspotStates.Values.ToList();
        }

        var now = DateTime.UtcNow;
        var result = new List<HotspotDto>();
        foreach (var state in states)
        {
            var spot = FishingData.FishingSpots.FirstOrDefault(s => s.Id == state.SpotId);
            var fish = FishingData.Find(state.FishId);
            if (spot is null || fish is null)
            {
                continue;
            }

            var remaining = state.ExpiresUtc - now;
            result.Add(new HotspotDto(spot.Id, spot.Name, fish.Id, fish.Name, fish.Rarity, fish.Color, remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero));
        }

        return result;
    }

    private HotspotState? GetHotspotForSpot(string spotId)
    {
        lock (_hotspotLock)
        {
            return _hotspotStates.GetValueOrDefault(spotId);
        }
    }

    private static FishingSpotDefinition? FindSpotContainingPoint(double x, double y)
    {
        foreach (var spot in FishingData.FishingSpots)
        {
            var dx = (x - spot.CenterX) / spot.RadiusX;
            var dy = (y - spot.CenterY) / spot.RadiusY;
            if (dx * dx + dy * dy <= 1)
            {
                return spot;
            }
        }

        return null;
    }

    private SemaphoreSlim GetUserLock(string twitchUserId) =>
        _userLocks.GetOrAdd(twitchUserId, _ => new SemaphoreSlim(1, 1));

    private void RemoveStalePlayers()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromSeconds(90);
        var changed = false;
        foreach (var kvp in _players)
        {
            if (kvp.Value.LastSeenUtc < cutoff && _players.TryRemove(kvp.Key, out _))
            {
                changed = true;
            }
        }

        if (changed)
        {
            OnChanged?.Invoke();
        }
    }

    public IReadOnlyCollection<PondPlayerState> GetPlayers(string requesterTwitchUserId)
    {
        if (!_players.TryGetValue(requesterTwitchUserId, out var requester))
        {
            return [];
        }

        var radius = FishingData.PlayerVisibilityRadius;
        return _players.Values
            .Where(p =>
            {
                if (p.TwitchUserId == requesterTwitchUserId)
                {
                    return true;
                }

                var dx = p.X - requester.X;
                var dy = p.Y - requester.Y;
                return dx * dx + dy * dy <= radius * radius;
            })
            .ToList();
    }

    public bool TryGetPlayerPosition(string twitchUserId, out double x, out double y)
    {
        if (_players.TryGetValue(twitchUserId, out var p))
        {
            x = p.X;
            y = p.Y;
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    public void Join(string twitchUserId, string displayName, string? profileImageUrl, double x, double y)
    {
        _players[twitchUserId] = new PondPlayerState
        {
            TwitchUserId = twitchUserId,
            DisplayName = displayName,
            ProfileImageUrl = profileImageUrl,
            X = x,
            Y = y,
            LastSeenUtc = DateTime.UtcNow,
        };
        OnChanged?.Invoke();
    }

    public void Leave(string twitchUserId)
    {
        if (_players.TryRemove(twitchUserId, out _))
        {
            OnChanged?.Invoke();
        }
    }

    public void UpdatePosition(string twitchUserId, string displayName, string? profileImageUrl, double x, double y)
    {
        var now = DateTime.UtcNow;
        if (!_players.TryGetValue(twitchUserId, out var p))
        {
            _players[twitchUserId] = new PondPlayerState
            {
                TwitchUserId = twitchUserId,
                DisplayName = displayName,
                ProfileImageUrl = profileImageUrl,
                X = Math.Clamp(x, 0, 1),
                Y = Math.Clamp(y, 0, 1),
                LastSeenUtc = now,
            };
            OnChanged?.Invoke();
            return;
        }

        var elapsedSec = Math.Max(0.05, (now - p.LastSeenUtc).TotalSeconds);
        var maxDist = FishingData.ServerMoveSpeedLimit * FishingData.MoveToleranceFactor * elapsedSec;
        var dx = x - p.X;
        var dy = y - p.Y;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist > maxDist && dist > 0)
        {
            var scale = maxDist / dist;
            x = p.X + dx * scale;
            y = p.Y + dy * scale;
        }

        p.X = Math.Clamp(x, 0, 1);
        p.Y = Math.Clamp(y, 0, 1);
        p.LastSeenUtc = now;
        OnChanged?.Invoke();
    }

    public bool StartCast(string twitchUserId, double castX, double castY)
    {
        if (!_players.TryGetValue(twitchUserId, out var p) || p.IsCasting)
        {
            return false;
        }

        if (FindSpotContainingPoint(castX, castY) is null)
        {
            return false;
        }

        var dx = castX - p.X;
        var dy = castY - p.Y;
        if (dx * dx + dy * dy > FishingData.MaxCastDistance * FishingData.MaxCastDistance)
        {
            return false;
        }

        p.IsCasting = true;
        p.CastX = castX;
        p.CastY = castY;
        p.HasBite = false;
        p.PendingFishId = null;
        p.LastSeenUtc = DateTime.UtcNow;
        OnChanged?.Invoke();
        return true;
    }

    public void SetCastColor(string twitchUserId, string? baitId)
    {
        if (_players.TryGetValue(twitchUserId, out var p))
        {
            var bait = baitId is null ? null : FishingData.FindBait(baitId);
            var color = bait?.Color ?? DefaultCastColor;
            if (p.CastColor != color)
            {
                p.CastColor = color;
                OnChanged?.Invoke();
            }
        }
    }

    public void CancelCast(string twitchUserId)
    {
        if (_players.TryGetValue(twitchUserId, out var p))
        {
            p.IsCasting = false;
            p.HasBite = false;
            p.PendingFishId = null;
            OnChanged?.Invoke();
        }
    }

    private static async Task<PlayerFishingProfile> GetOrCreateProfileInternalAsync(ShakerDbContext db, string twitchUserId)
    {
        var profile = await db.PlayerFishingProfiles.FirstOrDefaultAsync(p => p.TwitchUserId == twitchUserId);
        if (profile is not null)
        {
            return profile;
        }

        profile = new PlayerFishingProfile
        {
            TwitchUserId = twitchUserId,
            Pearls = FishingData.StartingPearls,
            RodLevel = FishingData.MinRodLevel,
        };
        db.PlayerFishingProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    public async Task<(FishDefinition? Fish, int Level, int QteSeed)> TriggerBiteAsync(string twitchUserId)
    {
        if (!_players.TryGetValue(twitchUserId, out var p) || !p.IsCasting)
        {
            return (null, 0, 0);
        }

        var userLock = GetUserLock(twitchUserId);
        await userLock.WaitAsync();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var profile = await GetOrCreateProfileInternalAsync(db, twitchUserId);

            var baitRarityBonus = 0;
            var maxRarity = FishingData.NoBaitMaxRarity;
            if (!string.IsNullOrEmpty(profile.ActiveBaitId))
            {
                var bait = FishingData.FindBait(profile.ActiveBaitId);
                var baitEntry = await db.PlayerBaitInventoryEntries.FirstOrDefaultAsync(b => b.TwitchUserId == twitchUserId && b.BaitId == profile.ActiveBaitId);
                if (bait is not null && baitEntry is not null && baitEntry.Count > 0)
                {
                    baitRarityBonus = bait.RarityBonusLevels;
                    maxRarity = bait.MaxRarity;
                    baitEntry.Count -= 1;
                    if (baitEntry.Count <= 0)
                    {
                        profile.ActiveBaitId = null;
                    }
                    await db.SaveChangesAsync();
                }
                else
                {
                    profile.ActiveBaitId = null;
                    await db.SaveChangesAsync();
                }
            }

            var castSpot = FindSpotContainingPoint(p.CastX, p.CastY);
            string? hotspotFishId = null;
            var hotspotState = castSpot is null ? null : GetHotspotForSpot(castSpot.Id);
            if (hotspotState is not null && hotspotState.ExpiresUtc > DateTime.UtcNow)
            {
                hotspotFishId = hotspotState.FishId;
            }

            var fish = FishingData.RollFish(profile.RodLevel, baitRarityBonus, Random.Shared, maxRarity, hotspotFishId);
            var level = FishingData.RollFishLevel(profile.RodLevel, Random.Shared);
            var seed = Random.Shared.Next(int.MinValue, int.MaxValue);

            p.HasBite = true;
            p.PendingFishId = fish.Id;
            p.PendingFishLevel = level;
            p.PendingQteSeed = seed;
            p.BiteStartUtc = DateTime.UtcNow;
            OnChanged?.Invoke();
            return (fish, level, seed);
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task<(bool Success, FishDefinition? Fish, int Level, long XpGained, bool QuestCompleted, IReadOnlyDictionary<string, int>? BatchBaitReward)> ResolveCatchAsync(string twitchUserId, IReadOnlyList<QteInputEventDto> events, double elapsedMs)
    {
        if (!_players.TryGetValue(twitchUserId, out var p) || !p.IsCasting || !p.HasBite || p.PendingFishId is null)
        {
            return (false, null, 0, 0, false, null);
        }

        var fish = FishingData.Find(p.PendingFishId);
        var level = p.PendingFishLevel;
        var seed = p.PendingQteSeed;
        var realElapsedMs = (DateTime.UtcNow - p.BiteStartUtc).TotalMilliseconds;

        p.IsCasting = false;
        p.HasBite = false;
        p.PendingFishId = null;
        OnChanged?.Invoke();

        if (fish is null)
        {
            return (false, null, 0, 0, false, null);
        }

        var replayEvents = events.Select(e => (e.TMs, e.Holding)).ToList();
        var quality = FishingData.SimulateQte(fish.Difficulty, seed, replayEvents, elapsedMs, realElapsedMs);

        if (quality < fish.Difficulty)
        {
            return (false, fish, level, 0, false, null);
        }

        var userLock = GetUserLock(twitchUserId);
        await userLock.WaitAsync();
        try
        {
            var xpGained = FishingData.CatchXp(fish, level);

            await using var db = await dbFactory.CreateDbContextAsync();
            db.PlayerFishCatches.Add(new PlayerFishCatch
            {
                TwitchUserId = twitchUserId,
                FishId = fish.Id,
                Level = level,
                CaughtUtc = DateTime.UtcNow,
            });

            var discovery = await db.PlayerFishDiscoveries.FirstOrDefaultAsync(d => d.TwitchUserId == twitchUserId && d.FishId == fish.Id);
            if (discovery is null)
            {
                discovery = new PlayerFishDiscovery
                {
                    TwitchUserId = twitchUserId,
                    FishId = fish.Id,
                    BestLevel = level,
                    FirstCaughtUtc = DateTime.UtcNow,
                };
                db.PlayerFishDiscoveries.Add(discovery);
            }
            else if (level > discovery.BestLevel)
            {
                discovery.BestLevel = level;
            }

            var questCompleted = false;
            IReadOnlyDictionary<string, int>? batchBaitReward = null;
            var activeQuests = await db.PlayerFishingQuests
                .Where(q => q.TwitchUserId == twitchUserId && !q.Completed)
                .ToListAsync();
            var matchingQuest = activeQuests.FirstOrDefault(q =>
                q.FishId == fish.Id && DateTime.UtcNow - q.BatchUtc < FishingData.QuestRefreshInterval);
            if (matchingQuest is not null)
            {
                matchingQuest.Completed = true;
                questCompleted = true;
                xpGained += FishingData.QuestXpForRarity(fish.Rarity);

                var batchQuests = await db.PlayerFishingQuests
                    .Where(q => q.TwitchUserId == twitchUserId && q.BatchUtc == matchingQuest.BatchUtc)
                    .ToListAsync();
                if (batchQuests.Count > 0 && batchQuests.All(q => q.Completed))
                {
                    xpGained += FishingData.QuestBatchXpBonus;

                    var rewardCounts = new Dictionary<string, int>();
                    for (var i = 0; i < FishingData.QuestBatchBaitReward; i++)
                    {
                        var rewardBaitId = FishingData.Baits[Random.Shared.Next(FishingData.Baits.Count)].Id;
                        rewardCounts[rewardBaitId] = rewardCounts.GetValueOrDefault(rewardBaitId) + 1;
                    }

                    foreach (var (rewardBaitId, count) in rewardCounts)
                    {
                        var entry = await db.PlayerBaitInventoryEntries.FirstOrDefaultAsync(b => b.TwitchUserId == twitchUserId && b.BaitId == rewardBaitId);
                        if (entry is null)
                        {
                            entry = new PlayerBaitInventoryEntry { TwitchUserId = twitchUserId, BaitId = rewardBaitId, Count = 0 };
                            db.PlayerBaitInventoryEntries.Add(entry);
                        }

                        entry.Count += count;
                    }

                    batchBaitReward = rewardCounts;
                }
            }

            var account = await db.PlayerAccounts.FindAsync(twitchUserId);
            if (account is not null)
            {
                account.FishingXp += xpGained;
            }

            await db.SaveChangesAsync();

            return (true, fish, level, xpGained, questCompleted, batchBaitReward);
        }
        finally
        {
            userLock.Release();
        }
    }

    private static async Task<List<PlayerFishingQuest>> GetOrRefreshQuestsInternalAsync(ShakerDbContext db, string twitchUserId, int rodLevel)
    {
        var quests = await db.PlayerFishingQuests
            .Where(q => q.TwitchUserId == twitchUserId)
            .OrderBy(q => q.Id)
            .ToListAsync();

        var needsRefresh = quests.Count == 0 || quests.Any(q => DateTime.UtcNow - q.BatchUtc >= FishingData.QuestRefreshInterval);
        if (!needsRefresh)
        {
            return quests;
        }

        if (quests.Count > 0)
        {
            db.PlayerFishingQuests.RemoveRange(quests);
        }

        var batchUtc = DateTime.UtcNow;
        var weights = FishingData.RarityWeightsForLevel(rodLevel);
        var achievableFish = FishingData.Fish.Where(f => weights.GetValueOrDefault(f.Rarity) > 0).ToList();
        var pool = achievableFish.Count >= FishingData.QuestCount ? achievableFish : FishingData.Fish;

        var chosenFishIds = pool
            .Select(f => f.Id)
            .OrderBy(_ => Random.Shared.Next())
            .Take(FishingData.QuestCount)
            .ToList();

        var fresh = chosenFishIds.Select(fishId => new PlayerFishingQuest
        {
            TwitchUserId = twitchUserId,
            FishId = fishId,
            Completed = false,
            BatchUtc = batchUtc,
        }).ToList();

        db.PlayerFishingQuests.AddRange(fresh);
        await db.SaveChangesAsync();
        return fresh;
    }

    public async Task<List<FishingQuestDto>> GetQuestsAsync(string twitchUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var profile = await GetOrCreateProfileInternalAsync(db, twitchUserId);
        var quests = await GetOrRefreshQuestsInternalAsync(db, twitchUserId, profile.RodLevel);

        return quests
            .Select(q =>
            {
                var fish = FishingData.Find(q.FishId);
                var remaining = FishingData.QuestRefreshInterval - (DateTime.UtcNow - q.BatchUtc);
                return new FishingQuestDto(
                    q.FishId,
                    fish?.Name ?? q.FishId,
                    fish?.Rarity ?? FishRarity.Common,
                    fish?.Color ?? "#8a5a2e",
                    q.Completed,
                    remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
            })
            .ToList();
    }

    public async Task<FishingProfileDto> GetProfileAsync(string twitchUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var profile = await GetOrCreateProfileInternalAsync(db, twitchUserId);

        var catchGroups = await db.PlayerFishCatches
            .AsNoTracking()
            .Where(c => c.TwitchUserId == twitchUserId)
            .GroupBy(c => new { c.FishId, c.Level })
            .Select(g => new { g.Key.FishId, g.Key.Level, Count = g.Count() })
            .ToListAsync();

        var inventory = catchGroups
            .GroupBy(g => g.FishId)
            .Select(byFish => (Fish: FishingData.Find(byFish.Key), Levels: byFish.ToList()))
            .Where(x => x.Fish is not null)
            .Select(x =>
            {
                var count = x.Levels.Sum(l => l.Count);
                return new FishInventoryItemDto(
                    x.Fish!.Id,
                    x.Fish.Name,
                    x.Fish.Rarity,
                    x.Fish.Color,
                    count,
                    (int)Math.Round(x.Levels.Sum(l => (double)l.Level * l.Count) / count),
                    x.Levels.Sum(l => FishingData.FishSellValue(x.Fish, l.Level, profile.RodLevel) * l.Count));
            })
            .OrderByDescending(i => i.Rarity)
            .ThenBy(i => i.Name)
            .ToList();

        var baitEntries = await db.PlayerBaitInventoryEntries
            .AsNoTracking()
            .Where(b => b.TwitchUserId == twitchUserId && b.Count > 0)
            .ToListAsync();

        var baitInventory = baitEntries
            .Select(b => (Bait: FishingData.FindBait(b.BaitId), Entry: b))
            .Where(x => x.Bait is not null)
            .Select(x => new BaitInventoryItemDto(x.Bait!.Id, x.Bait.Name, x.Bait.Price, x.Entry.Count))
            .ToList();

        var account = await db.PlayerAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.TwitchUserId == twitchUserId);
        var xp = account?.FishingXp ?? 0;
        var level = FishingData.LevelForXp(xp);
        var xpAtLevel = FishingData.CumulativeXpForLevel(level);
        var xpAtNext = FishingData.CumulativeXpForLevel(level + 1);

        return new FishingProfileDto(
            profile.Pearls,
            profile.RodLevel,
            FishingData.RodUpgradeCost(profile.RodLevel + 1),
            profile.ActiveBaitId,
            inventory,
            baitInventory,
            level,
            xp - xpAtLevel,
            xpAtNext - xpAtLevel);
    }

    public async Task<List<FishDexEntryDto>> GetFishDexAsync(string twitchUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var discoveries = await db.PlayerFishDiscoveries
            .AsNoTracking()
            .Where(d => d.TwitchUserId == twitchUserId)
            .ToDictionaryAsync(d => d.FishId);

        return FishingData.Fish
            .Select(f =>
            {
                var d = discoveries.GetValueOrDefault(f.Id);
                return new FishDexEntryDto(f.Id, f.Name, f.Rarity, f.Color, d is not null, d?.BestLevel ?? 0, d?.FirstCaughtUtc);
            })
            .OrderBy(e => e.Rarity)
            .ThenBy(e => e.Name)
            .ToList();
    }

    public async Task<(bool Success, string Message)> UpgradeRodAsync(string twitchUserId)
    {
        var userLock = GetUserLock(twitchUserId);
        await userLock.WaitAsync();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var profile = await GetOrCreateProfileInternalAsync(db, twitchUserId);

            if (profile.RodLevel >= FishingData.MaxRodLevel)
            {
                return (false, "Maximale Stufe erreicht.");
            }

            var nextLevel = profile.RodLevel + 1;
            var cost = FishingData.RodUpgradeCost(nextLevel);
            if (profile.Pearls < cost)
            {
                return (false, $"Nicht genug Perlen (benötigt {cost}).");
            }

            profile.Pearls -= cost;
            profile.RodLevel = nextLevel;
            await db.SaveChangesAsync();

            return (true, $"Angelrute ist jetzt Level {nextLevel}!");
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task<(bool Success, string Message)> BuyBaitAsync(string twitchUserId, string baitId, int quantity)
    {
        var bait = FishingData.FindBait(baitId);
        if (bait is null || quantity <= 0)
        {
            return (false, "Ungültiger Köder.");
        }

        var userLock = GetUserLock(twitchUserId);
        await userLock.WaitAsync();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var profile = await GetOrCreateProfileInternalAsync(db, twitchUserId);

            var cost = bait.Price * quantity;
            if (profile.Pearls < cost)
            {
                return (false, $"Nicht genug Perlen (benötigt {cost}).");
            }

            profile.Pearls -= cost;

            var entry = await db.PlayerBaitInventoryEntries.FirstOrDefaultAsync(b => b.TwitchUserId == twitchUserId && b.BaitId == baitId);
            if (entry is null)
            {
                entry = new PlayerBaitInventoryEntry { TwitchUserId = twitchUserId, BaitId = baitId, Count = 0 };
                db.PlayerBaitInventoryEntries.Add(entry);
            }

            entry.Count += quantity;
            await db.SaveChangesAsync();

            return (true, $"{quantity}x {bait.Name} gekauft.");
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task SetActiveBaitAsync(string twitchUserId, string? baitId)
    {
        if (baitId is not null && FishingData.FindBait(baitId) is null)
        {
            return;
        }

        var userLock = GetUserLock(twitchUserId);
        await userLock.WaitAsync();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var profile = await GetOrCreateProfileInternalAsync(db, twitchUserId);

            if (baitId is not null)
            {
                var owned = await db.PlayerBaitInventoryEntries.FirstOrDefaultAsync(b => b.TwitchUserId == twitchUserId && b.BaitId == baitId);
                if (owned is null || owned.Count <= 0)
                {
                    return;
                }
            }

            profile.ActiveBaitId = baitId;
            await db.SaveChangesAsync();
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task<long> SellFishAsync(string twitchUserId, string fishId)
    {
        var fish = FishingData.Find(fishId);
        if (fish is null)
        {
            return 0;
        }

        var userLock = GetUserLock(twitchUserId);
        await userLock.WaitAsync();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var profile = await GetOrCreateProfileInternalAsync(db, twitchUserId);
            var catches = await db.PlayerFishCatches
                .Where(c => c.TwitchUserId == twitchUserId && c.FishId == fishId)
                .ToListAsync();

            if (catches.Count == 0)
            {
                return 0;
            }

            var total = catches.Sum(c => FishingData.FishSellValue(fish, c.Level, profile.RodLevel));
            profile.Pearls += total;
            db.PlayerFishCatches.RemoveRange(catches);
            await db.SaveChangesAsync();
            return total;
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task<long> SellAllFishAsync(string twitchUserId)
    {
        var userLock = GetUserLock(twitchUserId);
        await userLock.WaitAsync();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var profile = await GetOrCreateProfileInternalAsync(db, twitchUserId);
            var catches = await db.PlayerFishCatches
                .Where(c => c.TwitchUserId == twitchUserId)
                .ToListAsync();

            if (catches.Count == 0)
            {
                return 0;
            }

            var total = 0L;
            foreach (var c in catches)
            {
                var fish = FishingData.Find(c.FishId);
                if (fish is not null)
                {
                    total += FishingData.FishSellValue(fish, c.Level, profile.RodLevel);
                }
            }

            profile.Pearls += total;
            db.PlayerFishCatches.RemoveRange(catches);
            await db.SaveChangesAsync();
            return total;
        }
        finally
        {
            userLock.Release();
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        _hotspotTimer.Dispose();
    }
}
