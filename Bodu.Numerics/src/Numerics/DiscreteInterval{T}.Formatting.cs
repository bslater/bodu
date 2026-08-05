// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct DiscreteInterval<T> :
    IFormattable,
    ISpanFormattable,
    IUtf8SpanFormattable
{
    /// <summary>
    /// Returns the canonical bracket-notation string representation — always closed on a finite side, using the
    /// infinity glyphs on an unbounded side, and the empty-set glyph for the empty interval.
    /// </summary>
    /// <returns>
    /// <c>"[first, last]"</c> for a bounded interval, <c>"[first, +&#x221E;)"</c> / <c>"(-&#x221E;, last]"</c> /
    /// <c>"(-&#x221E;, +&#x221E;)"</c> for unbounded shapes, or <c>"&#x2205;"</c> when empty.
    /// </returns>
    /// <remarks>
    /// All formatting delegates to the equivalent continuous <see cref="Interval{T}" /> (via <see cref="ToInterval" />),
    /// whose canonical closed form over the same endpoints renders identically.
    /// </remarks>
    public override string ToString() =>
        ToInterval().ToString();

    /// <summary>
    /// Returns a string representation of this interval using the endpoint format specifier <paramref name="format" />
    /// applied to <see cref="First" /> and <see cref="Last" />.
    /// </summary>
    /// <param name="format">
    /// A format specifier accepted by <typeparamref name="T" /> (e.g. <c>"D4"</c>, <c>"N0"</c>).
    /// </param>
    /// <returns>The interval text with each endpoint formatted by <paramref name="format" />.</returns>
    public string ToString(string? format) =>
        ToInterval().ToString(format);

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
        ToInterval().ToString(format, formatProvider);

    /// <summary>
    /// Attempts to format this interval into the provided character span.
    /// </summary>
    /// <param name="destination">The span that receives the formatted characters.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="format">The endpoint format specifier.</param>
    /// <param name="provider">The culture used to render each endpoint.</param>
    /// <returns><see langword="true" /> if formatting succeeded; otherwise, <see langword="false" />.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        ToInterval().TryFormat(destination, out charsWritten, format, provider);

    /// <summary>
    /// Attempts to format this interval into the provided UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Destination">The span that receives the formatted UTF-8 bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="format">The endpoint format specifier.</param>
    /// <param name="provider">The culture used to render each endpoint.</param>
    /// <returns><see langword="true" /> if formatting succeeded; otherwise, <see langword="false" />.</returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        ToInterval().TryFormat(utf8Destination, out bytesWritten, format, provider);
}
