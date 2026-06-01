// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16.BufferWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base16
{
    /// <summary>
    /// Encodes <paramref name="source" /> as hexadecimal characters into <paramref name="writer" />, suitable for use
    /// in pipelines and other <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the encoded characters.</param>
    /// <param name="options">
    /// Formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is supported on this overload —
    /// formatted output (spacing, prefix, line breaks) is not compatible with the writer's contiguous span contract.
    /// For decorated output, call <see cref="Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> and write the
    /// resulting string explicitly.
    /// </param>
    /// <returns>The number of characters written to <paramref name="writer" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.UpperCase" />.
    /// </exception>
    public static int Encode(ReadOnlySpan<byte> source, IBufferWriter<char> writer, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(writer);
        EnsureSpanOutputOptionsSupported(options);

        if (source.IsEmpty)
            return 0;

        var required = source.Length * 2;
        Span<char> destination = writer.GetSpan(required);
        EncodeToHexCore(source, destination, options.HasFlag(BaseFormattingOptions.UpperCase));
        writer.Advance(required);
        return required;
    }

    /// <summary>
    /// Encodes <paramref name="source" /> as UTF-8 hexadecimal bytes into <paramref name="writer" />, suitable for use
    /// in pipelines and other <see cref="IBufferWriter{T}" />-based scenarios.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="writer">The buffer writer that receives the UTF-8 hexadecimal bytes.</param>
    /// <param name="options">
    /// Formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is supported on this overload.
    /// </param>
    /// <returns>The number of UTF-8 bytes written to <paramref name="writer" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.UpperCase" />.
    /// </exception>
    public static int EncodeToUtf8(ReadOnlySpan<byte> source, IBufferWriter<byte> writer, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(writer);
        EnsureSpanOutputOptionsSupported(options);

        if (source.IsEmpty)
            return 0;

        var required = source.Length * 2;
        Span<byte> destination = writer.GetSpan(required);
        EncodeToUtf8Core(source, destination, options.HasFlag(BaseFormattingOptions.UpperCase));
        writer.Advance(required);
        return required;
    }
}
