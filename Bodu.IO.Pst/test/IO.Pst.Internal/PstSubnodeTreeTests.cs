// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSubnodeTreeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Verifies the subnode tree - the private namespace of child nodes an <c>SLBLOCK</c> holds directly or an
/// <c>SIBLOCK</c> spreads over several <c>SLBLOCK</c>s.
/// </summary>
/// <remarks>
/// The reference fixtures reach the leaf form only; the index form appears once a node has more subnodes than one
/// block can list, which none of them do. That leaves the recursion, its one-level bound, and every malformed-geometry
/// rejection unexercised, so the fixtures here are authored to produce each shape exactly.
/// </remarks>
[TestClass]
public partial class PstSubnodeTreeTests
{
    /// <summary>The node identifier the fixtures use for the owning node.</summary>
    private const uint NodeId = 0x21;

    /// <summary>
    /// Opens a container whose single node carries the supplied subnode tree.
    /// </summary>
    /// <param name="builder">The builder holding the blocks.</param>
    /// <param name="subnodeBlockId">The subnode-tree block identifier.</param>
    /// <returns>The open node.</returns>
    private static PstNode OpenOwner(PstFixtureBuilder builder, ulong subnodeBlockId)
    {
        builder.AddNode(NodeId, dataBlockId: 0, subnodeBlockId);

        PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        return file.GetNode(new PstNodeId(NodeId));
    }

    /// <summary>
    /// Verifies that a node with no subnode tree reports none and enumerates to nothing.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenNodeHasNoSubnodeTree_ShouldYieldNothing()
    {
        PstNode node = OpenOwner(new PstFixtureBuilder(), subnodeBlockId: 0);

        Assert.IsFalse(node.HasSubnodes);
        Assert.AreEqual(0, node.EnumerateSubnodes().Count());
    }

    /// <summary>
    /// Verifies that an <c>SLBLOCK</c> yields its rows in stored order, with the node identifiers it records.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenTreeIsALeafBlock_ShouldYieldItsRowsInOrder()
    {
        var builder = new PstFixtureBuilder();
        ulong first = builder.AddDataBlock([1, 2, 3]);
        ulong second = builder.AddDataBlock([4, 5]);
        ulong tree = builder.AddSubnodeLeafBlock((0x8022u, first, 0UL), (0x8042u, second, 0UL));

        PstNode node = OpenOwner(builder, tree);

        Assert.IsTrue(node.HasSubnodes);
        CollectionAssert.AreEqual(
            new uint[] { 0x8022, 0x8042 },
            node.EnumerateSubnodes().Select(static s => s.NodeId.Value).ToArray());
    }

    /// <summary>
    /// Verifies that a subnode resolves to its own payload, so the private namespace is usable rather than merely
    /// enumerable.
    /// </summary>
    [TestMethod]
    public void TryGetSubnode_WhenSubnodeExists_ShouldResolveItsPayload()
    {
        var builder = new PstFixtureBuilder();
        byte[] payload = [9, 8, 7, 6];
        ulong tree = builder.AddSubnodeLeafBlock((0x8022u, builder.AddDataBlock(payload), 0UL));

        PstNode node = OpenOwner(builder, tree);

        Assert.IsTrue(node.TryGetSubnode(new PstNodeId(0x8022), out PstNode? subnode));
        CollectionAssert.AreEqual(payload, subnode.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that an absent subnode is reported as a miss rather than surfacing an unrelated sibling.
    /// </summary>
    [TestMethod]
    public void TryGetSubnode_WhenSubnodeIsAbsent_ShouldReturnFalse()
    {
        var builder = new PstFixtureBuilder();
        ulong tree = builder.AddSubnodeLeafBlock((0x8022u, builder.AddDataBlock([1]), 0UL));

        PstNode node = OpenOwner(builder, tree);

        Assert.IsFalse(node.TryGetSubnode(new PstNodeId(0x9999), out _));
    }

    /// <summary>
    /// Verifies that an <c>SIBLOCK</c> yields the rows of every <c>SLBLOCK</c> it lists, flattened in list order -
    /// the form a node with more subnodes than one block can hold takes.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenTreeIsAnIndexBlock_ShouldFlattenEveryLeaf()
    {
        var builder = new PstFixtureBuilder();
        ulong firstLeaf = builder.AddSubnodeLeafBlock((0x8022u, 0UL, 0UL), (0x8042u, 0UL, 0UL));
        ulong secondLeaf = builder.AddSubnodeLeafBlock((0x8062u, 0UL, 0UL));
        ulong index = builder.AddSubnodeIndexBlock(firstLeaf, secondLeaf);

        PstNode node = OpenOwner(builder, index);

        CollectionAssert.AreEqual(
            new uint[] { 0x8022, 0x8042, 0x8062 },
            node.EnumerateSubnodes().Select(static s => s.NodeId.Value).ToArray());
    }

    /// <summary>
    /// Verifies that an <c>SIBLOCK</c> nested beneath another is rejected. The format permits exactly one index
    /// level, and following a second would recurse without bound on a crafted file.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenIndexBlockIsNested_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        ulong inner = builder.AddSubnodeIndexBlock(builder.AddSubnodeLeafBlock((0x8022u, 0UL, 0UL)));
        ulong outer = builder.AddSubnodeIndexBlock(inner);

        PstNode node = OpenOwner(builder, outer);

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.EnumerateSubnodes().ToList());
    }

    /// <summary>
    /// Verifies that a subnode-tree block identifier absent from the block B-tree is rejected rather than treated as
    /// an empty tree.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenTreeBlockIsMissing_ShouldThrowPstFileFormatException()
    {
        PstNode node = OpenOwner(new PstFixtureBuilder(), subnodeBlockId: 0x9999);

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.EnumerateSubnodes().ToList());
    }

    /// <summary>
    /// Verifies that a subnode block whose header is not a well-formed <c>SLBLOCK</c> or <c>SIBLOCK</c> is rejected,
    /// covering a wrong block-type byte, a truncated header, an out-of-range level, and a row count that overruns the
    /// block at each level.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="block">The malformed subnode-block payload.</param>
    [TestMethod]
    [DataRow("wrong block type", new byte[] { 0x01, 0x00, 0x00, 0x00, 0, 0, 0, 0 })]
    [DataRow("header truncated", new byte[] { 0x02, 0x00, 0x00, 0x00 })]
    [DataRow("level above one", new byte[] { 0x02, 0x02, 0x00, 0x00, 0, 0, 0, 0 })]
    [DataRow("leaf row count overruns the block", new byte[] { 0x02, 0x00, 0x04, 0x00, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 })]
    [DataRow("index row count overruns the block", new byte[] { 0x02, 0x01, 0x04, 0x00, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 })]
    public void EnumerateSubnodes_WhenBlockIsMalformed_ShouldThrowPstFileFormatException(string testName, byte[] block)
    {
        Assert.IsNotNull(testName);

        var builder = new PstFixtureBuilder();
        PstNode node = OpenOwner(builder, builder.AddRawBlock(block, isInternal: true));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.EnumerateSubnodes().ToList());
    }
}
