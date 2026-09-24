using System.Globalization;
using System.Numerics;

namespace ShakerBusiness.Models;

public enum AngelUpgradeEffectType
{
    AllProfitMultiplier,
    BusinessProfitMultiplier,
    BusinessInstantCount,
    AngelEffectivenessBonus,
}

public sealed record AngelUpgradeDefinition(
    string Id,
    string Name,
    string Description,
    BigInteger Cost,
    AngelUpgradeEffectType Type,
    string? BusinessId,
    double Value,
    BigInteger RecommendedAt);

public static class AngelUpgradeData
{
    private const string Lemonade = "ShakerAngsthase";
    private const string Newspaper = "ShakerStihl";
    private const string CarWash = "ShakerSchlaf";
    private const string Pizza = "ShakerRot";
    private const string Donut = "ShakerStrong";
    private const string Shrimp = "ShakerDealer";
    private const string Hockey = "ShakerMexico";
    private const string Movie = "ShakerHorny";
    private const string Bank = "ShakerPrinzessin";
    private const string Oil = "ShakerFinalBoss";

    private static string BizName(string id) => GameData.GetBusiness(id).Name;

    private static BigInteger Illion(long lead, int n) => new BigInteger(lead) * BigInteger.Pow(10, (3 * n) + 3);

    private static string FormatMult(double m) => m == Math.Floor(m)
        ? m.ToString("0", CultureInfo.InvariantCulture)
        : m.ToString("0.######", CultureInfo.InvariantCulture);

    private static AngelUpgradeDefinition All(string id, string name, BigInteger cost, double multiplier, BigInteger recommendedAt) =>
        new(id, name, $"Alle Profite x{FormatMult(multiplier)}", cost, AngelUpgradeEffectType.AllProfitMultiplier, null, multiplier, recommendedAt);

    private static AngelUpgradeDefinition Biz(string id, string name, string businessId, BigInteger cost, double multiplier, BigInteger recommendedAt) =>
        new(id, name, $"{BizName(businessId)} Profit x{FormatMult(multiplier)}", cost, AngelUpgradeEffectType.BusinessProfitMultiplier, businessId, multiplier, recommendedAt);

    private static AngelUpgradeDefinition Count(string id, string name, string businessId, BigInteger cost, int amount, BigInteger recommendedAt) =>
        new(id, name, $"+{amount} {BizName(businessId)}", cost, AngelUpgradeEffectType.BusinessInstantCount, businessId, amount, recommendedAt);

    private static AngelUpgradeDefinition Effectiveness(string id, string name, BigInteger cost, double percent, BigInteger recommendedAt) =>
        new(id, name, $"Investor-Effektivität +{Math.Round(percent * 100)}%", cost, AngelUpgradeEffectType.AngelEffectivenessBonus, null, percent, recommendedAt);

