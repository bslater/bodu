// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.WriteUtf8To.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="text" /> as UTF-8 and writes the bytes into <paramref name="writer" />.
    /// </summary>
    /// <param name="text">The string to encode.</param>
    /// <param name="writer">The buffer writer to receive the UTF-8 bytes.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> or <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public static void WriteUtf8To(this string text, IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(text);
        ThrowHelper.ThrowIfNull(writer);

        System.Text.Encoding.UTF8.WriteBytes(text.AsSpan(), writer);
    }
}
