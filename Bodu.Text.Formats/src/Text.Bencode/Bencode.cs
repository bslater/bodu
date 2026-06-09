// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bencode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Bencode;

/// <summary>
/// Provides convenience entry points for decoding and encoding values using the Bencode serialization format used by
/// the BitTorrent protocol (BEP 3) and a handful of related peer-to-peer protocols, delegating to the
/// <see cref="BencodeReader" /> and <see cref="BencodeWriter" /> types that own deserialization and serialization
/// respectively. The document structure itself is the <see cref="BencodedValue" /> model.
/// </summary>
/// <remarks>
/// <para>
/// Bencode is a compact, deterministic, byte-oriented serialization format. Every value falls into one of four kinds:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>i</c>&lt;integer&gt;<c>e</c> — an arbitrarily large signed integer encoded as ASCII decimal digits.
/// </description>
/// </item>
/// <item>
/// <description>
/// &lt;length&gt;<c>:</c>&lt;bytes&gt; — a raw byte string prefixed with its length in ASCII decimal.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>l</c>&lt;values…&gt;<c>e</c> — an ordered list of any other bencoded values.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>d</c>&lt;key,value pairs…&gt;<c>e</c> — a dictionary whose keys are raw byte strings sorted lexicographically in
/// bytewise order. Dictionaries with out-of-order keys are not valid bencoded data.
/// </description>
/// </item>
/// </list>
/// <para>
/// These static helpers exist for ergonomic one-liners; the read/write pair (<see cref="BencodeReader" /> and
/// <see cref="BencodeWriter" />) is the primary surface, mirroring the relationship between <c>XmlReader</c> /
/// <c>XmlWriter</c> and their document model. <see cref="Parse(ReadOnlySpan{byte})" /> consumes a complete document and
/// rejects trailing bytes; <see cref="Format(BencodedValue)" /> produces a fresh byte array; and
/// <see cref="GetFormattedLength(BencodedValue)" /> reports the exact size in advance so callers can pool destination
/// buffers.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Parse a BitTorrent-style dictionary and inspect a known field.
/// byte[] payload = File.ReadAllBytes("example.torrent");
/// BencodedValue root = Bencode.Parse(payload);
///
/// if (root is BencodedDictionary dict
///     && dict.TryGetValue("announce", out BencodedValue? announce)
///     && announce is BencodedString url)
/// {
///     Console.WriteLine(url.GetUtf8String());
/// }
///
/// Round-trip: encode the value back to bytes.
/// int length = Bencode.GetFormattedLength(root);
/// byte[] reencoded = Bencode.Format(root);
///]]>
/// </example>
public static class Bencode
{
    /// <summary>
    /// The shared, stateless reader used by the convenience parse methods.
    /// </summary>
    private static readonly BencodeReader s_reader = new();

    /// <summary>
    /// The shared, stateless writer used by the convenience format methods.
    /// </summary>
    private static readonly BencodeWriter s_writer = new();

    /// <summary>
    /// Decodes a complete bencoded document under the default parsing options.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed, contains trailing bytes, or nests more deeply than
    /// <see cref="BencodeParseOptions.DefaultMaxDepth" />.
    /// </exception>
    public static BencodedValue Parse(ReadOnlySpan<byte> source) =>
        s_reader.Read(source);

    /// <summary>
    /// Decodes a complete bencoded document using the supplied parsing options.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <param name="options">
    /// Options that control parsing policy, including the maximum permitted nesting depth.
    /// </param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed, contains trailing bytes, or nests more deeply than
    /// <see cref="BencodeParseOptions.MaxDepth" />.
    /// </exception>
    public static BencodedValue Parse(ReadOnlySpan<byte> source, BencodeParseOptions options) =>
        s_reader.Read(source, options);

    /// <summary>
    /// Decodes a complete bencoded document from the supplied byte array.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed or contains trailing bytes.
    /// </exception>
    public static BencodedValue Parse(byte[] source) =>
        s_reader.Read(source);

    /// <summary>
    /// Decodes a complete bencoded document by reading <paramref name="source" /> to its end.
    /// </summary>
    /// <param name="source">The readable stream containing the bencoded bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the stream contents are malformed or contain trailing bytes.
    /// </exception>
    public static BencodedValue Parse(Stream source) =>
        s_reader.Read(source);

    /// <summary>
    /// Asynchronously decodes a complete bencoded document by reading <paramref name="source" /> to its end.
    /// </summary>
    /// <param name="source">The readable stream containing the bencoded bytes.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the read.</param>
    /// <returns>A task that completes with the decoded value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the stream contents are malformed or contain trailing bytes.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the read completes.
    /// </exception>
    public static ValueTask<BencodedValue> ParseAsync(Stream source, CancellationToken cancellationToken = default) =>
        s_reader.ReadAsync(source, cancellationToken);

    /// <summary>
    /// Attempts to decode a single bencoded value.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <param name="value">When this method returns, contains the decoded value, when successful.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed.</param>
    /// <returns><see langword="true" /> when a value was decoded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(
        ReadOnlySpan<byte> source,
        [NotNullWhen(true)] out BencodedValue? value,
        out int bytesConsumed) =>
        s_reader.TryRead(source, out value, out bytesConsumed);

    /// <summary>
    /// Attempts to decode a single bencoded value under the supplied parsing options.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <param name="options">Options that control parsing policy.</param>
    /// <param name="value">When this method returns, contains the decoded value, when successful.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed.</param>
    /// <returns><see langword="true" /> when a value was decoded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(
        ReadOnlySpan<byte> source,
        BencodeParseOptions options,
        [NotNullWhen(true)] out BencodedValue? value,
        out int bytesConsumed) =>
        s_reader.TryRead(source, options, out value, out bytesConsumed);

    /// <summary>
    /// Encodes a bencoded value into a new byte array.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    public static byte[] Format(BencodedValue value) =>
        s_writer.Write(value);

    /// <summary>
    /// Encodes a bencoded value and writes the result to the supplied stream.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The writable stream that receives the encoded bytes.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    public static void Format(BencodedValue value, Stream destination) =>
        s_writer.Write(value, destination);

    /// <summary>
    /// Asynchronously encodes a bencoded value and writes the result to the supplied stream.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The writable stream that receives the encoded bytes.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the write.</param>
    /// <returns>A task that completes once the encoded bytes have been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the write completes.
    /// </exception>
    public static ValueTask FormatAsync(BencodedValue value, Stream destination, CancellationToken cancellationToken = default) =>
        s_writer.WriteAsync(value, destination, cancellationToken);

    /// <summary>
    /// Attempts to encode a bencoded value into the supplied destination buffer.
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
    public static bool TryFormat(
        BencodedValue value,
        Span<byte> destination,
        out int bytesWritten) =>
        s_writer.TryWrite(value, destination, out bytesWritten);

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
    public static int GetFormattedLength(BencodedValue value) =>
        s_writer.GetWrittenLength(value);
}
