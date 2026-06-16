// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Encoding.Preamble.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> emits a byte-order-mark preamble.
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when <see cref="System.Text.Encoding.Preamble" /> is non-empty; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool HasPreamble(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return !encoding.Preamble.IsEmpty;
    }

    /// <summary>
    /// Returns the length in bytes of <paramref name="encoding" />'s preamble.
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns>The preamble length in bytes; zero when the encoding has no preamble.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static int GetPreambleLength(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.Preamble.Length;
    }

    /// <summary>
    /// Attempts to write <paramref name="encoding" />'s preamble into <paramref name="destination" />.
    /// </summary>
    /// <param name="encoding">The encoding whose preamble is written.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="bytesWritten">
    /// When this method returns <see langword="true" />, contains the number of preamble bytes written; otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the preamble fit (including the zero-length case when the encoding has no preamble);
    /// <see langword="false" /> when <paramref name="destination" /> is too small.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool TryWritePreamble(
        this System.Text.Encoding encoding,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(encoding);

        ReadOnlySpan<byte> preamble = encoding.Preamble;
        if (destination.Length < preamble.Length)
        {
            bytesWritten = 0;
            return false;
        }

        preamble.CopyTo(destination);
        bytesWritten = preamble.Length;
        return true;
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="bytes" /> begins with the preamble of
    /// <paramref name="encoding" />.
    /// </summary>
    /// <param name="encoding">The encoding whose preamble is compared.</param>
    /// <param name="bytes">The byte span to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="bytes" /> starts with the preamble; <see langword="false" /> when
    /// the encoding has no preamble or when the bytes do not match.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool StartsWithPreamble(this System.Text.Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        ThrowHelper.ThrowIfNull(encoding);

        ReadOnlySpan<byte> preamble = encoding.Preamble;
        return !preamble.IsEmpty && bytes.StartsWith(preamble);
    }

    /// <summary>
    /// Returns <paramref name="bytes" /> with <paramref name="encoding" />'s preamble removed if present.
    /// </summary>
    /// <param name="encoding">The encoding whose preamble is stripped.</param>
    /// <param name="bytes">The byte span to inspect.</param>
    /// <returns>
    /// A <see cref="ReadOnlySpan{T}" /> equal to <paramref name="bytes" /> when no preamble is present, or the sub-span
    /// starting after the preamble when one is matched at the start.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static ReadOnlySpan<byte> StripPreamble(this System.Text.Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        ThrowHelper.ThrowIfNull(encoding);

        ReadOnlySpan<byte> preamble = encoding.Preamble;
        return preamble.IsEmpty || !bytes.StartsWith(preamble)
            ? bytes
            : bytes.Slice(preamble.Length);
    }

    /// <summary>
    /// Decodes <paramref name="bytes" /> after skipping <paramref name="encoding" />'s preamble.
    /// </summary>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <param name="bytes">The byte span to decode.</param>
    /// <returns>The decoded string with any matching preamble removed before decoding.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static string GetStringSkippingPreamble(this System.Text.Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetString(encoding.StripPreamble(bytes));
    }

    /// <summary>
    /// Returns the exact number of bytes required to encode <paramref name="chars" /> together with
    /// <paramref name="encoding" />'s preamble.
    /// </summary>
    /// <param name="encoding">The encoding used to compute the byte count.</param>
    /// <param name="chars">The character span to measure.</param>
    /// <returns>The preamble length plus the encoded byte count for <paramref name="chars" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static int GetByteCountWithPreamble(this System.Text.Encoding encoding, ReadOnlySpan<char> chars)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.Preamble.Length + encoding.GetByteCount(chars);
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> into a freshly allocated byte array preceded by <paramref name="encoding" />'s
    /// preamble.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
    /// <returns>A new byte array containing the preamble followed by the encoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    public static byte[] GetBytesWithPreamble(this System.Text.Encoding encoding, ReadOnlySpan<char> chars)
    {
        ThrowHelper.ThrowIfNull(encoding);

        ReadOnlySpan<byte> preamble = encoding.Preamble;
        int encodedCount = encoding.GetByteCount(chars);
        int total = preamble.Length + encodedCount;
        if (total == 0) return [];

        byte[] buffer = new byte[total];
        if (!preamble.IsEmpty) preamble.CopyTo(buffer);
        if (encodedCount > 0) encoding.GetBytes(chars, buffer.AsSpan(preamble.Length));

        return buffer;
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> into <paramref name="destination" /> preceded by <paramref name="encoding" />
    /// 's preamble and returns the total number of bytes written.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="destination">
    /// The destination buffer. Must be large enough to hold preamble plus encoded output.
    /// </param>
    /// <returns>The preamble length plus the encoded byte count.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the preamble and encoded bytes.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    public static int GetBytesWithPreamble(
        this System.Text.Encoding encoding,
        ReadOnlySpan<char> chars,
        Span<byte> destination)
    {
        ThrowHelper.ThrowIfNull(encoding);

        ReadOnlySpan<byte> preamble = encoding.Preamble;
        int encodedCount = encoding.GetByteCount(chars);
        int total = preamble.Length + encodedCount;

        if (destination.Length < total)
            throw new ArgumentException(
                ResourceStrings.Arg_Invalid_DestinationTooSmallForEncoded,
                nameof(destination));

        if (!preamble.IsEmpty) preamble.CopyTo(destination);
        if (encodedCount > 0) encoding.GetBytes(chars, destination.Slice(preamble.Length));

        return total;
    }

    /// <summary>
    /// Attempts to encode <paramref name="chars" /> into <paramref name="destination" /> preceded by
    /// <paramref name="encoding" />'s preamble without throwing when the destination is too small.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="bytesWritten">
    /// When this method returns <see langword="true" />, contains the total number of bytes written (preamble plus
    /// encoded output); otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the preamble and encoded bytes fit; <see langword="false" /> when
    /// <paramref name="destination" /> is too small.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    public static bool TryGetBytesWithPreamble(
        this System.Text.Encoding encoding,
        ReadOnlySpan<char> chars,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(encoding);

        ReadOnlySpan<byte> preamble = encoding.Preamble;
        int encodedCount = encoding.GetByteCount(chars);
        int total = preamble.Length + encodedCount;
        if (destination.Length < total)
        {
            bytesWritten = 0;
            return false;
        }

        if (!preamble.IsEmpty) preamble.CopyTo(destination);
        if (encodedCount > 0) encoding.GetBytes(chars, destination.Slice(preamble.Length));
        bytesWritten = total;

        return true;
    }
}
