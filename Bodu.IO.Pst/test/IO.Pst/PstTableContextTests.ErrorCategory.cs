// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContextTests.ErrorCategory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

public partial class PstTableContextTests
{
    /// <summary>
    /// Verifies that a table context whose <c>TCINFO</c> carries the wrong type byte reports the table-context error
    /// category rather than leaving the exception uncategorized.
    /// </summary>
    [TestMethod]
    public void ReadTableContext_WhenTcInfoTypeIsWrong_ShouldReportInvalidTableContextCategory()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0x7C };
        ltp.UserRootHid = ltp.AddItem([0x7B, 0, 4, 0, 4, 0, 4, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
        ltp.AddHeapNode(builder, NodeId);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = node.ReadTableContext();
        });

        Assert.AreEqual(PstFileError.InvalidTableContext, ex.Error);
    }

    /// <summary>
    /// Verifies that a table-context heap with no client root reports the table-context error category — it is the
    /// table that is malformed, not the heap addressing beneath it.
    /// </summary>
    [TestMethod]
    public void ReadTableContext_WhenUserRootIsNull_ShouldReportInvalidTableContextCategory()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0x7C, UserRootHid = 0 };
        _ = ltp.AddItem([0]);
        ltp.AddHeapNode(builder, NodeId);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = node.ReadTableContext();
        });

        Assert.AreEqual(PstFileError.InvalidTableContext, ex.Error);
    }
}