    public static readonly IReadOnlyList<AngelUpgradeDefinition> Upgrades =
    [
        All("angel-sacrifice", "Investor Sacrifice", Illion(10, 0), 3, BigInteger.Parse("40000")),
        Effectiveness("angelic-mutiny", "Investor Mutiny", Illion(100, 0), 0.02, BigInteger.Parse("800000")),
        Effectiveness("angelic-rebellion", "Investor Rebellion", Illion(100, 1), 0.02, BigInteger.Parse("1029000000")),
        All("angelic-selection", "Investor Selection", Illion(1, 2), 5, BigInteger.Parse("2500000000")),
        Count("newspaper-swap", $"{BizName(Newspaper)} Swap", Newspaper, Illion(25, 1), 10, BigInteger.Parse("2500000000")),
        Count("car-wash-swap", $"{BizName(CarWash)} Swap", CarWash, Illion(25, 1), 10, BigInteger.Parse("2500000000")),
        Count("pizza-swap", $"{BizName(Pizza)} Swap", Pizza, Illion(25, 1), 10, BigInteger.Parse("2500000000")),
        Count("donut-swap", $"{BizName(Donut)} Swap", Donut, Illion(25, 1), 10, BigInteger.Parse("2500000000")),
        All("divine-intervention", "Kartell-Dekret", Illion(100, 2), 9, BigInteger.Parse("200000000000")),
        Count("newspaper-surge", $"{BizName(Newspaper)} Surge", Newspaper, Illion(250, 1), 50, BigInteger.Parse("4236000000")),
        Count("car-wash-surge", $"{BizName(CarWash)} Surge", CarWash, Illion(250, 1), 50, BigInteger.Parse("4236000000")),
        Count("pizza-surge", $"{BizName(Pizza)} Surge", Pizza, Illion(250, 1), 50, BigInteger.Parse("4236000000")),
        Count("donut-surge", $"{BizName(Donut)} Surge", Donut, Illion(250, 1), 50, BigInteger.Parse("4236000000")),
        Count("newspaper-merger", $"{BizName(Newspaper)} Merger", Newspaper, Illion(25, 2), 50, BigInteger.Parse("423607000000")),
        Count("car-wash-merger", $"{BizName(CarWash)} Merger", CarWash, Illion(25, 2), 50, BigInteger.Parse("423607000000")),
        Count("pizza-merger", $"{BizName(Pizza)} Merger", Pizza, Illion(25, 2), 50, BigInteger.Parse("423607000000")),
        Count("donut-merger", $"{BizName(Donut)} Merger", Donut, Illion(25, 2), 50, BigInteger.Parse("423607000000")),
        All("rapture-contingent", "Jefes Notplan", Illion(1, 3), 11, BigInteger.Parse("1900000000000")),
        Biz("newspaperion-ultrus", "Kettensäge Deluxe", Newspaper, Illion(250, 3), 3, BigInteger.Parse("25000000000000000")),
        Biz("washicus-maximus", "Schlummer XXL", CarWash, Illion(750, 3), 3, BigInteger.Parse("75000000000000000")),
        Biz("pizzeria-primus", "Rotlicht Extra", Pizza, Illion(2, 4), 3, BigInteger.Parse("200000000000000000")),
        Biz("ultimus-donuticus", "Muckis Turbo", Donut, Illion(5, 4), 3, BigInteger.Parse("500000000000000000")),
        Biz("shrimpus-glorious", "Ware Erste Klasse", Shrimp, Illion(10, 4), 3, BigInteger.Parse("1000000000000000000")),
        Biz("puckus-alotus", "Grenzgänger-Bonus", Hockey, Illion(25, 4), 3, BigInteger.Parse("2500000000000000000")),
        Biz("cinemaxima-spiritus", "Flirt-Faktor Plus", Movie, Illion(75, 4), 3, BigInteger.Parse("7500000000000000000")),
        Biz("fundorium-sanctum", "Kronjuwelen-Fonds", Bank, Illion(200, 4), 3, BigInteger.Parse("20000000000000000000")),
        Biz("oil-vincit-omnia", "Endgegner-Modus", Oil, Illion(400, 4), 3, BigInteger.Parse("40000000000000000000")),
        Biz("lemonus-supremus", "Mutprobe Bestanden", Lemonade, Illion(1, 5), 3, BigInteger.Parse("4100000000000000000")),
        All("buy-earth", "Shakerstadt Kaufen", Illion(1, 6), 15, BigInteger.Parse("1800000000000000000000")),
        Count("newspaper-takeover", $"{BizName(Newspaper)} Takeover", Newspaper, Illion(10, 6), 75, BigInteger.Parse("300000000000000000000000")),
        Count("car-wash-takeover", $"{BizName(CarWash)} Takeover", CarWash, Illion(10, 6), 75, BigInteger.Parse("300000000000000000000000")),
        Count("pizza-takeover", $"{BizName(Pizza)} Takeover", Pizza, Illion(10, 6), 75, BigInteger.Parse("300000000000000000000000")),
        Count("donut-takeover", $"{BizName(Donut)} Takeover", Donut, Illion(10, 6), 75, BigInteger.Parse("300000000000000000000000")),
        Count("shrimp-takeover", $"{BizName(Shrimp)} Takeover", Shrimp, Illion(10, 6), 75, BigInteger.Parse("300000000000000000000000")),
        Count("newspaper-assimilation", $"{BizName(Newspaper)} Assimilation", Newspaper, Illion(100, 6), 75, BigInteger.Parse("3000000000000000000000000")),
        Count("car-wash-assimilation", $"{BizName(CarWash)} Assimilation", CarWash, Illion(100, 6), 75, BigInteger.Parse("3000000000000000000000000")),
        Count("pizza-assimilation", $"{BizName(Pizza)} Assimilation", Pizza, Illion(100, 6), 75, BigInteger.Parse("3000000000000000000000000")),
        Count("donut-assimilation", $"{BizName(Donut)} Assimilation", Donut, Illion(100, 6), 75, BigInteger.Parse("3000000000000000000000000")),
        Count("shrimp-assimilation", $"{BizName(Shrimp)} Assimilation", Shrimp, Illion(100, 6), 75, BigInteger.Parse("3000000000000000000000000")),
        Count("first-amen-dment", "Kettensäge Im Akkord", Newspaper, Illion(10, 9), 100, BigInteger.Parse("61634000000000000000000000000000")),
        Count("unrefusable-offer", "Angebot Zum Nichtablehnen", CarWash, Illion(100, 9), 100, BigInteger.Parse("3861000000000000000000000000000000")),
        Effectiveness("paradise-lost-and-found", "Fundbüro Der Verlorenen", Illion(1, 10), 0.10, BigInteger.Parse("6000000000000000000000000000000000")),
        All("black-friday-the-13th", "Schwarzer Freitag, Der 13.", Illion(10, 10), 15, BigInteger.Parse("18000000000000000000000000000000000")),
        All("divine-write-off", "Kreative Buchführung", Illion(1, 11), 3, BigInteger.Parse("4000000000000000000000000000000000000")),
        All("in-brightest-day", "Am Hellichten Tag", Illion(10, 12), 5, BigInteger.Parse("25000000000000000000000000000000000000000")),
        All("in-darkest-night", "Tief In Der Nacht", Illion(1, 13), 5, BigInteger.Parse("2500000000000000000000000000000000000000000")),
        Count("the-good-news", "Gute Neuigkeiten Vom Jefe", Newspaper, Illion(2, 13), 50, BigInteger.Parse("161764000000000000000000000000000000000000000")),
        Biz("synergize-suds", "Traumhafte Synergie", CarWash, Illion(100, 14), 4, BigInteger.Parse("10000000000000000000000000000000000000000000000000")),
        Biz("feta-beta-testing", "Rezeptur In Der Testphase", Pizza, Illion(200, 14), 6, BigInteger.Parse("20000000000000000000000000000000000000000000000000")),
        Biz("pumpkin-spice", "Saisonale Sonderware", Donut, Illion(700, 14), 3, BigInteger.Parse("70000000000000000000000000000000000000000000000000")),
        Biz("cocktail-parties", "After-Hour-Deals", Shrimp, Illion(2, 15), 3, BigInteger.Parse("200000000000000000000000000000000000000000000000000")),
        Biz("free-jerseys", "Freibier Für Die Crew", Hockey, Illion(25, 15), 3, BigInteger.Parse("2500000000000000000000000000000000000000000000000000")),
        Biz("in-universe-continuity", "Dauerbrenner-Bonus", Movie, Illion(500, 15), 3, BigInteger.Parse("50000000000000000000000000000000000000000000000000000")),
        Biz("dolla-bills-yall", "Dicke Kohle, Amigo", Bank, Illion(20, 16), 3, BigInteger.Parse("2000000000000000000000000000000000000000000000000000000")),
        Biz("anti-solar-research", "Forschung Im Untergrund", Oil, Illion(80, 16), 3, BigInteger.Parse("8000000000000000000000000000000000000000000000000000000")),
        Biz("divine-squeeze", "Mutig Ausgepresst", Lemonade, Illion(150, 16), 3, BigInteger.Parse("15000000000000000000000000000000000000000000000000000000")),
        Biz("perfumed-pages", "Parfümiertes Kleingedrucktes", Newspaper, Illion(300, 16), 3, BigInteger.Parse("30000000000000000000000000000000000000000000000000000000")),
        Effectiveness("hark", "Ruf Des Kartells", Illion(500, 16), 0.10, BigInteger.Parse("3637000000000000000000000000000000000000000000000000000")),
        Biz("fold-into-hats", "Falt-Trick Vom Jefe", Newspaper, Illion(1, 17), 3, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000")),
        Biz("free-breakfast", "Frühstück Inklusive", CarWash, Illion(4, 17), 3, BigInteger.Parse("400000000000000000000000000000000000000000000000000000000")),
        Biz("little-neros", "Kleiner Cäsar Vom Block", Pizza, Illion(9, 17), 3, BigInteger.Parse("900000000000000000000000000000000000000000000000000000000")),
        Biz("donut-isotopes", "Radioaktive Kraftdosis", Donut, Illion(25, 17), 3, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000")),
        Biz("son-of-a-shrimp", "Erbe Der Ware", Shrimp, Illion(75, 17), 3, BigInteger.Parse("7500000000000000000000000000000000000000000000000000000000")),
        Biz("quack", "Quatsch Mit Soße", Hockey, Illion(177, 17), 3, BigInteger.Parse("17700000000000000000000000000000000000000000000000000000000")),
        Biz("rotten-potatoes", "Verfaulte Kritiken", Movie, Illion(300, 17), 3, BigInteger.Parse("30000000000000000000000000000000000000000000000000000000000")),
        Biz("money-bin-life-guards", "Wächter Des Tresors", Bank, Illion(500, 17), 3, BigInteger.Parse("50000000000000000000000000000000000000000000000000000000000")),
        Biz("texas-tea", "Schwarzes Gold", Oil, Illion(800, 17), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000")),
        Biz("lemonster", "Monster Ohne Angst", Lemonade, Illion(1, 18), 3, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000")),
        Count("recycling-boom", "Recycling-Boom", Newspaper, Illion(30, 19), 30, BigInteger.Parse("180000000000000000000000000000000000000000000000000000000000000")),
        Count("redundant-facilities", "Doppelt Gemoppelt", CarWash, Illion(30, 19), 30, BigInteger.Parse("3000000000000000000000000000000000000000000000000000000000000000")),
        Count("no-competitors", "Keine Konkurrenz Weit Und Breit", Pizza, Illion(30, 19), 30, BigInteger.Parse("3000000000000000000000000000000000000000000000000000000000000000")),
        Count("donut-boutiques", "Boutique-Erweiterung", Donut, Illion(30, 19), 30, BigInteger.Parse("3000000000000000000000000000000000000000000000000000000000000000")),
        Count("hokey-honky-hockey", "Grenzlärm", Hockey, Illion(30, 19), 30, BigInteger.Parse("3000000000000000000000000000000000000000000000000000000000000000")),
        All("sacred-trust-fund", "Heiliger Kartellfonds", Illion(100, 19), 5, BigInteger.Parse("250000000000000000000000000000000000000000000000000000000000000")),
        Biz("effective-marketing", "Effektive Mundpropaganda", Newspaper, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        Biz("car-dirty-ers", "Absichtlich Eingeschlafen", CarWash, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        Biz("guaranteed-horse-free", "Garantiert Astrein", Pizza, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        Biz("cartoon-endorsements", "Werbegesicht Des Kartells", Donut, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        Biz("government-subsidy", "Staatliche Förderung", Shrimp, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        Biz("union-busting", "Gewerkschaft Ausgetrickst", Hockey, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        Biz("hire-viewers", "Zuschauer Gekauft", Movie, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        Biz("sue-everything", "Klage Gegen Alle", Bank, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        Biz("accident-free-week", "Unfallfreie Woche", Oil, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        Biz("fund-favorable-studies", "Gekaufte Gutachten", Lemonade, Illion(2, 20), 3, BigInteger.Parse("80000000000000000000000000000000000000000000000000000000000000000")),
        All("hallelujah", "Hallelujah, Amigo!", Illion(100, 20), 7, BigInteger.Parse("210000000000000000000000000000000000000000000000000000000000000000")),
        Biz("heavenly-news", "Himmlische Schlagzeilen", Newspaper, Illion(1, 21), 3, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("discounts-for-investors", "Rabatt Für Investoren", CarWash, Illion(4, 21), 3, BigInteger.Parse("400000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("ascended-pizza", "Aufgestiegene Ware", Pizza, Illion(13, 21), 3, BigInteger.Parse("1300000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("holy-donut-holes", "Heilige Lücken Im Teig", Donut, Illion(20, 21), 3, BigInteger.Parse("200000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("shrimp-saints", "Heilige Der Straße", Shrimp, Illion(29, 21), 3, BigInteger.Parse("2900000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("angelic-refs", "Investor Refs", Hockey, Illion(38, 21), 3, BigInteger.Parse("3800000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("angel-best-boys", "Investor Best Boys", Movie, Illion(52, 21), 3, BigInteger.Parse("5200000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("seraph-saturdays", "Samstage Im Thronsaal", Bank, Illion(67, 21), 3, BigInteger.Parse("6700000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("the-cleansing-fires", "Reinigendes Feuer", Oil, Illion(72, 21), 3, BigInteger.Parse("7200000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("thirsty-cherubs", "Durstige Zitterhasen", Lemonade, Illion(96, 21), 3, BigInteger.Parse("9600000000000000000000000000000000000000000000000000000000000000000000")),
        Count("it-is-done", "Es Ist Vollbracht", Newspaper, Illion(125, 21), 50, BigInteger.Parse("4266000000000000000000000000000000000000000000000000000000000000000000")),
        All("profit-dence", "Gewinn-Vorsehung", Illion(777, 21), 7.777777, BigInteger.Parse("1600000000000000000000000000000000000000000000000000000000000000000000")),
        Count("emergency-reserves", "Notreserve Vom Jefe", Newspaper, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("bite-the-cost", "In Den Sauren Apfel Beißen", CarWash, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("a-little-further", "Nur Noch Ein Stückchen", Pizza, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("desperate-measures", "Verzweifelte Maßnahmen", Donut, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("impulse-buy", "Spontankauf", Shrimp, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("seize-every-inch", "Jeden Zentimeter Sichern", Hockey, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("small-opportunity", "Kleine Chance, Großer Deal", Movie, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("off-shore-refuge", "Offshore-Versteck", Bank, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("shell-corporations", "Briefkastenfirmen", Oil, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("expensive", "Teuer, Aber Es Lohnt Sich", Lemonade, Illion(5, 22), 10, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("manufacture-the-news", "Schlagzeilen Selbst Gemacht", Newspaper, Illion(1, 23), 3, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("freak-dust-storms", "Skurrile Staubstürme", CarWash, Illion(5, 23), 3, BigInteger.Parse("500000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("famine-zone-delivery", "Lieferung In Die Krisenzone", Pizza, Illion(22, 23), 3, BigInteger.Parse("2200000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("heritage-appeal", "Alteingesessener Charme", Donut, Illion(44, 23), 3, BigInteger.Parse("4400000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("onboard-orchestra", "Live-Musik Beim Deal", Shrimp, Illion(111, 23), 3, BigInteger.Parse("11100000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("pay-per-view", "Bezahl-Show An Der Grenze", Hockey, Illion(222, 23), 3, BigInteger.Parse("22200000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("enough-parking-spots", "Genug Parkplätze Für Alle", Movie, Illion(333, 23), 3, BigInteger.Parse("33300000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("catapults", "Katapultierter Aufstieg", Bank, Illion(444, 23), 3, BigInteger.Parse("44400000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("large-hadron-pumps", "Teilchenbeschleuniger-Pumpe", Oil, Illion(555, 23), 3, BigInteger.Parse("55500000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("oktoberlemon", "Oktober-Zitterfest", Lemonade, Illion(666, 23), 3, BigInteger.Parse("66600000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("propped-up", "Künstlich Hochgehalten", CarWash, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("aunty-idol", "Tantchens Idol", Newspaper, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("pizza-clicker", "Klick-Rausch", Pizza, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("donut-box", "Großpackung", Donut, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("clicker-shrimps", "Klick-Ware", Shrimp, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("dark-hockey", "Dunkle Grenzgeschäfte", Hockey, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("farleys-angels", "Farley's Investors", Movie, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("aggressive-negotiation", "Aggressives Verhandeln", Bank, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("worth-it", "Wert Jeden Cent", Oil, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("lemon-scented-angels", "Lemon Scented Investors", Lemonade, Illion(25, 24), 25, BigInteger.Parse("2500000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("loyal-readers", "Treue Stammkundschaft", Newspaper, Illion(11, 25), 3, BigInteger.Parse("1100000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("mud-enthusiasts", "Schlamm-Fans", CarWash, Illion(27, 25), 3, BigInteger.Parse("2700000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("gamers", "Zocker-Klientel", Pizza, Illion(43, 25), 3, BigInteger.Parse("4300000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("police-officers", "Blaulicht-Rabatt", Donut, Illion(87, 25), 3, BigInteger.Parse("8700000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("voracious-tourists", "Hungrige Touristen", Shrimp, Illion(190, 25), 3, BigInteger.Parse("19000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("canadian-angels", "Canadian Investors", Hockey, Illion(321, 25), 3, BigInteger.Parse("32100000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("movie-buffs", "Kino-Fanatiker", Movie, Illion(495, 25), 3, BigInteger.Parse("49500000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("up-and-coming-capitalists", "Aufstrebende Kapitalisten", Bank, Illion(600, 25), 3, BigInteger.Parse("60000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("off-roaders", "Geländegänger", Oil, Illion(725, 25), 3, BigInteger.Parse("72500000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("thirsty-neighbors", "Durstige Nachbarschaft", Lemonade, Illion(898, 25), 3, BigInteger.Parse("89800000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        All("proverbs", "Weisheiten Vom Jefe", Illion(3, 27), 13.11, BigInteger.Parse("5500000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        All("pile-of-haloes", "Stapel Voller Heiligenscheine", Illion(13, 28), 5, BigInteger.Parse("32500000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        All("last-lunch-special", "Letztes Mittagsangebot", Illion(3, 29), 3, BigInteger.Parse("12000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        All("holy-guacamole", "Heilige Guacamole", Illion(13, 30), 4, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        All("low-sinterest-rates", "Niedrige Sündenzinsen", Illion(24, 31), 5, BigInteger.Parse("60000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("newspaper-dealing", $"{BizName(Newspaper)} Dealing", Newspaper, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("car-wash-dealing", $"{BizName(CarWash)} Dealing", CarWash, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("pizza-dealing", $"{BizName(Pizza)} Dealing", Pizza, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("donut-dealing", $"{BizName(Donut)} Dealing", Donut, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("shrimp-dealing", $"{BizName(Shrimp)} Dealing", Shrimp, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("hockey-dealing", $"{BizName(Hockey)} Dealing", Hockey, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("movie-dealing", $"{BizName(Movie)} Dealing", Movie, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("bank-dealing", $"{BizName(Bank)} Dealing", Bank, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("oil-dealing", $"{BizName(Oil)} Dealing", Oil, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("lemon-dealing", $"{BizName(Lemonade)} Dealing", Lemonade, Illion(1, 33), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        All("ultra-mega-death-holiness", "Ultra-Mega-Todesheiligkeit", Illion(333, 35), 3, BigInteger.Parse("1332000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("angel-sourced-ingredients", "Investor-Sourced Ingredients", Newspaper, Illion(1, 37), 3, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("blessed-fuzzy-dice", "Gesegnete Glückswürfel", CarWash, Illion(20, 37), 3, BigInteger.Parse("180000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("holy-moly-guacamole", "Heilige Scheiße, Guacamole!", Pizza, Illion(50, 37), 3, BigInteger.Parse("400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("heavenly-sprinkles", "Himmlische Streusel", Donut, Illion(100, 37), 3, BigInteger.Parse("10000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("tridents", "Dreizack Der Straße", Shrimp, Illion(200, 37), 3, BigInteger.Parse("20000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("turn-water-into-teeth", "Wasser Zu Zähnen", Hockey, Illion(300, 37), 3, BigInteger.Parse("30000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("answered-movie-prayers", "Erhörte Gebete", Movie, Illion(400, 37), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("security-guardians", "Wächter Der Krone", Bank, Illion(500, 37), 3, BigInteger.Parse("50000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("peppy-laborforce", "Hochmotivierte Truppe", Oil, Illion(750, 37), 3, BigInteger.Parse("75000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("awe-inspiring-toasts", "Ehrfurcht Gebietende Trinksprüche", Lemonade, Illion(2, 38), 3, BigInteger.Parse("200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("newspaper-transaction", $"{BizName(Newspaper)} Transaction", Newspaper, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("car-wash-transaction", $"{BizName(CarWash)} Transaction", CarWash, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("pizza-transaction", $"{BizName(Pizza)} Transaction", Pizza, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("donut-transaction", $"{BizName(Donut)} Transaction", Donut, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("shrimp-transaction", $"{BizName(Shrimp)} Transaction", Shrimp, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("hockey-transaction", $"{BizName(Hockey)} Transaction", Hockey, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("movie-transaction", $"{BizName(Movie)} Transaction", Movie, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("bank-transaction", $"{BizName(Bank)} Transaction", Bank, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("oil-transaction", $"{BizName(Oil)} Transaction", Oil, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("lemon-transaction", $"{BizName(Lemonade)} Transaction", Lemonade, Illion(1, 42), 25, BigInteger.Parse("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-bike-baskets", "Verehrter Sattelkorb", Newspaper, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-hoses", "Verehrter Schlauch", CarWash, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-pizza-savers", "Verehrter Pizzaretter", Pizza, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-novelty-mugs", "Verehrte Sammeltasse", Donut, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-rain-caps", "Verehrte Regenhaube", Shrimp, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-hockey-tape", "Verehrtes Klebeband", Hockey, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-clackers", "Verehrte Klappern", Movie, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-spreadsheets", "Verehrte Excel-Tabelle", Bank, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-sippy-cups", "Verehrter Schnabelbecher", Lemonade, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Biz("venerated-lunch-boxes", "Verehrte Brotdose", Oil, Illion(1, 45), 3, BigInteger.Parse("40000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        All("forever-and-ever", "Für Immer Und Ewig", Illion(2, 45), 19, BigInteger.Parse("3000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-clean", "Es Werde Sauber", CarWash, Illion(2, 45), 25, BigInteger.Parse("200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-cocktails", "Es Werde Cocktail", Shrimp, Illion(2, 45), 25, BigInteger.Parse("200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-games", "Es Werde Spiel", Hockey, Illion(2, 45), 25, BigInteger.Parse("200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-cashflow", "Es Werde Cashflow", Bank, Illion(2, 45), 25, BigInteger.Parse("200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-feasts", "Es Werde Festmahl", Pizza, Illion(2, 45), 25, BigInteger.Parse("200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-lemons", "Es Werde Zitrone", Lemonade, Illion(2, 45), 25, BigInteger.Parse("200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-stories", "Es Werde Schlagzeile", Newspaper, Illion(3, 45), 25, BigInteger.Parse("300000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-pastries", "Es Werde Gebäck", Donut, Illion(3, 45), 25, BigInteger.Parse("300000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-classics", "Es Werde Klassiker", Movie, Illion(3, 45), 25, BigInteger.Parse("300000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
        Count("let-there-be-blood", "Es Werde Blut", Oil, Illion(4, 45), 25, BigInteger.Parse("400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")),
    ];

    public static AngelUpgradeDefinition? GetById(string id) =>
        Upgrades.FirstOrDefault(u => u.Id == id);
}
