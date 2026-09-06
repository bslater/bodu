// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTreeTests.Ansi.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

public partial class PstBTreeTests
{
    /// <summary>
    /// Verifies that an ANSI node B-tree with more entries than one page holds (31 sixteen-byte entries) descends its
    /// 12-byte branch entries and enumerates every node.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenAnsiTreeHasBranchLevel_ShouldEnumerateEveryNode()
    {
        var builder = new PstFixtureBuilder { Format = PstFileFormat.Ansi };
        for (uint i = 0; i < 100; i++)
            builder.AddNode(0x21 + (i << 5), builder.AddDataBlock([(byte)i, (byte)(i + 1)]));

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict });

        Assert.AreEqual(100, file.EnumerateNodes().Count());
        CollectionAssert.AreEqual(new byte[] { 57, 58 }, file.GetNode(new PstNodeId(0x21 + (57u << 5))).ReadAllBytes());
    }

    /// <summary>
    /// Verifies that a forced ANSI branch level (two entries per page) still resolves point lookups on both sides.
    /// </summary>
    [TestMethod]
    public void GetNode_WhenAnsiTreeIsForcedToBranch_ShouldFindEveryNode()
    {
        var builder = new PstFixtureBuilder { Format = PstFileFormat.Ansi, MaxEntriesPerPage = 2 };
        for (uint i = 0; i < 5; i++)
            builder.AddNode(0x21 + (i << 5), builder.AddDataBlock([(byte)(i * 3)]));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        for (uint i = 0; i < 5; i++)
            CollectionAssert.AreEqual(new byte[] { (byte)(i * 3) }, file.GetNode(new PstNodeId(0x21 + (i << 5))).ReadAllBytes());

        Assert.IsFalse(file.TryGetNode(new PstNodeId(0x21 + (9u << 5)), out _));
    }
}
