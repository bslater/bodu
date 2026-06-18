// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64.BclAliases.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base64
{
    /// <summary>
    /// Decodes <paramref name="s" /> as a Standard Base64 string into a byte array, mirroring the lenient whitespace
    /// behaviour of <see cref="System.Convert.FromBase64String(string)" />. ASCII whitespace anywhere in the input is
    /// silently ignored; the canonical-padding rule and alphabet are otherwise enforced strictly.
    /// </summary>
    /// <param name="s">The Base64 input.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Standard Base64.</exception>
    /// <remarks>
    /// To reject whitespace strictly, call <see cref="Decode(string, Base64Variant, BaseFormatStyles)" /> directly with
    /// <see cref="BaseFormatStyles.None" />.
    /// </remarks>
    public static byte[] FromBase64String(string s) =>
        Decode(s, Base64Variant.Standard, BaseFormatStyles.IgnoreWhitespace);

    /// <summary>
    /// Decodes <paramref name="chars" /> as a Standard Base64 character span into a byte array, mirroring the lenient
    /// whitespace behaviour of <see cref="System.Convert.FromBase64String(string)" />.
    /// </summary>
    /// <param name="chars">The Base64 character span.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not valid Standard Base64.</exception>
    public static byte[] FromBase64String(ReadOnlySpan<char> chars) =>
        Decode(chars, Base64Variant.Standard, BaseFormatStyles.IgnoreWhitespace);

    /// <summary>
    /// Decodes UTF-8 Base64 bytes into a byte array using the Standard variant.
    /// </summary>
    /// <param name="utf8Source">The UTF-8 Base64 source.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not valid Standard Base64.</exception>
    public static byte[] FromBase64String(ReadOnlySpan<byte> utf8Source)
    {
        if (utf8Source.IsEmpty)
            return [];

        byte[] destination = new byte[GetMaxDecodedLength(utf8Source.Length)];
        OperationStatus status = DecodeFromUtf8(utf8Source, destination, out _, out int bytesWritten, Base64Variant.Standard, BaseFormatStyles.IgnoreWhitespace, isFinalBlock: true);
        if (status != OperationStatus.Done)
            throw new FormatException(EncodingResourceStrings.Format_Invalid_StandardBase64);

        if (bytesWritten == destination.Length)
            return destination;

        byte[] trimmed = new byte[bytesWritten];
        Buffer.BlockCopy(destination, 0, trimmed, 0, bytesWritten);
        return trimmed;
    }

    /// <summary>
    /// Strict-mode Standard Base64 decode from a character span using the <see cref="OperationStatus" /> return
    /// convention.
    /// </summary>
    /// <param name="source">The Base64 characters.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="charsConsumed">When this method returns, contains the number of characters consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    public static OperationStatus FromBase64String(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten) =>
        DecodeWithStatus(source, destination, out charsConsumed, out bytesWritten, Base64Variant.Standard, BaseFormatStyles.None, isFinalBlock: true);

    /// <summary>
    /// Strict-mode Standard Base64 decode from a UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Source">The UTF-8 Base64 source.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    public static OperationStatus FromBase64String(ReadOnlySpan<byte> utf8Source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
        DecodeFromUtf8(utf8Source, destination, out bytesConsumed, out bytesWritten, Base64Variant.Standard, BaseFormatStyles.None, isFinalBlock: true);

    /// <summary>
    /// Encodes <paramref name="inArray" /> into a Standard Base64 string with default formatting.
    /// </summary>
    /// <param name="inArray">The byte array to encode.</param>
    /// <returns>A Base64 (RFC 4648 §4) string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inArray" /> is <see langword="null" />.
    /// </exception>
    public static string ToBase64String(byte[] inArray) =>
        Encode(inArray, Base64Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Encodes <paramref name="bytes" /> into a Standard Base64 string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A Base64 string.</returns>
    public static string ToBase64String(ReadOnlySpan<byte> bytes) =>
        Encode(bytes, Base64Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Encodes a portion of <paramref name="inArray" /> into a Standard Base64 string.
    /// </summary>
    /// <param name="inArray">The byte array to encode.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="length">The number of bytes to encode.</param>
    /// <returns>A Base64 string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inArray" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="length" /> is out of range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="length" /> exceeds the
    /// available range of <paramref name="inArray" />.
    /// </exception>
    public static string ToBase64String(byte[] inArray, int offset, int length) =>
        Encode(inArray, offset, length, Base64Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> as a Standard Base64
    /// character sequence.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryToBase64String(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
        TryEncode(source, destination, out charsWritten, Base64Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> as a Standard Base64 UTF-8 byte sequence into
    /// <paramref name="utf8Destination" />.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="utf8Destination">The UTF-8 destination span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryToBase64String(ReadOnlySpan<byte> source, Span<byte> utf8Destination, out int bytesWritten) =>
        TryEncodeToUtf8(source, utf8Destination, out bytesWritten, Base64Variant.Standard, BaseFormattingOptions.None);
}
