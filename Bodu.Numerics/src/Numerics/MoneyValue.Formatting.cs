// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValue.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Numerics;

public readonly partial struct MoneyValue :
    IFormattable,
    ISpanFormattable,
    IUtf8SpanFormattable
{
    /// <summary>
    /// Returns the default string representation: ISO code followed by the amount at the currency's minor-unit
    /// precision under the current culture.
    /// </summary>
    /// <returns>The formatted string.</returns>
    public override string ToString() =>
        Format(default, CultureInfo.CurrentCulture);

    /// <summary>
    /// Returns a string representation using the supplied format specifier.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <returns>The formatted string.</returns>
    public string ToString(string? format) =>
        Format(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// Returns a string representation using the supplied format specifier and culture.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <param name="formatProvider">The culture used to render the numeric component.</param>
    /// <returns>The formatted string.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Format(format, formatProvider);

    /// <summary>
    /// Attempts to format this value into a character span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture provider.</param>
    /// <returns><see langword="true" /> if the span was large enough.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        string text = Format(format, provider);
        if (text.Length <= destination.Length)
        {
            text.AsSpan().CopyTo(destination);
            charsWritten = text.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    /// <summary>
    /// Attempts to format this value into a UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Destination">The destination UTF-8 span.</param>
    /// <param name="bytesWritten">The number of bytes written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture provider.</param>
    /// <returns><see langword="true" /> if the span was large enough.</returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        string text = Format(format, provider);
        return Encoding.UTF8.TryGetBytes(text, utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Formats this amount according to the supplied specifier, mirroring the vocabulary of
    /// <see cref="Money{TCurrency}" />.
    /// </summary>
    /// <param name="format">The format specifier (<c>null</c>, <c>G</c>, <c>C</c>, <c>N</c>, <c>F</c>, <c>D</c> with optional precision suffix).</param>
    /// <param name="provider">The culture used for the numeric portion.</param>
    /// <returns>The formatted string.</returns>
    /// <exception cref="FormatException">The format specifier is not supported.</exception>
    private string Format(ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        char specifier;
        int decimals;
        if (format.IsEmpty)
        {
            specifier = 'G';
            decimals = MinorUnits;
        }
        else
        {
            specifier = char.ToUpperInvariant(format[0]);
            if (format.Length == 1)
            {
                decimals = MinorUnits;
            }
            else
            {
                if (!int.TryParse(format[1..], NumberStyles.None, CultureInfo.InvariantCulture, out decimals) || decimals < 0)
                    throw new FormatException($"The format string '{format.ToString()}' is not supported.");
            }
        }

        IFormatProvider effective = provider ?? CultureInfo.CurrentCulture;
        string suffix = decimals.ToString(CultureInfo.InvariantCulture);

        return specifier switch
        {
            'G' or 'C' => string.Concat(IsoCode, " ", _amount.ToString("N" + suffix, effective)),
            'N' => _amount.ToString("N" + suffix, effective),
            'F' or 'D' => _amount.ToString("F" + suffix, effective),
            _ => throw new FormatException($"The format string '{format.ToString()}' is not supported."),
        };
    }

    /// <summary>
    /// String-based override of <see cref="Format(ReadOnlySpan{char}, IFormatProvider?)" />.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture provider.</param>
    /// <returns>The formatted string.</returns>
    private string Format(string? format, IFormatProvider? provider) =>
        Format(format is null ? default : format.AsSpan(), provider);
}
