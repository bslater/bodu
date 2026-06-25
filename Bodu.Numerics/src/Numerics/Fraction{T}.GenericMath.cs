// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction{T}.GenericMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        value.IsCanonical;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsComplexNumber(Fraction<T> value) =>
        false;

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.IsEvenInteger(Fraction<T> value) =>
        value.IsEvenInteger;

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
        value.IsOddInteger;

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
    static bool INumberBase<Fraction<T>>.TryConvertFromChecked<TOther>(TOther value, out Fraction<T> result)
    {
        try
        {
            return TryConvertFromCore(value, out result);
        }
        catch (NotSupportedException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertFromSaturating<TOther>(TOther value, out Fraction<T> result) =>
        TryConvertFromClamped(value, out result);

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertFromTruncating<TOther>(TOther value, out Fraction<T> result) =>
        TryConvertFromClamped(value, out result);

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertToChecked<TOther>(Fraction<T> value, out TOther result)
    {
        try
        {
            result = value.IsInteger
                ? TOther.CreateChecked(value.BigNumerator)
                : typeof(TOther) == typeof(decimal)
                    ? TOther.CreateChecked(value.ToDecimal())
                    : TOther.CreateChecked(value.ToDouble());

            return true;
        }
        catch (NotSupportedException)
        {
            result = default!;
            return false;
        }
    }

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertToSaturating<TOther>(Fraction<T> value, out TOther result)
    {
        try
        {
            result = value.IsInteger
                ? TOther.CreateSaturating(value.BigNumerator)
                : TOther.CreateSaturating(value.ToDouble());
            return true;
        }
        catch (NotSupportedException)
        {
            result = default!;
            return false;
        }
    }

    /// <inheritdoc />
    static bool INumberBase<Fraction<T>>.TryConvertToTruncating<TOther>(Fraction<T> value, out TOther result)
    {
        try
        {
            result = value.IsInteger
                ? TOther.CreateTruncating(value.BigNumerator)
                : TOther.CreateTruncating(value.ToDouble());
            return true;
        }
        catch (NotSupportedException)
        {
            result = default!;
            return false;
        }
    }

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.Clamp(Fraction<T> value, Fraction<T> min, Fraction<T> max) =>
        Clamp(value, min, max);

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.CopySign(Fraction<T> value, Fraction<T> sign) =>
        sign.IsNegative ? -value.Abs() : value.Abs();

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.Max(Fraction<T> x, Fraction<T> y) =>
        Max(x, y);

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.MaxNumber(Fraction<T> x, Fraction<T> y) =>
        Compare(x, y) >= 0 ? x : y;

    /// <inheritdoc />
    static Fraction<T> INumber<Fraction<T>>.Min(Fraction<T> x, Fraction<T> y) =>
        Min(x, y);

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
    /// <exception cref="OverflowException">Thrown if the result does not fit <typeparamref name="T" />.</exception>
    /// <exception cref="NotSupportedException">Thrown if <typeparamref name="TOther" /> is not supported.</exception>
    /// <remarks>
    /// Integer and <see cref="decimal" /> sources are converted exactly; other finite values are converted through
    /// their nearest <see cref="double" /> approximation. Non-finite values yield <see langword="false" />.
    /// </remarks>
    private static bool TryConvertFromCore<TOther>(TOther value, out Fraction<T> result)
        where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) == typeof(decimal))
        {
            result = FromDecimal((decimal)(object)value);
            return true;
        }

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
    /// Converts a value of an arbitrary numeric type to a <see cref="Fraction{T}" />, clamping on overflow.
    /// </summary>
    /// <typeparam name="TOther">The source numeric type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">When this method returns, contains the converted value, or zero on failure.</param>
    /// <returns><see langword="true" /> if the value was converted; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// This method backs the saturating and truncating conversions, which must not raise an
    /// <see cref="OverflowException" /> for an in-domain but out-of-range value.
    /// </remarks>
    private static bool TryConvertFromClamped<TOther>(TOther value, out Fraction<T> result)
        where TOther : INumberBase<TOther>
    {
        try
        {
            return TryConvertFromCore(value, out result);
        }
        catch (NotSupportedException)
        {
            result = default;
            return false;
        }
        catch (OverflowException)
        {
            result = TOther.IsNegative(value) ? MinValue : MaxValue;
            return true;
        }
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
        return comparison > 0
            ? x
            : comparison < 0
                ? y
                : x.Sign >= y.Sign
                    ? x
                    : y;
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
        return comparison < 0
            ? x
            : comparison > 0
                ? y
                : x.Sign <= y.Sign
                    ? x
                    : y;
    }
}

#pragma warning restore SA1648 // inheritdoc should be used with inheriting class
