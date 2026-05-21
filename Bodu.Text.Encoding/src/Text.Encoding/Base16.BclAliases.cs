// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16.BclAliases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base16
{
    /// <summary>
    /// Decodes a hexadecimal string into a byte array using strict parsing (no prefix, no whitespace).
    /// </summary>
    /// <param name="s">The hexadecimal input.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not strict hex.</exception>
    public static byte[] FromHexString(string s) =>
        Decode(s, BaseFormatStyles.None);

    /// <summary>
    /// Decodes a hexadecimal character span into a byte array using strict parsing.
    /// </summary>
    /// <param name="chars">The hexadecimal characters.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not strict hex.</exception>
    public static byte[] FromHexString(ReadOnlySpan<char> chars) =>
        Decode(chars, BaseFormatStyles.None);

    /// <summary>
    /// Decodes a UTF-8 encoded hexadecimal byte span into a byte array using strict parsing.
    /// </summary>
    /// <param name="utf8Source">The UTF-8 hexadecimal source.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not strict hex.</exception>
    /// <remarks>
    /// Hexadecimal characters are ASCII, so the UTF-8 representation is bit-identical to the character form. This
    /// overload avoids allocating a <see cref="string" /> when the caller already has the input as bytes.
    /// </remarks>
    public static byte[] FromHexString(ReadOnlySpan<byte> utf8Source)
    {
        if (utf8Source.IsEmpty)
            return Array.Empty<byte>();

        if ((utf8Source.Length & 1) != 0)
            throw new FormatException(EncodingResourceStrings.Format_Invalid_HexDigitCountOdd);

        var result = new byte[utf8Source.Length / 2];
        return !DecodeHexPairsFromUtf8(utf8Source, result)
            ? throw new FormatException(EncodingResourceStrings.Format_Invalid_HexCharacters)
            : result;
    }

    /// <summary>
    /// Strict-mode streaming decode of a hexadecimal character span into a byte span.
    /// </summary>
    /// <param name="source">The hexadecimal characters.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsConsumed">When this method returns, contains the number of characters consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see cref="OperationStatus.Done" /> on full success; <see cref="OperationStatus.DestinationTooSmall" /> when
    /// <paramref name="destination" /> cannot fit the result; <see cref="OperationStatus.InvalidData" /> for invalid
    /// characters or odd source length.
    /// </returns>
    public static OperationStatus FromHexString(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten) =>
        DecodeStrictWithStatus(source, destination, out charsConsumed, out bytesWritten, isFinalBlock: true);

    /// <summary>
    /// Strict-mode streaming decode of a UTF-8 hexadecimal byte span into a byte span.
    /// </summary>
    /// <param name="utf8Source">The UTF-8 hexadecimal source.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see cref="OperationStatus.Done" /> on full success; <see cref="OperationStatus.DestinationTooSmall" /> when
    /// <paramref name="destination" /> cannot fit the result; <see cref="OperationStatus.InvalidData" /> for invalid
    /// characters or odd source length.
    /// </returns>
    public static OperationStatus FromHexString(ReadOnlySpan<byte> utf8Source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
        DecodeUtf8WithStatus(utf8Source, destination, out bytesConsumed, out bytesWritten, isFinalBlock: true);

    /// <summary>
    /// Encodes <paramref name="inArray" /> into an upper case hexadecimal string. Aliases
    /// <see cref="Convert.ToHexString(byte[])" /> with no formatting decorations.
    /// </summary>
    /// <param name="inArray">The byte array to encode.</param>
    /// <returns>An upper case hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inArray" /> is <see langword="null" />.
    /// </exception>
    public static string ToHexString(byte[] inArray) =>
        Encode(inArray, BaseFormattingOptions.UpperCase);

    /// <summary>
    /// Encodes <paramref name="bytes" /> into an upper case hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>An upper case hexadecimal string.</returns>
    public static string ToHexString(ReadOnlySpan<byte> bytes) =>
        Encode(bytes, BaseFormattingOptions.UpperCase);

    /// <summary>
    /// Encodes a portion of <paramref name="inArray" /> into an upper case hexadecimal string.
    /// </summary>
    /// <param name="inArray">The byte array to encode.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="length">The number of bytes to encode.</param>
    /// <returns>An upper case hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inArray" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="length" /> is out of range for
    /// <paramref name="inArray" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="length" /> exceeds the
    /// available range of <paramref name="inArray" />.
    /// </exception>
    public static string ToHexString(byte[] inArray, int offset, int length) =>
        Encode(inArray, offset, length, BaseFormattingOptions.UpperCase);

    /// <summary>
    /// Encodes <paramref name="inArray" /> into a lower case hexadecimal string. Mirrors
    /// <c>Convert.ToHexStringLower</c> introduced in .NET 9.
    /// </summary>
    /// <param name="inArray">The byte array to encode.</param>
    /// <returns>A lower case hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inArray" /> is <see langword="null" />.
    /// </exception>
    public static string ToHexStringLower(byte[] inArray) =>
        Encode(inArray, BaseFormattingOptions.None);

    /// <summary>
    /// Encodes <paramref name="bytes" /> into a lower case hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A lower case hexadecimal string.</returns>
    public static string ToHexStringLower(ReadOnlySpan<byte> bytes) =>
        Encode(bytes, BaseFormattingOptions.None);

    /// <summary>
    /// Encodes a portion of <paramref name="inArray" /> into a lower case hexadecimal string.
    /// </summary>
    /// <param name="inArray">The byte array to encode.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="length">The number of bytes to encode.</param>
    /// <returns>A lower case hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inArray" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="length" /> is out of range for
    /// <paramref name="inArray" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="length" /> exceeds the
    /// available range of <paramref name="inArray" />.
    /// </exception>
    public static string ToHexStringLower(byte[] inArray, int offset, int length) =>
        Encode(inArray, offset, length, BaseFormattingOptions.None);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> using upper case hex.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination character span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryToHexString(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
        TryEncode(source, destination, out charsWritten, BaseFormattingOptions.UpperCase);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into UTF-8 <paramref name="utf8Destination" /> using upper case
    /// hex.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="utf8Destination">The UTF-8 destination span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryToHexString(ReadOnlySpan<byte> source, Span<byte> utf8Destination, out int bytesWritten) =>
        TryEncodeToUtf8(source, utf8Destination, out bytesWritten, BaseFormattingOptions.UpperCase);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> using lower case hex.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination character span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryToHexStringLower(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
        TryEncode(source, destination, out charsWritten, BaseFormattingOptions.None);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into UTF-8 <paramref name="utf8Destination" /> using lower case
    /// hex.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="utf8Destination">The UTF-8 destination span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryToHexStringLower(ReadOnlySpan<byte> source, Span<byte> utf8Destination, out int bytesWritten) =>
        TryEncodeToUtf8(source, utf8Destination, out bytesWritten, BaseFormattingOptions.None);
}
