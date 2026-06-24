// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8CellDecoder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.Formats.Excel.Biff8;

/// <summary>
/// Decodes individual BIFF8 value-bearing cell records into <see cref="ExcelCell" /> values, applying bounds-checked
/// reads so a malformed record fails with <see cref="ExcelBinaryFormatException" /> rather than a generic exception.
/// </summary>
/// <remarks>
/// Each decoder consumes the payload of one record. The multi-record sequencing of a <c>FORMULA</c> cell with a string
/// result — whose text is carried by a following <c>STRING</c> record — is coordinated by the caller using
/// <see cref="ReadFormula(ReadOnlySpan{byte}, Biff8FormatTable, out bool)" /> and
/// <see cref="ReadCachedString(ReadOnlySpan{byte})" />.
/// </remarks>
internal static class Biff8CellDecoder
{
    /// <summary>
    /// Decodes a <c>LABELSST</c> cell, resolving its shared-string index.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="sharedStrings">The workbook shared string table.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded text cell.</returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the payload is too short or the referenced string index is out of range.
    /// </exception>
    public static ExcelCell ReadLabelSst(ReadOnlySpan<byte> payload, string[] sharedStrings, Biff8FormatTable formats)
    {
        Biff8Payload.RequireLength(payload, 10, Biff8RecordType.LabelSst);
        int row = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.LabelSst);
        int column = Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.LabelSst);
        ushort xfIndex = Biff8Payload.ReadUInt16(payload, 4, Biff8RecordType.LabelSst);
        uint stringIndex = Biff8Payload.ReadUInt32(payload, 6, Biff8RecordType.LabelSst);

        if (stringIndex >= (uint)sharedStrings.Length)
        {
            throw new ExcelBinaryFormatException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Format_Invalid_Biff8StringIndex, stringIndex));
        }

        return ExcelCell.Text(row, column, sharedStrings[stringIndex], formats.GetFormatIndex(xfIndex));
    }

    /// <summary>
    /// Decodes a <c>LABEL</c> cell carrying an inline (non-shared) string.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded text cell.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the payload is too short.</exception>
    public static ExcelCell ReadLabel(ReadOnlySpan<byte> payload, Biff8FormatTable formats)
    {
        Biff8Payload.RequireLength(payload, 9, Biff8RecordType.Label);
        int row = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.Label);
        int column = Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.Label);
        ushort xfIndex = Biff8Payload.ReadUInt16(payload, 4, Biff8RecordType.Label);
        int charCount = Biff8Payload.ReadUInt16(payload, 6, Biff8RecordType.Label);
        bool highByte = (Biff8Payload.ReadByte(payload, 8, Biff8RecordType.Label) & 0x01) != 0;

        string value = Biff8StringReader.Decode(payload, 9, charCount, highByte, Biff8RecordType.Label);
        return ExcelCell.Text(row, column, value, formats.GetFormatIndex(xfIndex));
    }

    /// <summary>
    /// Decodes a <c>NUMBER</c> cell carrying an IEEE 754 double.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded numeric cell.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the payload is too short.</exception>
    public static ExcelCell ReadNumber(ReadOnlySpan<byte> payload, Biff8FormatTable formats)
    {
        Biff8Payload.RequireLength(payload, 14, Biff8RecordType.Number);
        int row = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.Number);
        int column = Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.Number);
        ushort xfIndex = Biff8Payload.ReadUInt16(payload, 4, Biff8RecordType.Number);
        double value = Biff8Payload.ReadDouble(payload, 6, Biff8RecordType.Number);

        return ExcelCell.Number(row, column, value, formats.GetFormatIndex(xfIndex), formats.IsDateFormatted(xfIndex));
    }

    /// <summary>
    /// Decodes an <c>RK</c> cell carrying a single RK-encoded number.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded numeric cell.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the payload is too short.</exception>
    public static ExcelCell ReadRk(ReadOnlySpan<byte> payload, Biff8FormatTable formats)
    {
        Biff8Payload.RequireLength(payload, 10, Biff8RecordType.Rk);
        int row = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.Rk);
        int column = Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.Rk);
        ushort xfIndex = Biff8Payload.ReadUInt16(payload, 4, Biff8RecordType.Rk);
        uint rk = Biff8Payload.ReadUInt32(payload, 6, Biff8RecordType.Rk);

        return ExcelCell.Number(row, column, DecodeRk(rk), formats.GetFormatIndex(xfIndex), formats.IsDateFormatted(xfIndex));
    }

    /// <summary>
    /// Decodes a <c>BOOLERR</c> cell, which carries either a boolean or an error code.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>A boolean cell, or an error cell when the value is an error code.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the payload is too short.</exception>
    public static ExcelCell ReadBoolErr(ReadOnlySpan<byte> payload, Biff8FormatTable formats)
    {
        Biff8Payload.RequireLength(payload, 8, Biff8RecordType.BoolErr);
        int row = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.BoolErr);
        int column = Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.BoolErr);
        ushort xfIndex = Biff8Payload.ReadUInt16(payload, 4, Biff8RecordType.BoolErr);
        byte value = payload[6];
        byte isError = payload[7];

        return isError != 0
            ? ExcelCell.Error(row, column, (ExcelErrorCode)value, formats.GetFormatIndex(xfIndex))
            : ExcelCell.Boolean(row, column, value != 0, formats.GetFormatIndex(xfIndex));
    }

    /// <summary>
    /// Decodes a <c>MULRK</c> record, expanding it into one numeric cell per contained RK value.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The decoded numeric cells, in column order.</returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the payload is too short or its cell run is not a whole number of entries.
    /// </exception>
    public static ExcelCell[] ReadMulRk(ReadOnlySpan<byte> payload, Biff8FormatTable formats)
    {
        // Payload: row(2) + colFirst(2) + N * (ixfe(2) + rk(4)) + colLast(2).
        Biff8Payload.RequireLength(payload, 6, Biff8RecordType.MulRk);
        int row = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.MulRk);
        int firstColumn = Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.MulRk);

        int runBytes = payload.Length - 6;
        if (runBytes < 0 || runBytes % 6 != 0)
            throw Biff8Payload.Malformed(Biff8RecordType.MulRk);

        int count = runBytes / 6;
        var cells = new ExcelCell[count];
        for (int k = 0; k < count; k++)
        {
            ushort xfIndex = Biff8Payload.ReadUInt16(payload, 4 + (k * 6), Biff8RecordType.MulRk);
            uint rk = Biff8Payload.ReadUInt32(payload, 4 + (k * 6) + 2, Biff8RecordType.MulRk);
            cells[k] = ExcelCell.Number(row, firstColumn + k, DecodeRk(rk), formats.GetFormatIndex(xfIndex), formats.IsDateFormatted(xfIndex));
        }

        return cells;
    }

    /// <summary>
    /// Decodes a <c>FORMULA</c> cell's cached result. No formula evaluation is performed; only the stored result value
    /// is read.
    /// </summary>
    /// <param name="payload">The formula record payload.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <param name="expectsString">
    /// When this method returns, <see langword="true" /> when the cached result is a string carried by a following
    /// <c>STRING</c> record; the returned cell is then an empty text placeholder the caller should complete.
    /// </param>
    /// <returns>The decoded cell carrying the cached result.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the formula record is malformed.</exception>
    /// <remarks>
    /// The eight-byte cached result is an IEEE 754 double unless its trailing two bytes are <c>0xFFFF</c>, which marks
    /// a non-numeric result whose leading byte selects a string, boolean, error, or empty-string value.
    /// </remarks>
    public static ExcelCell ReadFormula(ReadOnlySpan<byte> payload, Biff8FormatTable formats, out bool expectsString)
    {
        Biff8Payload.RequireLength(payload, 14, Biff8RecordType.Formula);
        int row = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.Formula);
        int column = Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.Formula);
        ushort xfIndex = Biff8Payload.ReadUInt16(payload, 4, Biff8RecordType.Formula);
        ushort formatIndex = formats.GetFormatIndex(xfIndex);
        ReadOnlySpan<byte> result = payload.Slice(6, 8);

        expectsString = false;

        // A trailing 0xFFFF marks a non-numeric cached result; the leading byte selects which kind.
        if (result[6] == 0xFF && result[7] == 0xFF)
        {
            switch (result[0])
            {
                case 0:
                    expectsString = true;
                    return ExcelCell.Text(row, column, string.Empty, formatIndex);

                case 1:
                    return ExcelCell.Boolean(row, column, result[2] != 0, formatIndex);

                case 2:
                    return ExcelCell.Error(row, column, (ExcelErrorCode)result[2], formatIndex);

                // 3 is an explicit empty string; any other marker is treated as one defensively.
                default:
                    return ExcelCell.Text(row, column, string.Empty, formatIndex);
            }
        }

        return ExcelCell.Number(row, column, BinaryPrimitives.ReadDoubleLittleEndian(result), formatIndex, formats.IsDateFormatted(xfIndex));
    }

    /// <summary>
    /// Decodes the cached string carried by the <c>STRING</c> record that follows a string-result formula cell.
    /// </summary>
    /// <param name="payload">The <c>STRING</c> record payload.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the <c>STRING</c> record is malformed.</exception>
    public static string ReadCachedString(ReadOnlySpan<byte> payload)
    {
        Biff8Payload.RequireLength(payload, 3, Biff8RecordType.String);
        int charCount = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.String);
        bool highByte = (payload[2] & 0x01) != 0;

        return Biff8StringReader.Decode(payload, 3, charCount, highByte, Biff8RecordType.String);
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
