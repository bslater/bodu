// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContextTests.EnumerateRowIds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

public partial class PstTableContextTests
{
    /// <summary>
    /// Verifies that the identifier enumeration yields the same identifiers, in the same order, as the row
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void EnumerateRowIds_WhenRowsPresent_ShouldMatchEnumerateRows()
    {
        (PstFile file, PstTableContext context) = OpenSharedContext();
        using (file)
        {
            CollectionAssert.AreEqual(
                context.EnumerateRows().Select(static r => r.RowId).ToArray(),
                context.EnumerateRowIds().ToArray());
        }
    }

    /// <summary>
    /// Verifies that enumerating identifiers over a subnode-resident matrix allocates nothing per row: a table
    /// walked for its identifiers alone must not copy every row out of its block.
    /// </summary>
    [TestMethod]
    public void EnumerateRowIds_WhenMatrixIsSubnodeResident_ShouldNotAllocatePerRow()
    {
        const uint subnodeId = 0x41;
        const int RowCount = 500;

        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        var matrix = new byte[RowCount * RowWidth];
        var index = new (ulong RowId, uint RowNumber)[RowCount];
        for (int i = 0; i < RowCount; i++)
        {
            Row((uint)(0x100 + i), i, 0, 0, 0, 0b1101_1000).CopyTo(matrix, i * RowWidth);
            index[i] = ((ulong)(0x100 + i), (uint)i);
        }

        ulong matrixData = builder.AddDataBlock(matrix);
        ulong subnodeTree = builder.AddSubnodeLeafBlock((subnodeId, matrixData, 0));
        _ = ltp.AddTableContext(
            [
                (RowIdTag, 0, 4, 0),
                (Int32Tag, 4, 4, 1),
                (StringTag, 8, 4, 2),
                (Int16Tag, 12, 2, 3),
                (BooleanTag, 14, 1, 4),
            ],
            endOffset4: 12,
            endOffset2: 14,
            endOffset1: 15,
            rowWidth: RowWidth,
            rowsHnid: subnodeId,
            index);
        ltp.AddHeapNode(builder, NodeId, subnodeTree);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstTableContext context = file.GetNode(new PstNodeId(NodeId)).ReadTableContext();

        // Warm the path so lazily initialized state is excluded from the measurement.
        Assert.AreEqual(RowCount, context.EnumerateRowIds().Count());

        long before = GC.GetAllocatedBytesForCurrentThread();
        int count = context.EnumerateRowIds().Count();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(RowCount, count);
        Assert.IsTrue(allocated < 8 * 1024, $"Enumerating {RowCount} row identifiers allocated {allocated} bytes — rows are being copied.");
    }
}
