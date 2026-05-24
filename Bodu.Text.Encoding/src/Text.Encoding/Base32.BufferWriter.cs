// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32.BufferWriter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base32
{
    /// <summary>
    /// Encodes <paramref name="source" /> as Base32 characters into <paramref name="writer" />, suitable for use in
    /// pipelines and other <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the encoded characters.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">
    /// Formatting options. Only <see cref="BaseFormattingOptions.OmitPadding" /> has an effect on this overload;
    /// line-break and spacing flags are not supported.
    /// </param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains an unsupported flag.
    /// </exception>
    public static int Encode(ReadOnlySpan<byte> source, IBufferWriter<char> writer, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(writer);
        EnsureUtf8EncodeOptionsSupported(options);

        if (source.IsEmpty)
            return 0;

        var required = GetEncodedLength(source.Length, variant, options);
        Span<char> destination = writer.GetSpan(required);
        var written = Encode(source, destination, variant, options);
        writer.Advance(written);
        return written;
    }

    /// <summary>
    /// Encodes <paramref name="source" /> as UTF-8 Base32 bytes into <paramref name="writer" />, suitable for use in
    /// pipelines and other <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the UTF-8 bytes.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">
    /// Formatting options. Only <see cref="BaseFormattingOptions.OmitPadding" /> is supported.
    /// </param>
    /// <returns>The number of UTF-8 bytes written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains an unsupported flag.
    /// </exception>
    public static int EncodeToUtf8(ReadOnlySpan<byte> source, IBufferWriter<byte> writer, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(writer);
        EnsureUtf8EncodeOptionsSupported(options);

        if (source.IsEmpty)
            return 0;

        var required = GetEncodedLength(source.Length, variant, options);
        Span<byte> destination = writer.GetSpan(required);
        var written = EncodeIntoUtf8Span(source, destination, variant, options);
        writer.Advance(written);
        return written;
    }
}
