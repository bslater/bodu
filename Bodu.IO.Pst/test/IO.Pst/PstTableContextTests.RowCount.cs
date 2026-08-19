// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContextTests.RowCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstTableContext.RowCount" />.
/// </summary>
public partial class PstTableContextTests
{
    /// <summary>
    /// Verifies that the row count reflects the row index's record count.
    /// </summary>
    [TestMethod]
    public void RowCount_WhenTableCarriesRows_ShouldReportIndexRecordCount()
    {
        (PstFile file, PstTableContext context) = OpenSharedContext();
        using (file)
        {
            Assert.AreEqual(2, context.RowCount);
        }
    }

    /// <summary>
    /// Verifies that an empty table — no index records and a null row matrix — reports a count of zero and enumerates
    /// no rows.
    /// </summary>
    [TestMethod]
    public void RowCount_WhenTableIsEmpty_ShouldReportZero()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();
        _ = ltp.AddTableContext(
            [(Int32Tag, 4, 4, 0)],
            endOffset4: 8,
            endOffset2: 8,
            endOffset1: 8,
            rowWidth: 9,
            rowsHnid: 0);
        ltp.AddHeapNode(builder, NodeId);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstTableContext context = file.GetNode(new PstNodeId(NodeId)).ReadTableContext();

        Assert.AreEqual(0, context.RowCount);
        Assert.AreEqual(0, context.EnumerateRows().Count());
    }
}
