namespace ShakerBusiness.Models;

public static class GameData
{
    public const int MaxPrestigeCount = 1000;
    public const int PrestigeUpgradeCount = 5;

    public static bool IsRetired(int prestigeCount) => prestigeCount >= MaxPrestigeCount;

    public static double PrestigeUpgradeProfitMultiplier(int prestigeLevel) => prestigeLevel switch
    {
        1 => 2,
        2 => 1.5,
        3 => 1.3,
        4 => 1.2,
        _ => 1.1,
    };

    public static string FormatMultiplier(double multiplier) =>
        multiplier.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    public static readonly IReadOnlyList<PrestigeUpgradeDefinition> PrestigeUpgrades = Enumerable.Range(1, PrestigeUpgradeCount)
        .Select(level => new PrestigeUpgradeDefinition(
            level,
            new UpgradeDefinition($"Prestige-{level}", $"Prestige {level}: Schwarzlieferung für alle Businesses (x{FormatMultiplier(PrestigeUpgradeProfitMultiplier(level))})", null, 0, 0, PrestigeUpgradeProfitMultiplier(level))))
        .ToArray();

    private static readonly Dictionary<string, PrestigeUpgradeDefinition> PrestigeUpgradesById =
        PrestigeUpgrades.ToDictionary(p => p.Upgrade.Id);

    public static PrestigeUpgradeDefinition? FindPrestigeUpgrade(string upgradeId) =>
        PrestigeUpgradesById.GetValueOrDefault(upgradeId);

    public static int PrestigeUpgradeLevel(string upgradeId) =>
        PrestigeUpgradesById.TryGetValue(upgradeId, out var upgrade) ? upgrade.PrestigeLevel : 0;

    public static readonly IReadOnlyList<BusinessDefinition> Businesses =
    [
        new("ShakerAngsthase", "Shaker Angsthase", "images/businesses/ShakerAngsthase.png", InitialCost: 3.738, Coefficient: 1.07, InitialCycleSeconds: 0.6, InitialRevenue: 1, ManagerCost: 1_000, InstantMilestoneOwned: int.MaxValue),
        new("ShakerStihl", "Shaker Stihl", "images/businesses/ShakerStihl.png", InitialCost: 60, Coefficient: 1.15, InitialCycleSeconds: 3, InitialRevenue: 60, ManagerCost: 15_000, InstantMilestoneOwned: int.MaxValue),
        new("ShakerSchlaf", "Shaker Schlaf", "images/businesses/ShakerSchlaf.png", InitialCost: 720, Coefficient: 1.14, InitialCycleSeconds: 6, InitialRevenue: 540, ManagerCost: 100_000, InstantMilestoneOwned: int.MaxValue),
        new("ShakerRot", "Shaker Rot", "images/businesses/ShakerRot.png", InitialCost: 8640, Coefficient: 1.13, InitialCycleSeconds: 12, InitialRevenue: 4320, ManagerCost: 500_000, InstantMilestoneOwned: int.MaxValue),
        new("ShakerStrong", "Shaker Strong", "images/businesses/ShakerStrong.png", InitialCost: 103680, Coefficient: 1.12, InitialCycleSeconds: 24, InitialRevenue: 51840, ManagerCost: 1_200_000, InstantMilestoneOwned: int.MaxValue),
        new("ShakerDealer", "Shaker Dealer", "images/businesses/ShakerDealer.png", InitialCost: 1244160, Coefficient: 1.11, InitialCycleSeconds: 96, InitialRevenue: 622080, ManagerCost: 10_000_000, InstantMilestoneOwned: int.MaxValue),
        new("ShakerMexico", "Shaker Mexico", "images/businesses/ShakerMexico.png", InitialCost: 14929920, Coefficient: 1.10, InitialCycleSeconds: 384, InitialRevenue: 7464960, ManagerCost: 111_111_111, InstantMilestoneOwned: int.MaxValue),
        new("ShakerHorny", "Shaker Horny", "images/businesses/ShakerHorny.png", InitialCost: 179159040, Coefficient: 1.09, InitialCycleSeconds: 1536, InitialRevenue: 89579520, ManagerCost: 555_555_555, InstantMilestoneOwned: int.MaxValue),
        new("ShakerPrinzessin", "Shaker Prinzessin", "images/businesses/ShakerPrinzessin.png", InitialCost: 2149908480.0, Coefficient: 1.08, InitialCycleSeconds: 6144.0, InitialRevenue: 1074954240.0, ManagerCost: 10_000_000_000.0, InstantMilestoneOwned: int.MaxValue),
        new("ShakerFinalBoss", "Shaker Final Boss", "images/businesses/ShakerFinalBoss.png", InitialCost: 25798901760.0, Coefficient: 1.07, InitialCycleSeconds: 36864.0, InitialRevenue: 29668737024.0, ManagerCost: 100_000_000_000.0, InstantMilestoneOwned: int.MaxValue),
    ];

    public static BusinessDefinition GetBusiness(string id) =>
        Businesses.First(b => b.Id == id);

    public static readonly int[] MilestoneThresholds = [25, 50, 100, 200, 300, 400];

    public static double MilestoneSpeedMultiplier(int owned)
    {
        var mult = 1.0;
        foreach (var threshold in MilestoneThresholds)
        {
            if (owned >= threshold)
            {
                mult *= 2;
            }
        }

        return mult;
    }

    public static readonly int[] CapitalistSpeedThresholds = [1, 25, 50, 100, 200, 300, 400];

    public static int MinOwned(IReadOnlyDictionary<string, int> ownedByBusinessId)
    {
        var min = int.MaxValue;
        foreach (var business in Businesses)
        {
            if (!ownedByBusinessId.TryGetValue(business.Id, out var owned))
            {
                return 0;
            }

            min = Math.Min(min, owned);
        }

        return min;
    }

    private static double DoubleForEachReached(int[] thresholds, int minOwned)
    {
        var mult = 1.0;
        foreach (var threshold in thresholds)
        {
            if (minOwned >= threshold)
            {
                mult *= 2;
            }
        }

        return mult;
    }

    public static double GlobalSyncSpeedMultiplier(IReadOnlyDictionary<string, int> ownedByBusinessId) =>
        DoubleForEachReached(CapitalistSpeedThresholds, MinOwned(ownedByBusinessId));

    public static readonly int[] CapitalistProfitThresholds =
    [
        500, 600, 666, 700, 777, 800, 900, 1000, 1100, 1111, 1200, 1300, 1400, 1500, 1600, 1700, 1800,
        1900, 2000, 2100, 2200, 2222, 2300, 2400, 2500, 2600, 2700, 2800, 2900, 3000, 3100, 3200, 3300,
        3333, 3400, 3500, 3600, 3700, 3800, 3900, 4000, 4100, 4200, 4300, 4400, 4500, 4600, 4700, 4800,
        4900, 5000,
    ];

    public static double GlobalSyncProfitMultiplier(IReadOnlyDictionary<string, int> ownedByBusinessId) =>
        GlobalSyncProfitMultiplierForMin(MinOwned(ownedByBusinessId));

    public static double GlobalSyncProfitMultiplierForMin(int minOwned) =>
        DoubleForEachReached(CapitalistProfitThresholds, minOwned);

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<int>> ExtraSpeedThresholds =
    new Dictionary<string, IReadOnlyList<int>>
    {
        ["ShakerMexico"] = [2100, 2300, 2500, 2700],
        ["ShakerHorny"] = [2100, 2300, 2500, 2700, 3250],
        ["ShakerPrinzessin"] = [2250, 2500, 2750, 3000, 3250, 3500, 3750, 4000],
        ["ShakerFinalBoss"] = [2250, 2500, 2750, 3000, 3250, 3500, 3750, 4000, 4250, 4500, 4750, 5000],
    };

    public static double ExtraSpeedMultiplier(string businessId, int owned)
    {
        if (!ExtraSpeedThresholds.TryGetValue(businessId, out var thresholds))
        {
            return 1.0;
        }

        var mult = 1.0;
        foreach (var threshold in thresholds)
        {
            if (owned >= threshold)
            {
                mult *= 2;
            }
        }

        return mult;
    }
    
    public static double EffectiveCycleSeconds(BusinessDefinition definition, int owned, double globalSyncMultiplier = 1.0)
    {
        if (owned >= definition.InstantMilestoneOwned)
        {
            return 0;
        }

        return definition.InitialCycleSeconds / (MilestoneSpeedMultiplier(owned) * ExtraSpeedMultiplier(definition.Id, owned) * globalSyncMultiplier);
    }
    
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<(int OwnedThreshold, double ProfitMultiplier)>> UnlockProfitBonuses =
    new Dictionary<string, IReadOnlyList<(int OwnedThreshold, double ProfitMultiplier)>>
    {
        ["ShakerAngsthase"] = [
            (500, 4),
            (600, 4),
            (700, 4),
            (800, 4),
            (900, 4),
            (1000, 5),
            (1100, 4),
            (1200, 4),
            (1300, 4),
            (1400, 4),
            (1500, 4),
            (1600, 4),
            (1700, 4),
            (1800, 4),
            (1900, 4),
            (2000, 5),
            (2250, 2),
            (2500, 2),
            (2750, 2),
            (3000, 5),
            (3250, 2),
            (3500, 2),
            (3750, 2),
            (4000, 5),
            (4250, 2),
            (4500, 2),
            (4750, 2),
            (5000, 5),
            (5250, 3),
            (5500, 3),
            (5750, 3),
            (6000, 5),
            (6250, 3),
            (6500, 3),
            (6750, 3),
            (7000, 15),
            (7250, 3),
            (7500, 3),
            (7777, 3),
            (8000, 3),
            (8200, 3),
            (8400, 3),
            (8600, 3),
            (8800, 3),
            (9000, 3),
            (9100, 3),
            (9200, 3),
            (9300, 3),
            (9400, 3),
            (9500, 3),
            (9600, 3),
            (9700, 3),
            (9800, 3),
            (9999, 1.9999),
            (10000, 5),
        ],
        ["ShakerStihl"] = [
            (1000, 7777777),
            (1300, 7777),
            (2000, 7777),
            (2500, 777),
            (3000, 777),
            (3500, 777),
            (4000, 30),
            (5000, 50),
            (5100, 50),
            (5200, 50),
            (5300, 50),
            (5400, 50),
        ],
        ["ShakerSchlaf"] = [
            (500, 2),
            (600, 2),
            (700, 2),
            (800, 2),
            (900, 2),
            (1000, 3),
            (1100, 2),
            (1200, 2),
            (1300, 2),
            (1400, 2),
            (1500, 2),
            (1600, 2),
            (1700, 2),
            (1800, 2),
            (1900, 2),
            (2000, 5),
            (2100, 3),
            (2200, 3),
            (2300, 3),
            (2400, 3),
            (2500, 3),
            (2600, 3),
            (2700, 3),
            (2800, 3),
            (2900, 3),
            (3000, 3),
            (3100, 3),
            (3200, 3),
            (3300, 3),
            (3400, 3),
            (3500, 3),
            (3600, 3),
            (3700, 3),
            (3800, 3),
            (3900, 3),
            (4000, 5),
            (4100, 3),
            (4200, 3),
            (4300, 3),
            (4400, 3),
            (4500, 3),
            (4600, 3),
            (4700, 3),
            (4800, 3),
            (4900, 3),
            (5000, 5),
            (5250, 3),
            (5500, 3),
        ],
        ["ShakerRot"] = [
            (500, 2),
            (600, 2),
            (700, 2),
            (800, 2),
            (900, 2),
            (1000, 3),
            (1100, 2),
            (1200, 2),
            (1300, 2),
            (1400, 2),
            (1500, 2),
            (1600, 2),
            (1700, 2),
            (1800, 2),
            (1900, 2),
            (2000, 5),
            (2100, 3),
            (2200, 3),
            (2300, 3),
            (2400, 3),
            (2500, 3),
            (2600, 3),
            (2700, 3),
            (2800, 3),
            (2900, 3),
            (3000, 3),
            (3100, 3),
            (3200, 3),
            (3300, 3),
            (3400, 3),
            (3500, 3),
            (3600, 3),
            (3700, 3),
            (3800, 5),
            (3900, 3),
            (4000, 5),
            (4100, 3),
            (4200, 3),
            (4300, 3),
            (4400, 3),
            (4500, 3),
            (4600, 3),
            (4700, 3),
            (4800, 3),
            (4900, 3),
            (5000, 5),
            (5250, 3),
            (5500, 3),
            (5750, 3),
        ],
        ["ShakerStrong"] = [
            (500, 2),
            (600, 2),
            (700, 2),
            (800, 2),
            (900, 2),
            (1000, 3),
            (1100, 2),
            (1200, 2),
            (1300, 2),
            (1400, 2),
            (1500, 2),
            (1600, 2),
            (1700, 2),
            (1800, 2),
            (1900, 2),
            (2000, 5),
            (2100, 3),
            (2200, 3),
            (2300, 3),
            (2400, 3),
            (2500, 3),
            (2600, 3),
            (2700, 3),
            (2800, 3),
            (2900, 3),
            (3000, 3),
            (3100, 3),
            (3200, 3),
            (3300, 3),
            (3400, 3),
            (3500, 3),
            (3600, 3),
            (3700, 3),
            (3800, 3),
            (3900, 3),
            (4000, 3),
            (4100, 3),
            (4200, 3),
            (4300, 3),
            (4400, 3),
            (4500, 3),
            (4750, 3),
            (5000, 3),
            (5250, 3),
            (5500, 3),
            (5750, 3),
            (6000, 3),
            (6250, 3),
        ],
        ["ShakerDealer"] = [
            (500, 2),
            (600, 2),
            (700, 2),
            (800, 2),
            (900, 2),
            (1000, 3),
            (1100, 2),
            (1200, 2),
            (1300, 2),
            (1400, 2),
            (1500, 2),
            (1600, 2),
            (1700, 2),
            (1800, 2),
            (1900, 2),
            (2000, 5),
            (2100, 3),
            (2200, 3),
            (2300, 3),
            (2400, 3),
            (2500, 3),
            (2600, 3),
            (2700, 3),
            (2800, 3),
            (2900, 3),
            (3000, 3),
            (3250, 5),
            (3500, 5),
            (3750, 3),
            (4000, 5),
            (4250, 3),
            (4500, 5),
            (4750, 3),
            (5000, 5),
            (5250, 3),
            (5500, 3),
            (5750, 3),
            (6000, 5),
            (6250, 3),
            (6500, 5),
        ],
        ["ShakerMexico"] = [
            (500, 2),
            (600, 2),
            (700, 2),
            (800, 2),
            (900, 2),
            (1000, 3),
            (1100, 2),
            (1200, 2),
            (1300, 2),
            (1400, 2),
            (1500, 2),
            (1600, 2),
            (1700, 2),
            (1800, 2),
            (1900, 2),
            (2000, 5),
            (2200, 3),
            (2400, 3),
            (2600, 3),
            (2800, 3),
            (2900, 3),
            (3000, 3),
            (3250, 3),
            (3500, 3),
            (3750, 3),
            (4000, 5),
            (4250, 3),
            (4500, 3),
            (4750, 3),
            (5000, 7),
            (5250, 3),
            (5500, 3),
            (5750, 3),
            (6000, 7),
            (6250, 3),
            (6500, 3),
            (6750, 3),
            (7000, 7),
        ],
        ["ShakerHorny"] = [
            (500, 2),
            (600, 2),
            (700, 2),
            (800, 2),
            (900, 2),
            (1000, 3),
            (1100, 2),
            (1200, 2),
            (1300, 2),
            (1400, 2),
            (1500, 2),
            (1600, 2),
            (1700, 2),
            (1800, 2),
            (1900, 2),
            (2000, 5),
            (2200, 2),
            (2400, 2),
            (2600, 2),
            (2800, 2),
            (2900, 2),
            (3000, 2),
            (3500, 2),
            (3750, 2),
            (4000, 2),
            (4250, 3),
            (4500, 3),
            (4750, 3),
            (5000, 5),
            (5250, 3),
            (5500, 3),
            (5750, 3),
            (6000, 9),
            (6250, 3),
            (6500, 3),
            (6750, 3),
            (7000, 9),
            (7250, 3),
            (7500, 3),
            (7750, 3),
        ],
        ["ShakerPrinzessin"] = [
            (500, 2),
            (600, 2),
            (700, 2),
            (800, 2),
            (900, 2),
            (1000, 3),
            (1100, 2),
            (1200, 2),
            (1300, 2),
            (1400, 2),
            (1500, 2),
            (1600, 2),
            (1700, 2),
            (1800, 2),
            (1900, 2),
            (2000, 5),
            (4250, 3),
            (4500, 3),
            (4750, 3),
            (5000, 5),
            (5250, 5),
            (5500, 3),
            (5750, 3),
            (6000, 5),
            (6250, 3),
            (6500, 3),
            (6750, 3),
            (7000, 5),
            (7250, 3),
            (7500, 3),
            (7750, 3),
            (8000, 5),
            (8250, 3),
            (8500, 3),
        ],
        ["ShakerFinalBoss"] = [
            (500, 2),
            (600, 2),
            (700, 2),
            (800, 2),
            (900, 2),
            (1000, 3),
            (1100, 2),
            (1200, 2),
            (1300, 2),
            (1400, 2),
            (1500, 2),
            (1600, 2),
            (1700, 2),
            (1800, 2),
            (1900, 2),
            (2000, 5),
            (5250, 3),
            (5500, 3),
            (5750, 3),
            (6000, 5),
            (6250, 3),
            (6500, 3),
            (6750, 3),
            (7000, 7),
            (7250, 3),
            (7500, 3),
            (7750, 3),
            (8000, 3),
            (8250, 3),
            (8500, 3),
            (8750, 3),
            (9000, 7),
            (9250, 3),
            (9500, 3),
            (9750, 3),
        ],
    };

