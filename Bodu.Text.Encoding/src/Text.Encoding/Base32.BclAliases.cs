// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32.BclAliases.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base32
{
    /// <summary>
    /// Decodes <paramref name="s" /> into a byte array using the Standard variant with strict parsing.
    /// </summary>
    /// <param name="s">The Base32 input.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not strict Standard Base32.</exception>
    public static byte[] FromBase32String(string s) =>
        Decode(s, Base32Variant.Standard, BaseFormatStyles.None);

    /// <summary>
    /// Decodes <paramref name="chars" /> into a byte array using the Standard variant with strict parsing.
    /// </summary>
    /// <param name="chars">The Base32 character span.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not strict Standard Base32.</exception>
    public static byte[] FromBase32String(ReadOnlySpan<char> chars) =>
        Decode(chars, Base32Variant.Standard, BaseFormatStyles.None);

    /// <summary>
    /// Decodes a UTF-8 encoded Base32 byte span into a byte array using the Standard variant with strict parsing.
    /// </summary>
    /// <param name="utf8Source">The UTF-8 hexadecimal source.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not strict Standard Base32.</exception>
    /// <remarks>
    /// Base32 alphabet characters are ASCII, so the UTF-8 byte form is byte-identical to the character form.
    /// </remarks>
    public static byte[] FromBase32String(ReadOnlySpan<byte> utf8Source)
    {
        if (utf8Source.IsEmpty)
            return [];

        var destination = new byte[GetMaxDecodedLength(utf8Source.Length)];
        OperationStatus status = DecodeFromUtf8(utf8Source, destination, out _, out var bytesWritten, Base32Variant.Standard, BaseFormatStyles.None, isFinalBlock: true);
        if (status != OperationStatus.Done)
            throw new FormatException(EncodingResourceStrings.Format_Invalid_StandardBase32);

        if (bytesWritten == destination.Length)
            return destination;

        var trimmed = new byte[bytesWritten];
        Buffer.BlockCopy(destination, 0, trimmed, 0, bytesWritten);
        return trimmed;
    }

    /// <summary>
    /// Strict-mode Base32 decode from a character span into a byte span, using the Standard variant and the
    /// <see cref="OperationStatus" /> return convention.
    /// </summary>
    /// <param name="source">The Base32 characters.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsConsumed">When this method returns, contains the number of characters consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    public static OperationStatus FromBase32String(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten) =>
        DecodeWithStatus(source, destination, out charsConsumed, out bytesWritten, Base32Variant.Standard, BaseFormatStyles.None, isFinalBlock: true);

    /// <summary>
    /// Strict-mode Base32 decode from a UTF-8 byte span into a byte span, using the Standard variant.
    /// </summary>
    /// <param name="utf8Source">The UTF-8 Base32 source.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    public static OperationStatus FromBase32String(ReadOnlySpan<byte> utf8Source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
        DecodeFromUtf8(utf8Source, destination, out bytesConsumed, out bytesWritten, Base32Variant.Standard, BaseFormatStyles.None, isFinalBlock: true);
    /// <summary>
    /// Encodes <paramref name="inArray" /> into a Base32 string using the Standard variant with default formatting.
    /// </summary>
    /// <param name="inArray">The byte array to encode.</param>
    /// <returns>A Base32 (RFC 4648 §6) string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inArray" /> is <see langword="null" />.
    /// </exception>
    public static string ToBase32String(byte[] inArray) =>
        Encode(inArray, Base32Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Encodes <paramref name="bytes" /> into a Base32 string using the Standard variant.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A Base32 string.</returns>
    public static string ToBase32String(ReadOnlySpan<byte> bytes) =>
        Encode(bytes, Base32Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Encodes a portion of <paramref name="inArray" /> into a Base32 string using the Standard variant.
    /// </summary>
    /// <param name="inArray">The byte array to encode.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="length">The number of bytes to encode.</param>
    /// <returns>A Base32 string.</returns>
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
    public static string ToBase32String(byte[] inArray, int offset, int length) =>
        Encode(inArray, offset, length, Base32Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> using the Standard variant
    /// without padding decoration.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryToBase32String(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
        TryEncode(source, destination, out charsWritten, Base32Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Attempts to encode <paramref name="source" /> as UTF-8 Base32 bytes into <paramref name="utf8Destination" />
    /// using the Standard variant.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="utf8Destination">The UTF-8 destination span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    public static bool TryToBase32String(ReadOnlySpan<byte> source, Span<byte> utf8Destination, out int bytesWritten) =>
        TryEncodeToUtf8(source, utf8Destination, out bytesWritten, Base32Variant.Standard, BaseFormattingOptions.None);
}
