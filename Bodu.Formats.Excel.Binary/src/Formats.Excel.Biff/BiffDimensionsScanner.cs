// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffDimensionsScanner.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;

namespace Bodu.Formats.Excel.Biff;

/// <summary>
/// Locates and decodes the <c>DIMENSIONS</c> record at the head of a sheet substream without loading the sheet's cell
/// records.
/// </summary>
/// <remarks>
/// <para>
/// The scan reads record headers from the stream, skipping payloads it does not need, and stops at the first cell or
/// row record (the dimensions precede them) or at the substream's end. A missing or malformed record yields the
/// default, empty dimensions rather than an error, because the used range is descriptive metadata: the cells are read
/// from the substream itself.
/// </para>
/// <para>
/// The scan keeps to the sheet's own substream by the rules its cells are read by: a substream nested in the sheet,
/// such as an embedded chart with a <c>DIMENSIONS</c> record of its own, is skipped from its beginning-of-file record
/// through the end-of-file record that matches it, and the sheet's substream ends at its own end-of-file record or
/// where another sheet's substream begins.
/// </para>
/// </remarks>
internal static class BiffDimensionsScanner
{
    /// <summary>
    /// Scans the substream at the specified offset for its <c>DIMENSIONS</c> record.
    /// </summary>
    /// <param name="stream">The workbook stream.</param>
    /// <param name="offset">The absolute offset of the substream's BOF record.</param>
    /// <param name="limit">
    /// The offset at which another sheet's substream begins, or the length of the stream when none begins after this
    /// one.
    /// </param>
    /// <param name="version">The BIFF version, which selects the record's layout.</param>
    /// <returns>The decoded used range, or the default when no valid record precedes the sheet body.</returns>
    public static ExcelWorksheetDimensions Read(Stream stream, long offset, long limit, BiffVersion version)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        Span<byte> header = stackalloc byte[BiffLimits.RecordHeaderSize];
        long position = offset;
        int depth = 0;

        while (position < limit && TryReadFull(stream, header))
        {
            _ = BiffRecordHeader.TryParse(header, out BiffRecordHeader record);
            position += BiffLimits.RecordHeaderSize + record.Length;

            // Each BOF opens a substream and each EOF closes the innermost one open. Only the records of the sheet's
            // own substream, at depth one, or at depth zero when it lacks a BOF, are the sheet's.
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
            else if (depth <= 1)
            {
                if (record.Type == BiffRecordType.Dimensions)
                {
                    byte[] payload = new byte[record.Length];
                    return TryReadFull(stream, payload) ? Decode(payload, version) : default;
                }

                if (IsSheetBody(record.Type))
                    break;
            }

            stream.Seek(record.Length, SeekOrigin.Current);
        }

        return default;
    }

    /// <summary>
    /// Decodes a <c>DIMENSIONS</c> payload into the worksheet's used range.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="version">The BIFF version.</param>
    /// <returns>The used range, or the default when the payload is malformed.</returns>
    private static ExcelWorksheetDimensions Decode(byte[] payload, BiffVersion version)
    {
        byte[] record = new byte[BiffLimits.RecordHeaderSize + payload.Length];
        new BiffRecordHeader((ushort)BiffRecordType.Dimensions, (ushort)payload.Length).WriteTo(record);
        payload.CopyTo(record.AsSpan(BiffLimits.RecordHeaderSize));

        try
        {
            var reader = new BiffReader(record, new BiffReaderOptions { Version = version });
            if (!reader.Read())
                return default;

            BiffDimensionsRecord dimensions = reader.GetDimensions();
            return new ExcelWorksheetDimensions(dimensions.FirstRow, dimensions.RowCount, dimensions.FirstColumn, dimensions.ColumnCount);
        }
        catch (BiffFormatException)
        {
            return default;
        }
    }

    /// <summary>
    /// Determines whether a record type belongs to the sheet body, after which no <c>DIMENSIONS</c> record appears.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <returns><see langword="true" /> for a row or cell record.</returns>
    private static bool IsSheetBody(BiffRecordType type) =>
        type is BiffRecordType.Row
            or BiffRecordType.Number
            or BiffRecordType.Rk
            or BiffRecordType.MulRk
            or BiffRecordType.LabelSst
            or BiffRecordType.Label
            or BiffRecordType.RString
            or BiffRecordType.BoolErr
            or BiffRecordType.Blank
            or BiffRecordType.MulBlank
            or BiffRecordType.Formula;

    /// <summary>
    /// Fills the buffer from the stream, reporting whether the stream held enough bytes.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="destination">The buffer to fill.</param>
    /// <returns><see langword="true" /> when the buffer was filled.</returns>
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