    public static double UnlockProfitMultiplier(string businessId, int owned)
    {
        if (!UnlockProfitBonuses.TryGetValue(businessId, out var bonuses))
        {
            return 1.0;
        }

        var mult = 1.0;
        foreach (var (threshold, multiplier) in bonuses)
        {
            if (owned >= threshold)
            {
                mult *= multiplier;
            }
        }

        return mult;
    }
    
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<(int StihlOwnedThreshold, double ProfitMultiplier)>> NewspaperCrossBonuses =
    new Dictionary<string, IReadOnlyList<(int StihlOwnedThreshold, double ProfitMultiplier)>>
    {
        ["ShakerAngsthase"] = [(125, 2), (250, 3), (375, 4), (525, 5), (650, 6), (775, 3), (1350, 9), (2900, 15), (4100, 30)],
        ["ShakerSchlaf"] = [(150, 2), (275, 3), (425, 4), (550, 5), (675, 6), (825, 7), (1075, 8), (1400, 9), (2100, 15), (3100, 20), (4200, 30)],
        ["ShakerRot"] = [(175, 2), (325, 3), (450, 4), (575, 5), (725, 6), (850, 7), (1100, 8), (1450, 9), (2200, 15), (4300, 30)],
        ["ShakerStrong"] = [(225, 2), (350, 3), (475, 4), (625, 5), (750, 6), (875, 7), (1125, 8), (1500, 9), (2300, 15), (4400, 30)],
        ["ShakerDealer"] = [(500, 11), (925, 7), (1150, 8), (1550, 9), (1800, 10), (2400, 15), (4500, 30)],
        ["ShakerMexico"] = [(600, 11), (950, 7), (1175, 8), (1600, 9), (1850, 10), (3200, 20), (3600, 25), (4600, 30)],
        ["ShakerHorny"] = [(700, 11), (975, 7), (1200, 8), (1650, 9), (1900, 10), (2600, 15), (3700, 25), (4700, 30)],
        ["ShakerPrinzessin"] = [(800, 11), (1025, 7), (1225, 8), (1700, 9), (1950, 10), (2700, 15), (3300, 20), (3800, 25), (4800, 30)],
        ["ShakerFinalBoss"] = [(900, 11), (1050, 7), (1250, 8), (1750, 9), (2800, 15), (3400, 20), (3900, 25), (4900, 30)],
    };

    public static double NewspaperCrossMultiplier(string businessId, int stihlOwned)
    {
        if (!NewspaperCrossBonuses.TryGetValue(businessId, out var bonuses))
        {
            return 1.0;
        }

        var mult = 1.0;
        foreach (var (threshold, multiplier) in bonuses)
        {
            if (stihlOwned >= threshold)
            {
                mult *= multiplier;
            }
        }

        return mult;
    }

    public static IReadOnlyList<int> AllUnlockThresholds(string businessId)
    {
        var thresholds = new List<int>(MilestoneThresholds);

        if (ExtraSpeedThresholds.TryGetValue(businessId, out var extraSpeed))
        {
            thresholds.AddRange(extraSpeed);
        }

        if (UnlockProfitBonuses.TryGetValue(businessId, out var profitBonuses))
        {
            thresholds.AddRange(profitBonuses.Select(b => b.OwnedThreshold));
        }

        if (businessId == "ShakerStihl")
        {
            thresholds.AddRange(NewspaperCrossBonuses.SelectMany(kvp => kvp.Value.Select(b => b.StihlOwnedThreshold)));
        }

        thresholds.Sort();
        return thresholds;
    }

    public static int? NextMilestone(string businessId, int owned) =>
        AllUnlockThresholds(businessId).FirstOrDefault(t => t > owned) is var next && next > 0 ? next : null;

    public static (int Achieved, int Total) UnlockProgress(IReadOnlyDictionary<string, int> ownedByBusinessId)
    {
        var achieved = 0;
        var total = 0;

        foreach (var businessId in UnlockProfitBonuses.Keys)
        {
            var owned = ownedByBusinessId.TryGetValue(businessId, out var o) ? o : 0;
            var thresholds = AllUnlockThresholds(businessId);
            total += thresholds.Count;
            achieved += thresholds.Count(t => owned >= t);
        }

        var minOwned = Businesses.Count == 0
            ? 0
            : Businesses.Min(b => ownedByBusinessId.TryGetValue(b.Id, out var o) ? o : 0);
        var capitalistThresholds = CapitalistSpeedThresholds.Concat(CapitalistProfitThresholds).ToList();
        total += capitalistThresholds.Count;
        achieved += capitalistThresholds.Count(t => minOwned >= t);

        return (achieved, total);
    }

    public static double UnlockProgressPercent(IReadOnlyDictionary<string, int> ownedByBusinessId)
    {
        var (achieved, total) = UnlockProgress(ownedByBusinessId);
        return total == 0 ? 0.0 : Math.Round(achieved * 100.0 / total, 1);
    }

    public static readonly string[] OliStallReplies1 =
    [
        "Amigo, schon wieder du? Ich hab ein Imperium zu führen.",
        "Na sowas, mein liebster Geschäftspartner meldet sich. Was brennt?",
        "Immer diese kleinen Fische, die was von mir wollen. Was ist los?",
        "Du klopfst schon wieder an meine Tür, amigo. Was gibt's diesmal?",
    ];

    public static readonly string[] OliStallReplies2 =
    [
        "Ein echter Boss macht sein Geld selbst, amigo. Streng dich an in deinem Business.",
        "Wieso brauchst DU mehr Investoren? Bau dir dein eigenes Imperium auf, so wie ich's getan hab.",
        "Reicht dir dein kleines Business nicht? Grind mehr, dann reden wir weiter.",
        "Ich weiß nicht... häng dich mal richtig rein, bevor du mich anbettelst.",
    ];

    public static readonly string[] OliNiceReplies =
    [
        "Na gut, amigo, du hast Mumm. Ich leg dir {0} drauf - Chef-Ehrenwort.",
        "Für dich mach ich mal eine Ausnahme, mein Freund: {0}. Sag niemandem, ich war großzügig.",
        "Du bist hartnäckig wie ein echter Geschäftsmann. Alles klar, {0} für dich.",
        "Okay, okay, genug gebettelt. Hier, {0} - und jetzt lass mich mein Imperium führen.",
        "Weil du's bist, amigo: {0} extra. So läuft das Geschäft.",
        "Na schön, du hast den Boss rumgekriegt. {0}, aber nur diesmal.",
    ];

    public static readonly string[] OliAnnoyedReplies =
    [
        "Ugh, du nervst wie 'ne Razzia mitten im Deal. Dafür gibt's {0}.",
        "Lass mich in Ruhe, amigo! Das kostet dich {0}.",
        "Ich hab keine Zeit für dein Gebettel, ich führ hier ein Imperium. {0} für dich.",
        "Frech, mich so anzuquatschen kleiner Mann! Denkst auch du wärst krass oder? Dafür wird's {0}.",
        "Musst du mich IMMER anbetteln? Diesmal gibt's {0}, capisce?",
        "Ernsthaft jetzt? Dafür zieh ich dir {0} ab - Geschäft ist Geschäft.",
    ];

    public static readonly string[] OliNotInterestedReplies =
    [
        "Amigo, ernsthaft? Mit null Anteilen komm mir nicht nochmal.",
        "Kein Interesse. Bau erstmal was auf, dann reden wir weiter.",
        "Ich hab gerade keine Zeit für Kleingeld-Gespräche, capisce?",
        "Noch nicht, amigo. Komm wieder, wenn's sich für mich lohnt.",
        "Du verschwendest meine Zeit. Gift mal lieber ein paar subs bei mir!",
        "Wie oft muss ich dir noch sagen, dass ich mit Pleitegeiern nicht rede?",
        "Ich hab Wichtigeres zu tun als mit dir über Peanuts zu quatschen.",
        "Denkst du, El Jefe hat Zeit für sowas? Verschwinde und grind erstmal.",
        "Amigo, du nervst. Komm zurück, wenn du überhaupt was zu bieten hast.",
        "Spar dir die Mühe - bei null Anteilen bist du für mich unsichtbar.",
        "Halt's Maul und bau dein Business auf, bevor du mich nochmal anschreibst!",
        "Ich schwör dir, wenn du mich nochmal mit NICHTS anschreibst, gibt's Ärger.",
        "Verpiss dich mit deinen leeren Taschen, amigo - so läuft das nicht!",
        "Du testest gerade meine Geduld, und die ist heute verdammt kurz!",
        "Noch ein Wort von dir mit null Anteilen und du bist Geschichte, capisce?!",
        "Denkst du echt, ich rede mit jedem Nichtsnutz, der mir schreibt?! Verschwinde!",
        "Geh mal lieber zu John Dark betteln du Hund. Ich ban dich gleich weg!",
    ];

    public static readonly string[] OliDealLockedReplies =
    [
        "Amigo, ich hab dir {0} zugesagt, was willst du noch? Entweder du unterschreibst, oder du wartest auf mehr Anteile - aber lass mich arbeiten.",
        "Ich hab keine Zeit für Wiederholungen. {0} steht, capisce? Schließ den Deal ab oder sammle weiter, bis mehr rausspringt.",
        "Ernsthaft, schon wieder du? {0} ist ausgehandelt und fertig. Reset jetzt oder warten - aber hör auf zu fragen.",
        "Der Deal ist {0}, amigo. Punkt. Entweder du machst den Reset, oder du wartest auf mehr Anteile - mich nervst du damit nicht weich.",
        "Denkst du, ich hab den ganzen Tag Zeit für dich? {0} liegt auf dem Tisch. Nimm's oder lass mehr Anteile wachsen.",
        "Ich hab Wichtigeres zu tun als mit dir dieselbe Nummer nochmal durchzukauen. {0}, fertig - Reset oder warten.",
        "Du testest meine Geduld, amigo. {0} steht fest, egal wie oft du schreibst - entweder abschließen oder mehr Anteile sammeln.",
        "Comprende? {0} ist der Deal. Entweder du unterschreibst jetzt, oder du lässt es wachsen und kommst nach dem nächsten Reset wieder.",
    ];

    public static readonly string[] OliWelcomeBackInstant =
    [
        "Amigo, du warst ja kaum weg! Das nenn ich Arbeitsmoral, capisce?",
        "Schon wieder da? Schneller zurück als meine Anwälte bei einer Razzia. Weiter so, amigo.",
        "Kurze Pause, großes Herz fürs Geschäft. Das gefällt mir, amigo.",
        "Fleiß wird nicht vergessen, amigo. Weiter so.",
    ];

    public static readonly string[] OliWelcomeBackShort =
    [
        "Ordentliche Pause, amigo. Das Geschäft läuft weiter wie geschmiert.",
        "Meine Manager haben dich gut vertreten. Weiter so, amigo.",
        "Kurze Auszeit, kein Problem - solange das Geld weiter fließt.",
        "Auch ein Boss braucht mal 'ne Verschnaufpause. Willkommen zurück, amigo.",
    ];

    public static readonly string[] OliWelcomeBackMedium =
    [
        "Wo warst du so lange, amigo? Meine Manager haben ohne dich geschuftet.",
        "Ein bisschen mehr Einsatz würde nicht schaden, capisce?",
        "Ein paar Stunden Funkstille, amigo. Das Kartell merkt sich sowas.",
        "Na, endlich zurück? Ich hab schon fast jemand Neues gesucht.",
    ];

    public static readonly string[] OliWelcomeBackHalfDay =
    [
        "Fast einen halben Tag abgetaucht, amigo? Das Kartell vergisst sowas nicht.",
        "Wo warst du, capisce? Das Geschäft läuft nicht von allein!",
        "Du testest meine Geduld, amigo. Und die ist heute verdammt kurz.",
        "So eine Pause hätte mir fast einen neuen Partner gekostet.",
    ];

