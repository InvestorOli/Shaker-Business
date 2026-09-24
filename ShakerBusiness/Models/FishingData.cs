namespace ShakerBusiness.Models;

public enum FishRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic,
    Divine,
}

public sealed record FishDefinition(string Id, string Name, FishRarity Rarity, long SellValue, string Color, double Difficulty);

public sealed record BaitDefinition(string Id, string Name, long Price, int RarityBonusLevels, FishRarity MaxRarity, string Color);

public sealed record FishingSpotDefinition(string Id, string Name, double CenterX, double CenterY, double RadiusX, double RadiusY);

public static class FishingData
{
    public static readonly IReadOnlyList<FishDefinition> Fish =
    [
        new("roach", "Kleingeld-Rotfeder", FishRarity.Common, 6, "#9fb0b8", 0.10),
        new("bleak", "Fusel-Ukelei", FishRarity.Common, 8, "#a8a8a8", 0.13),
        new("gudgeon", "Quittungs-Gründling", FishRarity.Common, 8, "#8fa5ad", 0.16),
        new("rudd", "Schwarzgeld-Rotauge", FishRarity.Common, 10, "#b0b0a0", 0.19),
        new("minnow", "Payday-Elritze", FishRarity.Common, 10, "#98a8b0", 0.22),
        new("falschgeldploetze", "Falschgeld-Plötze", FishRarity.Common, 5, "#7f95a0", 0.09),
        new("tarnkappenstichling", "Tarnkappen-Stichling", FishRarity.Common, 6, "#8398a0", 0.10),
        new("importexportgrundel", "Import-Export-Grundel", FishRarity.Common, 6, "#869ba1", 0.109),
        new("nachtschichtkarausche", "Nachtschicht-Karausche", FishRarity.Common, 6, "#8a9ea2", 0.119),
        new("zolldoebel", "Zoll-Döbel", FishRarity.Common, 7, "#8da1a2", 0.129),
        new("hinterzimmernase", "Hinterzimmer-Nase", FishRarity.Common, 8, "#91a4a2", 0.138),
        new("laufburscherapfen", "Laufbursche-Rapfen", FishRarity.Common, 8, "#94a7a3", 0.148),
        new("handlangerbarbe", "Handlanger-Barbe", FishRarity.Common, 8, "#98aaa4", 0.158),
        new("grauzonenguester", "Grauzonen-Güster", FishRarity.Common, 9, "#9caca4", 0.167),
        new("wettbuerozope", "Wettbüro-Zope", FishRarity.Common, 10, "#9fafa4", 0.177),
        new("prohibitionmoderlieschen", "Prohibitions-Moderlieschen", FishRarity.Common, 10, "#a3b2a5", 0.187),
        new("schwarzbrennersteinbeisser", "Schwarzbrenner-Steinbeißer", FishRarity.Common, 10, "#a6b5a6", 0.197),
        new("absackerschmerle", "Absacker-Schmerle", FishRarity.Common, 11, "#aab8a6", 0.206),
        new("trickbetruegergroppe", "Trickbetrüger-Groppe", FishRarity.Common, 12, "#adbba6", 0.216),
        new("huetchenspielerneunauge", "Hütchenspieler-Neunauge", FishRarity.Common, 12, "#b1bea7", 0.226),
        new("kredithaistint", "Kredithai-Stint", FishRarity.Common, 12, "#b4c1a8", 0.235),
        new("bootleggeraland", "Bootlegger-Aland", FishRarity.Common, 13, "#b8c4a8", 0.245),

        new("carp", "Kartellkarpfen", FishRarity.Uncommon, 20, "#b58a4a", 0.26),
        new("bream", "Dealerbrasse", FishRarity.Uncommon, 24, "#a97c3d", 0.30),
        new("tench", "Schmuggler-Schleie", FishRarity.Uncommon, 28, "#c49a5c", 0.34),
        new("perch", "Bestechungsbarsch", FishRarity.Uncommon, 32, "#9c7a4a", 0.38),
        new("eel", "Waschanlagen-Aal", FishRarity.Uncommon, 36, "#b8935a", 0.42),
        new("consigliermaraene", "Consigliere-Maräne", FishRarity.Uncommon, 16, "#c9995a", 0.245),
        new("tuersteherrenke", "Türsteher-Renke", FishRarity.Uncommon, 18, "#c49557", 0.261),
        new("rausschmeisserseesaibling", "Rausschmeißer-Saibling", FishRarity.Uncommon, 20, "#be9154", 0.277),
        new("zinswucherbachforelle", "Zinswucher-Bachforelle", FishRarity.Uncommon, 22, "#b98d51", 0.292),
        new("falschspielermeerforelle", "Falschspieler-Meerforelle", FishRarity.Uncommon, 25, "#b4894e", 0.308),
        new("ganovenlachs", "Ganoven-Lachs", FishRarity.Uncommon, 27, "#af854b", 0.324),
        new("bandebeluga", "Bande-Beluga", FishRarity.Uncommon, 29, "#aa8248", 0.34),
        new("revierpiranha", "Revier-Piranha", FishRarity.Uncommon, 31, "#a47e44", 0.356),
        new("alibirochen", "Alibi-Rochen", FishRarity.Uncommon, 33, "#9f7a41", 0.372),
        new("kronzeugemuraene", "Kronzeuge-Muräne", FishRarity.Uncommon, 36, "#94723b", 0.388),
        new("erpresserzackenbarsch", "Erpresser-Zackenbarsch", FishRarity.Uncommon, 38, "#8f6e38", 0.403),
        new("loesegeldbarrakuda", "Lösegeld-Barrakuda", FishRarity.Uncommon, 40, "#8a6a35", 0.419),
        new("geiselschwertfisch", "Geisel-Schwertfisch", FishRarity.Uncommon, 42, "#856636", 0.435),

        new("pike", "Stihl-Hecht", FishRarity.Rare, 56, "#3f8f4f", 0.46),
        new("zander", "Razzia-Zander", FishRarity.Rare, 64, "#2f7a4a", 0.50),
        new("trout", "Undercover-Forelle", FishRarity.Rare, 72, "#4a9d5c", 0.54),
        new("grayling", "Geldwäscher-Äsche", FishRarity.Rare, 80, "#35875a", 0.58),
        new("catfish", "Schutzgeld-Wels", FishRarity.Rare, 90, "#529c68", 0.62),
        new("auftragskillermarlin", "Auftragskiller-Marlin", FishRarity.Rare, 48, "#2f7a45", 0.435),
        new("kopfgeldtuemmler", "Kopfgeld-Tümmler", FishRarity.Rare, 53, "#35804a", 0.455),
        new("fahndungsnarwal", "Fahndungs-Narwal", FishRarity.Rare, 58, "#3b864f", 0.475),
        new("steckbriefanglerfisch", "Steckbrief-Anglerfisch", FishRarity.Rare, 63, "#418c54", 0.495),
        new("maulwurfdrachenfisch", "Maulwurf-Drachenfisch", FishRarity.Rare, 68, "#479259", 0.515),
        new("informantenriesenkalmar", "Informanten-Riesenkalmar", FishRarity.Rare, 73, "#4d985e", 0.535),
        new("spitzeltiefseeaal", "Spitzel-Tiefseeaal", FishRarity.Rare, 78, "#539d64", 0.555),
        new("doppelagentgeisterfisch", "Doppelagent-Geisterfisch", FishRarity.Rare, 83, "#59a369", 0.575),
        new("piranhakoenig", "Piranha-König", FishRarity.Rare, 88, "#5fa96e", 0.595),
        new("diamantenmuraene", "Diamanten-Muräne", FishRarity.Rare, 93, "#65af73", 0.615),
        new("perlenkettenwels", "Perlenketten-Wels", FishRarity.Rare, 98, "#6bb578", 0.635),

        new("sturgeon", "Angsthasen-Stör", FishRarity.Epic, 140, "#2f4f7a", 0.64),
        new("giantcarp", "Schlaf-Riesenkarpfen", FishRarity.Epic, 170, "#35558a", 0.68),
        new("goldperch", "Investor-Goldbarsch", FishRarity.Epic, 190, "#2a4570", 0.72),
        new("silvereel", "Prinzessin-Silberaal", FishRarity.Epic, 210, "#3d5f95", 0.76),
        new("moonfish", "Horny-Mondfisch", FishRarity.Epic, 240, "#274468", 0.80),
        new("tresorknackerstoer", "Tresorknacker-Stör", FishRarity.Epic, 125, "#254566", 0.635),
        new("geldschrankriesenwels", "Geldschrank-Riesenwels", FishRarity.Epic, 141, "#2a4a6e", 0.657),
        new("bankraubbarrakuda", "Bankraub-Barrakuda", FishRarity.Epic, 158, "#2e5076", 0.68),
        new("geiselnahmehai", "Geiselnahme-Hai", FishRarity.Epic, 174, "#33557e", 0.703),
        new("schutzengelrochen", "Schutzengel-Rochen", FishRarity.Epic, 190, "#385a86", 0.725),
        new("rueckendeckungszackenbarsch", "Rückendeckungs-Zackenbarsch", FishRarity.Epic, 206, "#3c5f8d", 0.747),
        new("faustpfandmarlin", "Faustpfand-Marlin", FishRarity.Epic, 222, "#416495", 0.77),
        new("wucherzinskraken", "Wucherzins-Kraken", FishRarity.Epic, 239, "#456a9d", 0.792),
        new("schuldenbergleviathan", "Schuldenberg-Leviathan", FishRarity.Epic, 255, "#4a6fa5", 0.815),

        new("crystaltrout", "Shaker-Kristallforelle", FishRarity.Legendary, 360, "#d9a441", 0.82),
        new("shadowpike", "Kartellboss-Schattenhecht", FishRarity.Legendary, 440, "#e8b552", 0.85),
        new("phantomcatfish", "Bunker-Phantomwels", FishRarity.Legendary, 520, "#c99333", 0.88),
        new("koi", "Goldener Bestechungskoi", FishRarity.Legendary, 600, "#f0c060", 0.91),
        new("nessie", "Steuerhinterzieher-Nessie", FishRarity.Legendary, 700, "#1f6b52", 0.94),
        new("offshorenessie", "Offshore-Nessie", FishRarity.Legendary, 355, "#c98a2a", 0.815),
        new("briefkastenfirmenhai", "Briefkastenfirmen-Hai", FishRarity.Legendary, 415, "#d09635", 0.835),
        new("strohmannwal", "Strohmann-Wal", FishRarity.Legendary, 475, "#d7a13f", 0.855),
        new("deckadressendrache", "Deckadressen-Drache", FishRarity.Legendary, 535, "#deac4a", 0.875),
        new("consiglieleviathan", "Consigliere-Leviathan", FishRarity.Legendary, 595, "#e4b855", 0.895),
        new("patepiranha", "Pate-Piranha", FishRarity.Legendary, 655, "#ebc45f", 0.915),
        new("schattenkartellaal", "Schattenkartell-Aal", FishRarity.Legendary, 715, "#f2cf6a", 0.935),

        new("mythicshark", "Shakerhai", FishRarity.Mythic, 900, "#4a3f8f", 0.95),
        new("mythicgodfather", "Der Pate der Tiefsee", FishRarity.Mythic, 1050, "#3a2f6f", 0.955),
        new("mythickraken", "Kartell-Kraken", FishRarity.Mythic, 1150, "#2f2555", 0.96),
        new("mythicgoldcatfish", "Goldbarren-Wels", FishRarity.Mythic, 1250, "#8f6a2f", 0.965),
        new("mythicwashleviathan", "Waschmaschinen-Leviathan", FishRarity.Mythic, 1400, "#1f1a45", 0.97),
        new("erzrivaledertiefsee", "Erzrivale der Tiefsee", FishRarity.Mythic, 900, "#241c4a", 0.935),
        new("bosswechselkraken", "Bosswechsel-Kraken", FishRarity.Mythic, 1032, "#32285f", 0.944),
        new("unsichtbarerbuchhalter", "Unsichtbarer Buchhalter", FishRarity.Mythic, 1165, "#3f3374", 0.954),
        new("derrichter", "Der Richter", FishRarity.Mythic, 1298, "#4c3e8a", 0.963),
        new("dieloyalefaust", "Die Loyale Faust", FishRarity.Mythic, 1430, "#5a4a9f", 0.972),

        new("divineceo", "Der CEO persönlich", FishRarity.Divine, 2000, "#c9a635", 0.975),
        new("divinegoldenshaker", "Der Goldene Shaker", FishRarity.Divine, 2600, "#f0c060", 0.98),
        new("divinetaxwhale", "Steuerparadies-Wal", FishRarity.Divine, 3200, "#0f3d5c", 0.985),
        new("derunsichtbareinvestor", "Der Unsichtbare Investor", FishRarity.Divine, 1950, "#0b2e45", 0.972),
        new("dieletzteinstanz", "Die Letzte Instanz", FishRarity.Divine, 2275, "#1a2f5c", 0.976),
        new("steuerfreielegende", "Steuerfreie Legende", FishRarity.Divine, 2600, "#3d2f6b", 0.98),
        new("derewigeshareholder", "Der Ewige Shareholder", FishRarity.Divine, 2925, "#8a6a1f", 0.984),
        new("shakerbusinesspersoenlich", "Shaker Business Persönlich", FishRarity.Divine, 3250, "#e0b84a", 0.988),
    ];

