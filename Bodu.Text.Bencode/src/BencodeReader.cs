// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bodu.Text.Bencode;

/// <summary>
/// Decodes Bencode-serialized bytes into a <see cref="BencodedValue" /> document model. The reader is the
/// deserialization half of the read/write pair; <see cref="BencodeWriter" /> is its serialization counterpart, and the
/// <see cref="BencodedValue" /> hierarchy holds the resulting structure.
/// </summary>
/// <remarks>
/// <para>
/// The reader implements the four Bencode value kinds — integers, byte strings, lists, and dictionaries — and enforces
/// the format's structural rules: integers reject leading zeros and negative zero, byte strings reject leading zeros in
/// their length prefix, and dictionary keys must be byte strings in strictly ascending bytewise order.
/// </para>
/// <para>
/// <see cref="BencodeParseOptions" /> controls parsing policy, including the maximum permitted container nesting depth
/// and, for the <c>TryRead</c> overloads, whether the source must be fully consumed. The class is unsealed so that
/// consumers may inherit its read capability without gaining a mutation surface.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var reader = new BencodeReader();
/// BencodedValue document = reader.Read(payload);
/// using var stream = File.OpenRead("example.torrent");
/// BencodedValue fromFile = reader.Read(stream);
///]]>
/// </example>
public partial class BencodeReader
{
    private const byte DictionaryPrefix = (byte)'d';
    private const byte EndMarker = (byte)'e';
    private const byte IntegerPrefix = (byte)'i';
    private const byte ListPrefix = (byte)'l';
    private const byte MinusSign = (byte)'-';
    private const byte StringLengthSeparator = (byte)':';

    private static readonly CompositeFormat s_nestingTooDeep =
        CompositeFormat.Parse(BencodeResourceStrings.Format_Invalid_BencodeNestingTooDeep);

    private static readonly CompositeFormat s_unexpectedToken =
        CompositeFormat.Parse(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedToken);

    /// <summary>
    /// Decodes a complete bencoded document from the specified byte span under the default parsing options.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed, contains trailing bytes, or nests more deeply than
    /// <see cref="BencodeParseOptions.DefaultMaxDepth" />.
    /// </exception>
    public BencodedValue Read(ReadOnlySpan<byte> source) =>
        Read(source, BencodeParseOptions.Default);

    /// <summary>
    /// Decodes a complete bencoded document from the specified byte span using the supplied parsing options.
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
    public BencodedValue Read(ReadOnlySpan<byte> source, BencodeParseOptions options)
    {
        Parser parser = new(source, options.MaxDepth);
        BencodedValue value = parser.ParseValue();

        if (parser.Position != source.Length)
            throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeTrailingData);

        return value;
    }

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
    public BencodedValue Read(byte[] source)
    {
        ThrowHelper.ThrowIfNull(source);

        return Read((ReadOnlySpan<byte>)source);
    }

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
    /// <remarks>
    /// The stream is read to its end into a pooled buffer; the span-based parser then decodes the buffered content. The
    /// stream is not closed.
    /// </remarks>
    public BencodedValue Read(Stream source)
    {
        ThrowHelper.ThrowIfNull(source);
        BencodeThrowHelper.ThrowIfStreamNotReadable(source);

        using MemoryStream buffer = new();
        source.CopyTo(buffer);

        return Read(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
    }

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
    /// <remarks>
    /// The stream is read to its end into a pooled buffer; the span-based parser then decodes the buffered content. The
    /// stream is not closed.
    /// </remarks>
    public async ValueTask<BencodedValue> ReadAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        BencodeThrowHelper.ThrowIfStreamNotReadable(source);

        await using MemoryStream buffer = new();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        return Read(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
    }

    /// <summary>
    /// Attempts to decode a single bencoded value from the specified byte span under the default parsing options.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <param name="value">When this method returns, contains the decoded value, when successful.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed.</param>
    /// <returns><see langword="true" /> when a value was decoded; otherwise, <see langword="false" />.</returns>
    public bool TryRead(
        ReadOnlySpan<byte> source,
        [NotNullWhen(true)] out BencodedValue? value,
        out int bytesConsumed) =>
        TryRead(source, BencodeParseOptions.Default, out value, out bytesConsumed);

    /// <summary>
    /// Attempts to decode a single bencoded value from the specified byte span under the supplied parsing options.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <param name="options">Options that control parsing policy.</param>
    /// <param name="value">When this method returns, contains the decoded value, when successful.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed.</param>
    /// <returns><see langword="true" /> when a value was decoded; otherwise, <see langword="false" />.</returns>
    public bool TryRead(
        ReadOnlySpan<byte> source,
        BencodeParseOptions options,
        [NotNullWhen(true)] out BencodedValue? value,
        out int bytesConsumed)
    {
        try
        {
            Parser parser = new(source, options.MaxDepth);
            value = parser.ParseValue();

            if (options.RequireCompleteDocument && parser.Position != source.Length)
            {
                value = null;
                bytesConsumed = 0;
                return false;
            }

            bytesConsumed = parser.Position;
            return true;
        }
        catch (BencodeFormatException)
        {
            value = null;
            bytesConsumed = 0;
            return false;
        }
    }

    /// <summary>
    /// Determines whether a byte is an ASCII decimal digit.
    /// </summary>
    /// <param name="value">The byte to test.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="value" /> is in the range <c>'0'</c>–<c>'9'</c>; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private static bool IsAsciiDigit(byte value) =>
        value is >= (byte)'0' and <= (byte)'9';
}
