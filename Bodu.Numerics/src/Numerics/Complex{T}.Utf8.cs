// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Numerics;

public readonly partial struct Complex<T> :
    IUtf8SpanFormattable,
    IUtf8SpanParsable<Complex<T>>
{
    /// <summary>
    /// Parses a UTF-8 byte span into a <see cref="Complex{T}" /> using the specified culture.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <returns>The complex value the text represents.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="utf8Text" /> is not a valid complex-number representation.
    /// </exception>
    public static Complex<T> Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        Parse(Encoding.UTF8.GetString(utf8Text).AsSpan(), provider);

    /// <summary>
    /// Attempts to parse a UTF-8 byte span into a <see cref="Complex{T}" /> using the specified culture.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to parse.</param>
    /// <param name="provider">The culture used to interpret the numeric components.</param>
    /// <param name="result">When this method returns, contains the parsed value, or the default value on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Complex<T> result) =>
        TryParse(Encoding.UTF8.GetString(utf8Text).AsSpan(), provider, out result);

    /// <summary>
    /// Attempts to format this complex value into the provided UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Destination">The span that receives the formatted UTF-8 bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="format">The numeric format specifier applied to each component.</param>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns><see langword="true" /> if formatting succeeded; otherwise, <see langword="false" />.</returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        string text = Format(format.IsEmpty ? null : format.ToString(), provider);
        return Encoding.UTF8.TryGetBytes(text, utf8Destination, out bytesWritten);
    }
}