    public static readonly IReadOnlyList<BaitDefinition> Baits =
    [
        new("worm", "Regenwurm", 58, 74, FishRarity.Uncommon, "#b58a4a"),
        new("maggot", "Made", 120, 68, FishRarity.Rare, "#3f8f4f"),
        new("wobbler", "Wobbler", 240, 72, FishRarity.Epic, "#2f6fa5"),
        new("lure", "Kunstköder", 510, 79, FishRarity.Legendary, "#d9a441"),
        new("scentbait", "Lockstoff", 850, 84, FishRarity.Mythic, "#6a4c93"),
        new("goldbait", "Goldköder", 1050, 99, FishRarity.Divine, "#f4c542"),
    ];

    public const FishRarity NoBaitMaxRarity = FishRarity.Uncommon;

    public static readonly IReadOnlyList<FishingSpotDefinition> FishingSpots =
    [
        new("central", "Zentralteich", 0.5, 0.5, 0.09, 0.08),
        new("north", "Nordsee", 0.5, 0.12, 0.07, 0.06),
        new("east", "Ostbucht", 0.88, 0.5, 0.07, 0.06),
        new("west", "Westbucht", 0.12, 0.5, 0.07, 0.06),
        new("south", "Südsumpf", 0.5, 0.88, 0.07, 0.06),
    ];

