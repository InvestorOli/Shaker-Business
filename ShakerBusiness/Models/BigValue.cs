using System.Globalization;
using System.Numerics;

namespace ShakerBusiness.Models;

public readonly struct BigValue : IComparable<BigValue>, IEquatable<BigValue>
{
    private const int DoubleExponentLimit = 300;
    private const int SignificantDigits = 15;
    private const long MaxBigIntegerExponent = 5000;
    private const long MaxAddExponentGap = 17;

    private static readonly BigInteger DoubleBoundary = BigInteger.Pow(10, DoubleExponentLimit);

    private readonly double _mantissa;
    private readonly long _exponent;

    private BigValue(double mantissa, long exponent)
    {
        _mantissa = mantissa;
        _exponent = exponent;
    }

    public static BigValue Zero => default;

    public static BigValue One => new(1.0, 0);

    public bool IsZero => _mantissa == 0;

    public bool FitsDouble => IsZero || _exponent < DoubleExponentLimit;

    public static BigValue FromDouble(double value)
    {
        value = EconomyMath.ClampFinite(value);
        if (value <= 0)
        {
            return Zero;
        }

        var shift = (int)Math.Floor(Math.Log10(value));
        if (shift < -DoubleExponentLimit)
        {
            return Zero;
        }

        var mantissa = shift >= 0 ? value / Math.Pow(10, shift) : value * Math.Pow(10, -shift);
        return Normalize(mantissa, shift);
    }

    public static BigValue FromBigInteger(BigInteger value)
    {
        if (value.Sign <= 0)
        {
            return Zero;
        }

        if (value < DoubleBoundary)
        {
            return FromDouble((double)value);
        }

        var text = value.ToString(CultureInfo.InvariantCulture);
        var mantissa = double.Parse(string.Concat(text.AsSpan(0, 1), ".", text.AsSpan(1, 16)), CultureInfo.InvariantCulture);
        return Normalize(mantissa, text.Length - 1);
    }

    public BigInteger ToBigInteger()
    {
        if (IsZero)
        {
            return BigInteger.Zero;
        }

        if (_exponent < SignificantDigits)
        {
            return new BigInteger(Math.Round(_mantissa * Math.Pow(10, _exponent)));
        }

        var digits = new BigInteger(Math.Round(_mantissa * Math.Pow(10, SignificantDigits - 1)));
        var exponent = (int)(Math.Min(_exponent, MaxBigIntegerExponent) - (SignificantDigits - 1));
        return digits * EconomyMath.Pow10(exponent);
    }

    public double ToDouble()
    {
        if (IsZero || _exponent < -DoubleExponentLimit)
        {
            return 0;
        }

        return _exponent > 308 ? double.MaxValue : EconomyMath.ClampFinite(_mantissa * Math.Pow(10, _exponent));
    }

    public BigValue Multiply(double factor) => this * FromDouble(factor);

    public BigValue DivideBy(double divisor)
    {
        var d = FromDouble(divisor);
        return IsZero || d.IsZero ? Zero : Normalize(_mantissa / d._mantissa, _exponent - d._exponent);
    }

    public static BigValue operator *(BigValue a, BigValue b) =>
        a.IsZero || b.IsZero ? Zero : Normalize(a._mantissa * b._mantissa, a._exponent + b._exponent);

    public static BigValue operator +(BigValue a, BigValue b)
    {
        if (a.IsZero)
        {
            return b;
        }

        if (b.IsZero)
        {
            return a;
        }

        var (high, low) = a.CompareTo(b) >= 0 ? (a, b) : (b, a);
        var gap = high._exponent - low._exponent;
        return gap > MaxAddExponentGap
            ? high
            : Normalize(high._mantissa + (low._mantissa * Math.Pow(10, -gap)), high._exponent);
    }

    public static bool operator <(BigValue a, BigValue b) => a.CompareTo(b) < 0;

    public static bool operator >(BigValue a, BigValue b) => a.CompareTo(b) > 0;

    public static bool operator <=(BigValue a, BigValue b) => a.CompareTo(b) <= 0;

    public static bool operator >=(BigValue a, BigValue b) => a.CompareTo(b) >= 0;

    public static bool operator ==(BigValue a, BigValue b) => a.Equals(b);

    public static bool operator !=(BigValue a, BigValue b) => !a.Equals(b);

    public int CompareTo(BigValue other)
    {
        if (IsZero || other.IsZero)
        {
            return IsZero == other.IsZero ? 0 : (IsZero ? -1 : 1);
        }

        var byExponent = _exponent.CompareTo(other._exponent);
        return byExponent != 0 ? byExponent : _mantissa.CompareTo(other._mantissa);
    }

    public bool Equals(BigValue other) => _mantissa == other._mantissa && _exponent == other._exponent;

    public override bool Equals(object? obj) => obj is BigValue other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_mantissa, _exponent);

    public override string ToString() => IsZero ? "0" : $"{_mantissa.ToString("R", CultureInfo.InvariantCulture)}e{_exponent}";

    private static BigValue Normalize(double mantissa, long exponent)
    {
        if (!(mantissa > 0) || double.IsInfinity(mantissa))
        {
            return Zero;
        }

        while (mantissa >= 10)
        {
            mantissa /= 10;
            exponent++;
        }

        while (mantissa < 1)
        {
            mantissa *= 10;
            exponent--;
        }

        return new BigValue(mantissa, exponent);
    }
}
