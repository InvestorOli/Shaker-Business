using System.Globalization;
using System.Numerics;
using System.Text;

namespace ShakerBusiness.Models;

public static class BigNumberFormatter
{
    public const int MaxStoredDigits = 1000;

    private static readonly string[] NamedSuffixes =
    [
        "BILLION", "TRILLION", "QUADRILLION",
        "QUINTILLION", "SEXTILLION", "SEPTILLION", "OCTILLION", "NONILLION",
        "DECILLION", "UNDECILLION", "DUODECILLION", "TREDECILLION", "QUATTUORDECILLION",
        "QUINDECILLION", "SEXDECILLION", "SEPTENDECILLION", "OCTODECILLION", "NOVEMDECILLION",
        "VIGINTILLION", "UNVIGINTILLION", "DUOVIGINTILLION", "TREVIGINTILLION", "QUATTUORVIGINTILLION",
        "QUINVIGINTILLION", "SEXVIGINTILLION", "SEPTENVIGINTILLION", "OCTOVIGINTILLION", "NOVEMVIGINTILLION",
        "TRIGINTILLION", "UNTRIGINTILLION", "DUOTRIGINTILLION", "TRETRIGINTILLION", "QUATTUORTRIGINTILLION",
        "QUINTRIGINTILLION", "SEXTRIGINTILLION", "SEPTENTRIGINTILLION", "OCTOTRIGINTILLION", "NOVEMTRIGINTILLION",
        "QUADRAGINTILLION", "UNQUADRAGINTILLION", "DUOQUADRAGINTILLION", "TREQUADRAGINTILLION", "QUATTUORQUADRAGINTILLION",
        "QUINQUADRAGINTILLION", "SEXQUADRAGINTILLION", "SEPTENQUADRAGINTILLION", "OCTOQUADRAGINTILLION", "NOVEMQUADRAGINTILLION",
        "QUINQUAGINTILLION", "UNQUINQUAGINTILLION", "DUOQUINQUAGINTILLION", "TREQUINQUAGINTILLION", "QUATTUORQUINQUAGINTILLION",
        "QUINQUINQUAGINTILLION", "SEXQUINQUAGINTILLION", "SEPTENQUINQUAGINTILLION", "OCTOQUINQUAGINTILLION", "NOVEMQUINQUAGINTILLION",
        "SEXAGINTILLION", "UNSEXAGINTILLION", "DUOSEXAGINTILLION", "TRESEXAGINTILLION", "QUATTUORSEXAGINTILLION",
        "QUINSEXAGINTILLION", "SEXSEXAGINTILLION", "SEPTENSEXAGINTILLION", "OCTOSEXAGINTILLION", "NOVEMSEXAGINTILLION",
        "SEPTUAGINTILLION", "UNSEPTUAGINTILLION", "DUOSEPTUAGINTILLION", "TRESEPTUAGINTILLION", "QUATTUORSEPTUAGINTILLION",
        "QUINSEPTUAGINTILLION", "SEXSEPTUAGINTILLION", "SEPTENSEPTUAGINTILLION", "OCTOSEPTUAGINTILLION", "NOVEMSEPTUAGINTILLION",
        "OCTOGINTILLION", "UNOCTOGINTILLION", "DUOOCTOGINTILLION", "TREOCTOGINTILLION", "QUATTUOROCTOGINTILLION",
        "QUINOCTOGINTILLION", "SEXOCTOGINTILLION", "SEPTENOCTOGINTILLION", "OCTOOCTOGINTILLION", "NOVEMOCTOGINTILLION",
        "NONAGINTILLION", "UNNONAGINTILLION", "DUONONAGINTILLION", "TRENONAGINTILLION", "QUATTUORNONAGINTILLION",
        "QUINNONAGINTILLION", "SEXNONAGINTILLION", "SEPTENNONAGINTILLION", "OCTONONAGINTILLION", "NOVEMNONAGINTILLION",
        "CENTILLION",
    ];

    public static readonly string[] Suffixes = NamedSuffixes
        .Concat(Enumerable.Range(1, HighestSuffixIndex(MaxStoredDigits) + 1 - NamedSuffixes.Length).Select(n => $"SHAKER-{ToRoman(n)}"))
        .ToArray();

    private const long BillionThreshold = 1_000_000_000L;

    private static readonly BigInteger StorageLimit = BigInteger.Pow(10, MaxStoredDigits);

    public static int NamedSuffixCount => NamedSuffixes.Length;

    public static bool IsStorable(BigInteger value) => value.Sign >= 0 && value < StorageLimit;

    private static int HighestSuffixIndex(int digits) => ((digits - 1) / 3) - 3;

