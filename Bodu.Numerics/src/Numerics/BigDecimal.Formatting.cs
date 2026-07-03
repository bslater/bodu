// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Text;

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
    : IFormattable, ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>
    /// Returns the plain (non-scientific) decimal string representation of the value using the invariant culture.
    /// </summary>
    /// <returns>The canonical decimal text, for example <c>"0.5"</c>, <c>"100"</c>, or <c>"-3.14"</c>.</returns>
    public override string ToString() =>
        ToString(null, CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats the value using the specified format specifier and format provider.
    /// </summary>
    /// <param name="format">
    /// The format specifier: <c>"G"</c> (or <see langword="null" />) for the plain canonical form, or <c>"F"</c>
    /// optionally followed by a digit count for a fixed number of decimal places.
    /// </param>
    /// <param name="formatProvider">The format provider supplying the decimal separator and sign symbols.</param>
    /// <returns>The formatted text.</returns>
    /// <exception cref="FormatException"><paramref name="format" /> is not a supported specifier.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        NumberFormatInfo info = NumberFormatInfo.GetInstance(formatProvider);

        if (string.IsNullOrEmpty(format))
            return FormatPlain(_mantissa, _scale, info);

        char specifier = char.ToUpperInvariant(format[0]);
        return specifier switch
        {
            'G' => FormatPlain(_mantissa, _scale, info),
            'F' => FormatFixed(format.AsSpan(1), info),
            _ => throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Format_Invalid_FractionFormat, format)),
        };
    }

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        string text = ToString(format.IsEmpty ? null : format.ToString(), provider);
        if (text.Length > destination.Length)
        {
            charsWritten = 0;
            return false;
        }

        text.CopyTo(destination);
        charsWritten = text.Length;
        return true;
    }

    /// <inheritdoc />
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        string text = ToString(format.IsEmpty ? null : format.ToString(), provider);
        return Encoding.UTF8.TryGetBytes(text, utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Builds the plain decimal text for an unscaled value and non-negative scale using the supplied culture symbols.
    /// </summary>
    /// <param name="mantissa">The unscaled value.</param>
    /// <param name="scale">The non-negative scale.</param>
    /// <param name="info">The number-format symbols.</param>
    /// <returns>The plain decimal text.</returns>
    private static string FormatPlain(BigInteger mantissa, int scale, NumberFormatInfo info)
    {
        bool negative = mantissa.Sign < 0;
        string digits = BigInteger.Abs(mantissa).ToString(CultureInfo.InvariantCulture);

        string body;
        if (scale == 0)
        {
            body = digits;
        }
        else
        {
            if (digits.Length <= scale)
                digits = digits.PadLeft(scale + 1, '0');

            int pointIndex = digits.Length - scale;
            body = string.Concat(digits.AsSpan(0, pointIndex), info.NumberDecimalSeparator, digits.AsSpan(pointIndex));
        }

        return negative ? info.NegativeSign + body : body;
    }

    /// <summary>
    /// Formats the value with a fixed number of decimal places, rounding half-to-even and padding with trailing zeros.
    /// </summary>
    /// <param name="precisionText">The optional digit count following the <c>F</c> specifier.</param>
    /// <param name="info">The number-format symbols.</param>
    /// <returns>The fixed-point text.</returns>
    /// <exception cref="FormatException">The precision suffix is not a non-negative integer.</exception>
    private string FormatFixed(ReadOnlySpan<char> precisionText, NumberFormatInfo info)
    {
        int places = _scale;
        if (!precisionText.IsEmpty)
        {
            if (!int.TryParse(precisionText, NumberStyles.None, CultureInfo.InvariantCulture, out places))
                throw new FormatException(
                    string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Format_Invalid_FractionFormat, "F" + precisionText.ToString()));
        }

        BigDecimal rounded = Round(this, places, MidpointRounding.ToEven);

        // Re-expand to exactly `places` fractional digits (canonicalization may have trimmed trailing zeros).
        BigInteger mantissa = rounded._mantissa * BigInteger.Pow(new BigInteger(10), places - rounded._scale);
        return FormatPlain(mantissa, places, info);
    }
}
