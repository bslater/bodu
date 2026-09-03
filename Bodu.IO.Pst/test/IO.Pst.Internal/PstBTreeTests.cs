// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTreeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Verifies the B-tree walk: descent through a branch level, and rejection of a page whose entry geometry escapes the
/// entry area.
/// </summary>
/// <remarks>
/// Every reference fixture's trees are shallow enough to fit one page each, so the descent path - the loop that picks
/// the child whose key is the greatest at or below the target, and stops at the first key above it - had never run.
/// A search that failed to stop, or stopped one child early, would still find every key in a single-page tree and
/// none of the fixtures would notice.
/// </remarks>
[TestClass]
public partial class PstBTreeTests
{
    /// <summary>The number of nodes the multi-page fixtures declare.</summary>
    private const int NodeCount = 12;

    /// <summary>
    /// Builds a container whose trees are forced onto several pages beneath a branch level.
    /// </summary>
    /// <returns>The container bytes.</returns>
    private static byte[] BuildMultiPageTree()
    {
        var builder = new PstFixtureBuilder { MaxEntriesPerPage = 3 };
        for (int i = 0; i < NodeCount; i++)
            builder.AddNode((uint)(0x21 + (i * 0x20)), builder.AddDataBlock([(byte)i, (byte)(i + 1)]));

        return builder.Build();
    }

    /// <summary>
    /// Verifies that enumeration returns every leaf across every page, in key order, when the tree has a branch
    /// level - so a caller surveying a large file sees the whole node database rather than one page of it.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenTreeHasABranchLevel_ShouldYieldEveryLeafInKeyOrder()
    {
        using PstFile file = PstFile.Open(new MemoryStream(BuildMultiPageTree(), writable: false), PstFileOptions.Default);

        uint[] ids = [.. file.EnumerateNodes().Select(static n => n.NodeId.Value)];

        Assert.AreEqual(NodeCount, ids.Length);
        CollectionAssert.AreEqual(ids.OrderBy(static i => i).ToArray(), ids);
    }

    /// <summary>
    /// Verifies that every node is reachable by search through a branch level, including the first and last of each
    /// leaf page - the boundaries a descent that stops one child early or late would miss.
    /// </summary>
    [TestMethod]
    public void TryGetNode_WhenTreeHasABranchLevel_ShouldFindEveryLeaf()
    {
        using PstFile file = PstFile.Open(new MemoryStream(BuildMultiPageTree(), writable: false), PstFileOptions.Default);

        for (int i = 0; i < NodeCount; i++)
        {
            var id = new PstNodeId((uint)(0x21 + (i * 0x20)));

            Assert.IsTrue(file.TryGetNode(id, out _), $"Node {id} was not found.");
        }
    }

    /// <summary>
    /// Verifies that a key below every leaf is reported as a miss. The descent finds no child at or below it and
    /// must stop rather than fall into the leftmost page and return its first entry.
    /// </summary>
    [TestMethod]
    public void TryGetNode_WhenKeyIsBelowEveryLeaf_ShouldReturnFalse()
    {
        using PstFile file = PstFile.Open(new MemoryStream(BuildMultiPageTree(), writable: false), PstFileOptions.Default);

        Assert.IsFalse(file.TryGetNode(new PstNodeId(0x01), out _));
    }

    /// <summary>
    /// Verifies that a key above every leaf, and one that falls between two leaves, are both reported as misses
    /// rather than resolving to the nearest entry.
    /// </summary>
    [TestMethod]
    public void TryGetNode_WhenKeyIsAbsent_ShouldReturnFalse()
    {
        using PstFile file = PstFile.Open(new MemoryStream(BuildMultiPageTree(), writable: false), PstFileOptions.Default);

        Assert.IsFalse(file.TryGetNode(new PstNodeId(0xFFFF), out _));
        Assert.IsFalse(file.TryGetNode(new PstNodeId(0x30), out _));
    }

    /// <summary>
    /// Verifies that an absent node is refused by the throwing accessor, with the identifier in the message so the
    /// caller can tell which lookup failed.
    /// </summary>
    [TestMethod]
    public void GetNode_WhenNodeIsAbsent_ShouldThrowPstFileException()
    {
        using PstFile file = PstFile.Open(new MemoryStream(BuildMultiPageTree(), writable: false), PstFileOptions.Default);

        _ = Assert.ThrowsExactly<PstNodeNotFoundException>(() => _ = file.GetNode(new PstNodeId(0xFFFF)));
    }

    /// <summary>
    /// Verifies that a page whose entry geometry escapes the 488-byte entry area is rejected, covering a stride
    /// below the smallest legal entry and a count that overruns the area. Either would otherwise read entries out of
    /// the page trailer.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="fieldOffset">The offset within the page of the geometry byte to overwrite.</param>
    /// <param name="value">The value to write.</param>
    [TestMethod]
    [DataRow("stride below the minimum", 490, (byte)8)]
    [DataRow("count overruns the entry area", 488, (byte)200)]
    public void EnumerateNodes_WhenPageGeometryIsInvalid_ShouldThrowPstFileFormatException(string testName, int fieldOffset, byte value)
    {
        Assert.IsNotNull(testName);

        var builder = new PstFixtureBuilder();
        builder.AddNode(0x21, builder.AddDataBlock([1, 2, 3]));
        byte[] file = builder.Build();
        file[PstFixtureBuilder.ReadNodeTreeRootOffset(file) + fieldOffset] = value;

        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), PstFileOptions.Default);

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = pst.EnumerateNodes().ToList());
    }
}
