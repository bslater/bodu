// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8SharedStringTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Decodes the BIFF8 shared string table (SST), the workbook-global pool of deduplicated text values that cell records
/// reference by index.
/// </summary>
/// <remarks>
/// A single string may straddle the boundary between the SST record and one or more following <c>CONTINUE</c> records.
/// The split can fall between characters, and the compression flag (one byte indicating 8-bit or 16-bit characters) is
/// repeated at the start of each continued segment. This decoder reconstructs strings across those boundaries.
/// </remarks>
internal static class Biff8SharedStringTable
{
    /// <summary>
    /// Decodes the shared string table from the SST record payload and any following continuation payloads.
    /// </summary>
    /// <param name="blocks">The SST payload followed, in order, by the payloads of its continuation records.</param>
    /// <returns>The decoded unique strings, indexed as cells reference them.</returns>
    /// <exception cref="Biff8FormatException">Thrown when the table is truncated or otherwise malformed.</exception>
    internal static string[] Parse(IReadOnlyList<ReadOnlyMemory<byte>> blocks)
    {
        if (blocks.Count == 0)
            return [];

        ReadOnlySpan<byte> first = blocks[0].Span;
        if (first.Length < 8)
            throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8SharedString);

        uint unique = BinaryPrimitives.ReadUInt32LittleEndian(first.Slice(4));
        List<string> result = new();

        int block = 0;
        int offset = 8;

        for (uint index = 0; index < unique; index++)
        {
            AdvancePastBlockEnd(blocks, ref block, ref offset);

            ReadOnlySpan<byte> header = blocks[block].Span;
            if (offset + 3 > header.Length)
                throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8SharedString);

            int charCount = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(offset));
            offset += 2;
            byte flags = header[offset];
            offset += 1;

            bool hasRichRuns = (flags & 0x08) != 0;
            bool hasExtended = (flags & 0x04) != 0;

            int richRunCount = 0;
            if (hasRichRuns)
            {
                richRunCount = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(offset));
                offset += 2;
            }

            int extendedSize = 0;
            if (hasExtended)
            {
                extendedSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(offset));
                offset += 4;
            }

            result.Add(ReadCharacters(blocks, ref block, ref offset, charCount, (flags & 0x01) != 0));
            SkipBytes(blocks, ref block, ref offset, (richRunCount * 4) + extendedSize);
        }

        return [.. result];
    }

    /// <summary>
    /// Reads <paramref name="charCount" /> characters, crossing into continuation blocks as needed and re-reading the
    /// compression flag at each block boundary.
    /// </summary>
    /// <param name="blocks">The ordered SST and continuation payloads.</param>
    /// <param name="block">The current block index; advanced as boundaries are crossed.</param>
    /// <param name="offset">The current byte offset within the block; advanced as bytes are consumed.</param>
    /// <param name="charCount">The number of characters to read.</param>
    /// <param name="highByte">Whether the current segment uses 16-bit characters.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="Biff8FormatException">
    /// Thrown when the character data runs past the available blocks.
    /// </exception>
    private static string ReadCharacters(IReadOnlyList<ReadOnlyMemory<byte>> blocks, ref int block, ref int offset, int charCount, bool highByte)
    {
        StringBuilder builder = new(charCount);
        int remaining = charCount;
        bool high = highByte;

        while (remaining > 0)
        {
            if (offset >= blocks[block].Length)
            {
                block++;
                offset = 0;
                if (block >= blocks.Count)
                    throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8SharedString);

                high = (blocks[block].Span[offset] & 0x01) != 0;
                offset++;
            }

            ReadOnlySpan<byte> current = blocks[block].Span;
            if (high)
            {
                int available = (current.Length - offset) / 2;
                int take = Math.Min(available, remaining);
                if (take <= 0)
                    throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8SharedString);

                builder.Append(Encoding.Unicode.GetString(current.Slice(offset, take * 2)));
                offset += take * 2;
                remaining -= take;
            }
            else
            {
                int available = current.Length - offset;
                int take = Math.Min(available, remaining);
                if (take <= 0)
                    throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8SharedString);

                // Compressed segments encode one ISO-8859-1 code point per byte.
                for (int i = 0; i < take; i++)
                    builder.Append((char)current[offset + i]);

                offset += take;
                remaining -= take;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Skips <paramref name="count" /> trailing bytes (rich-text runs and phonetic data), crossing block boundaries.
    /// </summary>
    /// <param name="blocks">The ordered SST and continuation payloads.</param>
    /// <param name="block">The current block index; advanced as boundaries are crossed.</param>
    /// <param name="offset">The current byte offset within the block; advanced as bytes are skipped.</param>
    /// <param name="count">The number of trailing bytes to skip.</param>
    /// <exception cref="Biff8FormatException">Thrown when the trailing data runs past the available blocks.</exception>
    private static void SkipBytes(IReadOnlyList<ReadOnlyMemory<byte>> blocks, ref int block, ref int offset, int count)
    {
        int remaining = count;
        while (remaining > 0)
        {
            if (offset >= blocks[block].Length)
            {
                block++;
                offset = 0;
                if (block >= blocks.Count)
                    throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8SharedString);
            }

            int available = blocks[block].Length - offset;
            int take = Math.Min(available, remaining);
            offset += take;
            remaining -= take;
        }
    }

    /// <summary>
    /// Advances to the next block when the current offset has reached the end of the current block.
    /// </summary>
    /// <param name="blocks">The ordered SST and continuation payloads.</param>
    /// <param name="block">The current block index; advanced when at a boundary.</param>
    /// <param name="offset">The current byte offset; reset to zero on advance.</param>
    private static void AdvancePastBlockEnd(IReadOnlyList<ReadOnlyMemory<byte>> blocks, ref int block, ref int offset)
    {
        if (block < blocks.Count && offset >= blocks[block].Length)
        {
            block++;
            offset = 0;
        }
    }
}
