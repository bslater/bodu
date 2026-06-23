// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8StringReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Formats.Excel.Biff8;

/// <summary>
/// Decodes BIFF8 character runs that lie entirely within a single record payload, handling both the 16-bit Unicode and
/// the compressed 8-bit (low-byte) representations.
/// </summary>
/// <remarks>
/// BIFF8 stores text as either UTF-16LE code units or, when every character has a zero high byte, a compressed run of
/// one low byte per character. A flags byte preceding the run selects which form is used (bit 0 set means 16-bit). This
/// helper decodes a run whose character count and form are already known; the shared string table handles runs that
/// straddle continuation records separately.
/// </remarks>
internal static class Biff8StringReader
{
    /// <summary>
    /// Decodes a character run within a record payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset at which the character data begins.</param>
    /// <param name="charCount">The number of characters to decode.</param>
    /// <param name="highByte">Whether the run is 16-bit Unicode (<see langword="true" />) or compressed 8-bit.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the character data runs past the payload.</exception>
    public static string Decode(ReadOnlySpan<byte> payload, int offset, int charCount, bool highByte, Biff8RecordType type)
    {
        int byteCount = highByte ? charCount * 2 : charCount;
        if (charCount < 0 || offset < 0 || offset + byteCount > payload.Length)
            throw Biff8Payload.Malformed(type);

        if (highByte)
            return Encoding.Unicode.GetString(payload.Slice(offset, byteCount));

        return DecodeCompressed(payload.Slice(offset, charCount));
    }

    /// <summary>
    /// Decodes a compressed (8-bit) BIFF8 character run, mapping each byte to the code point of the same value.
    /// </summary>
    /// <param name="bytes">The compressed character bytes.</param>
    /// <returns>The decoded string.</returns>
    public static string DecodeCompressed(ReadOnlySpan<byte> bytes)
    {
        char[] characters = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
            characters[i] = (char)bytes[i];

        return new string(characters);
    }
}
