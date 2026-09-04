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

    /// <summary>The key width the row index declares in both formats: the 32-bit row identifier.</summary>
    private const byte RowIndexKeySize = 4;

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
        // The row index maps the 32-bit row identifier to a row number that is four bytes wide in a Unicode store and
        // two in an ANSI store (MS-PST §2.3.4.3 TCROWID).
        if (rowIndex.KeySize != RowIndexKeySize || rowIndex.DataSize is not (2 or 4))
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
    internal static IEnumerable<ReadOnlyMemory<byte>> EnumerateRowBlocks(PstHeapNode heap, PstLtpContext context, PstTcInfo info)
    {
        if (PstHnid.IsNull(info.RowsHnid))
            yield break;

        if (PstHnid.IsHeapId(info.RowsHnid))
        {
            yield return heap.GetItem(info.RowsHnid);
            yield break;
        }

        if (!context.TryGetSubnodeSegments(info.RowsHnid, out List<byte[]> segments))
            throw Malformed(context.NodeId);

        foreach (byte[] segment in segments)
            yield return segment;
    }

    /// <summary>
    /// Computes how many rows one row-matrix block holds: the block's usable payload divided by the row width.
    /// </summary>
    /// <param name="layout">The file layout, whose block payload is 8,176 bytes for Unicode and 8,180 for ANSI.</param>
    /// <param name="rowWidth">The row width.</param>
    /// <returns>The rows per block, at least one.</returns>
    /// <remarks>
    /// MS-PST §2.3.4.4 packs ⌊payload / row width⌋ rows per block. The payload differs by format because the block
    /// trailer does (16 bytes against 12), so a point lookup that used the Unicode figure on an ANSI store would land
    /// on the wrong row whenever the quotients differ.
    /// </remarks>
    internal static int RowsPerBlock(PstLayout layout, int rowWidth) =>
        Math.Max(1, layout.MaxBlockPayload / rowWidth);

    /// <summary>
    /// Creates the malformed-table-context exception for a node identifier.
    /// </summary>
    /// <param name="nodeId">The owning node identifier.</param>
    /// <returns>The exception to throw.</returns>
    internal static PstFileFormatException Malformed(uint nodeId) =>
        new(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstTableContext, new PstNodeId(nodeId)), PstFileError.InvalidTableContext);
}
