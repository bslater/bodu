// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45.BufferWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base45
{
    /// <summary>
    /// Encodes <paramref name="source" /> as Base45 characters into <paramref name="writer" />, for pipelines and other
    /// <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the encoded characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public static int Encode(ReadOnlySpan<byte> source, IBufferWriter<char> writer)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (source.IsEmpty)
            return 0;

        int length = GetEncodedLength(source.Length);
        EncodeInto(source, writer.GetSpan(length)[..length]);
        writer.Advance(length);
        return length;
    }

    /// <summary>
    /// Encodes <paramref name="source" /> as UTF-8 Base45 text into <paramref name="writer" />, for pipelines and other
    /// <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the UTF-8 bytes.</param>
    /// <returns>The number of UTF-8 bytes written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public static int EncodeToUtf8(ReadOnlySpan<byte> source, IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (source.IsEmpty)
            return 0;

        int length = GetEncodedLength(source.Length);
        WriteUtf8(source, writer.GetSpan(length)[..length]);
        writer.Advance(length);
        return length;
    }
}
