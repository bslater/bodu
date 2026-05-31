// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Span.Utf8.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Returns the exact number of UTF-8 bytes required to encode <paramref name="chars" />.
    /// </summary>
    /// <param name="chars">The character span to measure.</param>
    /// <returns>The exact UTF-8 byte count.</returns>
    public static int GetUtf8ByteCount(this ReadOnlySpan<char> chars) =>
        System.Text.Encoding.UTF8.GetByteCount(chars);

    /// <summary>
    /// Encodes <paramref name="chars" /> to a freshly allocated UTF-8 byte array.
    /// </summary>
    /// <param name="chars">The character span to encode.</param>
    /// <returns>A new byte array containing the UTF-8 representation of <paramref name="chars" />.</returns>
    /// <remarks>
    /// Equivalent to <c>chars.ToBytes(Encoding.UTF8)</c>; provided as a discoverable shortcut and to avoid the
    /// <see cref="System.Text.Encoding" /> indirection at the call site for the very common UTF-8 case.
    /// </remarks>
    public static byte[] ToUtf8Bytes(this ReadOnlySpan<char> chars)
    {
        var count = System.Text.Encoding.UTF8.GetByteCount(chars);
        if (count == 0) return Array.Empty<byte>();

        var buffer = new byte[count];
        System.Text.Encoding.UTF8.GetBytes(chars, buffer);
        return buffer;
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> as UTF-8 into <paramref name="destination" /> and returns the number of bytes
    /// written.
    /// </summary>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="destination">The destination buffer. Must be large enough to hold the UTF-8 encoded output.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the UTF-8 encoded output.
    /// </exception>
    public static int EncodeUtf8To(this ReadOnlySpan<char> chars, Span<byte> destination) =>
        System.Text.Encoding.UTF8.GetBytes(chars, destination);

    /// <summary>
    /// Attempts to encode <paramref name="chars" /> as UTF-8 into <paramref name="destination" /> without throwing when
    /// the destination is too small.
    /// </summary>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="bytesWritten">
    /// When this method returns <see langword="true" />, contains the number of bytes written; otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the encoding completed successfully; <see langword="false" /> when
    /// <paramref name="destination" /> is too small.
    /// </returns>
    public static bool TryEncodeUtf8To(
        this ReadOnlySpan<char> chars,
        Span<byte> destination,
        out int bytesWritten) =>
        System.Text.Encoding.UTF8.TryGetBytes(chars, destination, out bytesWritten);

    /// <summary>
    /// Decodes <paramref name="bytes" /> as UTF-8 into a new <see cref="string" />.
    /// </summary>
    /// <param name="bytes">The UTF-8 encoded byte span.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="bytes" /> contains an invalid UTF-8 sequence (only when the default
    /// <see cref="System.Text.Encoding.UTF8" /> instance has been configured with exception fallbacks).
    /// </exception>
    public static string FromUtf8(this ReadOnlySpan<byte> bytes) =>
        System.Text.Encoding.UTF8.GetString(bytes);

    /// <summary>
    /// Decodes <paramref name="bytes" /> as UTF-8 into <paramref name="destination" /> and returns the number of
    /// characters written.
    /// </summary>
    /// <param name="bytes">The UTF-8 encoded byte span.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The number of characters written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the decoded output.
    /// </exception>
    public static int DecodeUtf8To(this ReadOnlySpan<byte> bytes, Span<char> destination) =>
        System.Text.Encoding.UTF8.GetChars(bytes, destination);

    /// <summary>
    /// Attempts to decode <paramref name="bytes" /> as UTF-8 into <paramref name="destination" /> without throwing when
    /// the destination is too small.
    /// </summary>
    /// <param name="bytes">The UTF-8 encoded byte span.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// When this method returns <see langword="true" />, contains the number of characters written; otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the decoding completed successfully; <see langword="false" /> when
    /// <paramref name="destination" /> is too small.
    /// </returns>
    public static bool TryDecodeUtf8To(
        this ReadOnlySpan<byte> bytes,
        Span<char> destination,
        out int charsWritten) =>
        System.Text.Encoding.UTF8.TryGetChars(bytes, destination, out charsWritten);
}
