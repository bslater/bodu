// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Numerics;

public readonly partial struct Money<TCurrency> :
    IFormattable,
    ISpanFormattable,
    IUtf8SpanFormattable
{
    /// <summary>
    /// Returns the default string representation: the ISO 4217 code followed by the amount with the currency's
    /// minor-unit precision, using thousand separators from the current culture.
    /// </summary>
    /// <returns>A string such as <c>"USD 1,234.56"</c>, <c>"JPY 100"</c>, or <c>"BHD 12.345"</c>.</returns>
    public override string ToString() =>
        Format(default, CultureInfo.CurrentCulture);

    /// <summary>
    /// Returns a string representation of this amount using the supplied format specifier.
    /// </summary>
    /// <param name="format">The format specifier; see <see cref="Format(ReadOnlySpan{char}, IFormatProvider?)" /> for the supported vocabulary.</param>
    /// <returns>The formatted representation.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported specifier.</exception>
    public string ToString(string? format) =>
        Format(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// Returns a string representation of this amount using the supplied format specifier and culture.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <param name="formatProvider">The culture used to render the numeric component.</param>
    /// <returns>The formatted representation.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported specifier.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Format(format, formatProvider);

    /// <summary>
    /// Attempts to format this amount into the provided character span.
    /// </summary>
    /// <param name="destination">The span that receives the formatted characters.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture used to render the numeric component.</param>
    /// <returns><see langword="true" /> when <paramref name="destination" /> was large enough; otherwise <see langword="false" />.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported specifier.</exception>
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
    /// Attempts to format this amount into the provided UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Destination">The span that receives the formatted UTF-8 bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture used to render the numeric component.</param>
    /// <returns><see langword="true" /> when <paramref name="utf8Destination" /> was large enough; otherwise <see langword="false" />.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported specifier.</exception>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        string text = Format(format, provider);
        return Encoding.UTF8.TryGetBytes(text, utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Formats this amount according to the supplied specifier.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Supported forms:
    /// <list type="bullet">
    ///   <item><description><c>null</c>, <c>""</c>, <c>"G"</c>, or <c>"C"</c> — the ISO code followed by the amount with minor-unit precision and thousand separators (e.g. <c>"USD 1,234.56"</c>).</description></item>
    ///   <item><description><c>"G"</c> or <c>"C"</c> with a numeric suffix (<c>"C4"</c>) — the same form with an explicit fractional-digit count.</description></item>
    ///   <item><description><c>"N"</c>, <c>"F"</c>, or <c>"D"</c> — bare numeric form without the ISO code. <c>"N"</c> includes thousand separators; <c>"F"</c> and <c>"D"</c> do not.</description></item>
    ///   <item><description>Any of the above with a numeric suffix (<c>"N4"</c>, <c>"F0"</c>) — explicit fractional-digit count.</description></item>
    /// </list>
    /// </param>
    /// <param name="provider">The culture used to render the numeric component.</param>
    /// <returns>The formatted representation.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported specifier.</exception>
    private string Format(ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        char specifier;
        int decimals;
        if (format.IsEmpty)
        {
            specifier = 'G';
            decimals = TCurrency.MinorUnits;
        }
        else
        {
            specifier = char.ToUpperInvariant(format[0]);

            if (format.Length == 1)
            {
                decimals = TCurrency.MinorUnits;
            }
            else
            {
                ReadOnlySpan<char> precisionPart = format[1..];
                if (!int.TryParse(precisionPart, NumberStyles.None, CultureInfo.InvariantCulture, out decimals)
                    || decimals < 0)
                {
                    throw new FormatException($"The format string '{format.ToString()}' is not supported.");
                }
            }
        }

        IFormatProvider effectiveProvider = provider ?? CultureInfo.CurrentCulture;
        return specifier switch
        {
            'G' or 'C' => string.Concat(TCurrency.IsoCode, " ", _amount.ToString("N" + decimals.ToString(CultureInfo.InvariantCulture), effectiveProvider)),
            'N' => _amount.ToString("N" + decimals.ToString(CultureInfo.InvariantCulture), effectiveProvider),
            'F' or 'D' => _amount.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), effectiveProvider),
            _ => throw new FormatException($"The format string '{format.ToString()}' is not supported."),
        };
    }

    /// <summary>
    /// Formats this amount using the supplied string specifier, delegating to the span-based implementation.
    /// </summary>
    /// <param name="format">The format specifier.</param>
    /// <param name="provider">The culture used to render the numeric component.</param>
    /// <returns>The formatted representation.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported specifier.</exception>
    private string Format(string? format, IFormatProvider? provider) =>
        Format(format is null ? default : format.AsSpan(), provider);
}
