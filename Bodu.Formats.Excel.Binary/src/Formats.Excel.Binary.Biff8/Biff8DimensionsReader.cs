// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8DimensionsReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Excel.Binary.Biff8;

/// <summary>
/// Reads the used range from the <c>DIMENSIONS</c> record near the start of a worksheet substream, scanning only as far
/// as that record rather than parsing the whole sheet.
/// </summary>
/// <remarks>
/// The <c>DIMENSIONS</c> record precedes a worksheet's cell and row records, so the scan stops at the first such record
/// (or at the end-of-file marker) when no dimensions are present, keeping the open-time cost of describing every sheet
/// proportional to the worksheet headers rather than their bodies.
/// </remarks>
internal static class Biff8DimensionsReader
{
    /// <summary>The record identifier of the worksheet <c>ROW</c> record, which begins the sheet body.</summary>
    private const ushort RowRecord = 0x0208;

    /// <summary>
    /// Reads the used range declared by the worksheet substream beginning at the specified offset.
    /// </summary>
    /// <param name="stream">The workbook stream; seeked to <paramref name="offset" /> by this method.</param>
    /// <param name="offset">The byte offset of the worksheet substream within the workbook stream.</param>
    /// <returns>
    /// The declared used range, or the default value when the substream declares no <c>DIMENSIONS</c> record.
    /// </returns>
    public static ExcelWorksheetDimensions Read(Stream stream, long offset)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        Span<byte> header = stackalloc byte[4];

        while (TryReadFull(stream, header))
        {
            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(header);
            int length = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(2));

            if (id == (ushort)Biff8RecordType.Dimensions)
            {
                byte[] payload = new byte[length];
                return TryReadFull(stream, payload) ? Parse(payload) : default;
            }

            if (id == (ushort)Biff8RecordType.Eof || IsSheetBody(id))
                break;

            stream.Seek(length, SeekOrigin.Current);
        }

        return default;
    }

    /// <summary>
    /// Parses a BIFF8 <c>DIMENSIONS</c> payload into a used range.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The declared used range, or the default value when the payload is too short.</returns>
    private static ExcelWorksheetDimensions Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 12)
            return default;

        // BIFF8 DIMENSIONS stores the last row and column as one-past-the-end (rwMac, colMac).
        int firstRow = (int)BinaryPrimitives.ReadUInt32LittleEndian(payload);
        int rowExtent = (int)BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4));
        int firstColumn = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(8));
        int columnExtent = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(10));

        return new ExcelWorksheetDimensions(firstRow, Math.Max(0, rowExtent - firstRow), firstColumn, Math.Max(0, columnExtent - firstColumn));
    }

    /// <summary>
    /// Determines whether a record identifier begins the worksheet body, after which no <c>DIMENSIONS</c> record appears.
    /// </summary>
    /// <param name="id">The record identifier.</param>
    /// <returns><see langword="true" /> when the record is a row or value-bearing cell record.</returns>
    private static bool IsSheetBody(ushort id) =>
        id == RowRecord || id is
            (ushort)Biff8RecordType.Number or
            (ushort)Biff8RecordType.Rk or
            (ushort)Biff8RecordType.MulRk or
            (ushort)Biff8RecordType.LabelSst or
            (ushort)Biff8RecordType.Label or
            (ushort)Biff8RecordType.BoolErr or
            (ushort)Biff8RecordType.Blank or
            (ushort)Biff8RecordType.MulBlank or
            (ushort)Biff8RecordType.Formula;

    /// <summary>
    /// Reads exactly <paramref name="destination" />.Length bytes from the stream.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="destination">The buffer to fill.</param>
    /// <returns><see langword="true" /> when the buffer was filled; <see langword="false" /> at the end of the stream.</returns>
    private static bool TryReadFull(Stream stream, Span<byte> destination)
    {
        int total = 0;
        while (total < destination.Length)
        {
            int read = stream.Read(destination.Slice(total));
            if (read == 0)
                return false;

            total += read;
        }

        return true;
    }
}
