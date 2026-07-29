// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval{T}.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Numerics;

public readonly partial struct Interval<T> :
    IUtf8SpanParsable<Interval<T>>
{
    /// <summary>
    /// Parses a UTF-8 byte span containing ISO 31-11 bracket notation into an <see cref="Interval{T}" />.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to parse.</param>
    /// <param name="provider">The culture used to parse each endpoint.</param>
    /// <returns>The parsed interval.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="utf8Text" /> is not a valid interval representation.
    /// </exception>
    public static Interval<T> Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        Parse(Encoding.UTF8.GetString(utf8Text).AsSpan(), provider);

    /// <summary>
    /// Attempts to parse a UTF-8 byte span containing ISO 31-11 bracket notation into an <see cref="Interval{T}" />.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to parse.</param>
    /// <param name="provider">The culture used to parse each endpoint.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the parsed interval; otherwise the default value.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Interval<T> result) =>
        TryParse(Encoding.UTF8.GetString(utf8Text).AsSpan(), provider, out result);
}
