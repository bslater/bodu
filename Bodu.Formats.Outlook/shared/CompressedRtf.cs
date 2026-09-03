// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompressedRtf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

#if MSG
namespace Bodu.Formats.Outlook.Msg;
#elif OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#endif

/// <summary>
/// Decompresses the LZFu compressed-RTF format (MS-OXRTFCP) carried by <c>PidTagRtfCompressed</c>.
/// </summary>
/// <remarks>
/// <para>
/// The payload starts with a 16-byte header: the compressed size (excluding its own field), the uncompressed size, the
/// format magic (<c>LZFu</c> compressed, <c>MELA</c> raw), and a CRC over the bytes that follow the header. The
/// compressed body is an LZ77 variant over a 4096-byte circular dictionary preseeded with a 207-byte RTF prologue:
/// control bytes are consumed bit by bit from the least significant bit, a clear bit copies a literal, and a set bit
/// reads a big-endian 16-bit reference — a 12-bit dictionary offset and a 4-bit length stored as length − 2. A
/// reference whose offset equals the current write position terminates the stream.
/// </para>
/// <para>
/// The CRC is table-driven CRC-32 (reflected polynomial <c>0xEDB88320</c>) with a zero initial value and no final
/// exclusive-or — the catalogued CRC-32 in <c>Bodu.IO.Hashing</c> applies the standard pre/post conditioning this
/// format omits, so the checksum runs the format's parameters over the shared <see cref="CrcCore" /> engine
/// source-compiled from <c>Bodu.IO.Hashing/shared</c> (no package dependency).
/// </para>
/// <para>
/// This file lives in <c>Bodu.Formats.Outlook/shared/</c> and is source-compiled into each Outlook format reader —
/// <c>PidTagRtfCompressed</c> carries the same MS-OXRTFCP payload in a <c>.msg</c> substream and a PST property
/// context. The consuming project selects the namespace and the format-specific exception/resource pair via its
/// <c>DefineConstants</c> (<c>MSG</c> or <c>OUTLOOK_PST</c>).
/// </para>
/// </remarks>
internal static class CompressedRtf
{
    /// <summary>The magic identifying a compressed body (<c>LZFu</c> read little-endian).</summary>
    private const uint CompressedMagic = 0x75465A4C;

    /// <summary>The magic identifying a raw body (<c>MELA</c> read little-endian).</summary>
    private const uint UncompressedMagic = 0x414C454D;

    /// <summary>The circular dictionary size.</summary>
    private const int DictionarySize = 4096;

    /// <summary>The 207-byte prologue the dictionary is preseeded with (MS-OXRTFCP §2.1.2.3); the remainder is spaces.</summary>
    private const string InitialDictionaryText =
        "{\\rtf1\\ansi\\mac\\deff0\\deftab720{\\fonttbl;}{\\f0\\fnil \\froman \\fswiss "
        + "\\fmodern \\fscript \\fdecor MS Sans SerifSymbolArialTimes New RomanCourier"
        + "{\\colortbl\\red0\\green0\\blue0\r\n\\par \\pard\\plain\\f0\\fs20\\b\\i\\u\\tab\\tx";

    /// <summary>The eight interleaved slicing-by-8 lookup tables for the reflected CRC-32 polynomial (<c>0x04C11DB7</c> in normal form, equivalent to the format's pre-reflected <c>0xEDB88320</c>).</summary>
    private static readonly ulong[][] s_crcTables = CrcCore.BuildReflectedSlicingTables(32, 0x04C11DB7);