    public const double CameraViewSize = 0.30;
    public const double PlayerVisibilityRadius = 0.24;
    public static readonly TimeSpan HotspotRotationInterval = TimeSpan.FromMinutes(10);
    public const int MaxActiveHotspots = 5;
    public const double ServerMoveSpeedLimit = 0.12;
    public const double MoveToleranceFactor = 4.0;
    public const double MaxCastDistance = 0.22;

    public const double OliShackX = 0.78;
    public const double OliShackY = 0.22;
    public const double OliShackInteractionRadius = 0.12;

    public const long StartingPearls = 100;

    public const int QuestCount = 5;
    public static readonly TimeSpan QuestRefreshInterval = TimeSpan.FromMinutes(30);
    public const int QuestBatchBaitReward = 10;
    public const long QuestBatchXpBonus = 5000;

    private static readonly IReadOnlyDictionary<FishRarity, long> QuestXpByRarity = new Dictionary<FishRarity, long>
    {
        [FishRarity.Common] = 400,
        [FishRarity.Uncommon] = 800,
        [FishRarity.Rare] = 1500,
        [FishRarity.Epic] = 2800,
        [FishRarity.Legendary] = 4500,
        [FishRarity.Mythic] = 7000,
        [FishRarity.Divine] = 10000,
    };

