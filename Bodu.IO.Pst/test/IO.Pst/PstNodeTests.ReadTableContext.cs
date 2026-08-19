// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.ReadTableContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstNode.ReadTableContext" />: the LTP table entry point on a node.
/// </summary>
public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that a node carrying a table context opens it and serves a row, exercising the primary LTP table read
    /// path end to end.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void ReadTableContext_WhenNodeCarriesTableContext_ShouldServeRows()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        var row = new byte[10];
        BinaryPrimitives.WriteUInt32LittleEndian(row, 0x100);
        BinaryPrimitives.WriteInt32LittleEndian(row.AsSpan(4), 42);
        row[9] = 0b1100_0000;
        uint matrixHid = ltp.AddItem(row);

        _ = ltp.AddTableContext(
            [(0x67F40003, 0, 4, 0), (0x11110003, 4, 4, 1)],
            endOffset4: 8,
            endOffset2: 8,
            endOffset1: 9,
            rowWidth: 10,
            rowsHnid: matrixHid,
            (0x100, 0));
        ltp.AddHeapNode(builder, 0x21);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstTableContext context = file.GetNode(new PstNodeId(0x21)).ReadTableContext();

        Assert.AreEqual(1, context.RowCount);
        Assert.IsTrue(context.TryGetRow(0x100, out PstTableRow? tableRow));
        Assert.IsTrue(tableRow.TryGetCell(0x1111, out PstPropertyValue value));
        Assert.AreEqual(42, value.GetInt32());
    }

    /// <summary>
    /// Verifies that a node whose heap declares a property context is rejected as a table context.
    /// </summary>
    [TestMethod]
    public void ReadTableContext_WhenNodeCarriesPropertyContext_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();
        _ = ltp.AddPropertyContext((0x1001, 0x0003, 42));
        ltp.AddHeapNode(builder, 0x21);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(0x21));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadTableContext());
    }

    /// <summary>
    /// Verifies that malformed table geometry — regressing region offsets, a bitmap too small for the declared
    /// columns, and a column overrunning the cell region — is rejected.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="endOffset4">The declared end of the 4-byte region.</param>
    /// <param name="endOffset1">The declared end of the 1-byte region.</param>
    /// <param name="rowWidth">The declared row width.</param>
    /// <param name="columnEnd">The single column's cell offset.</param>
    [TestMethod]
    [DataRow("regions regress", (ushort)8, (ushort)6, (ushort)9, (ushort)0)]
    [DataRow("bitmap too small", (ushort)8, (ushort)9, (ushort)9, (ushort)0)]
    [DataRow("column overruns the cell region", (ushort)8, (ushort)8, (ushort)9, (ushort)6)]
    public void ReadTableContext_WhenGeometryIsMalformed_ShouldThrowPstFileFormatException(
        string testName, ushort endOffset4, ushort endOffset1, ushort rowWidth, ushort columnEnd)
    {
        Assert.IsNotNull(testName);

        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();
        _ = ltp.AddTableContext(
            [(0x11110003, columnEnd, 4, 0)],
            endOffset4,
            endOffset2: endOffset4,
            endOffset1,
            rowWidth,
            rowsHnid: 0);
        ltp.AddHeapNode(builder, 0x21);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(0x21));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadTableContext());
    }
}
