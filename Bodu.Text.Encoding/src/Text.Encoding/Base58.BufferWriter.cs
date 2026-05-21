// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58.BufferWriter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base58
{
    /// <summary>
    /// Encodes <paramref name="source" /> as Base58 characters into <paramref name="writer" />, suitable for use in
    /// pipelines and other <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the encoded characters.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static int Encode(ReadOnlySpan<byte> source, IBufferWriter<char> writer, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (source.IsEmpty)
            return 0;

        var upper = GetMaxEncodedLength(source.Length);
        Span<char> destination = writer.GetSpan(upper);
        var written = Encode(source, destination, variant);
        writer.Advance(written);
        return written;
    }

    /// <summary>
    /// Encodes <paramref name="source" /> as UTF-8 Base58 bytes into <paramref name="writer" />, suitable for use in
    /// pipelines and other <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the UTF-8 bytes.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns>The number of UTF-8 bytes written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static int EncodeToUtf8(ReadOnlySpan<byte> source, IBufferWriter<byte> writer, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (source.IsEmpty)
            return 0;

        var utf8 = EncodeToUtf8(source, variant);
        Span<byte> destination = writer.GetSpan(utf8.Length);
        utf8.CopyTo(destination);
        writer.Advance(utf8.Length);
        return utf8.Length;
    }
}