    public static long QuestXpForRarity(FishRarity rarity) => QuestXpByRarity.GetValueOrDefault(rarity);

    public const int MinRodLevel = 1;
    public const int MaxRodLevel = 100;
    public const double RodUpgradeCostBase = 25;
    public const double RodUpgradeCostExponent = 1.9;

    public const int MaxFishLevel = 100;
    public const int FishLevelCapPerRodLevel = 3;
    public const double FishLevelValueMultiplier = 6.0;
    public const double XpLevelValueMultiplier = 3.0;

    public const double QteDurationMs = 7000;
    public const double QteZoneHeight = 0.42;
    public const double QteFillRate = 0.62;
    public const double QteMaxSpeed = 1.1;
    public const double QteRiseAccel = 5.2;
    public const double QteFallAccel = 3.4;
    public const double QteInitialRetargetMs = 700;
    public const double QteRetargetMinMs = 550;
    public const double QteRetargetRangeMs = 650;
    public const double QteReplayStepMs = 8;
    public const int QteMaxEvents = 500;
    public const double QteRealtimeToleranceMs = 400;

    private static readonly IReadOnlyDictionary<FishRarity, long> XpBaseByRarity = new Dictionary<FishRarity, long>
    {
        [FishRarity.Common] = 80,
        [FishRarity.Uncommon] = 200,
        [FishRarity.Rare] = 500,
        [FishRarity.Epic] = 1200,
        [FishRarity.Legendary] = 3000,
        [FishRarity.Mythic] = 7000,
        [FishRarity.Divine] = 16000,
    };

