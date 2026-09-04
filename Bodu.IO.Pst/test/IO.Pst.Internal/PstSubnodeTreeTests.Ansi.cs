// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSubnodeTreeTests.Ansi.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

public partial class PstSubnodeTreeTests
{
    /// <summary>
    /// Verifies that an ANSI subnode leaf block (12-byte entries) exposes each subnode and its payload.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenAnsiLeafBlock_ShouldExposeEntries()
    {
        var builder = new PstFixtureBuilder { Format = PstFileFormat.Ansi };
        ulong first = builder.AddDataBlock([1, 1, 1]);
        ulong second = builder.AddDataBlock([2, 2]);
        ulong tree = builder.AddSubnodeLeafBlock((0x41, first, 0), (0x61, second, 0));

        PstNode node = OpenOwner(builder, tree);

        CollectionAssert.AreEqual(new uint[] { 0x41, 0x61 }, node.EnumerateSubnodes().Select(static s => s.NodeId.Value).ToArray());
        Assert.IsTrue(node.TryGetSubnode(new PstNodeId(0x61), out PstNode? subnode));
        CollectionAssert.AreEqual(new byte[] { 2, 2 }, subnode.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that an ANSI subnode index block (8-byte entries) over two leaf blocks enumerates all four subnodes.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenAnsiIndexBlock_ShouldFlattenLeaves()
    {
        var builder = new PstFixtureBuilder { Format = PstFileFormat.Ansi };
        ulong data = builder.AddDataBlock([9]);
        ulong leftLeaf = builder.AddSubnodeLeafBlock((0x41, data, 0), (0x61, data, 0));
        ulong rightLeaf = builder.AddSubnodeLeafBlock((0x81, data, 0), (0xA1, data, 0));
        ulong index = builder.AddSubnodeIndexBlock(leftLeaf, rightLeaf);

        PstNode node = OpenOwner(builder, index);

        CollectionAssert.AreEqual(new uint[] { 0x41, 0x61, 0x81, 0xA1 }, node.EnumerateSubnodes().Select(static s => s.NodeId.Value).ToArray());
    }
}
