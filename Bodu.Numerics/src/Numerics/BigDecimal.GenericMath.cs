// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.GenericMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

// StyleCop's SA1648 does not recognize <inheritdoc /> on explicit implementations of static abstract interface
// members; the documentation is correctly inherited from the System.Numerics generic-math interfaces.
#pragma warning disable SA1648 // inheritdoc should be used with inheriting class

public readonly partial struct BigDecimal :
    INumber<BigDecimal>,
    ISignedNumber<BigDecimal>
{
    /// <inheritdoc />
    static BigDecimal IAdditiveIdentity<BigDecimal, BigDecimal>.AdditiveIdentity => Zero;

    /// <inheritdoc />
    static BigDecimal IMultiplicativeIdentity<BigDecimal, BigDecimal>.MultiplicativeIdentity => One;

    /// <inheritdoc />
    static BigDecimal ISignedNumber<BigDecimal>.NegativeOne => MinusOne;

    /// <inheritdoc />
    static int INumberBase<BigDecimal>.Radix => 10;

    /// <inheritdoc />
    static BigDecimal INumberBase<BigDecimal>.Abs(BigDecimal value) =>
        Abs(value);

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsCanonical(BigDecimal value) =>
        true;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsComplexNumber(BigDecimal value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsEvenInteger(BigDecimal value) =>
        value.IsInteger && value._mantissa.IsEven;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsFinite(BigDecimal value) =>
        true;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsImaginaryNumber(BigDecimal value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsInfinity(BigDecimal value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsInteger(BigDecimal value) =>
        value.IsInteger;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsNaN(BigDecimal value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsNegative(BigDecimal value) =>
        value.IsNegative;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsNegativeInfinity(BigDecimal value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsNormal(BigDecimal value) =>
        !value.IsZero;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsOddInteger(BigDecimal value) =>
        value.IsInteger && !value._mantissa.IsEven;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsPositive(BigDecimal value) =>
        value.IsPositive;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsPositiveInfinity(BigDecimal value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsRealNumber(BigDecimal value) =>
        true;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsSubnormal(BigDecimal value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.IsZero(BigDecimal value) =>
        value.IsZero;

    /// <inheritdoc />
    static BigDecimal INumberBase<BigDecimal>.MaxMagnitude(BigDecimal x, BigDecimal y) =>
        MaxMagnitudeInternal(x, y);

    /// <inheritdoc />
    static BigDecimal INumberBase<BigDecimal>.MaxMagnitudeNumber(BigDecimal x, BigDecimal y) =>
        MaxMagnitudeInternal(x, y);

    /// <inheritdoc />
    static BigDecimal INumberBase<BigDecimal>.MinMagnitude(BigDecimal x, BigDecimal y) =>
        MinMagnitudeInternal(x, y);

    /// <inheritdoc />
    static BigDecimal INumberBase<BigDecimal>.MinMagnitudeNumber(BigDecimal x, BigDecimal y) =>
        MinMagnitudeInternal(x, y);

    /// <inheritdoc />
    static BigDecimal INumberBase<BigDecimal>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) =>
        Parse(s, provider);

    /// <inheritdoc />
    static BigDecimal INumberBase<BigDecimal>.Parse(string s, NumberStyles style, IFormatProvider? provider) =>
        Parse(s, provider);

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out BigDecimal result) =>
        TryParse(s, provider, out result);

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.TryParse(string? s, NumberStyles style, IFormatProvider? provider, out BigDecimal result) =>
        TryParse(s, provider, out result);

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.TryConvertFromChecked<TOther>(TOther value, out BigDecimal result) =>
        TryConvertFromCore(value, out result);

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.TryConvertFromSaturating<TOther>(TOther value, out BigDecimal result) =>
        TryConvertFromCore(value, out result);

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.TryConvertFromTruncating<TOther>(TOther value, out BigDecimal result) =>
        TryConvertFromCore(value, out result);

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.TryConvertToChecked<TOther>(BigDecimal value, out TOther result)
    {
        result = value.IsInteger
            ? TOther.CreateChecked(value.ToBigInteger())
            : typeof(TOther) == typeof(decimal)
                ? TOther.CreateChecked(value.ToDecimal())
                : TOther.CreateChecked(value.ToDouble());
        return true;
    }

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.TryConvertToSaturating<TOther>(BigDecimal value, out TOther result)
    {
        result = value.IsInteger
            ? TOther.CreateSaturating(value.ToBigInteger())
            : TOther.CreateSaturating(value.ToDouble());
        return true;
    }

    /// <inheritdoc />
    static bool INumberBase<BigDecimal>.TryConvertToTruncating<TOther>(BigDecimal value, out TOther result)
    {
        result = value.IsInteger
            ? TOther.CreateTruncating(value.ToBigInteger())
            : TOther.CreateTruncating(value.ToDouble());
        return true;
    }

    /// <inheritdoc />
    static BigDecimal INumber<BigDecimal>.Clamp(BigDecimal value, BigDecimal min, BigDecimal max)
    {
        if (min > max)
            throw new ArgumentException(NumericsResourceStrings.Arg_Invalid_ClampBounds, nameof(min));

        return value < min ? min : value > max ? max : value;
    }

    /// <inheritdoc />
    static BigDecimal INumber<BigDecimal>.CopySign(BigDecimal value, BigDecimal sign) =>
        sign.IsNegative ? Negate(Abs(value)) : Abs(value);

    /// <inheritdoc />
    static BigDecimal INumber<BigDecimal>.Max(BigDecimal x, BigDecimal y) =>
        Max(x, y);

    /// <inheritdoc />
    static BigDecimal INumber<BigDecimal>.MaxNumber(BigDecimal x, BigDecimal y) =>
        Max(x, y);

    /// <inheritdoc />
    static BigDecimal INumber<BigDecimal>.Min(BigDecimal x, BigDecimal y) =>
        Min(x, y);

    /// <inheritdoc />
    static BigDecimal INumber<BigDecimal>.MinNumber(BigDecimal x, BigDecimal y) =>
        Min(x, y);

    /// <inheritdoc />
    static int INumber<BigDecimal>.Sign(BigDecimal value) =>
        value.Sign;

    /// <summary>
    /// Converts a value of an arbitrary numeric type to a <see cref="BigDecimal" />.
    /// </summary>
    /// <typeparam name="TOther">The source numeric type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">When this method returns, contains the converted value, or zero on failure.</param>
    /// <returns><see langword="true" /> if the value was converted; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// Integer and <see cref="decimal" /> sources are converted exactly; other finite values are converted through
    /// their shortest round-trip <see cref="double" /> text. Non-finite values yield <see langword="false" />. Because
    /// <see cref="BigDecimal" /> is unbounded, the checked, saturating, and truncating conversions never overflow, so
    /// they share this implementation.
    /// </remarks>
    private static bool TryConvertFromCore<TOther>(TOther value, out BigDecimal result)
        where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) == typeof(decimal))
        {
            result = FromDecimal((decimal)(object)value);
            return true;
        }

        if (TOther.IsInteger(value))
        {
            result = new BigDecimal(BigInteger.CreateChecked(value));
            return true;
        }

        if (!TOther.IsFinite(value))
        {
            result = default;
            return false;
        }

        double approximation = double.CreateChecked(value);
        if (!double.IsFinite(approximation))
        {
            result = default;
            return false;
        }

        result = FromDouble(approximation);
        return true;
    }

    /// <summary>
    /// Returns whichever argument has the greater magnitude, preferring the positive value on a tie.
    /// </summary>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The argument with the greater magnitude.</returns>
    private static BigDecimal MaxMagnitudeInternal(BigDecimal x, BigDecimal y)
    {
        int comparison = Abs(x).CompareTo(Abs(y));
        return comparison > 0 ? x : comparison < 0 ? y : x.Sign >= y.Sign ? x : y;
    }

    /// <summary>
    /// Returns whichever argument has the smaller magnitude, preferring the negative value on a tie.
    /// </summary>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The argument with the smaller magnitude.</returns>
    private static BigDecimal MinMagnitudeInternal(BigDecimal x, BigDecimal y)
    {
        int comparison = Abs(x).CompareTo(Abs(y));
        return comparison < 0 ? x : comparison > 0 ? y : x.Sign <= y.Sign ? x : y;
    }
}

#pragma warning restore SA1648 // inheritdoc should be used with inheriting class
