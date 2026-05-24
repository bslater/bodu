// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Bencode;

/// <summary>
/// Provides fluent extension methods on <see cref="byte" /> arrays, <see cref="ReadOnlySpan{T}" /> of byte, and
/// <see cref="BencodedValue" /> that route through the canonical <see cref="Bencode" /> decoder and encoder.
/// </summary>
/// <remarks>
/// <para>
/// These extensions exist purely for ergonomic discoverability — typing <c>bytes.</c> in an editor brings up
/// <c>ParseBencode</c> and <c>TryParseBencode</c>, and typing <c>value.</c> on a <see cref="BencodedValue" /> brings up
/// <c>FormatBencode</c>, <c>TryFormatBencode</c>, and <c>GetBencodedLength</c>. They delegate without overhead to the
/// static <see cref="Bencode" /> class.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Parse and round-trip via the receiver-typed extensions.
/// byte[] payload = File.ReadAllBytes("example.torrent");
/// BencodedValue root = payload.ParseBencode();
/// byte[] reencoded = root.FormatBencode();
///]]>
/// </example>
public static class BencodeExtensions
{
    /// <summary>
    /// Decodes a complete bencoded document from the specified byte span.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed or contains trailing bytes.
    /// </exception>
    public static BencodedValue ParseBencode(this ReadOnlySpan<byte> source) =>
        Bencode.Decode(source);

    /// <summary>
    /// Decodes a complete bencoded document from the specified byte array.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed or contains trailing bytes.
    /// </exception>
    public static BencodedValue ParseBencode(this byte[] source) =>
        Bencode.Decode(source);

    /// <summary>
    /// Attempts to decode a single bencoded value from the specified byte span.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <param name="value">When this method returns, contains the decoded value, when successful.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed.</param>
    /// <returns><see langword="true" /> when a value was decoded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseBencode(
        this ReadOnlySpan<byte> source,
        [NotNullWhen(true)] out BencodedValue? value,
        out int bytesConsumed) =>
        Bencode.TryDecode(source, out value, out bytesConsumed);

    /// <summary>
    /// Encodes the specified bencoded value into a new byte array.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    public static byte[] FormatBencode(this BencodedValue value) =>
        Bencode.Encode(value);

    /// <summary>
    /// Attempts to encode the specified bencoded value into the supplied destination buffer.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see langword="true" /> when the value was encoded; otherwise, <see langword="false" /> when the destination
    /// buffer is too small.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    public static bool TryFormatBencode(
        this BencodedValue value,
        Span<byte> destination,
        out int bytesWritten) =>
        Bencode.TryEncode(value, destination, out bytesWritten);

    /// <summary>
    /// Gets the exact number of bytes required to encode the specified value.
    /// </summary>
    /// <param name="value">The value to measure.</param>
    /// <returns>The exact encoded length, in bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    public static int GetBencodedLength(this BencodedValue value) =>
        Bencode.GetEncodedLength(value);
}
