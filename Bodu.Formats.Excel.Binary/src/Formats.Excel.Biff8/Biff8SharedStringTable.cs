// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8SharedStringTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.Formats.Excel.Biff8;

/// <summary>
/// Decodes the BIFF8 shared string table (SST), the workbook-global pool of deduplicated text values that cell records
/// reference by index.
/// </summary>
/// <remarks>
/// A single string may straddle the boundary between the SST record and one or more following <c>CONTINUE</c> records.
/// The split can fall between characters, and the compression flag (one byte indicating 8-bit or 16-bit characters) is
/// repeated at the start of each continued segment. This decoder reconstructs strings across those boundaries and
/// rejects a table that is truncated or otherwise malformed with <see cref="ExcelBinaryFormatException" />.
/// </remarks>
internal static class Biff8SharedStringTable
{
    /// <summary>
    /// Decodes the shared string table from the SST record payload and any following continuation payloads.
    /// </summary>
    /// <param name="blocks">The SST payload followed, in order, by the payloads of its continuation records.</param>
    /// <returns>The decoded unique strings, indexed as cells reference them.</returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the table is truncated or otherwise malformed.
    /// </exception>
    internal static string[] Parse(IReadOnlyList<ReadOnlyMemory<byte>> blocks)
    {
        if (blocks.Count == 0)
            return [];

        ReadOnlySpan<byte> first = blocks[0].Span;
        if (first.Length < 8)
            throw Malformed();

        uint unique = BinaryPrimitives.ReadUInt32LittleEndian(first.Slice(4));

        // Guard against a hostile or corrupt count that would otherwise drive an unbounded allocation; the count cannot
        // exceed the number of three-byte minimum string headers the available bytes could hold.
        long totalBytes = 0;
        for (int i = 0; i < blocks.Count; i++)
            totalBytes += blocks[i].Length;
        if (unique > totalBytes)
            throw Malformed();

        List<string> result = new((int)Math.Min(unique, 1024));

        int block = 0;
        int offset = 8;

        for (uint index = 0; index < unique; index++)
        {
            AdvancePastBlockEnd(blocks, ref block, ref offset);
            if (block >= blocks.Count)
                throw Malformed();

            ReadOnlySpan<byte> header = blocks[block].Span;
            if (offset + 3 > header.Length)
                throw Malformed();

            int charCount = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(offset));
            offset += 2;
            byte flags = header[offset];
            offset += 1;

            bool hasRichRuns = (flags & 0x08) != 0;
            bool hasExtended = (flags & 0x04) != 0;

            int richRunCount = 0;
            if (hasRichRuns)
            {
                if (offset + 2 > header.Length)
                    throw Malformed();

                richRunCount = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(offset));
                offset += 2;
            }

            int extendedSize = 0;
            if (hasExtended)
            {
                if (offset + 4 > header.Length)
                    throw Malformed();

                extendedSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(offset));
                offset += 4;
            }

            if (richRunCount < 0 || extendedSize < 0)
                throw Malformed();

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
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the character data runs past the available blocks.
    /// </exception>
    private static string ReadCharacters(IReadOnlyList<ReadOnlyMemory<byte>> blocks, ref int block, ref int offset, int charCount, bool highByte)
    {
        if (charCount < 0)
            throw Malformed();

        StringBuilder builder = new(charCount);
        int remaining = charCount;
        bool high = highByte;

        while (remaining > 0)
        {
            if (offset >= blocks[block].Length)
            {
                block++;
                offset = 0;

                // A continued string re-reads its compression flag from the first byte of the next block. A crafted
                // zero-length CONTINUE block would index an empty span; reject it as a catchable format error rather
                // than letting an IndexOutOfRangeException escape the documented ExcelBinaryFormatException contract.
                if (block >= blocks.Count || blocks[block].Length == 0)
                    throw Malformed();

                high = (blocks[block].Span[offset] & 0x01) != 0;
                offset++;
            }

            ReadOnlySpan<byte> current = blocks[block].Span;
            if (high)
            {
                int available = (current.Length - offset) / 2;
                int take = Math.Min(available, remaining);
                if (take <= 0)
                    throw Malformed();

                builder.Append(Encoding.Unicode.GetString(current.Slice(offset, take * 2)));
                offset += take * 2;
                remaining -= take;
            }
            else
            {
                int available = current.Length - offset;
                int take = Math.Min(available, remaining);
                if (take <= 0)
                    throw Malformed();

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
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the trailing data runs past the available blocks.
    /// </exception>
    private static void SkipBytes(IReadOnlyList<ReadOnlyMemory<byte>> blocks, ref int block, ref int offset, int count)
    {
        int remaining = count;
        while (remaining > 0)
        {
            if (block >= blocks.Count)
                throw Malformed();

            if (offset >= blocks[block].Length)
            {
                block++;
                offset = 0;
                continue;
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

    /// <summary>
    /// Creates the malformed shared-string-table exception.
    /// </summary>
    /// <returns>An <see cref="ExcelBinaryFormatException" /> describing the failure.</returns>
    private static ExcelBinaryFormatException Malformed() =>
        new(ExcelBinaryResourceStrings.Format_Invalid_Biff8SharedString);
}
