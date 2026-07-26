// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Numerics;

public readonly partial struct Complex<T> :
    IUtf8SpanFormattable
{
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
