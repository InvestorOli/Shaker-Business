using System.Globalization;
using System.Numerics;
using ShakerBusiness.Data;
using ShakerBusiness.Models;

namespace ShakerBusiness.Services;

public sealed record CertificateData(
    string PlayerName,
    int PrestigeCount,
    BigInteger LifetimeEarnings,
    BigInteger AngelCount,
    double ProgressPercent,
    int SnakeHighScore,
    int TimberHighScore,
    int FishingLevel,
    DateTime IssuedUtc);

public static class CertificateService
{
    private const double PageWidth = 841.89;
    private const double PageHeight = 595.28;
    private const double MessageWidth = 620;
    private const double NameMaxWidth = 640;
    private const int MaxNameLength = 40;
    private const double MinFontSize = 7;
    private const int MediumPrestigeThreshold = 5;
    private const double BadgeRadius = 23;
    private const double StarBottomFactor = 0.81;
    private const int HighPrestigeThreshold = 20;

    private static readonly PdfColor Cream = PdfColor.FromHex(0xF1E6D2);
    private static readonly PdfColor Espresso = PdfColor.FromHex(0x2E2119);
    private static readonly PdfColor Gold = PdfColor.FromHex(0xD9A441);
    private static readonly PdfColor GoldLight = PdfColor.FromHex(0xF0CF7A);
    private static readonly PdfColor Crimson = PdfColor.FromHex(0x9C3232);
    private static readonly PdfColor Ink = PdfColor.FromHex(0x1C1410);
    private static readonly PdfColor Muted = PdfColor.FromHex(0x6B5B4E);

    public static bool HasPlayed(BigInteger lifetimeEarnings, BigInteger angelCount, int prestigeCount) =>
        lifetimeEarnings.Sign > 0 || angelCount.Sign > 0 || prestigeCount > 0;

    public static bool HasPlayed(PlayerAccount account) =>
        HasPlayed(account.LifetimeEarnings, account.AngelCount, account.PrestigeCount);

    public static CertificateData FromAccount(PlayerAccount account, DateTime issuedUtc) => new(
        account.DisplayName,
        account.PrestigeCount,
        account.LifetimeEarnings,
        account.AngelCount,
        GameData.UnlockProgressPercent(account.Businesses.ToDictionary(b => b.BusinessId, b => b.Owned)),
        account.SnakeHighScore,
        account.TimberHighScore,
        FishingData.LevelForXp(account.FishingXp),
        issuedUtc);

    public static string RankTitle(int prestigeCount) => prestigeCount switch
    {
        <= 0 => "Kleiner Fisch im Kartell",
        <= 2 => "Handlanger",
        <= 4 => "Rechte Hand von Oli",
        _ => "Kartellboss",
    };

    public static string OliMessage(CertificateData data)
    {
        var name = Shorten(data.PlayerName);
        var prestige = data.PrestigeCount > 0
            ? $"Du hast {data.PrestigeCount} Mal alles hingeschmissen und wieder aufgebaut, und jedes Mal ein bisschen frecher als davor. "
            : "Du hast dich durch jede Stufe gearbeitet, ohne dich je aufs Prestige zu verlassen. ";
        return $"Amigo {name}, wenn du das hier liest, sitze ich schon im Knast. Das Finanzamt hat mich am Ende doch erwischt - " +
               "aber ehrlich, ich bereue nichts. Du hast mein Kartell mit aufgebaut, Business für Business, und keinen einzigen Cent davon hat die Steuer je gesehen. " +
               prestige +
               "Danke, dass du dabei warst. Und wenn dich einer fragt: Du kennst mich nicht. Capisce?";
    }

