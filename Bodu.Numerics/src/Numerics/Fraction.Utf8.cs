// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Utf8.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T> :
    IUtf8SpanParsable<Fraction<T>>
{
    /// <summary>
    /// Parses a UTF-8 byte span into a <see cref="Fraction{T}" /> using the specified culture.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <returns>The rational value the text represents.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="utf8Text" /> is not a valid fraction.</exception>
    public static Fraction<T> Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        Parse(Encoding.UTF8.GetString(utf8Text), provider);

    /// <summary>
    /// Attempts to parse a UTF-8 byte span into a <see cref="Fraction{T}" /> using the specified culture.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <param name="result">When this method returns, contains the parsed value, or zero on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Fraction<T> result) =>
        TryParse(Encoding.UTF8.GetString(utf8Text), provider, out result);
}
