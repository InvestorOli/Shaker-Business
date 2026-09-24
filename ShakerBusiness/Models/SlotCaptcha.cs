namespace ShakerBusiness.Models;

public sealed record SlotCaptchaTile(string ImagePath, bool IsPlayer, int RotationDegrees, double Scale);

public sealed record SlotCaptchaChallenge(IReadOnlyList<SlotCaptchaTile> Tiles)
{
    public int PlayerTileCount => Tiles.Count(t => t.IsPlayer);
}

public static class SlotCaptcha
{
    public const int GridSize = 3;
    public const int TileCount = GridSize * GridSize;
    public const int MinPlayerTiles = 2;
    public const int MaxPlayerTiles = 4;
    private const int MaxRotationDegrees = 8;
    private const double MaxExtraScale = 0.25;

    public static readonly IReadOnlyList<string> DecoyImages =
    [
        "images/businesses/Oli.png",
        "images/businesses/ShakerAWP.png",
        "images/businesses/ShakerAngsthase.png",
        "images/businesses/ShakerBig.png",
        "images/businesses/ShakerDealer.png",
        "images/businesses/ShakerFinalBoss.png",
        "images/businesses/ShakerGewinner.png",
        "images/businesses/ShakerHead.png",
        "images/businesses/ShakerHorny.png",
        "images/businesses/ShakerMexico.png",
        "images/businesses/ShakerMogged.png",
        "images/businesses/ShakerPrinzessin.png",
        "images/businesses/ShakerPsycho.png",
        "images/businesses/ShakerRaucher.png",
        "images/businesses/ShakerRot.png",
        "images/businesses/ShakerScared.png",
        "images/businesses/ShakerSchlaf.png",
        "images/businesses/ShakerStihl.png",
        "images/businesses/ShakerStrong.png",
        "images/businesses/ShakerUnfall.png",
        "images/businesses/ShakerWeihnachtsmann.png",
    ];

    public static SlotCaptchaChallenge Create(string playerImagePath, Random random)
    {
        var playerTileCount = random.Next(MinPlayerTiles, MaxPlayerTiles + 1);
        var playerPositions = Enumerable.Range(0, TileCount).OrderBy(_ => random.Next()).Take(playerTileCount).ToHashSet();
        var decoys = DecoyImages.Where(image => image != playerImagePath).OrderBy(_ => random.Next()).ToList();

        var tiles = new List<SlotCaptchaTile>(TileCount);
        var nextDecoy = 0;
        for (var position = 0; position < TileCount; position++)
        {
            var isPlayer = playerPositions.Contains(position);
            tiles.Add(new SlotCaptchaTile(
                isPlayer ? playerImagePath : decoys[nextDecoy++],
                isPlayer,
                random.Next(-MaxRotationDegrees, MaxRotationDegrees + 1),
                1 + (random.NextDouble() * MaxExtraScale)));
        }

        return new SlotCaptchaChallenge(tiles);
    }

    public static bool IsSolved(SlotCaptchaChallenge challenge, IReadOnlySet<int> selectedPositions)
    {
        for (var position = 0; position < challenge.Tiles.Count; position++)
        {
            if (challenge.Tiles[position].IsPlayer != selectedPositions.Contains(position))
            {
                return false;
            }
        }

        return selectedPositions.All(position => position >= 0 && position < challenge.Tiles.Count);
    }
}
