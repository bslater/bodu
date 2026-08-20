// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Represents a node's table context (<c>TC</c>): the LTP table of typed columns over identifier-keyed rows, with
/// forward-only row enumeration and keyed row lookup — format-agnostic, with no MAPI semantics.
/// </summary>
/// <remarks>
/// Row enumeration streams the row matrix one block at a time and never materializes the whole table; each yielded
/// row copies its own bytes, so rows remain valid after enumeration advances. The row count comes from the table's
/// row index.
/// </remarks>
public sealed class PstTableContext
{
    /// <summary>The context's heap.</summary>
    private readonly PstHeapNode _heap;

    /// <summary>The value-reference resolver over the owning node.</summary>
    private readonly PstLtpContext _context;

    /// <summary>The table geometry.</summary>
    private readonly PstTcInfo _info;

    /// <summary>The row-index tree header.</summary>
    private readonly PstBthHeader _rowIndex;

    /// <summary>The public column views, materialized once.</summary>
    private readonly PstTableColumn[] _columns;

    /// <summary>The row count the row index records.</summary>
    private readonly int _rowCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstTableContext" /> class.
    /// </summary>
    /// <param name="heap">The context's heap.</param>
    /// <param name="context">The value-reference resolver.</param>
    /// <param name="info">The table geometry.</param>
    /// <param name="rowIndex">The row-index tree header.</param>
    /// <param name="validationLevel">The active validation level.</param>
    internal PstTableContext(PstHeapNode heap, PstLtpContext context, PstTcInfo info, PstBthHeader rowIndex, PstValidationLevel validationLevel)
    {
        _heap = heap;
        _context = context;
        _info = info;
        _rowIndex = rowIndex;

        _columns = new PstTableColumn[info.Columns.Length];
        for (int i = 0; i < info.Columns.Length; i++)
            _columns[i] = new PstTableColumn(info.Columns[i].PropertyId, info.Columns[i].WireType, info.Columns[i].DataSize);

        _rowCount = PstBTreeOnHeap.EnumerateRecords(heap, rowIndex, validationLevel).Count();
    }

    /// <summary>
    /// Gets the table's columns in stored order.
    /// </summary>
    /// <value>The column views.</value>
    public IReadOnlyList<PstTableColumn> Columns => _columns;

    /// <summary>
    /// Gets the number of rows the table's row index records.
    /// </summary>
    /// <value>The row count.</value>
    public int RowCount => _rowCount;

    /// <summary>
    /// Enumerates the table's rows in matrix order, one matrix block resident at a time.
    /// </summary>
    /// <returns>The rows.</returns>
    /// <exception cref="PstFileFormatException">
    /// The row matrix does not resolve or holds fewer rows than the row index records.
    /// </exception>
    public IEnumerable<PstTableRow> EnumerateRows()
    {
        int remaining = _rowCount;
        foreach (byte[] block in PstTableContextReader.EnumerateRowBlocks(_heap, _context, _info))
        {
            int available = block.Length / _info.RowWidth;
            for (int i = 0; i < available && remaining > 0; i++, remaining--)
                yield return CreateRow(block, i);

            if (remaining == 0)
                yield break;
        }

        if (remaining > 0)
            throw PstTableContextReader.Malformed(_context.NodeId);
    }

    /// <summary>
    /// Attempts to retrieve a row by its identifier through the table's row index.
    /// </summary>
    /// <param name="rowId">The row identifier.</param>
    /// <param name="row">When this method returns <see langword="true" />, the row.</param>
    /// <returns><see langword="true" /> when the row exists.</returns>
    /// <exception cref="PstFileFormatException">
    /// The row index names a row the matrix does not hold, or the matrix does not resolve.
    /// </exception>
    public bool TryGetRow(uint rowId, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out PstTableRow row)
    {
        row = null;
        if (!PstBTreeOnHeap.TryFind(_heap, _rowIndex, rowId, out ReadOnlyMemory<byte> data))
            return false;

        // Rows never span blocks and every block (a heap item included) fits the block payload, so the row's block
        // and position follow directly from the per-block row capacity.
        int rowNumber = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Span);
        int rowsPerBlock = PstTableContextReader.RowsPerBlock(_info.RowWidth);
        int targetBlock = rowNumber / rowsPerBlock;
        int positionInBlock = rowNumber % rowsPerBlock;

        int blockIndex = 0;
        foreach (byte[] block in PstTableContextReader.EnumerateRowBlocks(_heap, _context, _info))
        {
            if (blockIndex == targetBlock)
            {
                if (positionInBlock >= block.Length / _info.RowWidth)
                    break;

                row = CreateRow(block, positionInBlock);
                return true;
            }

            blockIndex++;
        }

        throw PstTableContextReader.Malformed(_context.NodeId);
    }

    /// <summary>
    /// Copies one row out of a matrix block.
    /// </summary>
    /// <param name="block">The matrix block.</param>
    /// <param name="positionInBlock">The row's position within the block.</param>
    /// <returns>The row.</returns>
    private PstTableRow CreateRow(byte[] block, int positionInBlock)
    {
        var bytes = new byte[_info.RowWidth];
        block.AsSpan(positionInBlock * _info.RowWidth, _info.RowWidth).CopyTo(bytes);
        return new PstTableRow(bytes, _heap, _context, _info);
    }
}
