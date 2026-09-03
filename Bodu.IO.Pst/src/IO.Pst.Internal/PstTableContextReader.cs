// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContextReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Reads a node's table context (<c>TC</c>) structures: the <c>TCINFO</c> with its column descriptors, the row-index
/// BTree-on-heap, and the row-matrix blocks.
/// </summary>
internal static class PstTableContextReader
{
    /// <summary>The <c>TCINFO</c> type byte.</summary>
    private const byte TcInfoType = 0x7C;

    /// <summary>The fixed <c>TCINFO</c> size before the column descriptors.</summary>
    private const int TcInfoFixedSize = 22;

    /// <summary>The size of one <c>TCOLDESC</c>.</summary>
    private const int ColumnDescriptorSize = 8;

    /// <summary>The key width the Unicode row index declares.</summary>
    private const byte RowIndexKeySize = 4;

    /// <summary>The record data width the Unicode row index declares.</summary>
    private const byte RowIndexDataSize = 4;

    /// <summary>The usable payload of one row-matrix block, which bounds the rows a block holds.</summary>
    private const int RowMatrixBlockPayload = 8176;

    /// <summary>
    /// Reads the table context of the supplied node entry.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="entry">The node whose heap carries the context.</param>
    /// <returns>The parsed heap, the table geometry, and the row-index header.</returns>
    /// <exception cref="PstFileFormatException">
    /// The node's heap does not declare a table context, or the <c>TCINFO</c>, column, or row-index geometry is
    /// malformed.
    /// </exception>
    internal static (PstHeapNode Heap, PstTcInfo Info, PstBthHeader RowIndex) Read(PstSource source, PstNbtEntry entry)
    {
        PstHeapNode heap = PstHeapNode.Parse(source, entry);
        if (heap.ClientSignature != PstHeapNode.TableContextSignature || heap.UserRootHid == 0)
            throw Malformed(entry.NodeId);

        ReadOnlySpan<byte> info = heap.GetItem(heap.UserRootHid).Span;
        if (info.Length < TcInfoFixedSize || info[0] != TcInfoType)
            throw Malformed(entry.NodeId);

        int columnCount = info[1];
        if (info.Length < TcInfoFixedSize + (columnCount * ColumnDescriptorSize))
            throw Malformed(entry.NodeId);

        ushort endOffset4 = BinaryPrimitives.ReadUInt16LittleEndian(info.Slice(2));
        ushort endOffset2 = BinaryPrimitives.ReadUInt16LittleEndian(info.Slice(4));
        ushort endOffset1 = BinaryPrimitives.ReadUInt16LittleEndian(info.Slice(6));
        ushort rowWidth = BinaryPrimitives.ReadUInt16LittleEndian(info.Slice(8));
        uint rowIndexHid = BinaryPrimitives.ReadUInt32LittleEndian(info.Slice(10));
        uint rowsHnid = BinaryPrimitives.ReadUInt32LittleEndian(info.Slice(14));

        // Row geometry is structural: the regions must nest, the row id needs its four leading bytes, and the
        // existence bitmap must cover every declared column.
        if (endOffset4 < 4 || endOffset2 < endOffset4 || endOffset1 < endOffset2 || rowWidth < endOffset1)
            throw Malformed(entry.NodeId);

        if (rowWidth - endOffset1 < (columnCount + 7) / 8)
            throw Malformed(entry.NodeId);

        var columns = new PstTcColumn[columnCount];
        for (int i = 0; i < columnCount; i++)
        {
            ReadOnlySpan<byte> descriptor = info.Slice(TcInfoFixedSize + (i * ColumnDescriptorSize), ColumnDescriptorSize);
            var column = new PstTcColumn(
                BinaryPrimitives.ReadUInt32LittleEndian(descriptor),
                BinaryPrimitives.ReadUInt16LittleEndian(descriptor.Slice(4)),
                descriptor[6],
                descriptor[7]);

            if (column.DataOffset + column.DataSize > endOffset1 || column.ExistenceBit >= columnCount)
                throw Malformed(entry.NodeId);

            columns[i] = column;
        }

        var tcInfo = new PstTcInfo(endOffset4, endOffset2, endOffset1, rowWidth, rowIndexHid, rowsHnid, columns);

        PstBthHeader rowIndex = PstBTreeOnHeap.ReadHeader(heap, rowIndexHid);
        if (rowIndex.KeySize != RowIndexKeySize || rowIndex.DataSize != RowIndexDataSize)
            throw Malformed(entry.NodeId);

        return (heap, tcInfo, rowIndex);
    }

    /// <summary>
    /// Enumerates the row-matrix blocks in order, one block at a time.
    /// </summary>
    /// <param name="heap">The context's heap, which serves a heap-resident matrix.</param>
    /// <param name="context">The value-reference resolver, which serves a subnode-resident matrix.</param>
    /// <param name="info">The table geometry.</param>
    /// <returns>The matrix blocks; empty when the table has no rows.</returns>
    /// <exception cref="PstFileFormatException">The row-matrix reference does not resolve.</exception>
    internal static IEnumerable<byte[]> EnumerateRowBlocks(PstHeapNode heap, PstLtpContext context, PstTcInfo info)
    {
        if (PstHnid.IsNull(info.RowsHnid))
            yield break;

        if (PstHnid.IsHeapId(info.RowsHnid))
        {
            yield return heap.GetItem(info.RowsHnid).ToArray();
            yield break;
        }

        if (!context.TryGetSubnodeSegments(info.RowsHnid, out List<byte[]> segments))
            throw Malformed(context.NodeId);

        foreach (byte[] segment in segments)
            yield return segment;
    }

    /// <summary>
    /// Computes the number of rows one row-matrix block holds when the matrix spans subnode blocks.
    /// </summary>
    /// <param name="rowWidth">The row width.</param>
    /// <returns>The rows per block; rows never span blocks.</returns>
    internal static int RowsPerBlock(int rowWidth) =>
        Math.Max(1, RowMatrixBlockPayload / rowWidth);

    /// <summary>
    /// Creates the malformed-table-context exception for a node identifier.
    /// </summary>
    /// <param name="nodeId">The owning node identifier.</param>
    /// <returns>The exception to throw.</returns>
    internal static PstFileFormatException Malformed(uint nodeId) =>
        new(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstTableContext, new PstNodeId(nodeId)), PstFileError.InvalidTableContext);
}
