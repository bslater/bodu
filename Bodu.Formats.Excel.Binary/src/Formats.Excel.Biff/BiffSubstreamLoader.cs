// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSubstreamLoader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;

namespace Bodu.Formats.Excel.Biff;

/// <summary>
/// Reads a single BIFF substream - the records from a beginning-of-file record up to and including the end-of-file
/// record that matches it - from a workbook stream into a contiguous buffer.
/// </summary>
/// <remarks>
/// <para>
/// A sheet's substream can hold whole substreams of its own: a chart embedded in a worksheet is a chart substream, from
/// its beginning-of-file record to its end-of-file record, after the chart's <c>OBJ</c> record ([MS-XLS] 2.1.7.20.6).
/// The two records are therefore counted in pairs, and only the end-of-file record that matches the opening
/// beginning-of-file record ends a sheet's substream. A sheet's substream without that end-of-file record ends where
/// another substream begins: the caller passes the nearest <c>lbPlyPos</c> offset past the sheet's own, or the end of
/// the stream when no sheet's substream begins after it.
/// </para>
/// <para>
/// The workbook globals never hold a nested substream, so a beginning-of-file record met inside them begins the first
/// sheet's substream: globals that have lost their end-of-file record end there, and that record is not part of them.
/// </para>
/// <para>
/// Reading a bounded substream keeps memory proportional to one sheet (or the workbook globals) rather than the whole
/// workbook. The caller positions the source stream at the substream's start (for a sheet, the <c>lbPlyPos</c> offset
/// of its bound-sheet record) before calling <see cref="ReadSubstream(System.IO.Stream, long)" /> or
/// <see cref="ReadGlobalsSubstream(System.IO.Stream)" />.
/// </para>
/// </remarks>
internal static class BiffSubstreamLoader
{
    /// <summary>
    /// Reads the workbook globals substream beginning at the stream's current position, stopping after its end-of-file
    /// record or before a beginning-of-file record that follows its own, whichever comes first.
    /// </summary>
    /// <param name="stream">The workbook stream, positioned at the globals' first record.</param>
    /// <returns>
    /// The globals bytes, from their beginning-of-file record through their end-of-file record, or up to the next
    /// beginning-of-file record when they have lost their end-of-file record.
    /// </returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the stream ends before a complete record header or payload, or before both an end-of-file record and
    /// a later beginning-of-file record.
    /// </exception>
    public static byte[] ReadGlobalsSubstream(Stream stream) =>
        Read(stream, long.MaxValue, nests: false);

    /// <summary>
    /// Reads the sheet substream beginning at the stream's current position, stopping after the end-of-file record that
    /// matches its beginning-of-file record or before the first record that begins at <paramref name="limit" />,
    /// whichever comes first.
    /// </summary>
    /// <param name="stream">The workbook stream, positioned at the substream's first record.</param>
    /// <param name="limit">
    /// The offset at which another substream begins, or the length of the stream when none begins after this one;
    /// <see cref="long.MaxValue" /> when the substream ends only at its matching end-of-file record.
    /// </param>
    /// <returns>
    /// The substream bytes, from its beginning-of-file record through the end-of-file record that matches it, or up to
    /// <paramref name="limit" />, with every substream nested in it.
    /// </returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the stream ends before a complete record header or payload, or before both the matching end-of-file
    /// record and <paramref name="limit" />.
    /// </exception>
    public static byte[] ReadSubstream(Stream stream, long limit) =>
        Read(stream, limit, nests: true);

    /// <summary>
    /// Reads a substream beginning at the stream's current position.
    /// </summary>
    /// <param name="stream">The workbook stream, positioned at the substream's first record.</param>
    /// <param name="limit">The offset at which the substream ends when no end-of-file record ends it first.</param>
    /// <param name="nests">
    /// Whether the substream can hold nested substreams; when it cannot, a beginning-of-file record after its own ends
    /// it, and that record is not read.
    /// </param>
    /// <returns>The substream bytes.</returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the stream ends before a complete record header or payload, or before the substream ends.
    /// </exception>
    private static byte[] Read(Stream stream, long limit, bool nests)
    {
        using MemoryStream buffer = new();
        Span<byte> header = stackalloc byte[BiffLimits.RecordHeaderSize];
        long position = stream.Position;
        int depth = 0;

        // The first record is read wherever it begins, so a substream that begins at the end of the stream is truncated
        // rather than empty; any later record that begins at the limit belongs to the next substream.
        do
        {
            ReadExactly(stream, header);
            _ = BiffRecordHeader.TryParse(header, out BiffRecordHeader record);

            // A substream that cannot nest, the globals, ends before a BOF met inside it: that BOF begins the next one.
            if (!nests && depth > 0 && record.Type == BiffRecordType.Bof)
                break;

            buffer.Write(header);
            CopyExactly(stream, buffer, record.Length);
            position += BiffLimits.RecordHeaderSize + record.Length;

            // Each BOF opens a substream and each EOF closes the innermost one open, so the EOF that closes the opening
            // BOF ends this substream. A substream that does not open with a BOF ends at its first EOF.
            if (record.Type == BiffRecordType.Bof)
            {
                depth++;
            }
            else if (record.Type == BiffRecordType.Eof)
            {
                depth--;
                if (depth <= 0)
                    break;
            }
        }
        while (position < limit);

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
