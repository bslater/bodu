// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
    : IUtf8SpanParsable<BigDecimal>
{
    /// <summary>
    /// Parses the UTF-8 decimal text representation of a value.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 text to parse.</param>
    /// <param name="provider">The format provider supplying the decimal separator and sign symbols.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="utf8Text" /> is not a valid decimal representation.
    /// </exception>
    public static BigDecimal Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        Parse(Encoding.UTF8.GetString(utf8Text).AsSpan(), provider);

    /// <summary>
    /// Attempts to parse the UTF-8 decimal text representation of a value.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 text to parse.</param>
    /// <param name="provider">The format provider supplying the decimal separator and sign symbols.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out BigDecimal result) =>
        TryParse(Encoding.UTF8.GetString(utf8Text).AsSpan(), provider, out result);
}
