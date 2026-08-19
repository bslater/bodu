// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Represents one row of a table context: the row identifier and cell access by column property identifier.
/// </summary>
/// <remarks>
/// A row copies its bytes when it is produced, so it remains valid independent of the matrix block it came from.
/// Variable-size cells hold value references that resolve on access, exactly like property-context values.
/// </remarks>
public sealed class PstTableRow
{
    /// <summary>The row's copied bytes.</summary>
    private readonly byte[] _row;

    /// <summary>The owning context's heap.</summary>
    private readonly PstHeapNode _heap;

    /// <summary>The value-reference resolver over the owning node.</summary>
    private readonly PstLtpContext _context;

    /// <summary>The table geometry.</summary>
    private readonly PstTcInfo _info;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstTableRow" /> class over a row's copied bytes.
    /// </summary>
    /// <param name="row">The row bytes, of the table's row width.</param>
    /// <param name="heap">The owning context's heap.</param>
    /// <param name="context">The value-reference resolver.</param>
    /// <param name="info">The table geometry.</param>
    internal PstTableRow(byte[] row, PstHeapNode heap, PstLtpContext context, PstTcInfo info)
    {
        _row = row;
        _heap = heap;
        _context = context;
        _info = info;
    }

    /// <summary>
    /// Gets the row identifier the row's leading dword records.
    /// </summary>
    /// <value>The row identifier.</value>
    public uint RowId =>
        BinaryPrimitives.ReadUInt32LittleEndian(_row);

    /// <summary>
    /// Attempts to retrieve a cell's value by its column's property identifier.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <param name="value">When this method returns <see langword="true" />, the cell value.</param>
    /// <returns>
    /// <see langword="true" /> when the table declares the column and the row's existence bitmap marks the cell
    /// present.
    /// </returns>
    /// <exception cref="PstFileFormatException">The cell's value reference does not resolve.</exception>
    public bool TryGetCell(ushort propertyId, out PstPropertyValue value)
    {
        foreach (PstTcColumn column in _info.Columns)
        {
            if (column.PropertyId == propertyId)
            {
                if (!IsPresent(column))
                    break;

                value = Materialize(column);
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Enumerates the row's present cells in column order, resolving each payload as it is yielded.
    /// </summary>
    /// <returns>The present cell values.</returns>
    /// <exception cref="PstFileFormatException">A cell's value reference does not resolve.</exception>
    public IEnumerable<PstPropertyValue> EnumerateCells()
    {
        foreach (PstTcColumn column in _info.Columns)
        {
            if (IsPresent(column))
                yield return Materialize(column);
        }
    }

    /// <summary>
    /// Tests a column's bit in the row's existence bitmap, which is indexed most-significant-bit first.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns><see langword="true" /> when the cell is present.</returns>
    private bool IsPresent(PstTcColumn column) =>
        (_row[_info.EndOffset1 + (column.ExistenceBit / 8)] & (1 << (7 - (column.ExistenceBit % 8)))) != 0;

    /// <summary>
    /// Resolves a cell into its value: fixed-width cells read inline from the row, variable-size cells resolve their
    /// value reference.
    /// </summary>
    /// <param name="column">The column to resolve.</param>
    /// <returns>The cell value.</returns>
    /// <exception cref="PstFileFormatException">The cell's value reference does not resolve.</exception>
    private PstPropertyValue Materialize(PstTcColumn column)
    {
        ReadOnlyMemory<byte> cell = _row.AsMemory(column.DataOffset, column.DataSize);

        if (PstWireType.TryGetInlineSize(column.WireType, out int inlineSize))
            return new PstPropertyValue(column.PropertyId, column.WireType, cell.Slice(0, Math.Min(inlineSize, cell.Length)));

        // A fixed eight- or sixteen-byte value sits inline when the cell is wide enough; a four-byte cell holds a
        // value reference instead.
        if (PstWireType.TryGetFixedHeapSize(column.WireType, out int fixedSize) && column.DataSize >= fixedSize)
            return new PstPropertyValue(column.PropertyId, column.WireType, cell.Slice(0, fixedSize));

        if (PstWireType.IsKnown(column.WireType) && column.DataSize >= 4)
        {
            uint hnid = BinaryPrimitives.ReadUInt32LittleEndian(cell.Span);
            return new PstPropertyValue(column.PropertyId, column.WireType, _context.ResolveHnidPayload(_heap, hnid));
        }

        return new PstPropertyValue(column.PropertyId, column.WireType, cell);
    }
}
