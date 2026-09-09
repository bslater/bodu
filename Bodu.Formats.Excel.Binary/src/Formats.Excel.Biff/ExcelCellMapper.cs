// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellMapper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.IO.Biff;

namespace Bodu.Formats.Excel.Biff;

/// <summary>
/// Maps the decoded cell records of the BIFF codec onto <see cref="ExcelCell" /> values, resolving each cell's number
/// format through the workbook's format table.
/// </summary>
/// <remarks>
/// The codec owns the wire layouts; this mapper owns the Excel interpretation — the shared-string lookup, the format
/// and date classification, and the projection of cached formula results onto cell kinds.
/// </remarks>
internal static class ExcelCellMapper
{
    /// <summary>
    /// Maps a <c>LABELSST</c> record through the shared string table.
    /// </summary>
    /// <param name="record">The decoded record.</param>
    /// <param name="sharedStrings">The workbook's shared strings.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The text cell.</returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the record's string index is outside the shared string table.
    /// </exception>
    public static ExcelCell FromLabelSst(in BiffLabelSstRecord record, string[] sharedStrings, BiffFormatTable formats)
    {
        if (record.SstIndex >= (uint)sharedStrings.Length)
        {
            throw new ExcelBinaryFormatException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Format_Invalid_Biff8StringIndex, record.SstIndex));
        }

        return ExcelCell.Text(record.Row, record.Column, sharedStrings[record.SstIndex], formats.GetFormatIndex(record.XfIndex));
    }

    /// <summary>
    /// Maps a <c>LABEL</c> record.
    /// </summary>
    /// <param name="record">The decoded record.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The text cell.</returns>
    public static ExcelCell FromLabel(in BiffLabelRecord record, BiffFormatTable formats) =>
        ExcelCell.Text(record.Row, record.Column, record.Text.GetString(), formats.GetFormatIndex(record.XfIndex));

    /// <summary>
    /// Maps an <c>RSTRING</c> record, the BIFF5 rich-text label, keeping its text and dropping its formatting runs.
    /// </summary>
    /// <param name="record">The decoded record.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The text cell.</returns>
    public static ExcelCell FromRString(in BiffRStringRecord record, BiffFormatTable formats) =>
        ExcelCell.Text(record.Row, record.Column, record.Text.GetString(), formats.GetFormatIndex(record.XfIndex));

    /// <summary>
    /// Maps a <c>NUMBER</c> record.
    /// </summary>
    /// <param name="record">The decoded record.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The numeric cell.</returns>
    public static ExcelCell FromNumber(in BiffNumberRecord record, BiffFormatTable formats) =>
        ExcelCell.Number(record.Row, record.Column, record.Value, formats.GetFormatIndex(record.XfIndex), formats.IsDateFormatted(record.XfIndex));

    /// <summary>
    /// Maps an <c>RK</c> record.
    /// </summary>
    /// <param name="record">The decoded record.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The numeric cell.</returns>
    public static ExcelCell FromRk(in BiffRkRecord record, BiffFormatTable formats) =>
        ExcelCell.Number(record.Row, record.Column, record.Value, formats.GetFormatIndex(record.XfIndex), formats.IsDateFormatted(record.XfIndex));

    /// <summary>
    /// Maps a <c>BOOLERR</c> record.
    /// </summary>
    /// <param name="record">The decoded record.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The boolean or error cell.</returns>
    public static ExcelCell FromBoolErr(in BiffBoolErrRecord record, BiffFormatTable formats) =>
        record.IsError
            ? ExcelCell.Error(record.Row, record.Column, (ExcelErrorCode)record.ErrorCode, formats.GetFormatIndex(record.XfIndex))
            : ExcelCell.Boolean(record.Row, record.Column, record.BooleanValue, formats.GetFormatIndex(record.XfIndex));

    /// <summary>
    /// Maps every cell of a <c>MULRK</c> run.
    /// </summary>
    /// <param name="record">The decoded record.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <returns>The numeric cells, in column order.</returns>
    public static ExcelCell[] FromMulRk(in BiffMulRkRecord record, BiffFormatTable formats)
    {
        var cells = new ExcelCell[record.Count];
        for (int i = 0; i < cells.Length; i++)
        {
            BiffRkCell cell = record[i];
            cells[i] = ExcelCell.Number(record.Row, record.GetColumn(i), cell.Value, formats.GetFormatIndex(cell.XfIndex), formats.IsDateFormatted(cell.XfIndex));
        }

        return cells;
    }

    /// <summary>
    /// Maps a <c>FORMULA</c> record's cached result.
    /// </summary>
    /// <param name="record">The decoded record.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <param name="expectsString">
    /// When this method returns, whether the cached result is text carried by a following <c>STRING</c> record.
    /// </param>
    /// <returns>The cell holding the cached result; an empty text cell when the text follows.</returns>
    public static ExcelCell FromFormula(in BiffFormulaRecord record, BiffFormatTable formats, out bool expectsString)
    {
        ushort formatIndex = formats.GetFormatIndex(record.XfIndex);
        expectsString = false;

        switch (record.CachedResultKind)
        {
            case BiffCachedResultKind.Number:
                return ExcelCell.Number(record.Row, record.Column, record.NumberValue, formatIndex, formats.IsDateFormatted(record.XfIndex));

            case BiffCachedResultKind.String:
                expectsString = true;
                return ExcelCell.Text(record.Row, record.Column, string.Empty, formatIndex);

            case BiffCachedResultKind.Boolean:
                return ExcelCell.Boolean(record.Row, record.Column, record.BooleanValue, formatIndex);

            case BiffCachedResultKind.Error:
                return ExcelCell.Error(record.Row, record.Column, (ExcelErrorCode)record.ErrorCode, formatIndex);

            default:
                return ExcelCell.Text(record.Row, record.Column, string.Empty, formatIndex);
        }
    }
}
