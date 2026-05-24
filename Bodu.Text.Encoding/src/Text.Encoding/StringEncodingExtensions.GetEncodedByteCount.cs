// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.GetEncodedByteCount.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Returns the exact number of bytes produced by encoding <paramref name="text" /> with
    /// <paramref name="encoding" />.
    /// </summary>
    /// <param name="text">The string to measure.</param>
    /// <param name="encoding">The encoding used to compute the byte count.</param>
    /// <returns>The exact number of bytes required to encode <paramref name="text" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> or <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Equivalent to <see cref="System.Text.Encoding.GetByteCount(string)" /> but expressed as an extension so fluent
    /// code can chain from a string variable.
    /// </remarks>
    public static int GetEncodedByteCount(this string text, System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(text);
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetByteCount(text);
    }
}
