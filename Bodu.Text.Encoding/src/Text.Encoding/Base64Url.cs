// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Url.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides the RFC 4648 §5 URL- and filename-safe Base64 encoding as a first-class type, mirroring the shape of
/// <c>System.Buffers.Text.Base64Url</c> introduced in .NET 9. URL-safe Base64 swaps <c>+</c> and <c>/</c> for <c>-</c>
/// and <c>_</c>, and conventionally omits trailing <c>=</c> padding (matching JWT and OAuth practice).
/// </summary>
/// <remarks>
/// This is a convenience wrapper over <see cref="Base64" /> with <see cref="Base64Variant.UrlSafe" /> pre-bound. Code
/// that knows it wants URL-safe Base64 should prefer this type over passing the variant explicitly to every call. Both
/// forms produce identical output.
/// </remarks>
/// <example>
///<![CDATA[
/// // Encode a JWT segment — URL-safe alphabet, no padding.
/// byte[] header   = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}"u8.ToArray();
/// string segment  = Base64Url.Encode(header);                                  // "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9"
///
/// // Decoder accepts inputs both with and without trailing '=' padding.
/// byte[] roundtrip = Base64Url.Decode(segment);
/// byte[] padded    = Base64Url.Decode("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9==");
///]]>
/// </example>
public static class Base64Url
{
    /// <summary>
    /// Decodes <paramref name="s" /> as a URL-safe Base64 string. Accepts inputs both with and without trailing
    /// <c>=</c> padding (the JWT / OAuth convention is to omit it).
    /// </summary>
    /// <param name="s">The URL-safe Base64 input.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid URL-safe Base64.</exception>
    public static byte[] Decode(string s) =>
        Base64.Decode(s, Base64Variant.UrlSafe, BaseFormatStyles.AllowMissingPadding);

    /// <summary>
    /// Decodes <paramref name="chars" /> as a URL-safe Base64 character span.
    /// </summary>
    /// <param name="chars">The URL-safe Base64 character span.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not valid URL-safe Base64.</exception>
    public static byte[] Decode(ReadOnlySpan<char> chars) =>
        Base64.Decode(chars, Base64Variant.UrlSafe, BaseFormatStyles.AllowMissingPadding);

    /// <summary>
    /// Decodes UTF-8 URL-safe Base64 bytes into a byte array.
    /// </summary>
    /// <param name="utf8Source">The UTF-8 URL-safe Base64 source.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not valid URL-safe Base64.</exception>
    public static byte[] Decode(ReadOnlySpan<byte> utf8Source)
    {
        if (utf8Source.IsEmpty)
            return Array.Empty<byte>();

        var destination = new byte[Base64.GetMaxDecodedLength(utf8Source.Length)];
        OperationStatus status = Base64.DecodeFromUtf8(
            utf8Source, destination, out _, out var bytesWritten,
            Base64Variant.UrlSafe, BaseFormatStyles.AllowMissingPadding, isFinalBlock: true);

        if (status != OperationStatus.Done)
            throw new FormatException(EncodingResourceStrings.Format_Invalid_UrlSafeBase64);

        if (bytesWritten == destination.Length)
            return destination;

        var trimmed = new byte[bytesWritten];
        Buffer.BlockCopy(destination, 0, trimmed, 0, bytesWritten);
        return trimmed;
    }

    /// <summary>
    /// Encodes <paramref name="bytes" /> into a URL-safe Base64 string without padding.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The URL-safe Base64 string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    public static string Encode(byte[] bytes) =>
        Base64.Encode(bytes, Base64Variant.UrlSafe);

    /// <summary>
    /// Encodes <paramref name="bytes" /> into a URL-safe Base64 string without padding.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The URL-safe Base64 string.</returns>
    public static string Encode(ReadOnlySpan<byte> bytes) =>
        Base64.Encode(bytes, Base64Variant.UrlSafe);

    /// <summary>
    /// Encodes <paramref name="source" /> into a UTF-8 byte array using the URL-safe Base64 alphabet.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The UTF-8 encoded URL-safe Base64 bytes.</returns>
    public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source) =>
        Base64.EncodeToUtf8(source, Base64Variant.UrlSafe);

    /// <summary>
    /// Returns the maximum number of characters required to encode <paramref name="byteCount" /> bytes in URL-safe
    /// Base64 (without padding by default).
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <returns>The encoded character count.</returns>
    public static int GetEncodedLength(int byteCount) =>
        Base64.GetEncodedLength(byteCount, Base64Variant.UrlSafe);

    /// <summary>
    /// Returns the maximum number of bytes that decoding <paramref name="charCount" /> characters can produce.
    /// </summary>
    /// <param name="charCount">The input character count.</param>
    /// <returns>The maximum decoded byte count.</returns>
    public static int GetMaxDecodedLength(int charCount) =>
        Base64.GetMaxDecodedLength(charCount);

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid URL-safe Base64 input.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <returns>
    /// <see langword="true" /> when the input would decode cleanly; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> source) =>
        Base64.IsValid(source, Base64Variant.UrlSafe, BaseFormatStyles.AllowMissingPadding);

    /// <summary>
    /// Attempts to decode <paramref name="chars" /> as URL-safe Base64 into <paramref name="destination" />.
    /// </summary>
    /// <param name="chars">The URL-safe Base64 character span.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> on malformed input or undersized destination.
    /// </returns>
    public static bool TryDecode(ReadOnlySpan<char> chars, Span<byte> destination, out int bytesWritten) =>
        Base64.TryDecode(chars, destination, out bytesWritten, Base64Variant.UrlSafe, BaseFormatStyles.AllowMissingPadding);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> as URL-safe Base64.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
        Base64.TryEncode(source, destination, out charsWritten, Base64Variant.UrlSafe);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> as URL-safe Base64 UTF-8
    /// bytes.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination UTF-8 byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
        Base64.TryEncodeToUtf8(source, destination, out bytesWritten, Base64Variant.UrlSafe);
}
