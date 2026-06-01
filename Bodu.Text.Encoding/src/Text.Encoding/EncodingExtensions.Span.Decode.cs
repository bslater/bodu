// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Span.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Returns the exact number of characters produced by decoding <paramref name="bytes" /> with
    /// <paramref name="encoding" />.
    /// </summary>
    /// <param name="bytes">The byte span to measure.</param>
    /// <param name="encoding">The encoding used to compute the character count.</param>
    /// <returns>The exact number of characters required to decode <paramref name="bytes" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This routes through <see cref="System.Text.Encoding.GetCharCount(ReadOnlySpan{byte})" /> and walks the input to
    /// produce the exact value, unlike <see cref="System.Text.Encoding.GetMaxCharCount(int)" /> which returns a
    /// worst-case upper bound.
    /// </remarks>
    public static int GetDecodedCharCount(this ReadOnlySpan<byte> bytes, System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetCharCount(bytes);
    }

    /// <summary>
    /// Decodes <paramref name="bytes" /> into a freshly allocated <see cref="char" /> array using
    /// <paramref name="encoding" />.
    /// </summary>
    /// <param name="bytes">The byte span to decode.</param>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <returns>
    /// A new character array whose length is exactly
    /// <see cref="GetDecodedCharCount(ReadOnlySpan{byte}, System.Text.Encoding)" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    public static char[] ToChars(this ReadOnlySpan<byte> bytes, System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        var count = encoding.GetCharCount(bytes);
        if (count == 0) return [];

        var buffer = new char[count];
        encoding.GetChars(bytes, buffer);
        return buffer;
    }

    /// <summary>
    /// Decodes <paramref name="bytes" /> into a new <see cref="string" /> using <paramref name="encoding" />.
    /// </summary>
    /// <param name="bytes">The byte span to decode.</param>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    /// <remarks>
    /// The method is named <c>DecodeToString</c> rather than <c>ToString</c> to avoid shadowing
    /// <see cref="object.ToString" /> when invoked through IntelliSense on a span variable.
    /// </remarks>
    public static string DecodeToString(this ReadOnlySpan<byte> bytes, System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Decodes <paramref name="bytes" /> into <paramref name="destination" /> using <paramref name="encoding" /> and
    /// returns the number of characters written.
    /// </summary>
    /// <param name="bytes">The byte span to decode.</param>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <param name="destination">The destination buffer. Must be large enough to hold the decoded output.</param>
    /// <returns>The number of characters written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the decoded output.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    public static int DecodeTo(
        this ReadOnlySpan<byte> bytes,
        System.Text.Encoding encoding,
        Span<char> destination)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetChars(bytes, destination);
    }

    /// <summary>
    /// Decodes <paramref name="bytes" /> into <paramref name="destination" /> using <paramref name="encoding" /> and
    /// asserts that <paramref name="destination" /> is exactly the size required.
    /// </summary>
    /// <param name="bytes">The byte span to decode.</param>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <param name="destination">The destination buffer. Must be exactly the size required by the encoding.</param>
    /// <returns>
    /// The number of characters written, which equals <paramref name="destination" />.Length on success.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is not exactly the character count required by
    /// <paramref name="encoding" /> for <paramref name="bytes" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    public static int DecodeExactlyTo(
        this ReadOnlySpan<byte> bytes,
        System.Text.Encoding encoding,
        Span<char> destination)
    {
        ThrowHelper.ThrowIfNull(encoding);

        var required = encoding.GetCharCount(bytes);
        return destination.Length == required
            ? encoding.GetChars(bytes, destination)
            : throw new ArgumentException(
                EncodingResourceStrings.Arg_Invalid_DestinationNotExactSizeForDecoded,
                nameof(destination));
    }

    /// <summary>
    /// Attempts to decode <paramref name="bytes" /> into <paramref name="destination" /> using
    /// <paramref name="encoding" /> without throwing when the destination is too small.
    /// </summary>
    /// <param name="bytes">The byte span to decode.</param>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">
    /// When this method returns <see langword="true" />, contains the number of characters written; otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the decoding completed successfully; <see langword="false" /> when
    /// <paramref name="destination" /> is too small.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    public static bool TryDecodeTo(
        this ReadOnlySpan<byte> bytes,
        System.Text.Encoding encoding,
        Span<char> destination,
        out int charsWritten)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.TryGetChars(bytes, destination, out charsWritten);
    }
}
