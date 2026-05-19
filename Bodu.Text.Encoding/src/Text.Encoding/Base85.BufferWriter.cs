// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.BufferWriter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base85
{

    /// <summary>
    /// Encodes <paramref name="source" /> as Base85 characters into <paramref name="writer" />, suitable for use in
    /// pipelines and other <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the encoded characters.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="options">
    /// Formatting options. See <see cref="Encode(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />.
    /// </param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the source length is not a
    /// multiple of four bytes.
    /// </exception>
    public static int Encode(ReadOnlySpan<byte> source, IBufferWriter<char> writer, Base85Variant variant = Base85Variant.Ascii85, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(writer);
        EnsureValidVariant(variant);

        var emitDelimiters = ShouldEmitAscii85Delimiters(variant, options);

        if (source.IsEmpty)
        {
            if (!emitDelimiters)
                return 0;

            Span<char> empty = writer.GetSpan(4);
            empty[0] = '<';
            empty[1] = '~';
            empty[2] = '~';
            empty[3] = '>';
            writer.Advance(4);
            return 4;
        }

        var upper = GetMaxEncodedLength(source.Length, variant, options);
        Span<char> destination = writer.GetSpan(upper);
        var written = Encode(source, destination, variant, options);
        writer.Advance(written);
        return written;
    }

    /// <summary>
    /// Encodes <paramref name="source" /> as UTF-8 Base85 bytes into <paramref name="writer" />, suitable for use in
    /// pipelines and other <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the UTF-8 bytes.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <returns>The number of UTF-8 bytes written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the source length is not a
    /// multiple of four bytes.
    /// </exception>
    public static int EncodeToUtf8(ReadOnlySpan<byte> source, IBufferWriter<byte> writer, Base85Variant variant = Base85Variant.Ascii85)
    {
        ThrowHelper.ThrowIfNull(writer);
        EnsureValidVariant(variant);

        if (source.IsEmpty)
            return 0;

        var utf8 = EncodeToUtf8(source, variant);
        Span<byte> destination = writer.GetSpan(utf8.Length);
        utf8.CopyTo(destination);
        writer.Advance(utf8.Length);
        return utf8.Length;
    }
}
