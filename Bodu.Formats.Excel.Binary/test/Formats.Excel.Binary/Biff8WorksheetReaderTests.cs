// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorksheetReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Verifies the behavior of <see cref="Biff8WorksheetReader" /> against synthetic BIFF8 cell records, exercising the
/// value-record paths that the real-world fixture does not contain.
/// </summary>
[TestClass]
public partial class Biff8WorksheetReaderTests
{
    /// <summary>
    /// Reads every cell from a sequence of records into a position-keyed dictionary.
    /// </summary>
    /// <param name="records">The records that constitute the worksheet substream.</param>
    /// <returns>A dictionary mapping each populated cell's (row, column) to its value.</returns>
    private static Dictionary<(int Row, int Column), ExcelCell> ReadGrid(params Biff8Record[] records)
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = new();
        foreach (ExcelCell cell in Biff8WorksheetReader.ReadCells(records, 0, records.Length, [], Biff8FormatTable.Empty))
            grid[(cell.RowIndex, cell.ColumnIndex)] = cell;

        return grid;
    }

    /// <summary>
    /// Builds a record from a type identifier and its payload bytes.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="payload">The record payload.</param>
    /// <returns>The constructed record.</returns>
    private static Biff8Record Record(Biff8RecordType type, byte[] payload) =>
        new((ushort)type, payload);

    /// <summary>
    /// Builds a <c>FORMULA</c> record carrying the supplied eight-byte cached result at the given position.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="result">The eight-byte cached result.</param>
    /// <returns>The constructed formula record.</returns>
    private static Biff8Record Formula(int row, int column, byte[] result)
    {
        // row(2) + col(2) + ixfe(2) + result(8) + grbit(2) + chn(4) + cce(2).
        byte[] payload = new byte[22];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), (ushort)column);
        result.CopyTo(payload.AsSpan(6));

        return Record(Biff8RecordType.Formula, payload);
    }

    /// <summary>
    /// Builds a <c>STRING</c> record carrying a compressed (8-bit) cached formula string.
    /// </summary>
    /// <param name="value">The cached string.</param>
    /// <returns>The constructed string record.</returns>
    private static Biff8Record CompressedString(string value)
    {
        byte[] payload = new byte[3 + value.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)value.Length);
        payload[2] = 0x00;
        for (int i = 0; i < value.Length; i++)
            payload[3 + i] = (byte)value[i];

        return Record(Biff8RecordType.String, payload);
    }

    /// <summary>
    /// Builds a <c>BOOLERR</c> record at the given position.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="value">The boolean or error byte.</param>
    /// <param name="isError">Whether the value is an error code.</param>
    /// <returns>The constructed record.</returns>
    private static Biff8Record BoolErr(int row, int column, byte value, bool isError)
    {
        byte[] payload = new byte[8];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), (ushort)column);
        payload[6] = value;
        payload[7] = (byte)(isError ? 1 : 0);

        return Record(Biff8RecordType.BoolErr, payload);
    }
}
