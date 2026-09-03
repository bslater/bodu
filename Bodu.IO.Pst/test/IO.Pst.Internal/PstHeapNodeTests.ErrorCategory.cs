// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstHeapNodeTests.ErrorCategory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstHeapNodeTests
{
    /// <summary>
    /// Verifies that a node whose data block is too short to carry a heap header reports the heap error category
    /// rather than leaving the exception uncategorized.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderIsTruncated_ShouldReportInvalidHeapCategory()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, builder.AddDataBlock([0x0C, 0x00, 0xEC]));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = node.ReadPropertyContext();
        });

        Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
    }
}
