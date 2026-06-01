// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Numerics;

public readonly partial struct Interval<T> :
    IFormattable,
    ISpanFormattable,
    IUtf8SpanFormattable
{
    /// <summary>
    /// The string representation used for any empty interval.
    /// </summary>
    private const string EmptyText = "∅";

    /// <summary>
    /// Returns the default string representation of this interval using ISO 31-11 bracket notation.
    /// </summary>
    /// <returns>
    /// <c>"[lower, upper]"</c> for closed-closed intervals, <c>"(lower, upper)"</c> for open-open intervals,
    /// <c>"[lower, upper)"</c> for closed-open intervals, <c>"(lower, upper]"</c> for open-closed intervals, or
    /// <c>"∅"</c> (the empty-set glyph) for any empty interval.
    /// </returns>
    public override string ToString() =>
        Format(default, CultureInfo.CurrentCulture);

    /// <summary>
    /// Returns a string representation of this interval using the endpoint format specifier <paramref name="format" />
    /// applied to <see cref="Lower" /> and <see cref="Upper" />.
    /// </summary>
    /// <param name="format">
    /// A format specifier accepted by <typeparamref name="T" /> (e.g. <c>"F2"</c>, <c>"N"</c>).
    /// </param>
    /// <returns>The interval text with each endpoint formatted by <paramref name="format" />.</returns>
    public string ToString(string? format) =>
        Format(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// Returns a string representation of this interval using the supplied endpoint format and culture.
    /// </summary>
    /// <param name="format">A format specifier accepted by <typeparamref name="T" />.</param>
    /// <param name="formatProvider">The culture used to render each endpoint.</param>
    /// <returns>
    /// The interval text with each endpoint formatted by <paramref name="format" /> under
    /// <paramref name="formatProvider" />.
    /// </returns>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Format(format, formatProvider);

    /// <summary>
    /// Attempts to format this interval into the provided character span.
    /// </summary>
    /// <param name="destination">The span that receives the formatted characters.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="format">The endpoint format specifier.</param>
    /// <param name="provider">The culture used to render each endpoint.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="destination" /> was large enough; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var text = Format(format.IsEmpty ? null : format.ToString(), provider);
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
    /// Attempts to format this interval into the provided UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Destination">The span that receives the formatted UTF-8 bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="format">The endpoint format specifier.</param>
    /// <param name="provider">The culture used to render each endpoint.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="utf8Destination" /> was large enough; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var text = Format(format.IsEmpty ? null : format.ToString(), provider);
        return Encoding.UTF8.TryGetBytes(text, utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Renders the interval to a string using the supplied endpoint format and culture.
    /// </summary>
    /// <param name="format">
    /// The format specifier applied to each endpoint, or <see langword="null" /> for the default.
    /// </param>
    /// <param name="provider">The culture used to render each endpoint.</param>
    /// <returns>The bracketed interval text or the empty-set glyph.</returns>
    private string Format(string? format, IFormatProvider? provider)
    {
        if (IsEmpty)
            return EmptyText;

        var lowerBracket = LowerInclusive ? '[' : '(';
        var upperBracket = UpperInclusive ? ']' : ')';
        var lowerText = ((IFormattable)_lower).ToString(format, provider);
        var upperText = ((IFormattable)_upper).ToString(format, provider);
        return $"{lowerBracket}{lowerText}, {upperText}{upperBracket}";
    }
}
