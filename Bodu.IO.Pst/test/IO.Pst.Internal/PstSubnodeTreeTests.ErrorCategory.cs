// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSubnodeTreeTests.ErrorCategory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstSubnodeTreeTests
{
    /// <summary>
    /// Verifies that a malformed subnode block reports the subnode-tree error category rather than leaving the
    /// exception uncategorized.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenBlockTypeIsWrong_ShouldReportInvalidSubnodeTreeCategory()
    {
        var builder = new PstFixtureBuilder();
        ulong tree = builder.AddRawBlock([0x03, 0x00, 0x01, 0x00, 0, 0, 0, 0], isInternal: true);

        PstNode node = OpenOwner(builder, tree);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = node.EnumerateSubnodes().ToList();
        });

        Assert.AreEqual(PstFileError.InvalidSubnodeTree, ex.Error);
    }
}