    /// <summary>
    /// Decompresses a compressed-RTF payload to the raw RTF bytes.
    /// </summary>
    /// <param name="data">The complete <c>PidTagRtfCompressed</c> payload, including the 16-byte header.</param>
    /// <param name="maxOutputBytes">
    /// The largest decompressed size the caller accepts; a payload whose declared or produced size exceeds it is
    /// rejected as malformed.
    /// </param>
    /// <returns>The RTF text bytes.</returns>
    /// <exception cref="OutlookFormatException">
    /// The header is truncated or carries an unknown magic, the declared sizes escape the payload, or the checksum does
    /// not match. The concrete type is the consuming format's exception (<c>OutlookMsgFormatException</c> or
    /// <c>OutlookPstFormatException</c>).
    /// </exception>
    internal static byte[] Decompress(ReadOnlySpan<byte> data, int maxOutputBytes)
    {
        ThrowHelper.ThrowIfZeroOrNegative(maxOutputBytes);
        if (data.Length < 16)
            throw MalformedHeader();

        uint compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(data);
        uint rawSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4));
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(8));
        uint crc = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(12));

        if (magic is not (CompressedMagic or UncompressedMagic))
            throw MalformedHeader();

        if (compressedSize < 12 || compressedSize - 12 > (uint)(data.Length - 16))
            throw MalformedData();

        ReadOnlySpan<byte> payload = data.Slice(16, (int)(compressedSize - 12));

        if (magic == UncompressedMagic)
        {
            if (crc != 0)
                throw ChecksumMismatch();
            if (rawSize > (uint)payload.Length)
                throw MalformedData();
            if (rawSize > (uint)maxOutputBytes)
                throw TooLarge();

            return payload.Slice(0, (int)rawSize).ToArray();
        }

        if (ComputeCrc(payload) != crc)
            throw ChecksumMismatch();

        // The declared size sits outside the checksum, so it is bounded rather than trusted: a control byte governs
        // eight tokens and a two-byte reference yields at most seventeen bytes, so seventeen input bytes expand to at
        // most 136 output bytes — eight to one — plus the dictionary the first references may replay.
        long ceiling = ((long)payload.Length * 8) + DictionarySize;
        if (rawSize > ceiling)
            throw MalformedData();
        if (rawSize > (uint)maxOutputBytes)
            throw TooLarge();

        return DecodeCompressed(payload, (int)rawSize);
    }

    /// <summary>
    /// Decompresses a <c>PidTagRtfCompressed</c> payload without a caller-imposed output ceiling; the declared size
    /// is still bounded by what the body can physically expand to.
    /// </summary>
    /// <param name="data">The complete payload, including the 16-byte header.</param>
    /// <returns>The decompressed RTF bytes.</returns>
    internal static byte[] Decompress(ReadOnlySpan<byte> data) =>
        Decompress(data, int.MaxValue);

    /// <summary>
    /// Computes the format's CRC over a byte span: zero initial value, table-driven, no final exclusive-or.
    /// </summary>
    /// <param name="data">The bytes to sum.</param>
    /// <returns>The checksum.</returns>
    internal static uint ComputeCrc(ReadOnlySpan<byte> data) =>
        (uint)CrcCore.UpdateReflectedSlicing(data, 0, s_crcTables, s_crcTables[0], 32);

    /// <summary>
    /// Decodes the LZ token stream against the preseeded circular dictionary into an exactly sized output buffer.
    /// </summary>
    /// <param name="payload">The compressed body after the header.</param>
    /// <param name="rawSize">The declared uncompressed size, which is also the output ceiling.</param>
    /// <returns>The decoded bytes: the whole buffer when the stream fills it, otherwise a copy of the bytes produced.</returns>
    /// <exception cref="OutlookFormatException">A token is cut short by the end of the body.</exception>
    /// <remarks>
    /// Decoding stops when the declared size is reached or the terminator reference (an offset equal to the current
    /// write position) is met; a body that ends early at a token boundary yields the bytes produced so far, while a
    /// body that ends inside a token is malformed.
    /// </remarks>
    private static byte[] DecodeCompressed(ReadOnlySpan<byte> payload, int rawSize)
    {
        var dictionary = new byte[DictionarySize];
        int seeded = System.Text.Encoding.ASCII.GetBytes(InitialDictionaryText, dictionary);
        dictionary.AsSpan(seeded).Fill((byte)' ');

        var output = new byte[rawSize];
        int written = 0;
        int writePosition = seeded;
        int position = 0;

        while (position < payload.Length)
        {
            byte control = payload[position++];
            for (int bit = 0; bit < 8; bit++)
            {
                if (written == rawSize)
                    return output;

                if ((control & (1 << bit)) != 0)
                {
                    // Dictionary reference: big-endian 16 bits — 12-bit offset, 4-bit (length - 2).
                    if (position + 2 > payload.Length)
                        throw MalformedData();

                    int token = (payload[position] << 8) | payload[position + 1];
                    position += 2;
                    int offset = token >> 4;
                    int length = (token & 0xF) + 2;
                    if (offset == writePosition)
                        return Trim(output, written);

                    for (int step = 0; step < length && written < rawSize; step++)
                    {
                        byte value = dictionary[(offset + step) % DictionarySize];
                        output[written++] = value;
                        dictionary[writePosition] = value;
                        writePosition = (writePosition + 1) % DictionarySize;
                    }
                }
                else
                {
                    // Literal byte.
                    if (position >= payload.Length)
                        throw MalformedData();

                    byte value = payload[position++];
                    output[written++] = value;
                    dictionary[writePosition] = value;
                    writePosition = (writePosition + 1) % DictionarySize;
                }
            }
        }

        return Trim(output, written);
    }

    /// <summary>
    /// Returns the produced prefix of an output buffer, avoiding a copy when the buffer was filled exactly.
    /// </summary>
    /// <param name="output">The output buffer.</param>
    /// <param name="written">The number of bytes produced.</param>
    /// <returns>The produced bytes.</returns>
    private static byte[] Trim(byte[] output, int written) =>
        written == output.Length ? output : output.AsSpan(0, written).ToArray();

    /// <summary>
    /// Creates the truncated-or-unknown-header exception for the consuming format.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static Exception MalformedHeader() =>
#if MSG
        new OutlookMsgFormatException(OutlookMsgResourceStrings.Format_Invalid_RtfCompressedHeader);
#elif OUTLOOK_PST
        new OutlookPstFormatException(OutlookPstResourceStrings.Format_Invalid_RtfCompressedHeader);
#endif

    /// <summary>
    /// Creates the declared-sizes-escape-the-payload exception for the consuming format.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static Exception MalformedData() =>
#if MSG
        new OutlookMsgFormatException(OutlookMsgResourceStrings.Format_Invalid_RtfCompressedData);
#elif OUTLOOK_PST
        new OutlookPstFormatException(OutlookPstResourceStrings.Format_Invalid_RtfCompressedData);
#endif

    /// <summary>
    /// Creates the format's over-limit exception for a declared or produced size above the caller's ceiling.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static Exception TooLarge() =>
#if MSG
        new OutlookMsgFormatException(OutlookMsgResourceStrings.Format_Invalid_RtfCompressedTooLarge);
#elif OUTLOOK_PST
        new OutlookPstFormatException(OutlookPstResourceStrings.Format_Invalid_RtfCompressedTooLarge);
#endif

    /// <summary>
    /// Creates the checksum-mismatch exception for the consuming format.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static Exception ChecksumMismatch() =>
#if MSG
        new OutlookMsgFormatException(OutlookMsgResourceStrings.Format_Invalid_RtfCompressedCrc);
#elif OUTLOOK_PST
        new OutlookPstFormatException(OutlookPstResourceStrings.Format_Invalid_RtfCompressedCrc);
#endif
}
