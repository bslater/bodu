// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.BclAliases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base85
{

    /// <summary>
    /// Decodes an Ascii85 string into a byte array.
    /// </summary>
    /// <param name="s">The Ascii85 input.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Ascii85.</exception>
    public static byte[] FromBase85String(string s) =>
        Decode(s, Base85Variant.Ascii85, BaseFormatStyles.None);

    /// <summary>
    /// Decodes an Ascii85 character span into a byte array.
    /// </summary>
    /// <param name="chars">The character span.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not valid Ascii85.</exception>
    public static byte[] FromBase85String(ReadOnlySpan<char> chars) =>
        Decode(chars, Base85Variant.Ascii85, BaseFormatStyles.None);

    /// <summary>
    /// Decodes UTF-8 Ascii85 bytes into a byte array.
    /// </summary>
    /// <param name="utf8Source">The UTF-8 Ascii85 source.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not valid Ascii85.</exception>
    public static byte[] FromBase85String(ReadOnlySpan<byte> utf8Source)
    {
        if (utf8Source.IsEmpty)
            return Array.Empty<byte>();

        var destination = new byte[GetMaxDecodedLength(utf8Source.Length)];
        OperationStatus status = DecodeFromUtf8(utf8Source, destination, out _, out var bytesWritten);
        if (status != OperationStatus.Done)
            throw new FormatException(EncodingResourceStrings.Format_Invalid_Ascii85);

        if (bytesWritten == destination.Length)
            return destination;

        var trimmed = new byte[bytesWritten];
        Buffer.BlockCopy(destination, 0, trimmed, 0, bytesWritten);
        return trimmed;
    }

    /// <summary>
    /// Ascii85 decode from a character span returning <see cref="OperationStatus" />.
    /// </summary>
    /// <param name="source">The character span.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="charsConsumed">When this method returns, contains the number of characters consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    public static OperationStatus FromBase85String(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten) =>
        DecodeWithStatus(source, destination, out charsConsumed, out bytesWritten, Base85Variant.Ascii85, BaseFormatStyles.None);

    /// <summary>
    /// Ascii85 decode from a UTF-8 byte span returning <see cref="OperationStatus" />.
    /// </summary>
    /// <param name="utf8Source">The UTF-8 Ascii85 source.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    public static OperationStatus FromBase85String(ReadOnlySpan<byte> utf8Source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
        DecodeFromUtf8(utf8Source, destination, out bytesConsumed, out bytesWritten, Base85Variant.Ascii85, BaseFormatStyles.None);
    /// <summary>
    /// Encodes <paramref name="inArray" /> into an Adobe Ascii85 string.
    /// </summary>
    /// <param name="inArray">The byte array to encode.</param>
    /// <returns>An Ascii85 string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inArray" /> is <see langword="null" />.</exception>
    public static string ToBase85String(byte[] inArray) =>
        Encode(inArray, Base85Variant.Ascii85);

    /// <summary>
    /// Encodes <paramref name="bytes" /> into an Ascii85 string.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>An Ascii85 string.</returns>
    public static string ToBase85String(ReadOnlySpan<byte> bytes) =>
        Encode(bytes, Base85Variant.Ascii85);

    /// <summary>
    /// Encodes a portion of <paramref name="inArray" /> into an Ascii85 string.
    /// </summary>
    /// <param name="inArray">The byte array.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="length">The number of bytes to encode.</param>
    /// <returns>An Ascii85 string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inArray" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="length" /> is out of range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="length" /> exceeds the
    /// available range of <paramref name="inArray" />.
    /// </exception>
    public static string ToBase85String(byte[] inArray, int offset, int length) =>
        Encode(inArray, offset, length, Base85Variant.Ascii85);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> using Ascii85.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns><see langword="true" /> on success; <see langword="false" /> when the destination is too small.</returns>
    public static bool TryToBase85String(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
        TryEncode(source, destination, out charsWritten, Base85Variant.Ascii85);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> as Ascii85 UTF-8 bytes into <paramref name="utf8Destination" />.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="utf8Destination">The UTF-8 destination span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns><see langword="true" /> on success; <see langword="false" /> when the destination is too small.</returns>
    public static bool TryToBase85String(ReadOnlySpan<byte> source, Span<byte> utf8Destination, out int bytesWritten) =>
        TryEncodeToUtf8(source, utf8Destination, out bytesWritten, Base85Variant.Ascii85);
}