    private static string ToRoman(int number)
    {
        (int Value, string Symbol)[] numerals =
        [
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"),
            (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
        ];

        var roman = new StringBuilder();
        foreach (var (value, symbol) in numerals)
        {
            while (number >= value)
            {
                roman.Append(symbol);
                number -= value;
            }
        }

        return roman.ToString();
    }

    private static string FormatHundredths(string digits, int integerDigits)
    {
        var leading = long.Parse(digits.AsSpan(0, integerDigits + 3), NumberStyles.None, CultureInfo.InvariantCulture);
        var hundredths = (leading + 5) / 10;
        return $"{hundredths / 100}.{hundredths % 100:00}";
    }

    public static (string Value, string Suffix) Split(BigInteger amount)
    {
        var negative = amount < 0;
        if (negative)
        {
            amount = -amount;
        }

        if (amount < BillionThreshold)
        {
            var plain = amount.ToString("N0", CultureInfo.InvariantCulture);
            return (negative ? $"-{plain}" : plain, "");
        }

        var digits = amount.ToString(CultureInfo.InvariantCulture);
        var tier = (digits.Length - 1) / 3;
        var suffixIndex = tier - 3;

        if (suffixIndex >= Suffixes.Length)
        {
            var sciText = FormatHundredths(digits, 1) + "e" + (digits.Length - 1);
            return (negative ? $"-{sciText}" : sciText, "");
        }

        var text = FormatHundredths(digits, digits.Length - (3 * tier));
        return (negative ? $"-{text}" : text, Suffixes[suffixIndex]);
    }

    private static readonly BigInteger[] SuffixThresholds =
        Enumerable.Range(0, Suffixes.Length).Select(i => BigInteger.Pow(10, 3 * i + 9)).ToArray();

    public static BigInteger SuffixThreshold(int suffixIndex) => SuffixThresholds[suffixIndex];

    public static readonly BigInteger EarningsCap = SuffixThresholds[^1];

    public static BigInteger Compose(string mantissa, int suffixIndex)
    {
        mantissa = mantissa.Trim();
        var negative = mantissa.StartsWith('-');
        if (negative)
        {
            mantissa = mantissa[1..];
        }

        if (suffixIndex < 0)
        {
            var raw = BigInteger.Parse(mantissa.Length == 0 ? "0" : mantissa);
            return negative ? -raw : raw;
        }

        var parts = mantissa.Split('.', 2);
        var integerPart = BigInteger.Parse(parts[0].Length == 0 ? "0" : parts[0]);
        var tier = suffixIndex + 3;
        var multiplier = BigInteger.Pow(1000, tier);
        var result = integerPart * multiplier;

        if (parts.Length == 2 && parts[1].Length > 0)
        {
            var fractionalDigits = parts[1].Length;
            var fractionalValue = BigInteger.Parse(parts[1]);
            var fractionalDivisor = BigInteger.Pow(10, fractionalDigits);
            result += fractionalValue * multiplier / fractionalDivisor;
        }

        return negative ? -result : result;
    }

    public static string FormatDollar(BigInteger amount)
    {
        var (value, suffix) = Split(amount);
        return suffix.Length == 0 ? $"${value}" : $"${value} {suffix}";
    }

    public static string FormatDollar(double amount) => FormatDollarOrPlain(amount, dollarSign: true);

    public static string FormatDollar(BigValue amount) =>
        amount.FitsDouble ? FormatDollar(amount.ToDouble()) : FormatDollar(amount.ToBigInteger());
    
    public static string FormatPercent(double rate)
    {
        var percent = EconomyMath.ClampFinite(rate * 100);
        return percent < 1000
            ? Math.Round(percent, 2).ToString("0.##", CultureInfo.InvariantCulture) + "%"
            : FormatPlain(new BigInteger(Math.Round(percent))) + "%";
    }

    public static string FormatPlain(BigInteger amount)
    {
        var (value, suffix) = Split(amount);
        return suffix.Length == 0 ? value : $"{value} {suffix}";
    }

    public static string FormatPlain(double amount) => FormatDollarOrPlain(amount, dollarSign: false);

    public static string FormatPlain(BigValue amount) =>
        amount.FitsDouble ? FormatPlain(amount.ToDouble()) : FormatPlain(amount.ToBigInteger());

    private static string FormatDollarOrPlain(double amount, bool dollarSign)
    {
        amount = EconomyMath.ClampFinite(amount);
        var sign = dollarSign ? "$" : "";
        var negative = amount < 0;
        var absAmount = Math.Abs(amount);
        if (absAmount < 1000)
        {
            var text = absAmount.ToString("0.00", CultureInfo.InvariantCulture);
            return negative ? $"-{sign}{text}" : $"{sign}{text}";
        }

        if (absAmount < BillionThreshold)
        {
            var text = absAmount.ToString("N0", CultureInfo.InvariantCulture);
            return negative ? $"-{sign}{text}" : $"{sign}{text}";
        }

        var (value, suffix) = Split(new BigInteger(Math.Round(amount)));
        return suffix.Length == 0 ? $"{sign}{value}" : $"{sign}{value} {suffix}";
    }
}
