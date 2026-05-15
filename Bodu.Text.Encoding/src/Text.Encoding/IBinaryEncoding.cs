// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBinaryEncoding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Represents a binary-to-text encoding scheme (Base16, Base32, Base64, Base58, Base85, …) with the variant
/// pre-bound, so that callers can pick or inject an encoding at runtime instead of dispatching through static
/// classes.
/// </summary>
/// <remarks>
/// <para>
/// Instances are obtained from <see cref="BinaryEncodings" /> — each property there returns a singleton
/// implementation bound to a specific variant. The pattern mirrors <see cref="System.Text.Encoding" />, but for
/// radix encoding rather than character-set transcoding.
/// </para>
/// <para>
/// Implementations are stateless and thread-safe. The static convenience methods on <see cref="Base16" />,
/// <see cref="Base32" />, <see cref="Base64" />, <see cref="Base58" />, and <see cref="Base85" /> remain the
/// recommended entry points when the encoding is known at compile time; the interface is intended for code that
/// must choose between encodings at runtime (configuration-driven serializers, plugin pipelines, generic
/// utilities).
/// </para>
/// </remarks>
public interface IBinaryEncoding
{
    /// <summary>
    /// Gets a short stable name identifying the encoding and variant — for example, <c>"base16-lower"</c>,
    /// <c>"base32"</c>, <c>"base64-urlsafe"</c>. Suitable as a key in configuration and for diagnostic output.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable description of the encoding and its origin (specification clause, RFC, etc.).
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Returns an upper bound on the number of characters required to encode <paramref name="byteCount" /> bytes.
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <returns>The maximum encoded character count.</returns>
    int GetMaxEncodedLength(int byteCount);

    /// <summary>
    /// Returns an upper bound on the number of bytes that decoding <paramref name="charCount" /> characters could
    /// produce.
    /// </summary>
    /// <param name="charCount">The input character count. Must be non-negative.</param>
    /// <returns>The maximum decoded byte count.</returns>
    int GetMaxDecodedLength(int charCount);

    /// <summary>
    /// Encodes <paramref name="bytes" /> into a newly allocated <see cref="string" /> using the variant's canonical
    /// formatting.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The encoded string.</returns>
    string Encode(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// Decodes <paramref name="chars" /> into a newly allocated byte array using the variant's strict parsing rules.
    /// </summary>
    /// <param name="chars">The encoded character span.</param>
    /// <returns>The decoded bytes.</returns>
    byte[] Decode(ReadOnlySpan<char> chars);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> without allocation.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns><see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.</returns>
    bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten);

    /// <summary>
    /// Attempts to decode <paramref name="source" /> into <paramref name="destination" /> without allocation.
    /// </summary>
    /// <param name="source">The encoded character span.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns><see langword="true" /> when the destination is large enough and the input is valid; otherwise
    /// <see langword="false" />.</returns>
    bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten);

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid encoded input under this encoding's variant rules.
    /// </summary>
    /// <param name="source">The character span to validate.</param>
    /// <returns><see langword="true" /> when <paramref name="source" /> would decode without error; otherwise
    /// <see langword="false" />.</returns>
    bool IsValid(ReadOnlySpan<char> source);
}
