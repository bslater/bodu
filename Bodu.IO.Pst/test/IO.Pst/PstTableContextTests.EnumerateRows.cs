// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContextTests.EnumerateRows.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstTableContext.EnumerateRows" />: matrix-order enumeration over heap- and subnode-resident
/// row matrices.
/// </summary>
public partial class PstTableContextTests
{
    /// <summary>
    /// Verifies that enumeration yields every indexed row in matrix order with resolved cells.
    /// </summary>
    [TestMethod]
    public void EnumerateRows_WhenMatrixIsHeapResident_ShouldYieldRowsInMatrixOrder()
    {
        (PstFile file, PstTableContext context) = OpenSharedContext();
        using (file)
        {
            var rows = context.EnumerateRows().ToList();

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(FirstRowId, rows[0].RowId);
            Assert.AreEqual(SecondRowId, rows[1].RowId);

            Assert.IsTrue(rows[0].TryGetCell(0x2222, out PstPropertyValue text));
            Assert.AreEqual("alpha", text.GetString());
        }
    }

    /// <summary>
    /// Verifies that a subnode-resident row matrix enumerates through the subnode's data blocks.
    /// </summary>
    [TestMethod]
    public void EnumerateRows_WhenMatrixIsSubnodeResident_ShouldYieldRowsFromSubnodeBlocks()
    {
        const uint subnodeId = 0x41;

        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        byte[] matrix =
        [
            .. Row(FirstRowId, 7, 0, 0x0102, 1, 0b1101_1000),
            .. Row(SecondRowId, 9, 0, 0x0304, 0, 0b1101_1000),
        ];
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
            (FirstRowId, 0),
            (SecondRowId, 1));

        ltp.AddHeapNode(builder, NodeId, subnodeTree);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        var rows = file.GetNode(new PstNodeId(NodeId)).ReadTableContext().EnumerateRows().ToList();

        Assert.AreEqual(2, rows.Count);
        Assert.IsTrue(rows[1].TryGetCell(0x1111, out PstPropertyValue value));
        Assert.AreEqual(9, value.GetInt32());
    }

    /// <summary>
    /// Verifies that a row matrix holding fewer rows than the row index records is rejected rather than yielding a
    /// truncated table silently.
    /// </summary>
    [TestMethod]
    public void EnumerateRows_WhenMatrixIsShorterThanTheIndex_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        uint matrixHid = ltp.AddItem(Row(FirstRowId, 7, 0, 0x0102, 1, 0b1101_1000));
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
            rowsHnid: matrixHid,
            (FirstRowId, 0),
            (SecondRowId, 1));
        ltp.AddHeapNode(builder, NodeId);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstTableContext context = file.GetNode(new PstNodeId(NodeId)).ReadTableContext();

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = context.EnumerateRows().ToList());
    }

    /// <summary>
    /// Verifies that a row matrix holding more rows than the row index records is rejected under strict validation
    /// — surplus rows are unindexed content the writer never committed — while the tolerant levels yield exactly the
    /// indexed rows.
    /// </summary>
    [TestMethod]
    public void EnumerateRows_WhenMatrixIsLongerThanTheIndex_ShouldThrowOnlyUnderStrict()
    {
        static PstFixtureBuilder Build()
        {
            var builder = new PstFixtureBuilder();
            var ltp = new PstLtpFixtureBuilder();

            byte[] matrix =
            [
                .. Row(FirstRowId, 7, 0, 0x0102, 1, 0b1101_1000),
                .. Row(SecondRowId, 9, 0, 0x0304, 0, 0b1101_1000),
            ];
            uint matrixHid = ltp.AddItem(matrix);
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
                rowsHnid: matrixHid,
                (FirstRowId, 0));
            ltp.AddHeapNode(builder, NodeId);
            return builder;
        }

        using (PstFile tolerant = PstFile.Open(Build().BuildStream(), PstFileOptions.Default))
        {
            Assert.AreEqual(1, tolerant.GetNode(new PstNodeId(NodeId)).ReadTableContext().EnumerateRows().Count());
        }

        using PstFile strict = PstFile.Open(Build().BuildStream(), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict });
        PstTableContext context = strict.GetNode(new PstNodeId(NodeId)).ReadTableContext();

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() => _ = context.EnumerateRows().ToList());

        Assert.AreEqual(PstFileError.InvalidTableContext, ex.Error);
    }
}