    private static readonly IReadOnlyDictionary<FishRarity, double> RodLevel1Weights = new Dictionary<FishRarity, double>
    {
        [FishRarity.Common] = 0.70,
        [FishRarity.Uncommon] = 0.25,
        [FishRarity.Rare] = 0.05,
        [FishRarity.Epic] = 0.0,
        [FishRarity.Legendary] = 0.0,
        [FishRarity.Mythic] = 0.0,
        [FishRarity.Divine] = 0.0,
    };

    private static readonly IReadOnlyDictionary<FishRarity, double> RodMaxLevelWeights = new Dictionary<FishRarity, double>
    {
        [FishRarity.Common] = 0.05,
        [FishRarity.Uncommon] = 0.15,
        [FishRarity.Rare] = 0.26,
        [FishRarity.Epic] = 0.23,
        [FishRarity.Legendary] = 0.18,
        [FishRarity.Mythic] = 0.10,
        [FishRarity.Divine] = 0.03,
    };

    private static readonly IReadOnlyDictionary<string, FishDefinition> FishById = Fish.ToDictionary(f => f.Id);

    private static readonly IReadOnlyDictionary<string, BaitDefinition> BaitById = Baits.ToDictionary(b => b.Id);

    public static FishDefinition? Find(string fishId) => FishById.GetValueOrDefault(fishId);

    public static BaitDefinition? FindBait(string? baitId) => baitId is null ? null : BaitById.GetValueOrDefault(baitId);

    public static long RodUpgradeCost(int targetLevel) => (long)Math.Round(RodUpgradeCostBase * Math.Pow(targetLevel, RodUpgradeCostExponent));

    public static IReadOnlyDictionary<FishRarity, double> RarityWeightsForLevel(int level)
    {
        var clampedLevel = Math.Clamp(level, MinRodLevel, MaxRodLevel);
        var linearT = (clampedLevel - MinRodLevel) / (double)(MaxRodLevel - MinRodLevel);
        var t = Math.Pow(linearT, 0.85);

        var weights = new Dictionary<FishRarity, double>();
        foreach (var rarity in Enum.GetValues<FishRarity>())
        {
            var from = RodLevel1Weights.GetValueOrDefault(rarity);
            var to = RodMaxLevelWeights.GetValueOrDefault(rarity);
            weights[rarity] = from + (to - from) * t;
        }

        return weights;
    }

