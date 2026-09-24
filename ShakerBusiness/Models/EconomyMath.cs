using System.Numerics;

namespace ShakerBusiness.Models;

public static class EconomyMath
{
    private const double AngelFormulaK = 1e15 / 22500.0;
    private const int AngelFormulaNumerator = 22500;
    private static readonly BigInteger AngelFormulaScale = BigInteger.Pow(10, 15);
    private static readonly BigInteger DoubleSafeLimit = BigInteger.Pow(10, 300);
    private const double DoubleSafeCost = 1e300;
    private const int Pow10TableSize = 1100;
    private const double MaxCostLog10 = 2000;
    private static readonly BigInteger[] Pow10Table = BuildPow10Table();

    public static BigInteger Pow10(int exponent) =>
        exponent >= 0 && exponent < Pow10TableSize ? Pow10Table[exponent] : BigInteger.Pow(10, exponent);

    private static BigInteger[] BuildPow10Table()
    {
        var table = new BigInteger[Pow10TableSize];
        table[0] = BigInteger.One;
        for (var i = 1; i < table.Length; i++)
        {
            table[i] = table[i - 1] * 10;
        }

        return table;
    }
    private const int MantissaDigits = 15;
    public static double ClampFinite(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        if (double.IsPositiveInfinity(value))
        {
            return double.MaxValue;
        }

        return double.IsNegativeInfinity(value) ? double.MinValue : value;
    }

    public static BigInteger AngelsFor(BigInteger earnings)
    {
        if (earnings <= DoubleSafeLimit)
        {
            return new BigInteger(Math.Floor(Math.Sqrt((double)earnings / AngelFormulaK)));
        }

        return IntegerSqrt(earnings * AngelFormulaNumerator / AngelFormulaScale);
    }

    public static BigInteger ScaleRounded(BigInteger amount, double factor)
    {
        if (amount <= DoubleSafeLimit)
        {
            return new BigInteger(Math.Round((double)amount * factor));
        }

        return amount * new BigInteger(Math.Round(factor * 10000)) / 10000;
    }

    public static BigInteger IntegerSqrt(BigInteger value)
    {
        if (value < 2)
        {
            return value;
        }

        var estimate = BigInteger.One << (int)((value.GetBitLength() + 1) / 2);
        while (true)
        {
            var next = (estimate + (value / estimate)) >> 1;
            if (next >= estimate)
            {
                return estimate;
            }

            estimate = next;
        }
    }

    public static BigInteger CostForQuantity(double initialCost, double coefficient, int owned, int quantity)
    {
        if (quantity <= 0)
        {
            return BigInteger.Zero;
        }

        var cost = CostForQuantityAsDouble(initialCost, coefficient, owned, quantity);
        if (double.IsFinite(cost) && cost < DoubleSafeCost)
        {
            return new BigInteger(Math.Ceiling(cost));
        }

        return FromLog10(Log10CostForQuantity(initialCost, coefficient, owned, quantity));
    }

    public static int MaxAffordable(double initialCost, double coefficient, int owned, BigInteger cash)
    {
        if (cash.Sign <= 0 || CostForQuantity(initialCost, coefficient, owned, 1) > cash)
        {
            return 0;
        }

        var limit = int.MaxValue - owned;
        var estimate = Math.Min(limit, EstimateMaxAffordable(initialCost, coefficient, owned, cash));

        while (estimate > 0 && CostForQuantity(initialCost, coefficient, owned, estimate) > cash)
        {
            estimate--;
        }

        while (estimate < limit && CostForQuantity(initialCost, coefficient, owned, estimate + 1) <= cash)
        {
            estimate++;
        }

        return Math.Max(0, estimate);
    }

    private static double CostForQuantityAsDouble(double initialCost, double coefficient, int owned, int quantity)
    {
        if (IsLinear(coefficient))
        {
            return initialCost * quantity;
        }

        var costAtOwned = initialCost * Math.Pow(coefficient, owned);
        return costAtOwned * (Math.Pow(coefficient, quantity) - 1) / (coefficient - 1);
    }

    private static double Log10CostForQuantity(double initialCost, double coefficient, int owned, int quantity)
    {
        if (IsLinear(coefficient))
        {
            return Math.Log10(initialCost) + Math.Log10(quantity);
        }

        var logCoefficient = Math.Log10(coefficient);
        var logCostAtOwned = Math.Log10(initialCost) + (owned * logCoefficient);
        if (quantity == 1)
        {
            return logCostAtOwned;
        }

        var logSeries = (quantity * logCoefficient) + Math.Log10(1 - Math.Pow(coefficient, -quantity)) - Math.Log10(coefficient - 1);
        return logCostAtOwned + logSeries;
    }

    private static int EstimateMaxAffordable(double initialCost, double coefficient, int owned, BigInteger cash)
    {
        if (IsLinear(coefficient))
        {
            return (int)BigInteger.Min(int.MaxValue, cash / new BigInteger(Math.Ceiling(initialCost)));
        }

        var logCoefficient = Math.Log10(coefficient);
        var logCostAtOwned = Math.Log10(initialCost) + (owned * logCoefficient);
        var logRatio = BigInteger.Log10(cash) + Math.Log10(coefficient - 1) - logCostAtOwned;
        var logTerms = logRatio > MantissaDigits ? logRatio : Math.Log10(Math.Pow(10, logRatio) + 1);
        return (int)Math.Min(int.MaxValue, Math.Floor(logTerms / logCoefficient));
    }

    private static BigInteger FromLog10(double log10)
    {
        log10 = Math.Min(log10, MaxCostLog10);
        var exponent = (int)Math.Floor(log10) - (MantissaDigits - 1);
        if (exponent <= 0)
        {
            return new BigInteger(Math.Ceiling(Math.Pow(10, log10)));
        }

        var mantissa = Math.Pow(10, log10 - Math.Floor(log10) + (MantissaDigits - 1));
        return new BigInteger(Math.Ceiling(mantissa)) * Pow10(exponent);
    }

    private static bool IsLinear(double coefficient) => Math.Abs(coefficient - 1.0) < 1e-9;
}
