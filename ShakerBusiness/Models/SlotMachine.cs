namespace ShakerBusiness.Models;

public sealed record SlotSymbol(int Id, string Name, string ImagePath, bool IsWild, int Weight, int PayThree, int PayFour, int PayFive)
{
    public int PayFor(int length) => length switch
    {
        3 => PayThree,
        4 => PayFour,
        >= 5 => PayFive,
        _ => 0,
    };
}

public sealed record SlotLineWin(int LineIndex, int SymbolId, int Length, int Multiplier);

public sealed record SlotSpin(int[][] Reels, IReadOnlyList<SlotLineWin> Wins)
{
    public bool IsWin => Wins.Count > 0;

    public int TotalMultiplier => Wins.Sum(w => w.Multiplier);
}

public static class SlotMachine
{
    public const int ReelCount = 5;
    public const int RowCount = 5;
    public const double WinChance = 0.05;
    public const int MinimumRun = 3;

    public static readonly IReadOnlyList<SlotSymbol> Symbols =
    [
        new(0, "Angsthase", "images/businesses/ShakerAngsthase.png", false, 16, 2, 5, 15),
        new(1, "Schlaf", "images/businesses/ShakerSchlaf.png", false, 16, 2, 5, 15),
        new(2, "Rot", "images/businesses/ShakerRot.png", false, 14, 3, 8, 20),
        new(3, "Stihl", "images/businesses/ShakerStihl.png", false, 14, 3, 8, 20),
        new(4, "Strong", "images/businesses/ShakerStrong.png", false, 11, 5, 12, 40),
        new(5, "Dealer", "images/businesses/ShakerDealer.png", false, 11, 5, 12, 40),
        new(6, "Mexico", "images/businesses/ShakerMexico.png", false, 8, 8, 20, 80),
        new(7, "Prinzessin", "images/businesses/ShakerPrinzessin.png", false, 6, 10, 30, 120),
        new(8, "Final Boss", "images/businesses/ShakerFinalBoss.png", false, 4, 20, 60, 250),
        new(9, "Oli", "images/businesses/Oli.png", true, 3, 25, 100, 500),
    ];

    public static readonly int WildId = Symbols.Single(s => s.IsWild).Id;

    public static readonly IReadOnlyList<int[]> PayLines =
    [
        [2, 2, 2, 2, 2],
        [0, 0, 0, 0, 0],
        [4, 4, 4, 4, 4],
        [1, 1, 1, 1, 1],
        [3, 3, 3, 3, 3],
        [0, 1, 2, 3, 4],
        [4, 3, 2, 1, 0],
        [0, 1, 2, 1, 0],
        [4, 3, 2, 3, 4],
        [1, 0, 1, 0, 1],
    ];

    private static readonly int[] RegularSymbolIds = Symbols.Where(s => !s.IsWild).Select(s => s.Id).ToArray();
    private static readonly int TotalWeight = Symbols.Sum(s => s.Weight);
    private const int MaxLoseAttempts = 64;

    public static SlotSpin Spin(Random random)
    {
        var reels = random.NextDouble() < WinChance ? BuildWinningReels(random) : BuildLosingReels(random);
        return new SlotSpin(reels, Evaluate(reels));
    }

    public static int[][] CreateIdleReels(Random random) => BuildLosingReels(random);

    public static IReadOnlyList<SlotLineWin> Evaluate(int[][] reels)
    {
        var wins = new List<SlotLineWin>();
        for (var lineIndex = 0; lineIndex < PayLines.Count; lineIndex++)
        {
            var line = PayLines[lineIndex];
            var win = EvaluateLine(lineIndex, Enumerable.Range(0, ReelCount).Select(reel => reels[reel][line[reel]]).ToArray());
            if (win is not null)
            {
                wins.Add(win);
            }
        }

        return wins;
    }

    public static int RandomSymbolId(Random random)
    {
        var roll = random.Next(TotalWeight);
        foreach (var symbol in Symbols)
        {
            roll -= symbol.Weight;
            if (roll < 0)
            {
                return symbol.Id;
            }
        }

        return Symbols[0].Id;
    }

    private static int RandomRegularSymbolId(Random random)
    {
        int symbolId;
        do
        {
            symbolId = RandomSymbolId(random);
        }
        while (symbolId == WildId);

        return symbolId;
    }

    private static SlotLineWin? EvaluateLine(int lineIndex, int[] cells)
    {
        var leadingWilds = 0;
        while (leadingWilds < cells.Length && cells[leadingWilds] == WildId)
        {
            leadingWilds++;
        }

        var best = leadingWilds >= MinimumRun
            ? new SlotLineWin(lineIndex, WildId, leadingWilds, Symbols[WildId].PayFor(leadingWilds))
            : null;

        if (leadingWilds < cells.Length)
        {
            var baseSymbol = cells[leadingWilds];
            var run = leadingWilds;
            while (run < cells.Length && (cells[run] == baseSymbol || cells[run] == WildId))
            {
                run++;
            }

            var multiplier = run >= MinimumRun ? Symbols[baseSymbol].PayFor(run) : 0;
            if (multiplier > 0 && (best is null || multiplier > best.Multiplier))
            {
                best = new SlotLineWin(lineIndex, baseSymbol, run, multiplier);
            }
        }

        return best;
    }

    private static int[][] BuildLosingReels(Random random)
    {
        for (var attempt = 0; attempt < MaxLoseAttempts; attempt++)
        {
            var reels = BuildRandomReels(random);
            if (Evaluate(reels).Count == 0)
            {
                return reels;
            }
        }

        return BuildFallbackLosingReels();
    }

    private static int[][] BuildWinningReels(Random random)
    {
        var reels = BuildRandomReels(random);
        var line = PayLines[random.Next(PayLines.Count)];
        var symbolId = RandomRegularSymbolId(random);
        var roll = random.NextDouble();
        var length = roll < 0.7 ? 3 : roll < 0.92 ? 4 : 5;
        for (var reel = 0; reel < length; reel++)
        {
            reels[reel][line[reel]] = reel > 0 && reel < length - 1 && random.NextDouble() < 0.1 ? WildId : symbolId;
        }

        return reels;
    }

    private static int[][] BuildRandomReels(Random random)
    {
        var reels = new int[ReelCount][];
        for (var reel = 0; reel < ReelCount; reel++)
        {
            reels[reel] = new int[RowCount];
            for (var row = 0; row < RowCount; row++)
            {
                reels[reel][row] = RandomSymbolId(random);
            }
        }

        return reels;
    }

    private static int[][] BuildFallbackLosingReels()
    {
        var reels = new int[ReelCount][];
        for (var reel = 0; reel < ReelCount; reel++)
        {
            reels[reel] = new int[RowCount];
            for (var row = 0; row < RowCount; row++)
            {
                reels[reel][row] = (reel * 3 + row) % RegularSymbolIds.Length;
            }
        }

        return reels;
    }
}
