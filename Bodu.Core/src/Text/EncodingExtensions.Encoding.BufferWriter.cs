// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Encoding.BufferWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Writes <paramref name="encoding" />'s preamble (BOM) into <paramref name="writer" />. No bytes are written when
    /// the encoding has no preamble.
    /// </summary>
    /// <param name="encoding">The encoding whose preamble is written.</param>
    /// <param name="writer">The buffer writer to receive the preamble bytes.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> or <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public static void WritePreamble(this System.Text.Encoding encoding, IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(encoding);
        ThrowHelper.ThrowIfNull(writer);

        ReadOnlySpan<byte> preamble = encoding.Preamble;
        if (preamble.IsEmpty) return;

        Span<byte> destination = writer.GetSpan(preamble.Length);
        preamble.CopyTo(destination);
        writer.Advance(preamble.Length);
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> with <paramref name="encoding" /> and writes the bytes into
    /// <paramref name="writer" />.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="writer">The buffer writer to receive the encoded bytes.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> or <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    public static void WriteBytes(
        this System.Text.Encoding encoding,
        ReadOnlySpan<char> chars,
        IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(encoding);
        ThrowHelper.ThrowIfNull(writer);

        int required = encoding.GetByteCount(chars);
        if (required == 0) return;

        Span<byte> destination = writer.GetSpan(required);
        int written = encoding.GetBytes(chars, destination);
        writer.Advance(written);
    }

    /// <summary>
    /// Writes <paramref name="encoding" />'s preamble (BOM) followed by the bytes produced by encoding
    /// <paramref name="chars" /> into <paramref name="writer" />.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="writer">The buffer writer to receive the preamble and encoded bytes.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> or <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    public static void WriteBytesWithPreamble(
        this System.Text.Encoding encoding,
        ReadOnlySpan<char> chars,
        IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(encoding);
        ThrowHelper.ThrowIfNull(writer);

        encoding.WritePreamble(writer);
        encoding.WriteBytes(chars, writer);
    }

    /// <summary>
    /// Decodes <paramref name="bytes" /> with <paramref name="encoding" /> and writes the characters into
    /// <paramref name="writer" />.
    /// </summary>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <param name="bytes">The byte span to decode.</param>
    /// <param name="writer">The buffer writer to receive the decoded characters.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> or <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    public static void WriteChars(
        this System.Text.Encoding encoding,
        ReadOnlySpan<byte> bytes,
        IBufferWriter<char> writer)
    {
        ThrowHelper.ThrowIfNull(encoding);
        ThrowHelper.ThrowIfNull(writer);

        int required = encoding.GetCharCount(bytes);
        if (required == 0) return;

        Span<char> destination = writer.GetSpan(required);
        int written = encoding.GetChars(bytes, destination);
        writer.Advance(written);
    }
}
