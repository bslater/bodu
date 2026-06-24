// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8SubstreamLoader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Excel.Biff8;

/// <summary>
/// Reads a single BIFF substream — the records from a beginning-of-file marker up to and including the matching
/// end-of-file marker — from a workbook stream into a contiguous buffer.
/// </summary>
/// <remarks>
/// Reading a bounded substream keeps memory proportional to one sheet (or the workbook globals) rather than the whole
/// workbook. The caller positions the source stream at the substream's start (for a sheet, the <c>lbPlyPos</c> offset
/// of its bound-sheet record) before calling <see cref="ReadSubstream(System.IO.Stream)" />.
/// </remarks>
internal static class Biff8SubstreamLoader
{
    /// <summary>
    /// Reads the substream beginning at the stream's current position, stopping after the first end-of-file record.
    /// </summary>
    /// <param name="stream">The workbook stream, positioned at the substream's first record.</param>
    /// <returns>The substream bytes, from its beginning-of-file record through its end-of-file record.</returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the stream ends before a complete record header or payload, or before an end-of-file record.
    /// </exception>
    public static byte[] ReadSubstream(Stream stream)
    {
        using MemoryStream buffer = new();
        Span<byte> header = stackalloc byte[4];

        while (true)
        {
            ReadExactly(stream, header);
            buffer.Write(header);

            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(header);
            int length = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(2));
            CopyExactly(stream, buffer, length);

            if (id == (ushort)Biff8RecordType.Eof)
                break;
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Reads exactly <paramref name="destination" />.Length bytes, failing if the stream ends first.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="destination">The buffer to fill.</param>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the stream ends before the buffer is filled.
    /// </exception>
    private static void ReadExactly(Stream stream, Span<byte> destination)
    {
        int total = 0;
        while (total < destination.Length)
        {
            int read = stream.Read(destination.Slice(total));
            if (read == 0)
                throw new ExcelBinaryFormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

            total += read;
        }
    }

    /// <summary>
    /// Copies exactly <paramref name="count" /> bytes from the stream into the buffer, failing if the stream ends
    /// first.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the stream ends before the bytes are copied.
    /// </exception>
    private static void CopyExactly(Stream stream, MemoryStream buffer, int count)
    {
        if (count == 0)
            return;

        Span<byte> chunk = stackalloc byte[1024];
        int remaining = count;
        while (remaining > 0)
        {
            int want = Math.Min(remaining, chunk.Length);
            int read = stream.Read(chunk.Slice(0, want));
            if (read == 0)
                throw new ExcelBinaryFormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

            buffer.Write(chunk.Slice(0, read));
            remaining -= read;
        }
    }
}
