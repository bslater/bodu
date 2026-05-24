// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.GetUtf8ByteCount.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Returns the exact number of UTF-8 bytes required to encode <paramref name="text" />.
    /// </summary>
    /// <param name="text">The string to measure.</param>
    /// <returns>The exact UTF-8 byte count.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    public static int GetUtf8ByteCount(this string text)
    {
        ThrowHelper.ThrowIfNull(text);

        return System.Text.Encoding.UTF8.GetByteCount(text);
    }
}
