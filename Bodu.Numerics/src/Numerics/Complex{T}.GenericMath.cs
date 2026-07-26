// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.GenericMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

// StyleCop's SA1648 does not recognize <inheritdoc /> on explicit implementations of static abstract interface
// members; the documentation is correctly inherited from the System.Numerics generic-math interfaces.
#pragma warning disable SA1648 // inheritdoc should be used with inheriting class

public readonly partial struct Complex<T> :
    INumberBase<Complex<T>>,
    ISignedNumber<Complex<T>>
{
    /// <inheritdoc />
    static Complex<T> IAdditiveIdentity<Complex<T>, Complex<T>>.AdditiveIdentity => Zero;

    /// <inheritdoc />
    static Complex<T> IMultiplicativeIdentity<Complex<T>, Complex<T>>.MultiplicativeIdentity => One;

    /// <inheritdoc />
    static Complex<T> ISignedNumber<Complex<T>>.NegativeOne => new(-T.One, T.Zero);

    /// <inheritdoc />
    static int INumberBase<Complex<T>>.Radix => 2;

    /// <inheritdoc />
    static Complex<T> INumberBase<Complex<T>>.Abs(Complex<T> value) =>
        new(Abs(value), T.Zero);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsCanonical(Complex<T> value) =>
        true;

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsComplexNumber(Complex<T> value) =>
        !T.IsZero(value.Real) && !T.IsZero(value.Imaginary);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsEvenInteger(Complex<T> value) =>
        T.IsZero(value.Imaginary) && T.IsEvenInteger(value.Real);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsFinite(Complex<T> value) =>
        IsFiniteCore(value);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsImaginaryNumber(Complex<T> value) =>
        T.IsZero(value.Real) && T.IsRealNumber(value.Imaginary);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsInfinity(Complex<T> value) =>
        IsInfinityCore(value);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsInteger(Complex<T> value) =>
        T.IsZero(value.Imaginary) && T.IsInteger(value.Real);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsNaN(Complex<T> value) =>
        !IsInfinityCore(value) && !IsFiniteCore(value);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsNegative(Complex<T> value) =>
        T.IsZero(value.Imaginary) && T.IsNegative(value.Real);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsNegativeInfinity(Complex<T> value) =>
        T.IsZero(value.Imaginary) && T.IsNegativeInfinity(value.Real);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsNormal(Complex<T> value) =>
        T.IsNormal(value.Real) && (T.IsZero(value.Imaginary) || T.IsNormal(value.Imaginary));

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsOddInteger(Complex<T> value) =>
        T.IsZero(value.Imaginary) && T.IsOddInteger(value.Real);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsPositive(Complex<T> value) =>
        T.IsZero(value.Imaginary) && T.IsPositive(value.Real);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsPositiveInfinity(Complex<T> value) =>
        T.IsZero(value.Imaginary) && T.IsPositiveInfinity(value.Real);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsRealNumber(Complex<T> value) =>
        T.IsZero(value.Imaginary) && T.IsRealNumber(value.Real);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsSubnormal(Complex<T> value) =>
        T.IsSubnormal(value.Real) || T.IsSubnormal(value.Imaginary);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.IsZero(Complex<T> value) =>
        T.IsZero(value.Real) && T.IsZero(value.Imaginary);

    /// <inheritdoc />
    static Complex<T> INumberBase<Complex<T>>.MaxMagnitude(Complex<T> x, Complex<T> y)
    {
        T ax = Abs(x);
        T ay = Abs(y);

        if (ax > ay || T.IsNaN(ax))
            return x;

        if (ax == ay)
            return T.Abs(x.Real) >= T.Abs(y.Real) ? x : y;

        return y;
    }

    /// <inheritdoc />
    static Complex<T> INumberBase<Complex<T>>.MaxMagnitudeNumber(Complex<T> x, Complex<T> y)
    {
        T ax = Abs(x);
        T ay = Abs(y);

        if (ax > ay || T.IsNaN(ay))
            return x;

        if (ax == ay)
            return T.Abs(x.Real) >= T.Abs(y.Real) ? x : y;

        return y;
    }

    /// <inheritdoc />
    static Complex<T> INumberBase<Complex<T>>.MinMagnitude(Complex<T> x, Complex<T> y)
    {
        T ax = Abs(x);
        T ay = Abs(y);

        if (ax < ay || T.IsNaN(ax))
            return x;

        if (ax == ay)
            return T.Abs(x.Real) <= T.Abs(y.Real) ? x : y;

        return y;
    }

    /// <inheritdoc />
    static Complex<T> INumberBase<Complex<T>>.MinMagnitudeNumber(Complex<T> x, Complex<T> y)
    {
        T ax = Abs(x);
        T ay = Abs(y);

        if (ax < ay || T.IsNaN(ay))
            return x;

        if (ax == ay)
            return T.Abs(x.Real) <= T.Abs(y.Real) ? x : y;

        return y;
    }

    /// <inheritdoc />
    static Complex<T> INumberBase<Complex<T>>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) =>
        Parse(s, provider);

    /// <inheritdoc />
    static Complex<T> INumberBase<Complex<T>>.Parse(string s, NumberStyles style, IFormatProvider? provider) =>
        Parse(s, provider);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Complex<T> result) =>
        TryParse(s, provider, out result);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.TryParse(string? s, NumberStyles style, IFormatProvider? provider, out Complex<T> result) =>
        TryParse(s, provider, out result);

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.TryConvertFromChecked<TOther>(TOther value, out Complex<T> result)
    {
        try
        {
            result = new Complex<T>(T.CreateChecked(value), T.Zero);
            return true;
        }
        catch (NotSupportedException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.TryConvertFromSaturating<TOther>(TOther value, out Complex<T> result)
    {
        try
        {
            result = new Complex<T>(T.CreateSaturating(value), T.Zero);
            return true;
        }
        catch (NotSupportedException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.TryConvertFromTruncating<TOther>(TOther value, out Complex<T> result)
    {
        try
        {
            result = new Complex<T>(T.CreateTruncating(value), T.Zero);
            return true;
        }
        catch (NotSupportedException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.TryConvertToChecked<TOther>(Complex<T> value, out TOther result)
    {
        if (!T.IsZero(value.Imaginary))
        {
            result = default!;
            return false;
        }

        try
        {
            result = TOther.CreateChecked(value.Real);
            return true;
        }
        catch (NotSupportedException)
        {
            result = default!;
            return false;
        }
    }

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.TryConvertToSaturating<TOther>(Complex<T> value, out TOther result)
    {
        if (!T.IsZero(value.Imaginary))
        {
            result = default!;
            return false;
        }

        try
        {
            result = TOther.CreateSaturating(value.Real);
            return true;
        }
        catch (NotSupportedException)
        {
            result = default!;
            return false;
        }
    }

    /// <inheritdoc />
    static bool INumberBase<Complex<T>>.TryConvertToTruncating<TOther>(Complex<T> value, out TOther result)
    {
        if (!T.IsZero(value.Imaginary))
        {
            result = default!;
            return false;
        }

        try
        {
            result = TOther.CreateTruncating(value.Real);
            return true;
        }
        catch (NotSupportedException)
        {
            result = default!;
            return false;
        }
    }

    /// <summary>
    /// Determines whether either component of a complex value is an infinity.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> if either component is infinite; otherwise, <see langword="false" />.</returns>
    private static bool IsInfinityCore(Complex<T> value) =>
        T.IsInfinity(value.Real) || T.IsInfinity(value.Imaginary);

    /// <summary>
    /// Determines whether both components of a complex value are finite.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> if both components are finite; otherwise, <see langword="false" />.</returns>
    private static bool IsFiniteCore(Complex<T> value) =>
        T.IsFinite(value.Real) && T.IsFinite(value.Imaginary);
}

#pragma warning restore SA1648 // inheritdoc should be used with inheriting class