    private static IReadOnlyDictionary<FishRarity, double> CappedRarityWeights(int rodLevel, int rarityBonusLevels, FishRarity maxRarity)
    {
        var weights = RarityWeightsForLevel(rodLevel + rarityBonusLevels);

        var cappedWeights = new Dictionary<FishRarity, double>();
        var total = 0.0;
        foreach (var rarity in Enum.GetValues<FishRarity>())
        {
            if (rarity > maxRarity)
            {
                continue;
            }

            var w = weights.GetValueOrDefault(rarity);
            cappedWeights[rarity] = w;
            total += w;
        }

        if (total <= 0)
        {
            return new Dictionary<FishRarity, double> { [FishRarity.Common] = 1.0 };
        }

        return cappedWeights.ToDictionary(kv => kv.Key, kv => kv.Value / total);
    }

    public static FishDefinition RollFish(int rodLevel, int rarityBonusLevels, Random random, FishRarity maxRarity, string? hotspotFishId = null)
    {
        var weights = CappedRarityWeights(rodLevel, rarityBonusLevels, maxRarity);
        var hotspotFish = hotspotFishId is null ? null : Find(hotspotFishId);

        var roll = random.NextDouble();
        var cumulative = 0.0;
        foreach (var rarity in Enum.GetValues<FishRarity>())
        {
            if (!weights.TryGetValue(rarity, out var w))
            {
                continue;
            }

            cumulative += w;
            if (roll <= cumulative)
            {
                if (hotspotFish is not null && hotspotFish.Rarity == rarity)
                {
                    return hotspotFish;
                }

                var candidates = Fish.Where(f => f.Rarity == rarity).ToList();
                if (candidates.Count > 0)
                {
                    return candidates[random.Next(candidates.Count)];
                }
            }
        }

        return Fish.First(f => f.Rarity <= maxRarity);
    }

    public sealed record RarityBreakdownEntry(FishRarity Rarity, double Percentage, IReadOnlyList<FishDefinition> Fish);

    public static IReadOnlyList<RarityBreakdownEntry> GetRarityBreakdown(int rodLevel, int rarityBonusLevels, FishRarity maxRarity)
    {
        var weights = CappedRarityWeights(rodLevel, rarityBonusLevels, maxRarity);
        return Enum.GetValues<FishRarity>()
            .Where(weights.ContainsKey)
            .Select(r => new RarityBreakdownEntry(r, weights[r] * 100.0, Fish.Where(f => f.Rarity == r).ToList()))
            .ToList();
    }

    public static int RollFishLevel(int rodLevel, Random random)
    {
        var cap = Math.Clamp(rodLevel * FishLevelCapPerRodLevel, 1, MaxFishLevel);
        return 1 + random.Next(cap);
    }

    public static long FishSellValue(FishDefinition fish, int level, int rodLevel)
    {
        var cap = Math.Clamp(rodLevel * FishLevelCapPerRodLevel, 1, MaxFishLevel);
        var clampedLevel = Math.Clamp(level, 1, MaxFishLevel);
        var fraction = cap <= 1 ? 1.0 : Math.Clamp((clampedLevel - 1) / (double)(cap - 1), 0.0, 1.0);
        var multiplier = 1.0 + fraction * FishLevelValueMultiplier;
        return (long)Math.Round(fish.SellValue * multiplier);
    }

    public static long CatchXp(FishDefinition fish, int level)
    {
        var clampedLevel = Math.Clamp(level, 1, MaxFishLevel);
        var multiplier = 1.0 + (clampedLevel - 1) / (double)(MaxFishLevel - 1) * XpLevelValueMultiplier;
        return (long)Math.Round(XpBaseByRarity.GetValueOrDefault(fish.Rarity) * multiplier);
    }

