// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffMulRkRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>MULRK</c> record: a run of adjacent RK-encoded number cells in one row, exposed by index
/// without materializing an array.
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.MulRk" />. The payload is the row and first column, six bytes per
/// cell (a 16-bit extended-format index and a 32-bit RK value), and the declared last column; the layout is identical
/// in BIFF5 and BIFF8. A payload whose cell area is not a whole number of cells is rejected as malformed.
/// </remarks>
/// <seealso cref="BiffReader.GetMulRk" /> <seealso cref="BiffWriter.WriteMulRk(int, int, ReadOnlySpan{BiffRkCell})" />
/// <seealso cref="BiffRkCell" />
public readonly ref struct BiffMulRkRecord
{
    /// <summary>The offset of the first cell within the payload.</summary>
    private const int CellsOffset = 4;

    /// <summary>The cell bytes: six bytes per cell.</summary>
    private readonly ReadOnlySpan<byte> _cells;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffMulRkRecord" /> struct.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="firstColumn">The zero-based index of the first cell's column.</param>
    /// <param name="lastColumn">The zero-based index of the last cell's column as declared by the record.</param>
    /// <param name="cells">The cell bytes.</param>
    private BiffMulRkRecord(int row, int firstColumn, int lastColumn, ReadOnlySpan<byte> cells)
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
    /// <value>
    /// The declared last column. A conformant record declares <see cref="FirstColumn" /> + <see cref="Count" /> − 1;
    /// the codec exposes the declared value rather than enforcing it.
    /// </value>
    public int LastColumn { get; }

    /// <summary>
    /// Gets the number of cells in the run.
    /// </summary>
    /// <value>The cell count.</value>
    public int Count => _cells.Length / BiffRkCell.Length;

    /// <summary>
    /// Gets the cell at the specified position in the run.
    /// </summary>
    /// <param name="index">The zero-based position within the run.</param>
    /// <value>The cell's format index and RK value.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is negative or not less than <see cref="Count" />.
    /// </exception>
    public BiffRkCell this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfOutOfRange(index, 0, Count - 1);

            int offset = index * BiffRkCell.Length;
            return new BiffRkCell(
                BiffPayload.ReadUInt16(_cells, offset, BiffRecordType.MulRk),
                BiffPayload.ReadUInt32(_cells, offset + 2, BiffRecordType.MulRk));
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
    /// Decodes a <c>MULRK</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when the payload is shorter than six bytes or its cell area is not a whole number of cells.
    /// </exception>
    internal static BiffMulRkRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.MulRk;
        BiffPayload.RequireLength(payload, 6, type);

        int runBytes = payload.Length - 6;
        if (runBytes % BiffRkCell.Length != 0)
            throw BiffPayload.Malformed(type);

        return new BiffMulRkRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, payload.Length - 2, type),
            payload.Slice(CellsOffset, runBytes));
    }
}