    public static readonly string[] OliWelcomeBackFullDay =
    [
        "Ein ganzer Tag ohne ein Wort, amigo? Das Kartell erinnert sich an sowas.",
        "So eine Pause kostet Vertrauen. Nächstes Mal meld dich, capisce?",
        "Noch so eine Pause, amigo, und wir reden über Konsequenzen.",
        "Ich hab schon Verträge mit weniger Ghosting gekündigt, amigo.",
    ];

    public static readonly string[] OliWelcomeBackMultiDay =
    [
        "Mehrere Tage ohne ein Wort, amigo? Fast hätt ich dich von der Gehaltsliste gestrichen.",
        "Wärst du noch länger weg gewesen, hätte ich längst jemand Neues gesucht.",
        "Zum Glück bin ich nachtragend, aber geschäftlich fair. Nächstes Mal meld dich, capisce?",
        "Ich dachte schon, die Polizei hätte dich geschnappt. Wo warst du, amigo?!",
    ];

    public static string OliWelcomeBackLine(TimeSpan offlineDuration)
    {
        var pool = offlineDuration switch
        {
            var d when d < TimeSpan.FromMinutes(5) => OliWelcomeBackInstant,
            var d when d < TimeSpan.FromHours(1) => OliWelcomeBackShort,
            var d when d < TimeSpan.FromHours(6) => OliWelcomeBackMedium,
            var d when d < TimeSpan.FromHours(24) => OliWelcomeBackHalfDay,
            var d when d < TimeSpan.FromDays(3) => OliWelcomeBackFullDay,
            _ => OliWelcomeBackMultiDay,
        };
        return pool[Random.Shared.Next(pool.Length)];
    }