    public static long CumulativeXpForLevel(int level)
    {
        if (level <= 0)
        {
            return 0;
        }

        var completeGroups = level / 10;
        var remainder = level % 10;
        var completeGroupsXp = (long)completeGroups * (completeGroups + 1) * 500;
        var remainderXp = (long)remainder * (completeGroups + 1) * 100;
        return completeGroupsXp + remainderXp;
    }

    public static int LevelForXp(long totalXp)
    {
        if (totalXp <= 0)
        {
            return 0;
        }

        var level = 0;
        while (level < 100000 && CumulativeXpForLevel(level + 1) <= totalXp)
        {
            level++;
        }

        return level;
    }

    private static double Mulberry32Next(ref int state)
    {
        unchecked
        {
            state += unchecked((int)0x6D2B79F5);
            var t = state;
            t = (t ^ (t >>> 15)) * (1 | t);
            t = (t + ((t ^ (t >>> 7)) * (61 | t))) ^ t;
            var raw = t ^ (t >>> 14);
            return (uint)raw / 4294967296.0;
        }
    }

    public static double SimulateQte(double difficulty, int seed, IReadOnlyList<(double TMs, bool Holding)> events, double claimedElapsedMs, double realElapsedMs)
    {
        var totalMs = Math.Clamp(Math.Min(claimedElapsedMs, realElapsedMs + QteRealtimeToleranceMs), 0, QteDurationMs);

        var sortedEvents = events
            .Where(e => e.TMs >= 0 && e.TMs <= QteDurationMs)
            .OrderBy(e => e.TMs)
            .Take(QteMaxEvents)
            .ToList();

        bool HoldingAt(double tMs)
        {
            var holding = false;
            foreach (var e in sortedEvents)
            {
                if (e.TMs > tMs)
                {
                    break;
                }

                holding = e.Holding;
            }

            return holding;
        }

        var rngState = seed;
        var fishSpeed = 0.55 + difficulty * 0.65;
        var zoneHeight = QteZoneHeight;
        var drainRate = 0.26 + difficulty * 0.16;
        var maxFishTarget = Math.Max(0, 1 - zoneHeight / 2 - 0.06);

        var fishPos = 0.5;
        var fishTarget = Mulberry32Next(ref rngState) * maxFishTarget;
        var fishRetarget = QteInitialRetargetMs;
        var barPos = 0.5;
        var barVelocity = 0.0;
        var progress = 0.4;

        var t = 0.0;
        while (t < totalMs)
        {
            var dtMs = Math.Min(QteReplayStepMs, totalMs - t);
            t += dtMs;
            var dtSec = dtMs / 1000.0;

            if (t >= fishRetarget)
            {
                fishTarget = Mulberry32Next(ref rngState) * maxFishTarget;
                fishRetarget = t + QteRetargetMinMs + Mulberry32Next(ref rngState) * QteRetargetRangeMs;
            }

            var fishLerp = 1 - Math.Pow(0.001, dtSec * fishSpeed);
            fishPos += (fishTarget - fishPos) * fishLerp;

            var holding = HoldingAt(t);
            barVelocity = holding
                ? Math.Min(QteMaxSpeed, barVelocity + QteRiseAccel * dtSec)
                : Math.Max(-QteMaxSpeed, barVelocity - QteFallAccel * dtSec);
            barPos += barVelocity * dtSec;
            if (barPos < 0)
            {
                barPos = 0;
                barVelocity = 0;
            }
            else if (barPos > 1)
            {
                barPos = 1;
                barVelocity = 0;
            }

            var overlap = Math.Abs(fishPos - (1 - barPos)) < zoneHeight / 2;
            progress += (overlap ? QteFillRate : -drainRate) * dtSec;
            progress = Math.Clamp(progress, 0, 1);

            if (progress >= 1 || progress <= 0)
            {
                break;
            }
        }

        return progress;
    }
}