    public static string FileName(string playerName)
    {
        var safe = new string(playerName.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_').ToArray());
        return $"Oli-Urkunde-{(safe.Length == 0 ? "Amigo" : safe)}.pdf";
    }

    public static byte[] Build(CertificateData data) =>
        SimplePdfWriter.Write(BuildPage(data), $"Urkunde für {Shorten(data.PlayerName)}", "Investor Oli");

    public static string BuildPreviewSvg(CertificateData data) => BuildPage(data).ToSvg();

    private static SimplePdfPage BuildPage(CertificateData data)
    {
        var page = new SimplePdfPage(PageWidth, PageHeight);
        page.FillRect(0, 0, PageWidth, PageHeight, Cream);
        page.StrokeRect(18, 18, PageWidth - 36, PageHeight - 36, Espresso, 3);
        page.StrokeRect(26, 26, PageWidth - 52, PageHeight - 52, Gold, 1.5);
        DrawCornerStars(page);

        DrawPrestigeMedallion(page, data.PrestigeCount);
        page.CenteredText(404, "URKUNDE", PdfFont.Bold, 46, Espresso);
        page.CenteredText(382, "Shaker Business  -  Das Vermächtnis von Investor Oli", PdfFont.Italic, 13, Muted);

        page.CenteredText(354, "Hiermit wird feierlich bestätigt, dass", PdfFont.Regular, 13, Ink);
        var name = Shorten(data.PlayerName);
        var nameSize = FitFontSize(name, PdfFont.Bold, 34, NameMaxWidth);
        page.CenteredText(316, name, PdfFont.Bold, nameSize, Crimson);
        var nameWidth = SimplePdfPage.MeasureText(name, PdfFont.Bold, nameSize);
        page.Line((PageWidth - nameWidth) / 2 - 12, 310, (PageWidth + nameWidth) / 2 + 12, 310, Gold, 1.2);
        page.CenteredText(288, $"als \"{RankTitle(data.PrestigeCount)}\" mit Prestige {data.PrestigeCount} in Olis Imperium gedient hat.", PdfFont.Regular, 13, Ink);

        DrawStats(page, data);

        var message = SimplePdfPage.WrapText(OliMessage(data), PdfFont.Italic, 12, MessageWidth);
        page.CenteredParagraph(186, message, PdfFont.Italic, 12, 16, Ink);

        var signatureY = 186 - (message.Count * 16) - 14;
        page.Line(PageWidth / 2 - 110, signatureY, PageWidth / 2 + 110, signatureY, Espresso, 0.8);
        page.CenteredText(signatureY - 15, "Investor Oli", PdfFont.Bold, 13, Espresso);
        page.CenteredText(signatureY - 28, "(aktuell in Untersuchungshaft)", PdfFont.Italic, 9, Muted);

        page.CenteredText(44, $"Ausgestellt am {data.IssuedUtc.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}  -  Jeder der Casino spielt ist ein Opfer", PdfFont.Regular, 9, Muted);

        return page;
    }

    private static void DrawStats(SimplePdfPage page, CertificateData data)
    {
        var entries = new (string Value, string Label, double Weight)[]
        {
            (FormatMoney(data.LifetimeEarnings), "Lifetime-Umsatz", 1.7),
            (BigNumberFormatter.FormatPlain(data.AngelCount), "Investor-Anteile", 1.4),
            ($"{data.ProgressPercent.ToString("0.#", CultureInfo.InvariantCulture)} %", "Durchgespielt", 1.0),
            (data.SnakeHighScore.ToString(CultureInfo.InvariantCulture), "Snake", 1.0),
            (data.TimberHighScore.ToString(CultureInfo.InvariantCulture), "Timberman", 1.0),
            (data.FishingLevel.ToString(CultureInfo.InvariantCulture), "Angel-Level", 0.9),
        };

        const double boxLeft = 70;
        const double boxTop = 254;
        const double boxHeight = 50;
        var boxWidth = PageWidth - (2 * boxLeft);
        page.CenteredText(boxTop + 6, "Stand bei Olis Verhaftung", PdfFont.Italic, 8.5, Muted);
        page.FillRect(boxLeft, boxTop - boxHeight, boxWidth, boxHeight, Espresso);
        page.StrokeRect(boxLeft, boxTop - boxHeight, boxWidth, boxHeight, Gold, 1.2);

        var totalWeight = entries.Sum(e => e.Weight);
        var cellLeft = boxLeft;
        foreach (var (value, label, weight) in entries)
        {
            var cellWidth = boxWidth * weight / totalWeight;
            var centerX = cellLeft + (cellWidth / 2);
            var valueSize = FitFontSize(value, PdfFont.Bold, 14, cellWidth - 10);
            page.Text(centerX - (SimplePdfPage.MeasureText(value, PdfFont.Bold, valueSize) / 2), boxTop - 24, value, PdfFont.Bold, valueSize, GoldLight);
            page.Text(centerX - (SimplePdfPage.MeasureText(label, PdfFont.Regular, 8.5) / 2), boxTop - 40, label, PdfFont.Regular, 8.5, Cream);
            cellLeft += cellWidth;
        }
    }

    private static void DrawPrestigeMedallion(SimplePdfPage page, int prestigeCount)
    {
        const double centerY = 510;
        const double radius = 52;
        var centerX = PageWidth / 2;

        if (prestigeCount >= HighPrestigeThreshold)
        {
            page.Star(centerX, centerY, radius + 16, Espresso, Gold, 1.5);
        }

        if (prestigeCount >= MediumPrestigeThreshold)
        {
            page.Star(centerX, centerY, radius + 8, Crimson, Espresso, 1.5);
        }

        page.Star(centerX, centerY, radius, prestigeCount > 0 ? Gold : Muted, Espresso, 2);

        var badgeCenterY = centerY - (radius * 0.08);
        page.Circle(centerX, badgeCenterY, BadgeRadius, Espresso, Gold, 1.5);
        var number = prestigeCount.ToString(CultureInfo.InvariantCulture);
        var numberSize = FitFontSize(number, PdfFont.Bold, 30, BadgeRadius * 1.6);
        page.CenteredText(badgeCenterY - (numberSize * 0.36), number, PdfFont.Bold, numberSize, GoldLight);
        page.CenteredText(centerY - (radius * StarBottomFactor) - 15, "P R E S T I G E", PdfFont.Bold, 12, prestigeCount > 0 ? Crimson : Muted);
    }

    private static double FitFontSize(string text, PdfFont font, double preferredSize, double maxWidth)
    {
        var size = preferredSize;
        while (size > MinFontSize && SimplePdfPage.MeasureText(text, font, size) > maxWidth)
        {
            size -= 0.5;
        }

        return size;
    }

    private static void DrawCornerStars(SimplePdfPage page)
    {
        foreach (var (x, y) in new[] { (46.0, 46.0), (PageWidth - 46, 46.0), (46.0, PageHeight - 46), (PageWidth - 46, PageHeight - 46) })
        {
            page.Star(x, y, 9, Gold, Espresso, 0.8);
        }
    }

    private static string FormatMoney(BigInteger amount)
    {
        var (value, suffix) = BigNumberFormatter.Split(amount);
        return suffix.Length == 0 ? $"${value}" : $"${value} {suffix}";
    }

    private static string Shorten(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return "Amigo";
        }

        return trimmed.Length <= MaxNameLength ? trimmed : trimmed[..MaxNameLength];
    }
}
