// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.GenericMath.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

// StyleCop's SA1648 does not recognize <inheritdoc /> on explicit implementations of static abstract interface
// members; the documentation is correctly inherited from the System.Numerics generic-math interfaces.
#pragma warning disable SA1648 // inheritdoc should be used with inheriting class

public readonly partial struct Fraction<T> :
    INumber<Fraction<T>>,
    ISignedNumber<Fraction<T>>
{
    /// <inheritdoc />
    static Fraction<T> IAdditiveIdentity<Fraction<T>, Fraction<T>>.AdditiveIdentity => Zero;

    /// <inheritdoc />
    static Fraction<T> IMultiplicativeIdentity<Fraction<T>, Fraction<T>>.MultiplicativeIdentity => One;

    /// <inheritdoc />
    static Fraction<T> ISignedNumber<Fraction<T>>.NegativeOne => MinusOne;

    /// <inheritdoc />
    static int INumberBase<Fraction<T>>.Radix => 2;

    /// <inheritdoc />
    static Fraction<T> INumberBase<Fraction<T>>.Abs(Fraction<T> value) =>
        value.Abs();

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsCanonical(Fraction<T> value) =>
        true;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsComplexNumber(Fraction<T> value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsEvenInteger(Fraction<T> value) =>
        value.IsInteger && value.BigNumerator.IsEven;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsFinite(Fraction<T> value) =>
        true;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsImaginaryNumber(Fraction<T> value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsInfinity(Fraction<T> value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsInteger(Fraction<T> value) =>
        value.IsInteger;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsNaN(Fraction<T> value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsNegative(Fraction<T> value) =>
        value.IsNegative;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsNegativeInfinity(Fraction<T> value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsNormal(Fraction<T> value) =>
        !value.IsZero;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsOddInteger(Fraction<T> value) =>
        value.IsInteger && !value.BigNumerator.IsEven;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsPositive(Fraction<T> value) =>
        value.IsPositive;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsPositiveInfinity(Fraction<T> value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsRealNumber(Fraction<T> value) =>
        true;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsSubnormal(Fraction<T> value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsZero(Fraction<T> value) =>
        value.IsZero;

    /// <inheritdoc />
    static Fraction<T> INumberBase<Fraction<T>>.MaxMagnitude(Fraction<T> x, Fraction<T> y) =>
        MaxMagnitudeInternal(x, y);

    /// <inheritdoc />
    static Fraction<T> INumberBase<Fraction<T>>.MaxMagnitudeNumber(Fraction<T> x, Fraction<T> y) =>
        MaxMagnitudeInternal(x, y);

    /// <inheritdoc />
    static Fraction<T> INumberBase<Fraction<T>>.MinMagnitude(Fraction<T> x, Fraction<T> y) =>
        MinMagnitudeInternal(x, y);

    /// <inheritdoc />
    static Fraction<T> INumberBase<Fraction<T>>.MinMagnitudeNumber(Fraction<T> x, Fraction<T> y) =>
        MinMagnitudeInternal(x, y);

    /// <inheritdoc />
    static Fraction<T> INumberBase<Fraction<T>>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) =>
        Parse(s, provider);

    /// <inheritdoc />
    static Fraction<T> INumberBase<Fraction<T>>.Parse(string s, NumberStyles style, IFormatProvider? provider) =>
        Parse(s, provider);

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Fraction<T> result) =>
        TryParse(s, provider, out result);

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryParse(string? s, NumberStyles style, IFormatProvider? provider, out Fraction<T> result) =>
        TryParse(s, provider, out result);

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertFromChecked<TOther>(TOther value, out Fraction<T> result) =>
        TryConvertFrom(value, out result);

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertFromSaturating<TOther>(TOther value, out Fraction<T> result) =>
        TryConvertFrom(value, out result);

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertFromTruncating<TOther>(TOther value, out Fraction<T> result) =>
        TryConvertFrom(value, out result);

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertToChecked<TOther>(Fraction<T> value, out TOther result)
    {
        result = value.IsInteger
            ? TOther.CreateChecked(value.BigNumerator)
            : TOther.CreateChecked(value.ToDouble());
        return true;
    }

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertToSaturating<TOther>(Fraction<T> value, out TOther result)
    {
        result = value.IsInteger
            ? TOther.CreateSaturating(value.BigNumerator)
            : TOther.CreateSaturating(value.ToDouble());
        return true;
    }

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertToTruncating<TOther>(Fraction<T> value, out TOther result)
    {
        result = value.IsInteger
            ? TOther.CreateTruncating(value.BigNumerator)
            : TOther.CreateTruncating(value.ToDouble());
        return true;
    }

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.Clamp(Fraction<T> value, Fraction<T> min, Fraction<T> max)
    {
        if (Compare(min, max) > 0)
            throw new ArgumentException("The minimum bound must not exceed the maximum bound.", nameof(min));

        if (Compare(value, min) < 0)
            return min;

        return Compare(value, max) > 0 ? max : value;
    }

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.CopySign(Fraction<T> value, Fraction<T> sign) =>
        sign.IsNegative ? -value.Abs() : value.Abs();

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.Max(Fraction<T> x, Fraction<T> y) =>
        Compare(x, y) >= 0 ? x : y;

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.MaxNumber(Fraction<T> x, Fraction<T> y) =>
        Compare(x, y) >= 0 ? x : y;

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.Min(Fraction<T> x, Fraction<T> y) =>
        Compare(x, y) <= 0 ? x : y;

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.MinNumber(Fraction<T> x, Fraction<T> y) =>
        Compare(x, y) <= 0 ? x : y;

    /// <inheritdoc />
    static int INumber<Fraction<T>>.Sign(Fraction<T> value) =>
        value.Sign;

    /// <summary>
    /// Converts a value of an arbitrary numeric type to a <see cref="Fraction{T}" />.
    /// </summary>
    /// <typeparam name="TOther">The source numeric type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">When this method returns, contains the converted value, or zero on failure.</param>
    /// <returns><see langword="true" /> if the value was converted; otherwise, <see langword="false" />.</returns>
    private static bool TryConvertFrom<TOther>(TOther value, out Fraction<T> result)
        where TOther : INumberBase<TOther>
    {
        if (TOther.IsInteger(value))
        {
            result = FromBigInteger(BigInteger.CreateChecked(value), BigInteger.One);
            return true;
        }

        if (!TOther.IsFinite(value))
        {
            result = default;
            return false;
        }

        result = FromDouble(double.CreateChecked(value));
        return true;
    }

    /// <summary>
    /// Returns whichever argument has the greater magnitude, preferring the positive value on a tie.
    /// </summary>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The argument with the greater magnitude.</returns>
    private static Fraction<T> MaxMagnitudeInternal(Fraction<T> x, Fraction<T> y)
    {
        int comparison = Compare(x.Abs(), y.Abs());
        if (comparison > 0)
            return x;

        if (comparison < 0)
            return y;

        return x.Sign >= y.Sign ? x : y;
    }

    /// <summary>
    /// Returns whichever argument has the smaller magnitude, preferring the negative value on a tie.
    /// </summary>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The argument with the smaller magnitude.</returns>
    private static Fraction<T> MinMagnitudeInternal(Fraction<T> x, Fraction<T> y)
    {
        int comparison = Compare(x.Abs(), y.Abs());
        if (comparison < 0)
            return x;

        if (comparison > 0)
            return y;

        return x.Sign <= y.Sign ? x : y;
    }
}

#pragma warning restore SA1648 // inheritdoc should be used with inheriting class
