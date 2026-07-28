// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
    : IParsable<BigDecimal>, ISpanParsable<BigDecimal>
{
    /// <summary>
    /// Parses the decimal text representation of a value.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException"><paramref name="s" /> is not a valid decimal representation.</exception>
    public static BigDecimal Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses the decimal text representation of a value using the specified format provider.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The format provider supplying the decimal separator and sign symbols.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException"><paramref name="s" /> is not a valid decimal representation.</exception>
    public static BigDecimal Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);

        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses the decimal text representation of a value from a character span.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The format provider supplying the decimal separator and sign symbols.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException"><paramref name="s" /> is not a valid decimal representation.</exception>
    public static BigDecimal Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out BigDecimal result)
            ? result
            : throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Format_Invalid_BigDecimalText, s.ToString()));

    /// <summary>
    /// Attempts to parse the decimal text representation of a value.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The format provider supplying the decimal separator and sign symbols.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out BigDecimal result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Attempts to parse the decimal text representation of a value from a character span.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The format provider supplying the decimal separator and sign symbols.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BigDecimal result)
    {
        result = default;

        NumberFormatInfo format = NumberFormatInfo.GetInstance(provider);
        s = s.Trim();
        if (s.IsEmpty)
            return false;

        int sign = 1;
        if (s.StartsWith(format.NegativeSign, StringComparison.Ordinal))
        {
            sign = -1;
            s = s[format.NegativeSign.Length..];
        }
        else if (s.StartsWith(format.PositiveSign, StringComparison.Ordinal))
        {
            s = s[format.PositiveSign.Length..];
        }

        int exponent = 0;
        int exponentIndex = s.IndexOfAny('e', 'E');
        if (exponentIndex >= 0)
        {
            // The exponent honors the same culture symbols as the mantissa so cultures with a non-ASCII negative sign
            // can round-trip their own scientific notation.
            if (!int.TryParse(s[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign, format, out exponent))
                return false;

            s = s[..exponentIndex];
        }

        ReadOnlySpan<char> separator = format.NumberDecimalSeparator;
        int separatorIndex = s.IndexOf(separator, StringComparison.Ordinal);

        ReadOnlySpan<char> integerPart = separatorIndex >= 0 ? s[..separatorIndex] : s;
        ReadOnlySpan<char> fractionPart = separatorIndex >= 0 ? s[(separatorIndex + separator.Length)..] : default;

        if (integerPart.IsEmpty && fractionPart.IsEmpty)
            return false;

        if (!IsAllDigits(integerPart) || !IsAllDigits(fractionPart))
            return false;

        // The canonical scale folds the parsed exponent into the fractional length. A negative scale widens the
        // value by 10^-scale during construction, so reject an exponent that would drive an unbounded widening here
        // rather than letting the constructor throw (TryParse must not throw on malformed input).
        long scale = (long)fractionPart.Length - exponent;
        if (scale is < -MaxNegativeScaleMagnitude or > int.MaxValue)
            return false;

        // Concatenate the integer and fraction digits without the separator. The scratch buffer is sized from the
        // (attacker-controlled) input length, so it must not be a stackalloc — rent from the pool for large inputs
        // to avoid a stack overflow while still supporting arbitrarily long values.
        int totalDigits = integerPart.Length + fractionPart.Length;
        char[]? rented = null;
        Span<char> digits = totalDigits <= 256
            ? stackalloc char[256]
            : (rented = ArrayPool<char>.Shared.Rent(totalDigits));

        try
        {
            Span<char> target = digits[..totalDigits];
            integerPart.CopyTo(target);
            fractionPart.CopyTo(target[integerPart.Length..]);

            BigInteger magnitude = target.IsEmpty ? BigInteger.Zero : BigInteger.Parse(target, NumberStyles.None, CultureInfo.InvariantCulture);
            result = new BigDecimal(sign < 0 ? -magnitude : magnitude, (int)scale);
            return true;
        }
        finally
        {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Determines whether every character in <paramref name="span" /> is an ASCII decimal digit.
    /// </summary>
    /// <param name="span">The span to test.</param>
    /// <returns>
    /// <see langword="true" /> when the span is empty or all digits; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsAllDigits(ReadOnlySpan<char> span)
    {
        foreach (char c in span)
        {
            if (c is < '0' or > '9')
                return false;
        }

        return true;
    }
}
