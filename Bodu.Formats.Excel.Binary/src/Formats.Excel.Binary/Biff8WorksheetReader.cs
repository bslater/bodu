// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorksheetReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Translates the cell records of a single worksheet substream into <see cref="ExcelCell" /> values.
/// </summary>
/// <remarks>
/// Only value-bearing records are emitted (string, number, boolean, and error cells). Blank cells and records the
/// reader does not interpret are skipped, so the result is a sparse sequence in record order.
/// </remarks>
internal static class Biff8WorksheetReader
{
    /// <summary>
    /// Reads the populated cells from the records of a worksheet substream.
    /// </summary>
    /// <param name="records">The full ordered record list of the workbook.</param>
    /// <param name="start">The inclusive record index at which the worksheet substream begins.</param>
    /// <param name="endExclusive">The exclusive record index at which the worksheet substream ends.</param>
    /// <param name="sharedStrings">The workbook shared string table.</param>
    /// <returns>The populated cells of the worksheet, in record order.</returns>
    /// <exception cref="Biff8FormatException">
    /// Thrown when a cell references a shared string outside the table or a record is malformed.
    /// </exception>
    internal static IEnumerable<ExcelCell> ReadCells(
        IReadOnlyList<Biff8Record> records,
        int start,
        int endExclusive,
        string[] sharedStrings)
    {
        for (var i = start; i < endExclusive; i++)
        {
            Biff8Record record = records[i];
            switch (record.Type)
            {
                case Biff8RecordType.LabelSst:
                    yield return ReadLabelSst(record.Payload.Span, sharedStrings);
                    break;

                case Biff8RecordType.Number:
                    yield return ReadNumber(record.Payload.Span);
                    break;

                case Biff8RecordType.Rk:
                    yield return ReadRk(record.Payload.Span);
                    break;

                case Biff8RecordType.BoolErr:
                    yield return ReadBoolErr(record.Payload.Span);
                    break;

                case Biff8RecordType.MulRk:
                    foreach (ExcelCell cell in ReadMulRk(record.Payload))
                        yield return cell;
                    break;

                default:
                    // BLANK, MULBLANK, ROW, formatting, and any unrecognized records carry no value to surface.
                    break;
            }
        }
    }

    /// <summary>
    /// Reads a <c>LABELSST</c> cell, resolving its shared-string index.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="sharedStrings">The workbook shared string table.</param>
    /// <returns>The decoded text cell.</returns>
    /// <exception cref="Biff8FormatException">Thrown when the referenced string index is out of range.</exception>
    private static ExcelCell ReadLabelSst(ReadOnlySpan<byte> payload, string[] sharedStrings)
    {
        int row = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        int column = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));
        uint stringIndex = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(6));

        if (stringIndex >= (uint)sharedStrings.Length)
        {
            throw new Biff8FormatException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Format_Invalid_Biff8StringIndex, stringIndex));
        }

        return ExcelCell.Text(row, column, sharedStrings[stringIndex]);
    }

    /// <summary>
    /// Reads a <c>NUMBER</c> cell carrying an IEEE 754 double.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded numeric cell.</returns>
    private static ExcelCell ReadNumber(ReadOnlySpan<byte> payload)
    {
        int row = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        int column = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));
        double value = BinaryPrimitives.ReadDoubleLittleEndian(payload.Slice(6));

        return ExcelCell.Number(row, column, value);
    }

    /// <summary>
    /// Reads an <c>RK</c> cell carrying a single RK-encoded number.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded numeric cell.</returns>
    private static ExcelCell ReadRk(ReadOnlySpan<byte> payload)
    {
        int row = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        int column = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));
        uint rk = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(6));

        return ExcelCell.Number(row, column, DecodeRk(rk));
    }

    /// <summary>
    /// Reads a <c>BOOLERR</c> cell, which carries either a boolean or an error code.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>A boolean cell, or an error cell when the value is an error code.</returns>
    private static ExcelCell ReadBoolErr(ReadOnlySpan<byte> payload)
    {
        int row = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        int column = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));
        byte value = payload[6];
        byte isError = payload[7];

        return isError != 0
            ? ExcelCell.Error(row, column)
            : ExcelCell.Boolean(row, column, value != 0);
    }

    /// <summary>
    /// Reads a <c>MULRK</c> record, expanding it into one numeric cell per contained RK value.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded numeric cells, in column order.</returns>
    private static ExcelCell[] ReadMulRk(ReadOnlyMemory<byte> payload)
    {
        ReadOnlySpan<byte> span = payload.Span;
        int row = BinaryPrimitives.ReadUInt16LittleEndian(span);
        int firstColumn = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(2));

        // Payload: row(2) + colFirst(2) + N * (ixfe(2) + rk(4)) + colLast(2).
        var count = (span.Length - 6) / 6;
        var cells = new ExcelCell[count];
        for (var k = 0; k < count; k++)
        {
            uint rk = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4 + (k * 6) + 2));
            cells[k] = ExcelCell.Number(row, firstColumn + k, DecodeRk(rk));
        }

        return cells;
    }

    /// <summary>
    /// Decodes an RK-encoded value into a <see cref="double" />.
    /// </summary>
    /// <param name="rk">The 32-bit RK value.</param>
    /// <returns>The decoded number.</returns>
    /// <remarks>
    /// The low two bits are flags: bit 0 indicates the value was multiplied by 100, and bit 1 indicates the remaining
    /// 30 bits are a signed integer rather than the high 30 bits of an IEEE 754 double.
    /// </remarks>
    private static double DecodeRk(uint rk)
    {
        var dividedByHundred = (rk & 0x01) != 0;
        var isInteger = (rk & 0x02) != 0;

        double value;
        if (isInteger)
        {
            // Arithmetic shift sign-extends the signed 30-bit integer stored in the high bits.
            value = ((int)rk) >> 2;
        }
        else
        {
            ulong bits = (ulong)(rk & 0xFFFFFFFC) << 32;
            value = BitConverter.Int64BitsToDouble((long)bits);
        }

        return dividedByHundred ? value / 100.0 : value;
    }
}
