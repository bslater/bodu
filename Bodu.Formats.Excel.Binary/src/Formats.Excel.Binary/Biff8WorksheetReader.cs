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
/// Only value-bearing records are emitted (string, number, boolean, and error cells, including the cached result of a
/// formula cell). Blank cells and records the reader does not interpret are skipped, so the result is a sparse sequence
/// in record order.
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
    /// <param name="formats">The workbook format table used to resolve each cell's number format.</param>
    /// <returns>The populated cells of the worksheet, in record order.</returns>
    /// <exception cref="Biff8FormatException">
    /// Thrown when a cell references a shared string outside the table or a record is malformed.
    /// </exception>
    internal static IEnumerable<ExcelCell> ReadCells(
        IReadOnlyList<Biff8Record> records,
        int start,
        int endExclusive,
        string[] sharedStrings,
        Biff8FormatTable formats)
    {
        for (int i = start; i < endExclusive; i++)
        {
            Biff8Record record = records[i];
            switch (record.Type)
            {
                case Biff8RecordType.LabelSst:
                    yield return ReadLabelSst(record.Payload.Span, sharedStrings, formats);
                    break;

                case Biff8RecordType.Number:
                    yield return ReadNumber(record.Payload.Span, formats);
                    break;

                case Biff8RecordType.Rk:
                    yield return ReadRk(record.Payload.Span, formats);
                    break;

                case Biff8RecordType.BoolErr:
                    yield return ReadBoolErr(record.Payload.Span, formats);
                    break;

                case Biff8RecordType.MulRk:
                    foreach (ExcelCell cell in ReadMulRk(record.Payload, formats))
                        yield return cell;
                    break;

                case Biff8RecordType.Formula:
                    yield return ReadFormula(records, ref i, endExclusive, formats);
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
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded text cell.</returns>
    /// <exception cref="Biff8FormatException">Thrown when the referenced string index is out of range.</exception>
    private static ExcelCell ReadLabelSst(ReadOnlySpan<byte> payload, string[] sharedStrings, Biff8FormatTable formats)
    {
        int row = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        int column = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));
        ushort xfIndex = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4));
        uint stringIndex = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(6));

        if (stringIndex >= (uint)sharedStrings.Length)
        {
            throw new Biff8FormatException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Format_Invalid_Biff8StringIndex, stringIndex));
        }

        return ExcelCell.Text(row, column, sharedStrings[stringIndex], formats.GetFormatIndex(xfIndex));
    }

    /// <summary>
    /// Reads a <c>NUMBER</c> cell carrying an IEEE 754 double.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded numeric cell.</returns>
    private static ExcelCell ReadNumber(ReadOnlySpan<byte> payload, Biff8FormatTable formats)
    {
        int row = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        int column = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));
        ushort xfIndex = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4));
        double value = BinaryPrimitives.ReadDoubleLittleEndian(payload.Slice(6));

        return ExcelCell.Number(row, column, value, formats.GetFormatIndex(xfIndex), formats.IsDateFormatted(xfIndex));
    }

    /// <summary>
    /// Reads an <c>RK</c> cell carrying a single RK-encoded number.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded numeric cell.</returns>
    private static ExcelCell ReadRk(ReadOnlySpan<byte> payload, Biff8FormatTable formats)
    {
        int row = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        int column = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));
        ushort xfIndex = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4));
        uint rk = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(6));

        return ExcelCell.Number(row, column, DecodeRk(rk), formats.GetFormatIndex(xfIndex), formats.IsDateFormatted(xfIndex));
    }

    /// <summary>
    /// Reads a <c>BOOLERR</c> cell, which carries either a boolean or an error code.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>A boolean cell, or an error cell when the value is an error code.</returns>
    private static ExcelCell ReadBoolErr(ReadOnlySpan<byte> payload, Biff8FormatTable formats)
    {
        int row = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        int column = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));
        ushort xfIndex = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4));
        byte value = payload[6];
        byte isError = payload[7];

        return isError != 0
            ? ExcelCell.Error(row, column, (ExcelErrorCode)value, formats.GetFormatIndex(xfIndex))
            : ExcelCell.Boolean(row, column, value != 0, formats.GetFormatIndex(xfIndex));
    }

    /// <summary>
    /// Reads a <c>MULRK</c> record, expanding it into one numeric cell per contained RK value.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded numeric cells, in column order.</returns>
    private static ExcelCell[] ReadMulRk(ReadOnlyMemory<byte> payload, Biff8FormatTable formats)
    {
        ReadOnlySpan<byte> span = payload.Span;
        int row = BinaryPrimitives.ReadUInt16LittleEndian(span);
        int firstColumn = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(2));

        // Payload: row(2) + colFirst(2) + N * (ixfe(2) + rk(4)) + colLast(2).
        int count = (span.Length - 6) / 6;
        var cells = new ExcelCell[count];
        for (int k = 0; k < count; k++)
        {
            ushort xfIndex = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(4 + (k * 6)));
            uint rk = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4 + (k * 6) + 2));
            cells[k] = ExcelCell.Number(row, firstColumn + k, DecodeRk(rk), formats.GetFormatIndex(xfIndex), formats.IsDateFormatted(xfIndex));
        }

        return cells;
    }

    /// <summary>
    /// Reads a <c>FORMULA</c> cell, surfacing the cached result of its last calculation. No formula evaluation is
    /// performed; only the stored result value is read.
    /// </summary>
    /// <param name="records">The full ordered record list of the workbook.</param>
    /// <param name="index">
    /// The index of the formula record; advanced past a trailing <c>STRING</c> record when the cached result is text.
    /// </param>
    /// <param name="endExclusive">The exclusive record index at which the worksheet substream ends.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded cell carrying the cached result.</returns>
    /// <exception cref="Biff8FormatException">Thrown when the formula record is malformed.</exception>
    /// <remarks>
    /// The eight-byte cached result is an IEEE 754 double unless its trailing two bytes are <c>0xFFFF</c>, which marks
    /// a non-numeric result whose leading byte selects a string, boolean, error, or empty-string value. A string result
    /// is carried by the immediately following <c>STRING</c> record.
    /// </remarks>
    private static ExcelCell ReadFormula(IReadOnlyList<Biff8Record> records, ref int index, int endExclusive, Biff8FormatTable formats)
    {
        ReadOnlySpan<byte> payload = records[index].Payload.Span;
        if (payload.Length < 14)
            throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

        int row = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        int column = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));
        ushort xfIndex = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4));
        ushort formatIndex = formats.GetFormatIndex(xfIndex);
        ReadOnlySpan<byte> result = payload.Slice(6, 8);

        // A trailing 0xFFFF marks a non-numeric cached result; the leading byte selects which kind.
        if (result[6] == 0xFF && result[7] == 0xFF)
        {
            return result[0] switch
            {
                0 => ExcelCell.Text(row, column, ReadCachedString(records, ref index, endExclusive), formatIndex),
                1 => ExcelCell.Boolean(row, column, result[2] != 0, formatIndex),
                2 => ExcelCell.Error(row, column, (ExcelErrorCode)result[2], formatIndex),

                // 3 is an explicit empty string; any other marker is treated as one defensively.
                _ => ExcelCell.Text(row, column, string.Empty, formatIndex),
            };
        }

        return ExcelCell.Number(row, column, BinaryPrimitives.ReadDoubleLittleEndian(result), formatIndex, formats.IsDateFormatted(xfIndex));
    }

    /// <summary>
    /// Reads the cached string result that follows a formula cell, consuming the trailing <c>STRING</c> record.
    /// </summary>
    /// <param name="records">The full ordered record list of the workbook.</param>
    /// <param name="index">The index of the formula record; advanced to the <c>STRING</c> record when present.</param>
    /// <param name="endExclusive">The exclusive record index at which the worksheet substream ends.</param>
    /// <returns>The decoded string, or an empty string when no <c>STRING</c> record follows.</returns>
    /// <exception cref="Biff8FormatException">Thrown when the <c>STRING</c> record is malformed.</exception>
    private static string ReadCachedString(IReadOnlyList<Biff8Record> records, ref int index, int endExclusive)
    {
        int next = index + 1;
        if (next >= endExclusive || records[next].Type != Biff8RecordType.String)
            return string.Empty;

        index = next;
        ReadOnlySpan<byte> payload = records[next].Payload.Span;
        if (payload.Length < 3)
            throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

        int charCount = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        bool highByte = (payload[2] & 0x01) != 0;

        return DecodeCharacters(payload, 3, charCount, highByte);
    }

    /// <summary>
    /// Decodes a BIFF8 character run that is either 16-bit Unicode or compressed 8-bit (one ISO-8859-1 code point per
    /// byte).
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset at which the character data begins.</param>
    /// <param name="charCount">The number of characters.</param>
    /// <param name="highByte">Whether the characters are 16-bit (<see langword="true" />) or compressed 8-bit.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="Biff8FormatException">Thrown when the character data runs past the payload.</exception>
    private static string DecodeCharacters(ReadOnlySpan<byte> payload, int offset, int charCount, bool highByte)
    {
        int byteCount = highByte ? charCount * 2 : charCount;
        if (offset + byteCount > payload.Length)
            throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

        if (highByte)
            return System.Text.Encoding.Unicode.GetString(payload.Slice(offset, byteCount));

        char[] characters = new char[charCount];
        for (int i = 0; i < charCount; i++)
            characters[i] = (char)payload[offset + i];

        return new string(characters);
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
        bool dividedByHundred = (rk & 0x01) != 0;
        bool isInteger = (rk & 0x02) != 0;

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