    public static readonly IReadOnlyList<UpgradeDefinition> Upgrades =
    [
        new("ShakerAngsthase-cu1", "Shaker Angsthase: Aufwertung 1 (x3)", "ShakerAngsthase", 0, 250000, 3),
        new("ShakerAngsthase-cu2", "Shaker Angsthase: Aufwertung 2 (x3)", "ShakerAngsthase", 0, 20000000000000, 3),
        new("ShakerAngsthase-cu3", "Shaker Angsthase: Aufwertung 3 (x3)", "ShakerAngsthase", 0, 2E+18, 3),
        new("ShakerAngsthase-cu4", "Shaker Angsthase: Aufwertung 4 (x3)", "ShakerAngsthase", 0, 2.4999999999999998E+22, 3),
        new("ShakerAngsthase-cu5", "Shaker Angsthase: Aufwertung 5 (x7)", "ShakerAngsthase", 0, 1E+27, 7),
        new("ShakerAngsthase-cu6", "Shaker Angsthase: Aufwertung 6 (x3)", "ShakerAngsthase", 0, 2.4999999999999996E+46, 3),
        new("ShakerAngsthase-cu7", "Shaker Angsthase: Aufwertung 7 (x3)", "ShakerAngsthase", 0, 5E+50, 3),
        new("ShakerAngsthase-cu8", "Shaker Angsthase: Aufwertung 8 (x3)", "ShakerAngsthase", 0, 1E+80, 3),
        new("ShakerAngsthase-cu9", "Shaker Angsthase: Aufwertung 9 (x3)", "ShakerAngsthase", 0, 1E+94, 3),
        new("ShakerAngsthase-cu10", "Shaker Angsthase: Aufwertung 10 (x2)", "ShakerAngsthase", 0, 2.9999999999999999E+101, 2),
        new("ShakerAngsthase-cu11", "Shaker Angsthase: Aufwertung 11 (x3)", "ShakerAngsthase", 0, 5.0000000000000003E+116, 3),
        new("ShakerAngsthase-cu12", "Shaker Angsthase: Aufwertung 12 (x3)", "ShakerAngsthase", 0, 7.5E+116, 3),
        new("ShakerAngsthase-cu13", "Shaker Angsthase: Aufwertung 13 (x3)", "ShakerAngsthase", 0, 1.0000000000000001E+117, 3),
        new("ShakerAngsthase-cu14", "Shaker Angsthase: Aufwertung 14 (x3)", "ShakerAngsthase", 0, 5.5500000000000002E+122, 3),
        new("ShakerAngsthase-cu15", "Shaker Angsthase: Aufwertung 15 (x3)", "ShakerAngsthase", 0, 1.0000000000000001E+126, 3),
        new("ShakerAngsthase-cu16", "Shaker Angsthase: Aufwertung 16 (x3)", "ShakerAngsthase", 0, 7.1000000000000002E+130, 3),
        new("ShakerAngsthase-cu17", "Shaker Angsthase: Aufwertung 17 (x2)", "ShakerAngsthase", 0, 9.0000000000000003E+137, 2),
        new("ShakerAngsthase-cu18", "Shaker Angsthase: Aufwertung 18 (x3)", "ShakerAngsthase", 0, 7.9900000000000003E+143, 3),
        new("ShakerAngsthase-cu19", "Shaker Angsthase: Aufwertung 19 (x3)", "ShakerAngsthase", 0, 3E+146, 3),
        new("ShakerAngsthase-cu20", "Shaker Angsthase: Aufwertung 20 (x5)", "ShakerAngsthase", 0, 2.3999999999999999E+152, 5),
        new("ShakerAngsthase-cu21", "Shaker Angsthase: Aufwertung 21 (x2)", "ShakerAngsthase", 0, 3.1999999999999999E+157, 2),
        new("ShakerAngsthase-cu22", "Shaker Angsthase: Aufwertung 22 (x3)", "ShakerAngsthase", 0, 1.9999999999999998E+161, 3),
        new("ShakerAngsthase-cu23", "Shaker Angsthase: Aufwertung 23 (x10)", "ShakerAngsthase", 0, 4.9999999999999992E+173, 10),
        new("ShakerAngsthase-cu24", "Shaker Angsthase: Aufwertung 24 (x3)", "ShakerAngsthase", 0, 1E+201, 3),
        new("ShakerAngsthase-cu25", "Shaker Angsthase: Aufwertung 25 (x3)", "ShakerAngsthase", 0, 2.3299999999999999E+206, 3),
        new("ShakerAngsthase-cu26", "Shaker Angsthase: Aufwertung 26 (x11)", "ShakerAngsthase", 0, 9.9999999999999995E+213, 11),
        new("ShakerAngsthase-cu27", "Shaker Angsthase: Aufwertung 27 (x3)", "ShakerAngsthase", 0, 1.5E+215, 3),
        new("ShakerAngsthase-cu28", "Shaker Angsthase: Aufwertung 28 (x3)", "ShakerAngsthase", 0, 8.0000000000000007E+218, 3),
        new("ShakerAngsthase-cu29", "Shaker Angsthase: Aufwertung 29 (x3)", "ShakerAngsthase", 0, 5.9999999999999995E+221, 3),
        new("ShakerAngsthase-cu30", "Shaker Angsthase: Aufwertung 30 (x7)", "ShakerAngsthase", 0, 9.9999999999999992E+227, 7),
        new("ShakerAngsthase-cu31", "Shaker Angsthase: Aufwertung 31 (x3)", "ShakerAngsthase", 0, 3.0000000000000002E+231, 3),
        new("ShakerAngsthase-cu32", "Shaker Angsthase: Aufwertung 32 (x3)", "ShakerAngsthase", 0, 6.3000000000000002E+235, 3),
        new("ShakerAngsthase-cu33", "Shaker Angsthase: Aufwertung 33 (x2)", "ShakerAngsthase", 0, 1E+240, 2),
        new("ShakerAngsthase-cu34", "Shaker Angsthase: Aufwertung 34 (x2)", "ShakerAngsthase", 0, 2.0000000000000001E+243, 2),
        new("ShakerAngsthase-cu35", "Shaker Angsthase: Aufwertung 35 (x3)", "ShakerAngsthase", 0, 1.0000000000000001E+253, 3),
        new("ShakerAngsthase-cu36", "Shaker Angsthase: Aufwertung 36 (x9)", "ShakerAngsthase", 0, 5.0000000000000003E+253, 9),
        new("ShakerAngsthase-cu37", "Shaker Angsthase: Aufwertung 37 (x3)", "ShakerAngsthase", 0, 6.400000000000001E+258, 3),
        new("ShakerAngsthase-cu38", "Shaker Angsthase: Aufwertung 38 (x3)", "ShakerAngsthase", 0, 7E+263, 3),
        new("ShakerAngsthase-cu39", "Shaker Angsthase: Aufwertung 39 (x7)", "ShakerAngsthase", 0, 9.9999999999999995E+272, 7),
        new("ShakerStihl-cu1", "Shaker Stihl: Aufwertung 1 (x3)", "ShakerStihl", 0, 500000, 3),
        new("ShakerStihl-cu2", "Shaker Stihl: Aufwertung 2 (x3)", "ShakerStihl", 0, 50000000000000, 3),
        new("ShakerStihl-cu3", "Shaker Stihl: Aufwertung 3 (x3)", "ShakerStihl", 0, 5E+18, 3),
        new("ShakerStihl-cu4", "Shaker Stihl: Aufwertung 4 (x3)", "ShakerStihl", 0, 4.9999999999999996E+22, 3),
        new("ShakerStihl-cu5", "Shaker Stihl: Aufwertung 5 (x7)", "ShakerStihl", 0, 4.9999999999999998E+27, 7),
        new("ShakerStihl-cu6", "Shaker Stihl: Aufwertung 6 (x3)", "ShakerStihl", 0, 5.0000000000000001E+42, 3),
        new("ShakerStihl-cu7", "Shaker Stihl: Aufwertung 7 (x3)", "ShakerStihl", 0, 2.4999999999999997E+47, 3),
        new("ShakerStihl-cu8", "Shaker Stihl: Aufwertung 8 (x3)", "ShakerStihl", 0, 9.9999999999999995E+60, 3),
        new("ShakerStihl-cu9", "Shaker Stihl: Aufwertung 9 (x3)", "ShakerStihl", 0, 9.9999999999999997E+89, 3),
        new("ShakerStihl-cu10", "Shaker Stihl: Aufwertung 10 (x2)", "ShakerStihl", 0, 2.0000000000000001E+96, 2),
        new("ShakerStihl-cu11", "Shaker Stihl: Aufwertung 11 (x3)", "ShakerStihl", 0, 2.0000000000000001E+117, 3),
        new("ShakerStihl-cu12", "Shaker Stihl: Aufwertung 12 (x3)", "ShakerStihl", 0, 2.0000000000000002E+118, 3),
        new("ShakerStihl-cu13", "Shaker Stihl: Aufwertung 13 (x3)", "ShakerStihl", 0, 1.5E+119, 3),
        new("ShakerStihl-cu14", "Shaker Stihl: Aufwertung 14 (x3)", "ShakerStihl", 0, 7.0000000000000001E+119, 3),
        new("ShakerStihl-cu15", "Shaker Stihl: Aufwertung 15 (x3)", "ShakerStihl", 0, 3.0000000000000001E+123, 3),
        new("ShakerStihl-cu16", "Shaker Stihl: Aufwertung 16 (x3)", "ShakerStihl", 0, 2.5E+131, 3),
        new("ShakerStihl-cu17", "Shaker Stihl: Aufwertung 17 (x2)", "ShakerStihl", 0, 5.0000000000000001E+132, 2),
        new("ShakerStihl-cu18", "Shaker Stihl: Aufwertung 18 (x3)", "ShakerStihl", 0, 1.3600000000000001E+140, 3),
        new("ShakerStihl-cu19", "Shaker Stihl: Aufwertung 19 (x3)", "ShakerStihl", 0, 3.0000000000000002E+144, 3),
        new("ShakerStihl-cu20", "Shaker Stihl: Aufwertung 20 (x5)", "ShakerStihl", 0, 3.0000000000000001E+148, 5),
        new("ShakerStihl-cu21", "Shaker Stihl: Aufwertung 21 (x2)", "ShakerStihl", 0, 8.8800000000000003E+155, 2),
        new("ShakerStihl-cu22", "Shaker Stihl: Aufwertung 22 (x3)", "ShakerStihl", 0, 9.9999999999999985E+159, 3),
        new("ShakerStihl-cu23", "Shaker Stihl: Aufwertung 23 (x22)", "ShakerStihl", 0, 4.9999999999999995E+164, 22),
        new("ShakerStihl-cu24", "Shaker Stihl: Aufwertung 24 (x3)", "ShakerStihl", 0, 1.4000000000000001E+202, 3),
        new("ShakerStihl-cu25", "Shaker Stihl: Aufwertung 25 (x3)", "ShakerStihl", 0, 4.2099999999999999E+206, 3),
        new("ShakerStihl-cu26", "Shaker Stihl: Aufwertung 26 (x11)", "ShakerStihl", 0, 9.9999999999999995E+213, 11),
        new("ShakerStihl-cu27", "Shaker Stihl: Aufwertung 27 (x3)", "ShakerStihl", 0, 1.66E+215, 3),
        new("ShakerStihl-cu28", "Shaker Stihl: Aufwertung 28 (x3)", "ShakerStihl", 0, 8.0000000000000007E+218, 3),
        new("ShakerStihl-cu29", "Shaker Stihl: Aufwertung 29 (x3)", "ShakerStihl", 0, 7.89E+221, 3),
        new("ShakerStihl-cu30", "Shaker Stihl: Aufwertung 30 (x7)", "ShakerStihl", 0, 9.9999999999999992E+227, 7),
        new("ShakerStihl-cu31", "Shaker Stihl: Aufwertung 31 (x3)", "ShakerStihl", 0, 8.0000000000000005E+231, 3),
        new("ShakerStihl-cu32", "Shaker Stihl: Aufwertung 32 (x3)", "ShakerStihl", 0, 1.99E+236, 3),
        new("ShakerStihl-cu33", "Shaker Stihl: Aufwertung 33 (x2)", "ShakerStihl", 0, 5.0000000000000003E+240, 2),
        new("ShakerStihl-cu34", "Shaker Stihl: Aufwertung 34 (x2)", "ShakerStihl", 0, 2.2000000000000003E+244, 2),
        new("ShakerStihl-cu35", "Shaker Stihl: Aufwertung 35 (x3)", "ShakerStihl", 0, 1.0000000000000001E+253, 3),
        new("ShakerStihl-cu36", "Shaker Stihl: Aufwertung 36 (x9)", "ShakerStihl", 0, 7.5000000000000002E+253, 9),
        new("ShakerStihl-cu37", "Shaker Stihl: Aufwertung 37 (x3)", "ShakerStihl", 0, 1.22E+259, 3),
        new("ShakerStihl-cu38", "Shaker Stihl: Aufwertung 38 (x3)", "ShakerStihl", 0, 1E+264, 3),
        new("ShakerStihl-cu39", "Shaker Stihl: Aufwertung 39 (x7)", "ShakerStihl", 0, 1.9999999999999999E+273, 7),
        new("ShakerStihl-cu40", "Shaker Stihl: Aufwertung 40 (x13)", "ShakerStihl", 0, 9.9999999999999998E+284, 13),
        new("ShakerSchlaf-cu1", "Shaker Schlaf: Aufwertung 1 (x3)", "ShakerSchlaf", 0, 1000000, 3),
        new("ShakerSchlaf-cu2", "Shaker Schlaf: Aufwertung 2 (x3)", "ShakerSchlaf", 0, 100000000000000, 3),
        new("ShakerSchlaf-cu3", "Shaker Schlaf: Aufwertung 3 (x3)", "ShakerSchlaf", 0, 7E+18, 3),
        new("ShakerSchlaf-cu4", "Shaker Schlaf: Aufwertung 4 (x3)", "ShakerSchlaf", 0, 9.9999999999999992E+22, 3),
        new("ShakerSchlaf-cu5", "Shaker Schlaf: Aufwertung 5 (x7)", "ShakerSchlaf", 0, 2.5000000000000002E+28, 7),
        new("ShakerSchlaf-cu6", "Shaker Schlaf: Aufwertung 6 (x3)", "ShakerSchlaf", 0, 2.5000000000000002E+43, 3),
        new("ShakerSchlaf-cu7", "Shaker Schlaf: Aufwertung 7 (x3)", "ShakerSchlaf", 0, 4.9999999999999994E+47, 3),
        new("ShakerSchlaf-cu8", "Shaker Schlaf: Aufwertung 8 (x3)", "ShakerSchlaf", 0, 9.9999999999999992E+61, 3),
        new("ShakerSchlaf-cu9", "Shaker Schlaf: Aufwertung 9 (x3)", "ShakerSchlaf", 0, 4.9999999999999995E+90, 3),
        new("ShakerSchlaf-cu10", "Shaker Schlaf: Aufwertung 10 (x2)", "ShakerSchlaf", 0, 1.1E+97, 2),
        new("ShakerSchlaf-cu11", "Shaker Schlaf: Aufwertung 11 (x3)", "ShakerSchlaf", 0, 5E+102, 3),
        new("ShakerSchlaf-cu12", "Shaker Schlaf: Aufwertung 12 (x3)", "ShakerSchlaf", 0, 1.4999999999999998E+104, 3),
        new("ShakerSchlaf-cu13", "Shaker Schlaf: Aufwertung 13 (x3)", "ShakerSchlaf", 0, 4E+104, 3),
        new("ShakerSchlaf-cu14", "Shaker Schlaf: Aufwertung 14 (x3)", "ShakerSchlaf", 0, 9.5000000000000008E+119, 3),
        new("ShakerSchlaf-cu15", "Shaker Schlaf: Aufwertung 15 (x3)", "ShakerSchlaf", 0, 6.0000000000000002E+123, 3),
        new("ShakerSchlaf-cu16", "Shaker Schlaf: Aufwertung 16 (x3)", "ShakerSchlaf", 0, 2E+129, 3),
        new("ShakerSchlaf-cu17", "Shaker Schlaf: Aufwertung 17 (x2)", "ShakerSchlaf", 0, 9.5E+133, 2),
        new("ShakerSchlaf-cu18", "Shaker Schlaf: Aufwertung 18 (x3)", "ShakerSchlaf", 0, 7.0000000000000006E+140, 3),
        new("ShakerSchlaf-cu19", "Shaker Schlaf: Aufwertung 19 (x3)", "ShakerSchlaf", 0, 6.0000000000000005E+144, 3),
        new("ShakerSchlaf-cu20", "Shaker Schlaf: Aufwertung 20 (x5)", "ShakerSchlaf", 0, 1.8E+149, 5),
        new("ShakerSchlaf-cu21", "Shaker Schlaf: Aufwertung 21 (x2)", "ShakerSchlaf", 0, 9.9899999999999994E+155, 2),
        new("ShakerSchlaf-cu22", "Shaker Schlaf: Aufwertung 22 (x3)", "ShakerSchlaf", 0, 2.4999999999999998E+160, 3),
        new("ShakerSchlaf-cu23", "Shaker Schlaf: Aufwertung 23 (x20)", "ShakerSchlaf", 0, 9.999999999999999E+164, 20),
        new("ShakerSchlaf-cu24", "Shaker Schlaf: Aufwertung 24 (x3)", "ShakerSchlaf", 0, 6.0699999999999999E+206, 3),
        new("ShakerSchlaf-cu25", "Shaker Schlaf: Aufwertung 25 (x11)", "ShakerSchlaf", 0, 9.9999999999999995E+213, 11),
        new("ShakerSchlaf-cu26", "Shaker Schlaf: Aufwertung 26 (x3)", "ShakerSchlaf", 0, 1.93E+215, 3),
        new("ShakerSchlaf-cu27", "Shaker Schlaf: Aufwertung 27 (x3)", "ShakerSchlaf", 0, 8.0000000000000007E+218, 3),
        new("ShakerSchlaf-cu28", "Shaker Schlaf: Aufwertung 28 (x3)", "ShakerSchlaf", 0, 8.4500000000000002E+221, 3),
        new("ShakerSchlaf-cu29", "Shaker Schlaf: Aufwertung 29 (x7)", "ShakerSchlaf", 0, 9.9999999999999992E+227, 7),
        new("ShakerSchlaf-cu30", "Shaker Schlaf: Aufwertung 30 (x3)", "ShakerSchlaf", 0, 6.9000000000000005E+232, 3),
        new("ShakerSchlaf-cu31", "Shaker Schlaf: Aufwertung 31 (x3)", "ShakerSchlaf", 0, 3.98E+236, 3),
        new("ShakerSchlaf-cu32", "Shaker Schlaf: Aufwertung 32 (x2)", "ShakerSchlaf", 0, 8.9999999999999996E+240, 2),
        new("ShakerSchlaf-cu33", "Shaker Schlaf: Aufwertung 33 (x2)", "ShakerSchlaf", 0, 4.4000000000000006E+244, 2),
        new("ShakerSchlaf-cu34", "Shaker Schlaf: Aufwertung 34 (x3)", "ShakerSchlaf", 0, 1.0000000000000001E+253, 3),
        new("ShakerSchlaf-cu35", "Shaker Schlaf: Aufwertung 35 (x9)", "ShakerSchlaf", 0, 1.2500000000000002E+254, 9),
        new("ShakerSchlaf-cu36", "Shaker Schlaf: Aufwertung 36 (x3)", "ShakerSchlaf", 0, 2.33E+260, 3),
        new("ShakerSchlaf-cu37", "Shaker Schlaf: Aufwertung 37 (x3)", "ShakerSchlaf", 0, 4.4999999999999999E+265, 3),
        new("ShakerSchlaf-cu38", "Shaker Schlaf: Aufwertung 38 (x7)", "ShakerSchlaf", 0, 3E+273, 7),
        new("ShakerSchlaf-cu39", "Shaker Schlaf: Aufwertung 39 (x13)", "ShakerSchlaf", 0, 9.9999999999999998E+284, 13),
        new("ShakerRot-cu1", "Shaker Rot: Aufwertung 1 (x3)", "ShakerRot", 0, 5000000, 3),
        new("ShakerRot-cu2", "Shaker Rot: Aufwertung 2 (x3)", "ShakerRot", 0, 500000000000000, 3),
        new("ShakerRot-cu3", "Shaker Rot: Aufwertung 3 (x3)", "ShakerRot", 0, 1E+19, 3),
        new("ShakerRot-cu4", "Shaker Rot: Aufwertung 4 (x3)", "ShakerRot", 0, 1.9999999999999998E+23, 3),
        new("ShakerRot-cu5", "Shaker Rot: Aufwertung 5 (x7)", "ShakerRot", 0, 1.0000000000000001E+29, 7),
        new("ShakerRot-cu6", "Shaker Rot: Aufwertung 6 (x3)", "ShakerRot", 0, 5.0000000000000004E+43, 3),
        new("ShakerRot-cu7", "Shaker Rot: Aufwertung 7 (x3)", "ShakerRot", 0, 7.4999999999999999E+47, 3),
        new("ShakerRot-cu8", "Shaker Rot: Aufwertung 8 (x3)", "ShakerRot", 0, 9.9999999999999998E+66, 3),
        new("ShakerRot-cu9", "Shaker Rot: Aufwertung 9 (x3)", "ShakerRot", 0, 2.4999999999999997E+91, 3),
        new("ShakerRot-cu10", "Shaker Rot: Aufwertung 10 (x2)", "ShakerRot", 0, 6.6000000000000003E+97, 2),
        new("ShakerRot-cu11", "Shaker Rot: Aufwertung 11 (x3)", "ShakerRot", 0, 8.9999999999999997E+104, 3),
        new("ShakerRot-cu12", "Shaker Rot: Aufwertung 12 (x3)", "ShakerRot", 0, 6.0000000000000001E+105, 3),
        new("ShakerRot-cu13", "Shaker Rot: Aufwertung 13 (x3)", "ShakerRot", 0, 1.4999999999999998E+106, 3),
        new("ShakerRot-cu14", "Shaker Rot: Aufwertung 14 (x3)", "ShakerRot", 0, 3.9999999999999999E+120, 3),
        new("ShakerRot-cu15", "Shaker Rot: Aufwertung 15 (x3)", "ShakerRot", 0, 1.2E+124, 3),
        new("ShakerRot-cu16", "Shaker Rot: Aufwertung 16 (x3)", "ShakerRot", 0, 1.2999999999999999E+130, 3),
        new("ShakerRot-cu17", "Shaker Rot: Aufwertung 17 (x2)", "ShakerRot", 0, 2.1300000000000001E+134, 2),
        new("ShakerRot-cu18", "Shaker Rot: Aufwertung 18 (x3)", "ShakerRot", 0, 9.2500000000000007E+140, 3),
        new("ShakerRot-cu19", "Shaker Rot: Aufwertung 19 (x3)", "ShakerRot", 0, 9.0000000000000007E+144, 3),
        new("ShakerRot-cu20", "Shaker Rot: Aufwertung 20 (x5)", "ShakerRot", 0, 5.0000000000000001E+150, 5),
        new("ShakerRot-cu21", "Shaker Rot: Aufwertung 21 (x2)", "ShakerRot", 0, 3.9999999999999999E+156, 2),
        new("ShakerRot-cu22", "Shaker Rot: Aufwertung 22 (x3)", "ShakerRot", 0, 7.499999999999999E+160, 3),
        new("ShakerRot-cu23", "Shaker Rot: Aufwertung 23 (x8)", "ShakerRot", 0, 1.0000000000000001E+174, 8),
        new("ShakerRot-cu24", "Shaker Rot: Aufwertung 24 (x3)", "ShakerRot", 0, 1.9799999999999999E+203, 3),
        new("ShakerRot-cu25", "Shaker Rot: Aufwertung 25 (x3)", "ShakerRot", 0, 7.7699999999999995E+206, 3),
        new("ShakerRot-cu26", "Shaker Rot: Aufwertung 26 (x11)", "ShakerRot", 0, 9.9999999999999995E+213, 11),
        new("ShakerRot-cu27", "Shaker Rot: Aufwertung 27 (x3)", "ShakerRot", 0, 4.0999999999999996E+215, 3),
        new("ShakerRot-cu28", "Shaker Rot: Aufwertung 28 (x3)", "ShakerRot", 0, 9.0000000000000002E+218, 3),
        new("ShakerRot-cu29", "Shaker Rot: Aufwertung 29 (x3)", "ShakerRot", 0, 2.0000000000000001E+222, 3),
        new("ShakerRot-cu30", "Shaker Rot: Aufwertung 30 (x7)", "ShakerRot", 0, 9.9999999999999992E+227, 7),
        new("ShakerRot-cu31", "Shaker Rot: Aufwertung 31 (x3)", "ShakerRot", 0, 1.8800000000000002E+233, 3),
        new("ShakerRot-cu32", "Shaker Rot: Aufwertung 32 (x3)", "ShakerRot", 0, 5.66E+236, 3),
        new("ShakerRot-cu33", "Shaker Rot: Aufwertung 33 (x2)", "ShakerRot", 0, 2.1E+241, 2),
        new("ShakerRot-cu34", "Shaker Rot: Aufwertung 34 (x2)", "ShakerRot", 0, 6.6000000000000009E+244, 2),
        new("ShakerRot-cu35", "Shaker Rot: Aufwertung 35 (x3)", "ShakerRot", 0, 1.0000000000000001E+253, 3),
        new("ShakerRot-cu36", "Shaker Rot: Aufwertung 36 (x9)", "ShakerRot", 0, 6.2500000000000002E+254, 9),
        new("ShakerRot-cu37", "Shaker Rot: Aufwertung 37 (x3)", "ShakerRot", 0, 3.9900000000000005E+260, 3),
        new("ShakerRot-cu38", "Shaker Rot: Aufwertung 38 (x3)", "ShakerRot", 0, 6.9000000000000006E+265, 3),
        new("ShakerRot-cu39", "Shaker Rot: Aufwertung 39 (x7)", "ShakerRot", 0, 5.9999999999999999E+273, 7),
        new("ShakerRot-cu40", "Shaker Rot: Aufwertung 40 (x13)", "ShakerRot", 0, 9.9999999999999998E+284, 13),
        new("ShakerStrong-cu1", "Shaker Strong: Aufwertung 1 (x3)", "ShakerStrong", 0, 10000000, 3),
        new("ShakerStrong-cu2", "Shaker Strong: Aufwertung 2 (x3)", "ShakerStrong", 0, 1000000000000000, 3),
        new("ShakerStrong-cu3", "Shaker Strong: Aufwertung 3 (x3)", "ShakerStrong", 0, 2E+19, 3),
        new("ShakerStrong-cu4", "Shaker Strong: Aufwertung 4 (x3)", "ShakerStrong", 0, 3.0000000000000001E+23, 3),
        new("ShakerStrong-cu5", "Shaker Strong: Aufwertung 5 (x7)", "ShakerStrong", 0, 2.5E+29, 7),
        new("ShakerStrong-cu6", "Shaker Strong: Aufwertung 6 (x3)", "ShakerStrong", 0, 1.0000000000000001E+44, 3),
        new("ShakerStrong-cu7", "Shaker Strong: Aufwertung 7 (x3)", "ShakerStrong", 0, 1E+48, 3),
        new("ShakerStrong-cu8", "Shaker Strong: Aufwertung 8 (x3)", "ShakerStrong", 0, 9.9999999999999995E+67, 3),
        new("ShakerStrong-cu9", "Shaker Strong: Aufwertung 9 (x3)", "ShakerStrong", 0, 4.9999999999999995E+91, 3),
        new("ShakerStrong-cu10", "Shaker Strong: Aufwertung 10 (x2)", "ShakerStrong", 0, 2.3000000000000001E+98, 2),
        new("ShakerStrong-cu11", "Shaker Strong: Aufwertung 11 (x2)", "ShakerStrong", 0, 5.9999999999999993E+106, 2),
        new("ShakerStrong-cu12", "Shaker Strong: Aufwertung 12 (x3)", "ShakerStrong", 0, 1.8499999999999999E+107, 3),
        new("ShakerStrong-cu13", "Shaker Strong: Aufwertung 13 (x3)", "ShakerStrong", 0, 4.9999999999999995E+107, 3),
        new("ShakerStrong-cu14", "Shaker Strong: Aufwertung 14 (x3)", "ShakerStrong", 0, 8.9999999999999995E+120, 3),
        new("ShakerStrong-cu15", "Shaker Strong: Aufwertung 15 (x3)", "ShakerStrong", 0, 2.4000000000000001E+124, 3),
        new("ShakerStrong-cu16", "Shaker Strong: Aufwertung 16 (x3)", "ShakerStrong", 0, 5.5500000000000003E+131, 3),
        new("ShakerStrong-cu17", "Shaker Strong: Aufwertung 17 (x2)", "ShakerStrong", 0, 3.9999999999999997E+134, 2),
        new("ShakerStrong-cu18", "Shaker Strong: Aufwertung 18 (x3)", "ShakerStrong", 0, 2.1E+142, 3),
        new("ShakerStrong-cu19", "Shaker Strong: Aufwertung 19 (x3)", "ShakerStrong", 0, 2.1000000000000002E+145, 3),
        new("ShakerStrong-cu20", "Shaker Strong: Aufwertung 20 (x5)", "ShakerStrong", 0, 8.0000000000000001E+151, 5),
        new("ShakerStrong-cu21", "Shaker Strong: Aufwertung 21 (x2)", "ShakerStrong", 0, 1.6E+157, 2),
        new("ShakerStrong-cu22", "Shaker Strong: Aufwertung 22 (x3)", "ShakerStrong", 0, 1.4999999999999998E+161, 3),
        new("ShakerStrong-cu23", "Shaker Strong: Aufwertung 23 (x4)", "ShakerStrong", 0, 5.0000000000000006E+176, 4),
        new("ShakerStrong-cu24", "Shaker Strong: Aufwertung 24 (x3)", "ShakerStrong", 0, 3.2200000000000004E+203, 3),
        new("ShakerStrong-cu25", "Shaker Strong: Aufwertung 25 (x3)", "ShakerStrong", 0, 9.1000000000000005E+206, 3),
        new("ShakerStrong-cu26", "Shaker Strong: Aufwertung 26 (x11)", "ShakerStrong", 0, 9.9999999999999995E+213, 11),
        new("ShakerStrong-cu27", "Shaker Strong: Aufwertung 27 (x3)", "ShakerStrong", 0, 6.7799999999999998E+215, 3),
        new("ShakerStrong-cu28", "Shaker Strong: Aufwertung 28 (x3)", "ShakerStrong", 0, 2.9999999999999997E+219, 3),
        new("ShakerStrong-cu29", "Shaker Strong: Aufwertung 29 (x3)", "ShakerStrong", 0, 5.0000000000000002E+222, 3),
        new("ShakerStrong-cu30", "Shaker Strong: Aufwertung 30 (x7)", "ShakerStrong", 0, 9.9999999999999992E+227, 7),
        new("ShakerStrong-cu31", "Shaker Strong: Aufwertung 31 (x3)", "ShakerStrong", 0, 2.3899999999999999E+233, 3),
        new("ShakerStrong-cu32", "Shaker Strong: Aufwertung 32 (x3)", "ShakerStrong", 0, 7.0000000000000005E+236, 3),
        new("ShakerStrong-cu33", "Shaker Strong: Aufwertung 33 (x2)", "ShakerStrong", 0, 4.4999999999999999E+241, 2),
        new("ShakerStrong-cu34", "Shaker Strong: Aufwertung 34 (x2)", "ShakerStrong", 0, 8.8000000000000013E+244, 2),
        new("ShakerStrong-cu35", "Shaker Strong: Aufwertung 35 (x3)", "ShakerStrong", 0, 1.0000000000000001E+253, 3),
        new("ShakerStrong-cu36", "Shaker Strong: Aufwertung 36 (x9)", "ShakerStrong", 0, 2.9999999999999998E+255, 9),
        new("ShakerStrong-cu37", "Shaker Strong: Aufwertung 37 (x3)", "ShakerStrong", 0, 7.6600000000000003E+260, 3),
        new("ShakerStrong-cu38", "Shaker Strong: Aufwertung 38 (x3)", "ShakerStrong", 0, 8.9000000000000004E+265, 3),
        new("ShakerStrong-cu39", "Shaker Strong: Aufwertung 39 (x7)", "ShakerStrong", 0, 2.4999999999999999E+274, 7),
        new("ShakerStrong-cu40", "Shaker Strong: Aufwertung 40 (x13)", "ShakerStrong", 0, 9.9999999999999998E+284, 13),
        new("ShakerDealer-cu1", "Shaker Dealer: Aufwertung 1 (x3)", "ShakerDealer", 0, 25000000, 3),
        new("ShakerDealer-cu2", "Shaker Dealer: Aufwertung 2 (x3)", "ShakerDealer", 0, 2000000000000000, 3),
        new("ShakerDealer-cu3", "Shaker Dealer: Aufwertung 3 (x3)", "ShakerDealer", 0, 3.5E+19, 3),
        new("ShakerDealer-cu4", "Shaker Dealer: Aufwertung 4 (x3)", "ShakerDealer", 0, 3.9999999999999997E+23, 3),
        new("ShakerDealer-cu5", "Shaker Dealer: Aufwertung 5 (x7)", "ShakerDealer", 0, 5.0000000000000001E+29, 7),
        new("ShakerDealer-cu6", "Shaker Dealer: Aufwertung 6 (x3)", "ShakerDealer", 0, 2.5000000000000002E+44, 3),
        new("ShakerDealer-cu7", "Shaker Dealer: Aufwertung 7 (x3)", "ShakerDealer", 0, 5.0000000000000004E+48, 3),
        new("ShakerDealer-cu8", "Shaker Dealer: Aufwertung 8 (x3)", "ShakerDealer", 0, 9.9999999999999998E+72, 3),
        new("ShakerDealer-cu9", "Shaker Dealer: Aufwertung 9 (x3)", "ShakerDealer", 0, 9.999999999999999E+91, 3),
        new("ShakerDealer-cu10", "Shaker Dealer: Aufwertung 10 (x2)", "ShakerDealer", 0, 4E+98, 2),
        new("ShakerDealer-cu11", "Shaker Dealer: Aufwertung 11 (x2)", "ShakerDealer", 0, 7.4999999999999996E+107, 2),
        new("ShakerDealer-cu12", "Shaker Dealer: Aufwertung 12 (x3)", "ShakerDealer", 0, 4.9999999999999999E+108, 3),
        new("ShakerDealer-cu13", "Shaker Dealer: Aufwertung 13 (x3)", "ShakerDealer", 0, 4.5000000000000004E+109, 3),
        new("ShakerDealer-cu14", "Shaker Dealer: Aufwertung 14 (x3)", "ShakerDealer", 0, 2.4000000000000002E+121, 3),
        new("ShakerDealer-cu15", "Shaker Dealer: Aufwertung 15 (x3)", "ShakerDealer", 0, 4.8000000000000001E+124, 3),
        new("ShakerDealer-cu16", "Shaker Dealer: Aufwertung 16 (x3)", "ShakerDealer", 0, 7.3600000000000007E+131, 3),
        new("ShakerDealer-cu17", "Shaker Dealer: Aufwertung 17 (x2)", "ShakerDealer", 0, 9.8500000000000006E+134, 2),
        new("ShakerDealer-cu18", "Shaker Dealer: Aufwertung 18 (x3)", "ShakerDealer", 0, 5.5000000000000003E+142, 3),
        new("ShakerDealer-cu19", "Shaker Dealer: Aufwertung 19 (x3)", "ShakerDealer", 0, 4.4000000000000002E+145, 3),
        new("ShakerDealer-cu20", "Shaker Dealer: Aufwertung 20 (x5)", "ShakerDealer", 0, 5.0000000000000002E+147, 5),
        new("ShakerDealer-cu21", "Shaker Dealer: Aufwertung 21 (x2)", "ShakerDealer", 0, 7.7700000000000003E+155, 2),
        new("ShakerDealer-cu22", "Shaker Dealer: Aufwertung 22 (x3)", "ShakerDealer", 0, 9.9999999999999993E+158, 3),
        new("ShakerDealer-cu23", "Shaker Dealer: Aufwertung 23 (x16)", "ShakerDealer", 0, 9.9999999999999993E+167, 16),
        new("ShakerDealer-cu24", "Shaker Dealer: Aufwertung 24 (x3)", "ShakerDealer", 0, 2.0000000000000001E+207, 3),
        new("ShakerDealer-cu25", "Shaker Dealer: Aufwertung 25 (x11)", "ShakerDealer", 0, 9.9999999999999995E+213, 11),
        new("ShakerDealer-cu26", "Shaker Dealer: Aufwertung 26 (x3)", "ShakerDealer", 0, 9.0000000000000005E+215, 3),
        new("ShakerDealer-cu27", "Shaker Dealer: Aufwertung 27 (x3)", "ShakerDealer", 0, 3.9999999999999999E+219, 3),
        new("ShakerDealer-cu28", "Shaker Dealer: Aufwertung 28 (x3)", "ShakerDealer", 0, 1.3999999999999999E+223, 3),
        new("ShakerDealer-cu29", "Shaker Dealer: Aufwertung 29 (x7)", "ShakerDealer", 0, 9.9999999999999992E+227, 7),
        new("ShakerDealer-cu30", "Shaker Dealer: Aufwertung 30 (x3)", "ShakerDealer", 0, 4.1100000000000004E+233, 3),
        new("ShakerDealer-cu31", "Shaker Dealer: Aufwertung 31 (x3)", "ShakerDealer", 0, 8.0000000000000004E+236, 3),
        new("ShakerDealer-cu32", "Shaker Dealer: Aufwertung 32 (x2)", "ShakerDealer", 0, 8.9000000000000005E+241, 2),
        new("ShakerDealer-cu33", "Shaker Dealer: Aufwertung 33 (x2)", "ShakerDealer", 0, 1.1100000000000001E+245, 2),
        new("ShakerDealer-cu34", "Shaker Dealer: Aufwertung 34 (x3)", "ShakerDealer", 0, 1.0000000000000001E+253, 3),
        new("ShakerDealer-cu35", "Shaker Dealer: Aufwertung 35 (x9)", "ShakerDealer", 0, 1.5E+256, 9),
        new("ShakerDealer-cu36", "Shaker Dealer: Aufwertung 36 (x3)", "ShakerDealer", 0, 9.9999999999999993E+260, 3),
        new("ShakerDealer-cu37", "Shaker Dealer: Aufwertung 37 (x3)", "ShakerDealer", 0, 1.8900000000000001E+266, 3),
        new("ShakerDealer-cu38", "Shaker Dealer: Aufwertung 38 (x7)", "ShakerDealer", 0, 1.9999999999999999E+275, 7),
        new("ShakerDealer-cu39", "Shaker Dealer: Aufwertung 39 (x13)", "ShakerDealer", 0, 9.9999999999999998E+284, 13),
        new("ShakerMexico-cu1", "Shaker Mexico: Aufwertung 1 (x3)", "ShakerMexico", 0, 500000000, 3),
        new("ShakerMexico-cu2", "Shaker Mexico: Aufwertung 2 (x3)", "ShakerMexico", 0, 5000000000000000, 3),
        new("ShakerMexico-cu3", "Shaker Mexico: Aufwertung 3 (x3)", "ShakerMexico", 0, 5E+19, 3),
        new("ShakerMexico-cu4", "Shaker Mexico: Aufwertung 4 (x3)", "ShakerMexico", 0, 4.9999999999999999E+23, 3),
        new("ShakerMexico-cu5", "Shaker Mexico: Aufwertung 5 (x7)", "ShakerMexico", 0, 1E+30, 7),
        new("ShakerMexico-cu6", "Shaker Mexico: Aufwertung 6 (x3)", "ShakerMexico", 0, 5.0000000000000004E+44, 3),
        new("ShakerMexico-cu7", "Shaker Mexico: Aufwertung 7 (x3)", "ShakerMexico", 0, 1.5000000000000001E+49, 3),
        new("ShakerMexico-cu8", "Shaker Mexico: Aufwertung 8 (x3)", "ShakerMexico", 0, 9.9999999999999995E+73, 3),
        new("ShakerMexico-cu9", "Shaker Mexico: Aufwertung 9 (x3)", "ShakerMexico", 0, 2.4999999999999998E+92, 3),
        new("ShakerMexico-cu10", "Shaker Mexico: Aufwertung 10 (x2)", "ShakerMexico", 0, 7.0000000000000007E+98, 2),
        new("ShakerMexico-cu11", "Shaker Mexico: Aufwertung 11 (x3)", "ShakerMexico", 0, 1.2500000000000001E+110, 3),
        new("ShakerMexico-cu12", "Shaker Mexico: Aufwertung 12 (x3)", "ShakerMexico", 0, 3.0000000000000001E+110, 3),
        new("ShakerMexico-cu13", "Shaker Mexico: Aufwertung 13 (x3)", "ShakerMexico", 0, 9.0000000000000005E+110, 3),
        new("ShakerMexico-cu14", "Shaker Mexico: Aufwertung 14 (x3)", "ShakerMexico", 0, 1.1099999999999999E+122, 3),
        new("ShakerMexico-cu15", "Shaker Mexico: Aufwertung 15 (x3)", "ShakerMexico", 0, 9.6000000000000003E+124, 3),
        new("ShakerMexico-cu16", "Shaker Mexico: Aufwertung 16 (x3)", "ShakerMexico", 0, 1.77E+131, 3),
        new("ShakerMexico-cu17", "Shaker Mexico: Aufwertung 17 (x2)", "ShakerMexico", 0, 7.9999999999999997E+135, 2),
        new("ShakerMexico-cu18", "Shaker Mexico: Aufwertung 18 (x3)", "ShakerMexico", 0, 1.1099999999999999E+143, 3),
        new("ShakerMexico-cu19", "Shaker Mexico: Aufwertung 19 (x3)", "ShakerMexico", 0, 8.9000000000000005E+145, 3),
        new("ShakerMexico-cu20", "Shaker Mexico: Aufwertung 20 (x5)", "ShakerMexico", 0, 7.1999999999999998E+152, 5),
        new("ShakerMexico-cu21", "Shaker Mexico: Aufwertung 21 (x2)", "ShakerMexico", 0, 6.3999999999999999E+157, 2),
        new("ShakerMexico-cu22", "Shaker Mexico: Aufwertung 22 (x3)", "ShakerMexico", 0, 2.9999999999999996E+161, 3),
        new("ShakerMexico-cu23", "Shaker Mexico: Aufwertung 23 (x14)", "ShakerMexico", 0, 4.9999999999999998E+170, 14),
        new("ShakerMexico-cu24", "Shaker Mexico: Aufwertung 24 (x3)", "ShakerMexico", 0, 8.8800000000000003E+203, 3),
        new("ShakerMexico-cu25", "Shaker Mexico: Aufwertung 25 (x11)", "ShakerMexico", 0, 9.9999999999999995E+213, 11),
        new("ShakerMexico-cu26", "Shaker Mexico: Aufwertung 26 (x3)", "ShakerMexico", 0, 1.1999999999999999E+217, 3),
        new("ShakerMexico-cu27", "Shaker Mexico: Aufwertung 27 (x3)", "ShakerMexico", 0, 5E+219, 3),
        new("ShakerMexico-cu28", "Shaker Mexico: Aufwertung 28 (x3)", "ShakerMexico", 0, 5.4000000000000001E+223, 3),
        new("ShakerMexico-cu29", "Shaker Mexico: Aufwertung 29 (x7)", "ShakerMexico", 0, 9.9999999999999992E+227, 7),
        new("ShakerMexico-cu30", "Shaker Mexico: Aufwertung 30 (x3)", "ShakerMexico", 0, 7.0000000000000005E+233, 3),
        new("ShakerMexico-cu31", "Shaker Mexico: Aufwertung 31 (x3)", "ShakerMexico", 0, 9.0000000000000004E+236, 3),
        new("ShakerMexico-cu32", "Shaker Mexico: Aufwertung 32 (x2)", "ShakerMexico", 0, 1.5299999999999999E+242, 2),
        new("ShakerMexico-cu33", "Shaker Mexico: Aufwertung 33 (x2)", "ShakerMexico", 0, 2.2200000000000002E+245, 2),
        new("ShakerMexico-cu34", "Shaker Mexico: Aufwertung 34 (x3)", "ShakerMexico", 0, 1.0000000000000001E+253, 3),
        new("ShakerMexico-cu35", "Shaker Mexico: Aufwertung 35 (x9)", "ShakerMexico", 0, 7.4999999999999999E+256, 9),
        new("ShakerMexico-cu36", "Shaker Mexico: Aufwertung 36 (x3)", "ShakerMexico", 0, 1.8999999999999998E+262, 3),
        new("ShakerMexico-cu37", "Shaker Mexico: Aufwertung 37 (x3)", "ShakerMexico", 0, 2.8900000000000001E+266, 3),
        new("ShakerMexico-cu38", "Shaker Mexico: Aufwertung 38 (x7)", "ShakerMexico", 0, 6.0000000000000001E+275, 7),
        new("ShakerMexico-cu39", "Shaker Mexico: Aufwertung 39 (x13)", "ShakerMexico", 0, 9.9999999999999998E+284, 13),
        new("ShakerHorny-cu1", "Shaker Horny: Aufwertung 1 (x3)", "ShakerHorny", 0, 10000000000, 3),
        new("ShakerHorny-cu2", "Shaker Horny: Aufwertung 2 (x3)", "ShakerHorny", 0, 7000000000000000, 3),
        new("ShakerHorny-cu3", "Shaker Horny: Aufwertung 3 (x3)", "ShakerHorny", 0, 7.5E+19, 3),
        new("ShakerHorny-cu4", "Shaker Horny: Aufwertung 4 (x3)", "ShakerHorny", 0, 6.0000000000000002E+23, 3),
        new("ShakerHorny-cu5", "Shaker Horny: Aufwertung 5 (x7)", "ShakerHorny", 0, 4.9999999999999998E+30, 7),
        new("ShakerHorny-cu6", "Shaker Horny: Aufwertung 6 (x3)", "ShakerHorny", 0, 9.9999999999999993E+44, 3),
        new("ShakerHorny-cu7", "Shaker Horny: Aufwertung 7 (x3)", "ShakerHorny", 0, 5.0000000000000004E+49, 3),
        new("ShakerHorny-cu8", "Shaker Horny: Aufwertung 8 (x3)", "ShakerHorny", 0, 9.9999999999999989E+75, 3),
        new("ShakerHorny-cu9", "Shaker Horny: Aufwertung 9 (x3)", "ShakerHorny", 0, 4.9999999999999996E+92, 3),
        new("ShakerHorny-cu10", "Shaker Horny: Aufwertung 10 (x2)", "ShakerHorny", 0, 3.9999999999999999E+99, 2),
        new("ShakerHorny-cu11", "Shaker Horny: Aufwertung 11 (x2)", "ShakerHorny", 0, 4.9999999999999997E+111, 2),
        new("ShakerHorny-cu12", "Shaker Horny: Aufwertung 12 (x3)", "ShakerHorny", 0, 6.9999999999999999E+112, 3),
        new("ShakerHorny-cu13", "Shaker Horny: Aufwertung 13 (x3)", "ShakerHorny", 0, 2.5E+113, 3),
        new("ShakerHorny-cu14", "Shaker Horny: Aufwertung 14 (x3)", "ShakerHorny", 0, 2.2199999999999998E+122, 3),
        new("ShakerHorny-cu15", "Shaker Horny: Aufwertung 15 (x3)", "ShakerHorny", 0, 1.9200000000000001E+125, 3),
        new("ShakerHorny-cu16", "Shaker Horny: Aufwertung 16 (x3)", "ShakerHorny", 0, 3.1000000000000002E+131, 3),
        new("ShakerHorny-cu17", "Shaker Horny: Aufwertung 17 (x2)", "ShakerHorny", 0, 2.8999999999999997E+136, 2),
        new("ShakerHorny-cu18", "Shaker Horny: Aufwertung 18 (x3)", "ShakerHorny", 0, 2.2300000000000001E+143, 3),
        new("ShakerHorny-cu19", "Shaker Horny: Aufwertung 19 (x3)", "ShakerHorny", 0, 1.29E+146, 3),
        new("ShakerHorny-cu20", "Shaker Horny: Aufwertung 20 (x5)", "ShakerHorny", 0, 2.0999999999999999E+154, 5),
        new("ShakerHorny-cu21", "Shaker Horny: Aufwertung 21 (x2)", "ShakerHorny", 0, 1.28E+158, 2),
        new("ShakerHorny-cu22", "Shaker Horny: Aufwertung 22 (x3)", "ShakerHorny", 0, 3.9999999999999997E+161, 3),
        new("ShakerHorny-cu23", "Shaker Horny: Aufwertung 23 (x24)", "ShakerHorny", 0, 9.9999999999999994E+161, 24),
        new("ShakerHorny-cu24", "Shaker Horny: Aufwertung 24 (x3)", "ShakerHorny", 0, 1.9E+205, 3),
        new("ShakerHorny-cu25", "Shaker Horny: Aufwertung 25 (x3)", "ShakerHorny", 0, 4.5000000000000001E+208, 3),
        new("ShakerHorny-cu26", "Shaker Horny: Aufwertung 26 (x11)", "ShakerHorny", 0, 9.9999999999999995E+213, 11),
        new("ShakerHorny-cu27", "Shaker Horny: Aufwertung 27 (x3)", "ShakerHorny", 0, 6.6999999999999999E+217, 3),
        new("ShakerHorny-cu28", "Shaker Horny: Aufwertung 28 (x3)", "ShakerHorny", 0, 5.9999999999999995E+219, 3),
        new("ShakerHorny-cu29", "Shaker Horny: Aufwertung 29 (x3)", "ShakerHorny", 0, 1.08E+224, 3),
        new("ShakerHorny-cu30", "Shaker Horny: Aufwertung 30 (x7)", "ShakerHorny", 0, 9.9999999999999992E+227, 7),
        new("ShakerHorny-cu31", "Shaker Horny: Aufwertung 31 (x3)", "ShakerHorny", 0, 9.1200000000000011E+233, 3),
        new("ShakerHorny-cu32", "Shaker Horny: Aufwertung 32 (x3)", "ShakerHorny", 0, 1.2E+238, 3),
        new("ShakerHorny-cu33", "Shaker Horny: Aufwertung 33 (x2)", "ShakerHorny", 0, 2.99E+242, 2),
        new("ShakerHorny-cu34", "Shaker Horny: Aufwertung 34 (x2)", "ShakerHorny", 0, 3.3300000000000004E+245, 2),
        new("ShakerHorny-cu35", "Shaker Horny: Aufwertung 35 (x3)", "ShakerHorny", 0, 1.0000000000000001E+253, 3),
        new("ShakerHorny-cu36", "Shaker Horny: Aufwertung 36 (x9)", "ShakerHorny", 0, 3.7499999999999999E+257, 9),
        new("ShakerHorny-cu37", "Shaker Horny: Aufwertung 37 (x3)", "ShakerHorny", 0, 9.7999999999999997E+262, 3),
        new("ShakerHorny-cu38", "Shaker Horny: Aufwertung 38 (x3)", "ShakerHorny", 0, 4.4800000000000004E+266, 3),
        new("ShakerHorny-cu39", "Shaker Horny: Aufwertung 39 (x7)", "ShakerHorny", 0, 9.9899999999999995E+275, 7),
        new("ShakerHorny-cu40", "Shaker Horny: Aufwertung 40 (x13)", "ShakerHorny", 0, 9.9999999999999998E+284, 13),
        new("ShakerPrinzessin-cu1", "Shaker Prinzessin: Aufwertung 1 (x3)", "ShakerPrinzessin", 0, 50000000000, 3),
        new("ShakerPrinzessin-cu2", "Shaker Prinzessin: Aufwertung 2 (x3)", "ShakerPrinzessin", 0, 10000000000000000, 3),
        new("ShakerPrinzessin-cu3", "Shaker Prinzessin: Aufwertung 3 (x3)", "ShakerPrinzessin", 0, 1E+20, 3),
        new("ShakerPrinzessin-cu4", "Shaker Prinzessin: Aufwertung 4 (x3)", "ShakerPrinzessin", 0, 7.0000000000000004E+23, 3),
        new("ShakerPrinzessin-cu5", "Shaker Prinzessin: Aufwertung 5 (x7)", "ShakerPrinzessin", 0, 2.5000000000000001E+31, 7),
        new("ShakerPrinzessin-cu6", "Shaker Prinzessin: Aufwertung 6 (x3)", "ShakerPrinzessin", 0, 5E+45, 3),
        new("ShakerPrinzessin-cu7", "Shaker Prinzessin: Aufwertung 7 (x3)", "ShakerPrinzessin", 0, 1.0000000000000001E+50, 3),
        new("ShakerPrinzessin-cu8", "Shaker Prinzessin: Aufwertung 8 (x3)", "ShakerPrinzessin", 0, 9.9999999999999998E+76, 3),
        new("ShakerPrinzessin-cu9", "Shaker Prinzessin: Aufwertung 9 (x3)", "ShakerPrinzessin", 0, 1E+93, 3),
        new("ShakerPrinzessin-cu10", "Shaker Prinzessin: Aufwertung 10 (x2)", "ShakerPrinzessin", 0, 2.9E+100, 2),
        new("ShakerPrinzessin-cu11", "Shaker Prinzessin: Aufwertung 11 (x3)", "ShakerPrinzessin", 0, 5.0000000000000001E+113, 3),
        new("ShakerPrinzessin-cu12", "Shaker Prinzessin: Aufwertung 12 (x3)", "ShakerPrinzessin", 0, 9.0000000000000001E+113, 3),
        new("ShakerPrinzessin-cu13", "Shaker Prinzessin: Aufwertung 13 (x3)", "ShakerPrinzessin", 0, 3E+114, 3),
        new("ShakerPrinzessin-cu14", "Shaker Prinzessin: Aufwertung 14 (x3)", "ShakerPrinzessin", 0, 3.3299999999999997E+122, 3),
        new("ShakerPrinzessin-cu15", "Shaker Prinzessin: Aufwertung 15 (x3)", "ShakerPrinzessin", 0, 3.8400000000000001E+125, 3),
        new("ShakerPrinzessin-cu16", "Shaker Prinzessin: Aufwertung 16 (x3)", "ShakerPrinzessin", 0, 5.0000000000000003E+129, 3),
        new("ShakerPrinzessin-cu17", "Shaker Prinzessin: Aufwertung 17 (x2)", "ShakerPrinzessin", 0, 2.22E+137, 2),
        new("ShakerPrinzessin-cu18", "Shaker Prinzessin: Aufwertung 18 (x3)", "ShakerPrinzessin", 0, 3.9300000000000003E+143, 3),
        new("ShakerPrinzessin-cu19", "Shaker Prinzessin: Aufwertung 19 (x3)", "ShakerPrinzessin", 0, 1.8000000000000001E+146, 3),
        new("ShakerPrinzessin-cu20", "Shaker Prinzessin: Aufwertung 20 (x5)", "ShakerPrinzessin", 0, 8.9999999999999996E+149, 5),
        new("ShakerPrinzessin-cu21", "Shaker Prinzessin: Aufwertung 21 (x2)", "ShakerPrinzessin", 0, 2E+156, 2),
        new("ShakerPrinzessin-cu22", "Shaker Prinzessin: Aufwertung 22 (x3)", "ShakerPrinzessin", 0, 4.9999999999999996E+160, 3),
        new("ShakerPrinzessin-cu23", "Shaker Prinzessin: Aufwertung 23 (x18)", "ShakerPrinzessin", 0, 4.9999999999999997E+167, 18),
        new("ShakerPrinzessin-cu24", "Shaker Prinzessin: Aufwertung 24 (x3)", "ShakerPrinzessin", 0, 2.0000000000000001E+209, 3),
        new("ShakerPrinzessin-cu25", "Shaker Prinzessin: Aufwertung 25 (x11)", "ShakerPrinzessin", 0, 9.9999999999999995E+213, 11),
        new("ShakerPrinzessin-cu26", "Shaker Prinzessin: Aufwertung 26 (x3)", "ShakerPrinzessin", 0, 1.23E+218, 3),
        new("ShakerPrinzessin-cu27", "Shaker Prinzessin: Aufwertung 27 (x3)", "ShakerPrinzessin", 0, 2.9999999999999997E+221, 3),
        new("ShakerPrinzessin-cu28", "Shaker Prinzessin: Aufwertung 28 (x3)", "ShakerPrinzessin", 0, 2.19E+224, 3),
        new("ShakerPrinzessin-cu29", "Shaker Prinzessin: Aufwertung 29 (x7)", "ShakerPrinzessin", 0, 9.9999999999999992E+227, 7),
        new("ShakerPrinzessin-cu30", "Shaker Prinzessin: Aufwertung 30 (x3)", "ShakerPrinzessin", 0, 1.2E+235, 3),
        new("ShakerPrinzessin-cu31", "Shaker Prinzessin: Aufwertung 31 (x3)", "ShakerPrinzessin", 0, 2.5E+238, 3),
        new("ShakerPrinzessin-cu32", "Shaker Prinzessin: Aufwertung 32 (x2)", "ShakerPrinzessin", 0, 5.7699999999999998E+242, 2),
        new("ShakerPrinzessin-cu33", "Shaker Prinzessin: Aufwertung 33 (x2)", "ShakerPrinzessin", 0, 4.4400000000000005E+245, 2),
        new("ShakerPrinzessin-cu34", "Shaker Prinzessin: Aufwertung 34 (x3)", "ShakerPrinzessin", 0, 1.0000000000000001E+253, 3),
        new("ShakerPrinzessin-cu35", "Shaker Prinzessin: Aufwertung 35 (x9)", "ShakerPrinzessin", 0, 1.0000000000000001E+258, 9),
        new("ShakerPrinzessin-cu36", "Shaker Prinzessin: Aufwertung 36 (x3)", "ShakerPrinzessin", 0, 2.5999999999999996E+263, 3),
        new("ShakerPrinzessin-cu37", "Shaker Prinzessin: Aufwertung 37 (x3)", "ShakerPrinzessin", 0, 9E+266, 3),
        new("ShakerPrinzessin-cu38", "Shaker Prinzessin: Aufwertung 38 (x7)", "ShakerPrinzessin", 0, 1.5000000000000001E+277, 7),
        new("ShakerPrinzessin-cu39", "Shaker Prinzessin: Aufwertung 39 (x13)", "ShakerPrinzessin", 0, 9.9999999999999998E+284, 13),
        new("ShakerFinalBoss-cu1", "Shaker Final Boss: Aufwertung 1 (x3)", "ShakerFinalBoss", 0, 250000000000, 3),
        new("ShakerFinalBoss-cu2", "Shaker Final Boss: Aufwertung 2 (x3)", "ShakerFinalBoss", 0, 20000000000000000, 3),
        new("ShakerFinalBoss-cu3", "Shaker Final Boss: Aufwertung 3 (x3)", "ShakerFinalBoss", 0, 2E+20, 3),
        new("ShakerFinalBoss-cu4", "Shaker Final Boss: Aufwertung 4 (x3)", "ShakerFinalBoss", 0, 7.9999999999999993E+23, 3),
        new("ShakerFinalBoss-cu5", "Shaker Final Boss: Aufwertung 5 (x7)", "ShakerFinalBoss", 0, 5.0000000000000003E+31, 7),
        new("ShakerFinalBoss-cu6", "Shaker Final Boss: Aufwertung 6 (x3)", "ShakerFinalBoss", 0, 9.9999999999999999E+45, 3),
        new("ShakerFinalBoss-cu7", "Shaker Final Boss: Aufwertung 7 (x3)", "ShakerFinalBoss", 0, 2.5E+50, 3),
        new("ShakerFinalBoss-cu8", "Shaker Final Boss: Aufwertung 8 (x3)", "ShakerFinalBoss", 0, 9.9999999999999997E+78, 3),
        new("ShakerFinalBoss-cu9", "Shaker Final Boss: Aufwertung 9 (x3)", "ShakerFinalBoss", 0, 5.0000000000000001E+93, 3),
        new("ShakerFinalBoss-cu10", "Shaker Final Boss: Aufwertung 10 (x2)", "ShakerFinalBoss", 0, 1.4499999999999999E+101, 2),
        new("ShakerFinalBoss-cu11", "Shaker Final Boss: Aufwertung 11 (x3)", "ShakerFinalBoss", 0, 1.5E+115, 3),
        new("ShakerFinalBoss-cu12", "Shaker Final Boss: Aufwertung 12 (x3)", "ShakerFinalBoss", 0, 7.4999999999999997E+115, 3),
        new("ShakerFinalBoss-cu13", "Shaker Final Boss: Aufwertung 13 (x3)", "ShakerFinalBoss", 0, 4.0000000000000001E+116, 3),
        new("ShakerFinalBoss-cu14", "Shaker Final Boss: Aufwertung 14 (x3)", "ShakerFinalBoss", 0, 4.4399999999999996E+122, 3),
        new("ShakerFinalBoss-cu15", "Shaker Final Boss: Aufwertung 15 (x3)", "ShakerFinalBoss", 0, 7.6800000000000002E+125, 3),
        new("ShakerFinalBoss-cu16", "Shaker Final Boss: Aufwertung 16 (x3)", "ShakerFinalBoss", 0, 2.8999999999999999E+130, 3),
        new("ShakerFinalBoss-cu17", "Shaker Final Boss: Aufwertung 17 (x2)", "ShakerFinalBoss", 0, 5.0000000000000002E+137, 2),
        new("ShakerFinalBoss-cu18", "Shaker Final Boss: Aufwertung 18 (x3)", "ShakerFinalBoss", 0, 6.0000000000000001E+143, 3),
        new("ShakerFinalBoss-cu19", "Shaker Final Boss: Aufwertung 19 (x3)", "ShakerFinalBoss", 0, 2.1000000000000001E+146, 3),
        new("ShakerFinalBoss-cu20", "Shaker Final Boss: Aufwertung 20 (x5)", "ShakerFinalBoss", 0, 2E+151, 5),
        new("ShakerFinalBoss-cu21", "Shaker Final Boss: Aufwertung 21 (x2)", "ShakerFinalBoss", 0, 7.9999999999999999E+156, 2),
        new("ShakerFinalBoss-cu22", "Shaker Final Boss: Aufwertung 22 (x3)", "ShakerFinalBoss", 0, 9.9999999999999991E+160, 3),
        new("ShakerFinalBoss-cu23", "Shaker Final Boss: Aufwertung 23 (x12)", "ShakerFinalBoss", 0, 9.9999999999999995E+170, 12),
        new("ShakerFinalBoss-cu24", "Shaker Final Boss: Aufwertung 24 (x3)", "ShakerFinalBoss", 0, 1.9899999999999999E+206, 3),
        new("ShakerFinalBoss-cu25", "Shaker Final Boss: Aufwertung 25 (x11)", "ShakerFinalBoss", 0, 9.9999999999999995E+213, 11),
        new("ShakerFinalBoss-cu26", "Shaker Final Boss: Aufwertung 26 (x3)", "ShakerFinalBoss", 0, 3.21E+218, 3),
        new("ShakerFinalBoss-cu27", "Shaker Final Boss: Aufwertung 27 (x3)", "ShakerFinalBoss", 0, 4.2100000000000001E+221, 3),
        new("ShakerFinalBoss-cu28", "Shaker Final Boss: Aufwertung 28 (x3)", "ShakerFinalBoss", 0, 4.6800000000000002E+224, 3),
        new("ShakerFinalBoss-cu29", "Shaker Final Boss: Aufwertung 29 (x7)", "ShakerFinalBoss", 0, 9.9999999999999992E+227, 7),
        new("ShakerFinalBoss-cu30", "Shaker Final Boss: Aufwertung 30 (x3)", "ShakerFinalBoss", 0, 2.3999999999999999E+235, 3),
        new("ShakerFinalBoss-cu31", "Shaker Final Boss: Aufwertung 31 (x3)", "ShakerFinalBoss", 0, 5E+238, 3),
        new("ShakerFinalBoss-cu32", "Shaker Final Boss: Aufwertung 32 (x2)", "ShakerFinalBoss", 0, 8.1300000000000002E+242, 2),
        new("ShakerFinalBoss-cu33", "Shaker Final Boss: Aufwertung 33 (x2)", "ShakerFinalBoss", 0, 5.5500000000000006E+245, 2),
        new("ShakerFinalBoss-cu34", "Shaker Final Boss: Aufwertung 34 (x3)", "ShakerFinalBoss", 0, 1.0000000000000001E+253, 3),
        new("ShakerFinalBoss-cu35", "Shaker Final Boss: Aufwertung 35 (x9)", "ShakerFinalBoss", 0, 8.0000000000000002E+257, 9),
        new("ShakerFinalBoss-cu36", "Shaker Final Boss: Aufwertung 36 (x3)", "ShakerFinalBoss", 0, 5.4399999999999992E+263, 3),
        new("ShakerFinalBoss-cu37", "Shaker Final Boss: Aufwertung 37 (x3)", "ShakerFinalBoss", 0, 4.9999999999999999E+267, 3),
        new("ShakerFinalBoss-cu38", "Shaker Final Boss: Aufwertung 38 (x7)", "ShakerFinalBoss", 0, 3.0000000000000002E+277, 7),
        new("ShakerFinalBoss-cu39", "Shaker Final Boss: Aufwertung 39 (x13)", "ShakerFinalBoss", 0, 9.9999999999999998E+284, 13),

        new("AllBusinesses-cu1", "Alle Businesses: Aufwertung 1 (x3)", null, 0, 1E+12, 3),
        new("AllBusinesses-cu2", "Alle Businesses: Aufwertung 2 (x3)", null, 0, 5E+16, 3),
        new("AllBusinesses-cu3", "Alle Businesses: Aufwertung 3 (x3)", null, 0, 5E+20, 3),
        new("AllBusinesses-cu4", "Alle Businesses: Aufwertung 4 (x3)", null, 0, 9E+23, 3),
        new("AllBusinesses-cu5", "Alle Businesses: Aufwertung 5 (x7)", null, 0, 1E+42, 7),
        new("AllBusinesses-cu6", "Alle Businesses: Aufwertung 6 (x3)", null, 0, 1E+47, 3),
        new("AllBusinesses-cu7", "Alle Businesses: Aufwertung 7 (x7)", null, 0, 1E+51, 7),
        new("AllBusinesses-cu8", "Alle Businesses: Aufwertung 8 (x5)", null, 0, 1E+54, 5),
        new("AllBusinesses-cu9", "Alle Businesses: Aufwertung 9 (x7)", null, 0, 1E+60, 7),
        new("AllBusinesses-cu10", "Alle Businesses: Aufwertung 10 (x9)", null, 0, 1E+66, 9),
        new("AllBusinesses-cu11", "Alle Businesses: Aufwertung 11 (x11)", null, 0, 1E+72, 11),
        new("AllBusinesses-cu12", "Alle Businesses: Aufwertung 12 (x13)", null, 0, 1E+75, 13),
        new("AllBusinesses-cu13", "Alle Businesses: Aufwertung 13 (x15)", null, 0, 1E+78, 15),
        new("AllBusinesses-cu14", "Alle Businesses: Aufwertung 14 (x3)", null, 0, 1E+84, 3),
        new("AllBusinesses-cu15", "Alle Businesses: Aufwertung 15 (x3.1415926)", null, 0, 3E+87, 3.1415926),
        new("AllBusinesses-cu16", "Alle Businesses: Aufwertung 16 (x3)", null, 0, 5E+95, 3),
        new("AllBusinesses-cu17", "Alle Businesses: Aufwertung 17 (x3)", null, 0, 1E+100, 3),
        new("AllBusinesses-cu18", "Alle Businesses: Aufwertung 18 (x6)", null, 0, 2E+100, 6),
        new("AllBusinesses-cu19", "Alle Businesses: Aufwertung 19 (x3)", null, 0, 5E+101, 3),
        new("AllBusinesses-cu20", "Alle Businesses: Aufwertung 20 (x5)", null, 0, 1E+102, 5),
        new("AllBusinesses-cu21", "Alle Businesses: Aufwertung 21 (x3)", null, 0, 6E+107, 3),
        new("AllBusinesses-cu22", "Alle Businesses: Aufwertung 22 (x3)", null, 0, 1E+111, 3),
        new("AllBusinesses-cu23", "Alle Businesses: Aufwertung 23 (x3)", null, 0, 4.5E+116, 3),
        new("AllBusinesses-cu24", "Alle Businesses: Aufwertung 24 (x5)", null, 0, 3.5E+119, 5),
        new("AllBusinesses-cu25", "Alle Businesses: Aufwertung 25 (x3)", null, 0, 5E+119, 3),
        new("AllBusinesses-cu26", "Alle Businesses: Aufwertung 26 (x6.66)", null, 0, 6.66E+122, 6.66),
        new("AllBusinesses-cu27", "Alle Businesses: Aufwertung 27 (x3)", null, 0, 1E+123, 3),
        new("AllBusinesses-cu28", "Alle Businesses: Aufwertung 28 (x5)", null, 0, 1E+127, 5),
        new("AllBusinesses-cu29", "Alle Businesses: Aufwertung 29 (x3)", null, 0, 9E+131, 3),
        new("AllBusinesses-cu30", "Alle Businesses: Aufwertung 30 (x3)", null, 0, 5E+138, 3),
        new("AllBusinesses-cu31", "Alle Businesses: Aufwertung 31 (x3)", null, 0, 3E+141, 3),
        new("AllBusinesses-cu32", "Alle Businesses: Aufwertung 32 (x3)", null, 0, 2E+144, 3),
        new("AllBusinesses-cu33", "Alle Businesses: Aufwertung 33 (x2.71828)", null, 0, 4.5E+146, 2.71828),
        new("AllBusinesses-cu34", "Alle Businesses: Aufwertung 34 (x4.444444444)", null, 0, 5E+155, 4.444444444),
        new("AllBusinesses-cu35", "Alle Businesses: Aufwertung 35 (x2.99792458)", null, 0, 5.14E+158, 2.99792458),
        new("AllBusinesses-cu36", "Alle Businesses: Aufwertung 36 (x2.35711)", null, 0, 9E+161, 2.35711),
        new("AllBusinesses-cu37", "Alle Businesses: Aufwertung 37 (x3)", null, 0, 2.5E+164, 3),
        new("AllBusinesses-cu38", "Alle Businesses: Aufwertung 38 (x3)", null, 0, 7.5E+164, 3),
        new("AllBusinesses-cu39", "Alle Businesses: Aufwertung 39 (x3)", null, 0, 2.5E+167, 3),
        new("AllBusinesses-cu40", "Alle Businesses: Aufwertung 40 (x3)", null, 0, 7.5E+167, 3),
        new("AllBusinesses-cu41", "Alle Businesses: Aufwertung 41 (x3)", null, 0, 2.5E+170, 3),
        new("AllBusinesses-cu42", "Alle Businesses: Aufwertung 42 (x3)", null, 0, 7.5E+170, 3),
        new("AllBusinesses-cu43", "Alle Businesses: Aufwertung 43 (x3)", null, 0, 2.5E+173, 3),
        new("AllBusinesses-cu44", "Alle Businesses: Aufwertung 44 (x3)", null, 0, 7.5E+173, 3),
        new("AllBusinesses-cu45", "Alle Businesses: Aufwertung 45 (x3)", null, 0, 2.5E+176, 3),
        new("AllBusinesses-cu46", "Alle Businesses: Aufwertung 46 (x1.8)", null, 0, 1E+177, 1.8),
        new("AllBusinesses-cu47", "Alle Businesses: Aufwertung 47 (x9.87654321)", null, 0, 5E+183, 9.87654321),
        new("AllBusinesses-cu48", "Alle Businesses: Aufwertung 48 (x5)", null, 0, 5E+189, 5),
        new("AllBusinesses-cu49", "Alle Businesses: Aufwertung 49 (x3)", null, 0, 2.7E+193, 3),
        new("AllBusinesses-cu50", "Alle Businesses: Aufwertung 50 (x4)", null, 0, 1.3E+196, 4),
        new("AllBusinesses-cu51", "Alle Businesses: Aufwertung 51 (x5)", null, 0, 2E+198, 5),
        new("AllBusinesses-cu52", "Alle Businesses: Aufwertung 52 (x5)", null, 0, 6E+209, 5),
        new("AllBusinesses-cu53", "Alle Businesses: Aufwertung 53 (x5)", null, 0, 5.55E+218, 5),
        new("AllBusinesses-cu54", "Alle Businesses: Aufwertung 54 (x5)", null, 0, 1E+230, 5),
        new("AllBusinesses-cu55", "Alle Businesses: Aufwertung 55 (x3)", null, 0, 1E+233, 3),
        new("AllBusinesses-cu56", "Alle Businesses: Aufwertung 56 (x4)", null, 0, 1E+238, 4),
        new("AllBusinesses-cu57", "Alle Businesses: Aufwertung 57 (x3)", null, 0, 1E+241, 3),
        new("AllBusinesses-cu58", "Alle Businesses: Aufwertung 58 (x6)", null, 0, 1E+244, 6),
        new("AllBusinesses-cu59", "Alle Businesses: Aufwertung 59 (x4)", null, 0, 1E+247, 4),
        new("AllBusinesses-cu60", "Alle Businesses: Aufwertung 60 (x7)", null, 0, 1E+250, 7),
        new("AllBusinesses-cu61", "Alle Businesses: Aufwertung 61 (x5)", null, 0, 1E+252, 5),
        new("AllBusinesses-cu62", "Alle Businesses: Aufwertung 62 (x3)", null, 0, 1E+253, 3),
        new("AllBusinesses-cu63", "Alle Businesses: Aufwertung 63 (x6)", null, 0, 1E+256, 6),
        new("AllBusinesses-cu64", "Alle Businesses: Aufwertung 64 (x3)", null, 0, 2.5E+258, 3),
        new("AllBusinesses-cu65", "Alle Businesses: Aufwertung 65 (x5)", null, 0, 1E+259, 5),
        new("AllBusinesses-cu66", "Alle Businesses: Aufwertung 66 (x5)", null, 0, 1E+262, 5),
        new("AllBusinesses-cu67", "Alle Businesses: Aufwertung 67 (x3)", null, 0, 1E+265, 3),
        new("AllBusinesses-cu68", "Alle Businesses: Aufwertung 68 (x7)", null, 0, 1E+268, 7),
        new("AllBusinesses-cu69", "Alle Businesses: Aufwertung 69 (x5)", null, 0, 1E+270, 5),
        new("AllBusinesses-cu70", "Alle Businesses: Aufwertung 70 (x4)", null, 0, 1E+277, 4),
        new("AllBusinesses-cu71", "Alle Businesses: Aufwertung 71 (x5)", null, 0, 1E+280, 5),
        new("AllBusinesses-cu72", "Alle Businesses: Aufwertung 72 (x7.77)", null, 0, 1E+283, 7.77),
        new("AllBusinesses-cu73", "Alle Businesses: Aufwertung 73 (x7.77)", null, 0, 1E+286, 7.77),
        new("AllBusinesses-cu74", "Alle Businesses: Aufwertung 74 (x77.77)", null, 0, 1E+288, 77.77),
        new("AllBusinesses-cu75", "Alle Businesses: Aufwertung 75 (x3)", null, 0, 1E+14, 3),
        new("AllBusinesses-cu76", "Alle Businesses: Aufwertung 76 (x6)", null, 0, 1E+18, 6),
        new("AllBusinesses-cu77", "Alle Businesses: Aufwertung 77 (x3)", null, 0, 5E+26, 3),
        new("AllBusinesses-cu78", "Alle Businesses: Aufwertung 78 (x3)", null, 0, 4.5E+28, 3),
        new("AllBusinesses-cu79", "Alle Businesses: Aufwertung 79 (x3)", null, 0, 9E+30, 3),
        new("AllBusinesses-cu80", "Alle Businesses: Aufwertung 80 (x4)", null, 0, 5E+32, 4),
        new("AllBusinesses-cu81", "Alle Businesses: Aufwertung 81 (x3)", null, 0, 8.5E+35, 3),
        new("AllBusinesses-cu82", "Alle Businesses: Aufwertung 82 (x7)", null, 0, 1E+37, 7),
        new("AllBusinesses-cu83", "Alle Businesses: Aufwertung 83 (x3)", null, 0, 1E+39, 3),
        new("AllBusinesses-cu84", "Alle Businesses: Aufwertung 84 (x3)", null, 0, 5E+44, 3),
        new("AllBusinesses-cu85", "Alle Businesses: Aufwertung 85 (x3.33)", null, 0, 5E+49, 3.33),
        new("AllBusinesses-cu86", "Alle Businesses: Aufwertung 86 (x5)", null, 0, 6.66E+57, 5),
        new("AllBusinesses-cu87", "Alle Businesses: Aufwertung 87 (x5)", null, 0, 2E+63, 5),
        new("AllBusinesses-cu88", "Alle Businesses: Aufwertung 88 (x3)", null, 0, 3.5E+69, 3),
        new("AllBusinesses-cu89", "Alle Businesses: Aufwertung 89 (x3)", null, 0, 1E+81, 3),
        new("AllBusinesses-cu90", "Alle Businesses: Aufwertung 90 (x3)", null, 0, 9E+85, 3),
        new("AllBusinesses-cu91", "Alle Businesses: Aufwertung 91 (x5)", null, 0, 1E+89, 5),
        new("AllBusinesses-cu92", "Alle Businesses: Aufwertung 92 (x6.2831853)", null, 0, 9E+91, 6.2831853),
        new("AllBusinesses-cu93", "Alle Businesses: Aufwertung 93 (x3)", null, 0, 1E+93, 3),
        new("AllBusinesses-cu94", "Alle Businesses: Aufwertung 94 (x3)", null, 0, 9E+97, 3),
        new("AllBusinesses-cu95", "Alle Businesses: Aufwertung 95 (x3)", null, 0, 1E+104, 3),
        new("AllBusinesses-cu96", "Alle Businesses: Aufwertung 96 (x3)", null, 0, 3E+113, 3),
        new("AllBusinesses-cu97", "Alle Businesses: Aufwertung 97 (x3)", null, 0, 5E+125, 3),
        new("AllBusinesses-cu98", "Alle Businesses: Aufwertung 98 (x6)", null, 0, 3E+129, 6),
        new("AllBusinesses-cu99", "Alle Businesses: Aufwertung 99 (x3)", null, 0, 1E+133, 3),
        new("AllBusinesses-cu100", "Alle Businesses: Aufwertung 100 (x5)", null, 0, 5E+135, 5),
        new("AllBusinesses-cu101", "Alle Businesses: Aufwertung 101 (x3)", null, 0, 5E+137, 3),
        new("AllBusinesses-cu102", "Alle Businesses: Aufwertung 102 (x3)", null, 0, 1E+148, 3),
        new("AllBusinesses-cu103", "Alle Businesses: Aufwertung 103 (x3)", null, 0, 1E+151, 3),
        new("AllBusinesses-cu104", "Alle Businesses: Aufwertung 104 (x3)", null, 0, 3E+153, 3),
        new("AllBusinesses-cu105", "Alle Businesses: Aufwertung 105 (x15)", null, 0, 3E+180, 15),
        new("AllBusinesses-cu106", "Alle Businesses: Aufwertung 106 (x3)", null, 0, 1E+186, 3),
        new("AllBusinesses-cu107", "Alle Businesses: Aufwertung 107 (x7)", null, 0, 5E+191, 7),
        new("AllBusinesses-cu108", "Alle Businesses: Aufwertung 108 (x3)", null, 0, 1E+201, 3),
        new("AllBusinesses-cu109", "Alle Businesses: Aufwertung 109 (x7)", null, 0, 2E+204, 7),
        new("AllBusinesses-cu110", "Alle Businesses: Aufwertung 110 (x3)", null, 0, 2.5E+206, 3),
        new("AllBusinesses-cu111", "Alle Businesses: Aufwertung 111 (x3)", null, 0, 5E+212, 3),
        new("AllBusinesses-cu112", "Alle Businesses: Aufwertung 112 (x7)", null, 0, 1E+214, 7),
        new("AllBusinesses-cu113", "Alle Businesses: Aufwertung 113 (x3)", null, 0, 1E+216, 3),
        new("AllBusinesses-cu114", "Alle Businesses: Aufwertung 114 (x4)", null, 0, 6.66E+221, 4),
        new("AllBusinesses-cu115", "Alle Businesses: Aufwertung 115 (x12.34)", null, 0, 1E+224, 12.34),
        new("AllBusinesses-cu116", "Alle Businesses: Aufwertung 116 (x4)", null, 0, 1E+227, 4),
        new("AllBusinesses-cu117", "Alle Businesses: Aufwertung 117 (x3)", null, 0, 8.5E+235, 3),
        new("AllBusinesses-cu118", "Alle Businesses: Aufwertung 118 (x3)", null, 0, 7.77E+271, 3),
        new("AllBusinesses-cu119", "Alle Businesses: Aufwertung 119 (x3)", null, 0, 5E+273, 3),
        new("AllBusinesses-cu120", "Alle Businesses: Aufwertung 120 (x3)", null, 0, 1E+275, 3),
        new("AngelInvestor-cu1", "Investor-Effektivität: Aufwertung 1 (+0.5%)", null, 0, 1E+13, 1, 0.005),
        new("AngelInvestor-cu2", "Investor-Effektivität: Aufwertung 2 (+0.5%)", null, 0, 1E+17, 1, 0.005),
        new("AngelInvestor-cu3", "Investor-Effektivität: Aufwertung 3 (+1%)", null, 0, 1E+21, 1, 0.01),
        new("AngelInvestor-cu4", "Investor-Effektivität: Aufwertung 4 (+1%)", null, 0, 1E+25, 1, 0.01),
        new("AngelInvestor-cu5", "Investor-Effektivität: Aufwertung 5 (+1%)", null, 0, 1E+30, 1, 0.01),
        new("AngelInvestor-cu6", "Investor-Effektivität: Aufwertung 6 (+1.5%)", null, 0, 1E+40, 1, 0.015),
        new("AngelInvestor-cu7", "Investor-Effektivität: Aufwertung 7 (+1.5%)", null, 0, 1E+50, 1, 0.015),
        new("AngelInvestor-cu8", "Investor-Effektivität: Aufwertung 8 (+2%)", null, 0, 1E+60, 1, 0.02),
        new("AngelInvestor-cu9", "Investor-Effektivität: Aufwertung 9 (+2%)", null, 0, 1E+70, 1, 0.02),
        new("AngelInvestor-cu10", "Investor-Effektivität: Aufwertung 10 (+2%)", null, 0, 1E+80, 1, 0.02),
        new("AngelInvestor-cu11", "Investor-Effektivität: Aufwertung 11 (+2.5%)", null, 0, 1E+95, 1, 0.025),
        new("AngelInvestor-cu12", "Investor-Effektivität: Aufwertung 12 (+2.5%)", null, 0, 1E+110, 1, 0.025),
        new("AngelInvestor-cu13", "Investor-Effektivität: Aufwertung 13 (+3%)", null, 0, 1E+130, 1, 0.03),
        new("AngelInvestor-cu14", "Investor-Effektivität: Aufwertung 14 (+3%)", null, 0, 1E+150, 1, 0.03),
        new("AngelInvestor-cu15", "Investor-Effektivität: Aufwertung 15 (+3%)", null, 0, 1E+170, 1, 0.03),
        new("AngelInvestor-cu16", "Investor-Effektivität: Aufwertung 16 (+4%)", null, 0, 1E+190, 1, 0.04),
        new("AngelInvestor-cu17", "Investor-Effektivität: Aufwertung 17 (+4%)", null, 0, 1E+210, 1, 0.04),
        new("AngelInvestor-cu18", "Investor-Effektivität: Aufwertung 18 (+4%)", null, 0, 1E+230, 1, 0.04),
        new("AngelInvestor-cu19", "Investor-Effektivität: Aufwertung 19 (+5%)", null, 0, 1E+250, 1, 0.05),
        new("AngelInvestor-cu20", "Investor-Effektivität: Aufwertung 20 (+5%)", null, 0, 1E+270, 1, 0.05),
    ];

