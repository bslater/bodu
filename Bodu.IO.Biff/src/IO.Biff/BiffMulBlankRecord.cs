// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffMulBlankRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>MULBLANK</c> record: a run of adjacent blank (formatted, valueless) cells in one row,
/// exposed by index without materializing an array.
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.MulBlank" />. The payload is the row and first column, one 16-bit
/// extended-format index per cell, and the declared last column; the layout is identical in BIFF5 and BIFF8. A payload
/// whose index area has an odd length is rejected as malformed.
/// </remarks>
/// <seealso cref="BiffReader.GetMulBlank" />
/// <seealso cref="BiffWriter.WriteMulBlank(int, int, ReadOnlySpan{ushort})" />
public readonly ref struct BiffMulBlankRecord
{
    /// <summary>The offset of the first format index within the payload.</summary>
    private const int CellsOffset = 4;

    /// <summary>The format indices: two bytes per cell.</summary>
    private readonly ReadOnlySpan<byte> _cells;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffMulBlankRecord" /> struct.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="firstColumn">The zero-based index of the first cell's column.</param>
    /// <param name="lastColumn">The zero-based index of the last cell's column as declared by the record.</param>
    /// <param name="cells">The format-index bytes.</param>
    private BiffMulBlankRecord(int row, int firstColumn, int lastColumn, ReadOnlySpan<byte> cells)
    {
        Row = row;
        FirstColumn = firstColumn;
        LastColumn = lastColumn;
        _cells = cells;
    }

    /// <summary>
    /// Gets the zero-based row index.
    /// </summary>
    /// <value>The row index.</value>
    public int Row { get; }

    /// <summary>
    /// Gets the zero-based index of the first cell's column.
    /// </summary>
    /// <value>The first column index.</value>
    public int FirstColumn { get; }

    /// <summary>
    /// Gets the zero-based index of the last cell's column as declared by the record's trailing field.
    /// </summary>
    /// <value>The declared last column.</value>
    public int LastColumn { get; }

    /// <summary>
    /// Gets the number of cells in the run.
    /// </summary>
    /// <value>The cell count.</value>
    public int Count => _cells.Length / 2;

    /// <summary>
    /// Gets the extended-format index of the cell at the specified position in the run.
    /// </summary>
    /// <param name="index">The zero-based position within the run.</param>
    /// <value>The XF index.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is negative or not less than <see cref="Count" />.
    /// </exception>
    public ushort this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfOutOfRange(index, 0, Count - 1);

            return BiffPayload.ReadUInt16(_cells, index * 2, BiffRecordType.MulBlank);
        }
    }

    /// <summary>
    /// Gets the zero-based column index of the cell at the specified position in the run.
    /// </summary>
    /// <param name="index">The zero-based position within the run.</param>
    /// <returns>The column index.</returns>
    public int GetColumn(int index) =>
        FirstColumn + index;

    /// <summary>
    /// Decodes a <c>MULBLANK</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when the payload is shorter than six bytes or its cell area is not a whole number of cells.
    /// </exception>
    internal static BiffMulBlankRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.MulBlank;
        BiffPayload.RequireLength(payload, 6, type);

        int runBytes = payload.Length - 6;
        if (runBytes % 2 != 0)
            throw BiffPayload.Malformed(type);

        return new BiffMulBlankRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, payload.Length - 2, type),
            payload.Slice(CellsOffset, runBytes));
    }
}
