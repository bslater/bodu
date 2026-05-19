// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodingExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides fluent extension methods on <see cref="byte" /> arrays, <see cref="ReadOnlySpan{T}" /> of byte and
/// <see cref="char" />, and <see cref="string" /> that route through the canonical encoding implementations.
/// </summary>
/// <remarks>
/// <para>
/// These extensions exist purely for ergonomic discoverability — typing <c>bytes.</c> in an editor brings up
/// <c>ToBase16String</c>, <c>ToBase64String</c>, <c>ToBase58String</c>, and so on. They delegate without overhead to
/// the static <see cref="Base16" />, <see cref="Base32" />, <see cref="Base64" />, <see cref="Base58" />, and
/// <see cref="Base85" /> classes.
/// </para>
/// <para>
/// The generic <see cref="Encode(byte[], IBinaryEncoding)" /> and <see cref="Decode(string, IBinaryEncoding)" />
/// overloads route through <see cref="IBinaryEncoding" /> so that runtime-selected encodings work with the same
/// extension surface.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// byte[] data = "hello"u8.ToArray();
///
/// // Direct shortcuts — discoverable from IntelliSense via the byte[] receiver.
/// string hex     = data.ToBase16String();                  // "68656c6c6f"
/// string base32  = data.ToBase32String();                  // "NBSWY3DP"
/// string base64  = data.ToBase64String();                  // "aGVsbG8="
/// string base58  = data.ToBase58String();                  // "Cn8eVZg"
///
/// // Round-trip via the matching FromBase*String extension.
/// byte[] roundtrip = base64.FromBase64String();
///
/// // Generic dispatch via IBinaryEncoding for runtime-selected encodings.
/// IBinaryEncoding chosen = BinaryEncodings.Get(appConfig["encoding"] ?? "base64");
/// string encoded = data.Encode(chosen);
/// byte[] decoded = encoded.Decode(chosen);
///]]>
/// </example>
public static class BinaryEncodingExtensions
{

    /// <summary>
    /// Decodes <paramref name="encoded" /> using <paramref name="encoding" />.
    /// </summary>
    /// <param name="encoded">The encoded string.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown when the input is not valid for the chosen encoding.</exception>
    public static byte[] Decode(this string encoded, IBinaryEncoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoded);
        ThrowHelper.ThrowIfNull(encoding);
        return encoding.Decode(encoded.AsSpan());
    }
    /// <summary>
    /// Encodes <paramref name="bytes" /> using <paramref name="encoding" />.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <returns>The encoded string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null" />.</exception>
    public static string Encode(this byte[] bytes, IBinaryEncoding encoding)
    {
        ThrowHelper.ThrowIfNull(bytes);
        ThrowHelper.ThrowIfNull(encoding);
        return encoding.Encode(bytes.AsSpan());
    }

    /// <summary>
    /// Decodes a Base16 hexadecimal string into a byte array using strict parsing.
    /// </summary>
    /// <param name="hex">The hex input.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="hex" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid hexadecimal.</exception>
    public static byte[] FromBase16String(this string hex) => Base16.FromHexString(hex);

    /// <summary>
    /// Decodes a Standard RFC 4648 Base32 string into a byte array.
    /// </summary>
    /// <param name="base32">The Base32 input.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="base32" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base32.</exception>
    public static byte[] FromBase32String(this string base32) => Base32.FromBase32String(base32);

    /// <summary>
    /// Decodes a Bitcoin/Flickr Base58 string into a byte array.
    /// </summary>
    /// <param name="base58">The Base58 input.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="base58" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Bitcoin/Flickr Base58.</exception>
    public static byte[] FromBase58String(this string base58) => Base58.FromBase58String(base58);

    /// <summary>
    /// Decodes a Standard RFC 4648 Base64 string into a byte array.
    /// </summary>
    /// <param name="base64">The Base64 input.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="base64" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base64.</exception>
    public static byte[] FromBase64String(this string base64) => Base64.FromBase64String(base64);

    /// <summary>
    /// Decodes an Adobe Ascii85 string into a byte array.
    /// </summary>
    /// <param name="ascii85">The Ascii85 input.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ascii85" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Ascii85.</exception>
    public static byte[] FromBase85String(this string ascii85) => Base85.FromBase85String(ascii85);

    /// <summary>
    /// Encodes <paramref name="bytes" /> to lower-case hexadecimal (the default Bodu Base16 form).
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lower-case hex string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    public static string ToBase16String(this byte[] bytes) => Base16.ToHexStringLower(bytes);

    /// <summary>
    /// Encodes <paramref name="bytes" /> to lower-case hexadecimal.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lower-case hex string.</returns>
    public static string ToBase16String(this ReadOnlySpan<byte> bytes) => Base16.ToHexStringLower(bytes);

    /// <summary>
    /// Encodes <paramref name="bytes" /> using the Standard RFC 4648 Base32 alphabet.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Base32 string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    public static string ToBase32String(this byte[] bytes) => Base32.ToBase32String(bytes);

    /// <summary>
    /// Encodes <paramref name="bytes" /> using the Standard RFC 4648 Base32 alphabet.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Base32 string.</returns>
    public static string ToBase32String(this ReadOnlySpan<byte> bytes) => Base32.ToBase32String(bytes);

    /// <summary>
    /// Encodes <paramref name="bytes" /> using the Bitcoin/Flickr Base58 alphabet.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Base58 string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    public static string ToBase58String(this byte[] bytes) => Base58.ToBase58String(bytes);

    /// <summary>
    /// Encodes <paramref name="bytes" /> using the Bitcoin/Flickr Base58 alphabet.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Base58 string.</returns>
    public static string ToBase58String(this ReadOnlySpan<byte> bytes) => Base58.ToBase58String(bytes);

    /// <summary>
    /// Encodes <paramref name="bytes" /> using the Standard RFC 4648 Base64 alphabet.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Base64 string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    public static string ToBase64String(this byte[] bytes) => Base64.ToBase64String(bytes);

    /// <summary>
    /// Encodes <paramref name="bytes" /> using the Standard RFC 4648 Base64 alphabet.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Base64 string.</returns>
    public static string ToBase64String(this ReadOnlySpan<byte> bytes) => Base64.ToBase64String(bytes);

    /// <summary>
    /// Encodes <paramref name="bytes" /> using Adobe Ascii85.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Ascii85 string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    public static string ToBase85String(this byte[] bytes) => Base85.ToBase85String(bytes);

    /// <summary>
    /// Encodes <paramref name="bytes" /> using Adobe Ascii85.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Ascii85 string.</returns>
    public static string ToBase85String(this ReadOnlySpan<byte> bytes) => Base85.ToBase85String(bytes);
}