    public static double AllProfitMultiplier(IEnumerable<string> purchasedUpgradeIds, int prestigeCount)
    {
        var mult = 1.0;
        var purchased = purchasedUpgradeIds.ToHashSet();

        foreach (var upgrade in Upgrades.Where(u => u.BusinessId is null))
        {
            if (purchased.Contains(upgrade.Id))
            {
                mult *= upgrade.ProfitMultiplier;
            }
        }

        foreach (var perk in PrestigeUpgrades)
        {
            if (perk.PrestigeLevel <= prestigeCount && purchased.Contains(perk.Upgrade.Id))
            {
                mult *= perk.Upgrade.ProfitMultiplier;
            }
        }

        return mult;
    }

    public static double BusinessUpgradeMultiplier(string businessId, IEnumerable<string> purchasedUpgradeIds)
    {
        var mult = 1.0;
        var purchased = purchasedUpgradeIds.ToHashSet();

        foreach (var upgrade in Upgrades.Where(u => u.BusinessId == businessId))
        {
            if (purchased.Contains(upgrade.Id))
            {
                mult *= upgrade.ProfitMultiplier;
            }
        }

        return mult;
    }

    public static double CashAngelEffectivenessBonus(IEnumerable<string> purchasedUpgradeIds)
    {
        var bonus = 0.0;
        var purchased = purchasedUpgradeIds.ToHashSet();

        foreach (var upgrade in Upgrades.Where(u => u.AngelEffectivenessBonus != 0))
        {
            if (purchased.Contains(upgrade.Id))
            {
                bonus += upgrade.AngelEffectivenessBonus;
            }
        }

        return bonus;
    }
}
