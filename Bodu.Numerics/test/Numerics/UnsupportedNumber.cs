// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnsupportedNumber.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// A minimal <see cref="INumberBase{TSelf}" /> implementation whose conversion hooks always fail, used to drive the
/// defensive <see cref="NotSupportedException" /> handling in <see cref="Fraction{T}" />'s generic-math conversion
/// members. The type reports itself as an integer so a conversion <em>from</em> it routes through
/// <see cref="BigInteger" />, and it refuses every <c>TryConvert</c> request so the framework raises
/// <see cref="NotSupportedException" /> rather than producing a value.
/// </summary>
internal readonly struct UnsupportedNumber : INumberBase<UnsupportedNumber>
{
    /// <summary>
    /// When <see langword="true" />, the value reports itself as non-integer yet finite, and its double conversion
    /// yields positive infinity, so a conversion <em>from</em> it exercises the guard that rejects a finite source
    /// whose floating-point approximation is not finite.
    /// </summary>
    private readonly bool _yieldInfiniteDoubleApproximation;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedNumber" /> struct.
    /// </summary>
    /// <param name="yieldInfiniteDoubleApproximation">
    /// <see langword="true" /> to report a finite, non-integer value whose double approximation is infinite.
    /// </param>
    private UnsupportedNumber(bool yieldInfiniteDoubleApproximation)
    {
        _yieldInfiniteDoubleApproximation = yieldInfiniteDoubleApproximation;
    }

    /// <inheritdoc />
    public static UnsupportedNumber One => default;

    /// <inheritdoc />
    public static UnsupportedNumber Zero => default;

    /// <inheritdoc />
    public static int Radix => 2;

    /// <inheritdoc />
    public static UnsupportedNumber AdditiveIdentity => default;

    /// <inheritdoc />
    public static UnsupportedNumber MultiplicativeIdentity => default;

    /// <inheritdoc />
    public static UnsupportedNumber Abs(UnsupportedNumber value) => default;

    /// <inheritdoc />
    public static bool IsCanonical(UnsupportedNumber value) => true;

    /// <inheritdoc />
    public static bool IsComplexNumber(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsEvenInteger(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsFinite(UnsupportedNumber value) => true;

    /// <inheritdoc />
    public static bool IsImaginaryNumber(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsInfinity(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsInteger(UnsupportedNumber value) => !value._yieldInfiniteDoubleApproximation;

    /// <inheritdoc />
    public static bool IsNaN(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsNegative(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsNegativeInfinity(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsNormal(UnsupportedNumber value) => true;

    /// <inheritdoc />
    public static bool IsOddInteger(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsPositive(UnsupportedNumber value) => true;

    /// <inheritdoc />
    public static bool IsPositiveInfinity(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsRealNumber(UnsupportedNumber value) => true;

    /// <inheritdoc />
    public static bool IsSubnormal(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static bool IsZero(UnsupportedNumber value) => false;

    /// <inheritdoc />
    public static UnsupportedNumber MaxMagnitude(UnsupportedNumber x, UnsupportedNumber y) => default;

    /// <inheritdoc />
    public static UnsupportedNumber MaxMagnitudeNumber(UnsupportedNumber x, UnsupportedNumber y) => default;

    /// <inheritdoc />
    public static UnsupportedNumber MinMagnitude(UnsupportedNumber x, UnsupportedNumber y) => default;

    /// <inheritdoc />
    public static UnsupportedNumber MinMagnitudeNumber(UnsupportedNumber x, UnsupportedNumber y) => default;

    /// <inheritdoc />
    public static UnsupportedNumber Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => default;

    /// <inheritdoc />
    public static UnsupportedNumber Parse(string s, NumberStyles style, IFormatProvider? provider) => default;

    /// <inheritdoc />
    public static UnsupportedNumber Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => default;

    /// <inheritdoc />
    public static UnsupportedNumber Parse(string s, IFormatProvider? provider) => default;

    /// <inheritdoc />
    public static bool TryConvertFromChecked<TOther>(TOther value, [MaybeNullWhen(false)] out UnsupportedNumber result)
        where TOther : INumberBase<TOther>
    {
        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryConvertFromSaturating<TOther>(TOther value, [MaybeNullWhen(false)] out UnsupportedNumber result)
        where TOther : INumberBase<TOther>
    {
        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryConvertFromTruncating<TOther>(TOther value, [MaybeNullWhen(false)] out UnsupportedNumber result)
        where TOther : INumberBase<TOther>
    {
        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryConvertToChecked<TOther>(UnsupportedNumber value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther> =>
        TryConvertToDouble(value, out result);

    /// <inheritdoc />
    public static bool TryConvertToSaturating<TOther>(UnsupportedNumber value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther> =>
        TryConvertToDouble(value, out result);

    /// <inheritdoc />
    public static bool TryConvertToTruncating<TOther>(UnsupportedNumber value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther> =>
        TryConvertToDouble(value, out result);

    /// <summary>
    /// Returns a value configured so that its <see cref="double" /> approximation is positive infinity, used to drive
    /// the finite-source-yet-infinite-approximation guard in <see cref="Fraction{T}" />.
    /// </summary>
    /// <returns>A value whose double conversion is positive infinity.</returns>
    public static UnsupportedNumber WithInfiniteDoubleApproximation() => new(yieldInfiniteDoubleApproximation: true);

    /// <summary>
    /// Yields positive infinity when this value is configured for an infinite double approximation and the requested
    /// type is <see cref="double" />; otherwise reports no supported conversion.
    /// </summary>
    /// <typeparam name="TOther">The requested destination type.</typeparam>
    /// <param name="value">The value being converted.</param>
    /// <param name="result">When this method returns, contains the converted value, or its default on failure.</param>
    /// <returns>
    /// <see langword="true" /> when an infinite double is produced; otherwise, <see langword="false" />.
    /// </returns>
    private static bool TryConvertToDouble<TOther>(UnsupportedNumber value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        if (value._yieldInfiniteDoubleApproximation && typeof(TOther) == typeof(double))
        {
            result = (TOther)(object)double.PositiveInfinity;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out UnsupportedNumber result)
    {
        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out UnsupportedNumber result)
    {
        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out UnsupportedNumber result)
    {
        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out UnsupportedNumber result)
    {
        result = default;
        return false;
    }

    /// <inheritdoc />
    public static UnsupportedNumber operator +(UnsupportedNumber left, UnsupportedNumber right) => default;

    /// <inheritdoc />
    public static UnsupportedNumber operator +(UnsupportedNumber value) => default;

    /// <inheritdoc />
    public static UnsupportedNumber operator -(UnsupportedNumber left, UnsupportedNumber right) => default;

    /// <inheritdoc />
    public static UnsupportedNumber operator -(UnsupportedNumber value) => default;

    /// <inheritdoc />
    public static UnsupportedNumber operator ++(UnsupportedNumber value) => default;

    /// <inheritdoc />
    public static UnsupportedNumber operator --(UnsupportedNumber value) => default;

    /// <inheritdoc />
    public static UnsupportedNumber operator *(UnsupportedNumber left, UnsupportedNumber right) => default;

    /// <inheritdoc />
    public static UnsupportedNumber operator /(UnsupportedNumber left, UnsupportedNumber right) => default;

    /// <inheritdoc />
    public static bool operator ==(UnsupportedNumber left, UnsupportedNumber right) => true;

    /// <inheritdoc />
    public static bool operator !=(UnsupportedNumber left, UnsupportedNumber right) => false;

    /// <inheritdoc />
    public bool Equals(UnsupportedNumber other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is UnsupportedNumber;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => nameof(UnsupportedNumber);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => nameof(UnsupportedNumber);

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        charsWritten = 0;
        return false;
    }

    /// <inheritdoc />
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bytesWritten = 0;
        return false;
    }
}
