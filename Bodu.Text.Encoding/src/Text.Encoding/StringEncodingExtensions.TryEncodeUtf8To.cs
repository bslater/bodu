// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.TryEncodeUtf8To.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Attempts to encode <paramref name="text" /> as UTF-8 into <paramref name="destination" /> without throwing when
    /// the destination is too small.
    /// </summary>
    /// <param name="text">The string to encode.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="bytesWritten">
    /// When this method returns <see langword="true" />, contains the number of bytes written; otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the encoding completed successfully; <see langword="false" /> when
    /// <paramref name="destination" /> is too small.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    public static bool TryEncodeUtf8To(
        this string text,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(text);

        return System.Text.Encoding.UTF8.TryGetBytes(text.AsSpan(), destination, out bytesWritten);
    }
}
