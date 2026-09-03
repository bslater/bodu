// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataTreeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Verifies the multi-block data trees - the <c>XBLOCK</c> and <c>XXBLOCK</c> layouts a node uses once its payload
/// exceeds a single block.
/// </summary>
/// <remarks>
/// Every fixture in the reference corpus is small enough that each node's payload fits one block, so this whole layer
/// had never executed. It is also where a reader is most likely to be wrong in a way a small file cannot reveal:
/// concatenation order, the distinction between a one-level and a two-level tree, and the recorded logical length are
/// all invisible until a payload actually spans blocks. The fixtures are authored rather than captured, so each test
/// controls the exact shape it is asserting.
/// </remarks>
[TestClass]
public partial class PstDataTreeTests
{
    /// <summary>The node identifier the fixtures use for the node under test.</summary>
    private const uint NodeId = 0x21;

    /// <summary>
    /// Builds a payload of a given length whose bytes vary with position, so a misordered or duplicated block is
    /// visible in the result rather than masked by a uniform fill.
    /// </summary>
    /// <param name="length">The payload length.</param>
    /// <param name="seed">A value mixed into each byte to distinguish one block from another.</param>
    /// <returns>The payload.</returns>
    private static byte[] Payload(int length, int seed) =>
        [.. Enumerable.Range(0, length).Select(i => (byte)((i * 31) + seed))];

    /// <summary>
    /// Verifies that a node whose data-block identifier is external reads back as that block alone, which is the
    /// single-block baseline the tree cases are measured against.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenNodeHasASingleBlock_ShouldReturnItsPayload()
    {
        var builder = new PstFixtureBuilder();
        byte[] expected = Payload(200, 1);
        builder.AddNode(NodeId, builder.AddDataBlock(expected));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        CollectionAssert.AreEqual(expected, file.GetNode(new PstNodeId(NodeId)).ReadAllBytes());
    }

    /// <summary>
    /// Verifies that a node behind an <c>XBLOCK</c> reads back as its data blocks concatenated in list order.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenNodeHasAnXBlock_ShouldConcatenateItsDataBlocksInOrder()
    {
        var builder = new PstFixtureBuilder();
        byte[] first = Payload(1000, 1);
        byte[] second = Payload(1000, 2);
        byte[] third = Payload(500, 3);

        ulong tree = builder.AddXBlock(
            (uint)(first.Length + second.Length + third.Length),
            builder.AddDataBlock(first),
            builder.AddDataBlock(second),
            builder.AddDataBlock(third));
        builder.AddNode(NodeId, tree);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        byte[] expected = [.. first, .. second, .. third];

        CollectionAssert.AreEqual(expected, file.GetNode(new PstNodeId(NodeId)).ReadAllBytes());
    }

    /// <summary>
    /// Verifies that a node behind an <c>XXBLOCK</c> reads back as the concatenation of every data block beneath each
    /// of its <c>XBLOCK</c>s, in order - the two-level descent a payload beyond one <c>XBLOCK</c>'s capacity needs.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenNodeHasAnXXBlock_ShouldConcatenateEveryLeafInOrder()
    {
        var builder = new PstFixtureBuilder();
        byte[][] leaves = [.. Enumerable.Range(1, 6).Select(i => Payload(700, i))];

        ulong firstX = builder.AddXBlock(2100, builder.AddDataBlock(leaves[0]), builder.AddDataBlock(leaves[1]), builder.AddDataBlock(leaves[2]));
        ulong secondX = builder.AddXBlock(2100, builder.AddDataBlock(leaves[3]), builder.AddDataBlock(leaves[4]), builder.AddDataBlock(leaves[5]));
        builder.AddNode(NodeId, builder.AddXXBlock(4200, firstX, secondX));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        CollectionAssert.AreEqual(leaves.SelectMany(static l => l).ToArray(), file.GetNode(new PstNodeId(NodeId)).ReadAllBytes());
    }

    /// <summary>
    /// Verifies that a payload spanning several maximum-size blocks reassembles exactly, which is the case the layout
    /// exists for and the one a small fixture cannot reach.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenPayloadSpansFullBlocks_ShouldReassembleExactly()
    {
        var builder = new PstFixtureBuilder();
        byte[][] leaves = [.. Enumerable.Range(1, 4).Select(i => Payload(PstFixtureBuilder.MaxBlockPayload, i))];

        ulong tree = builder.AddXBlock(
            (uint)(PstFixtureBuilder.MaxBlockPayload * leaves.Length),
            [.. leaves.Select(builder.AddDataBlock)]);
        builder.AddNode(NodeId, tree);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        byte[] actual = file.GetNode(new PstNodeId(NodeId)).ReadAllBytes();

        Assert.AreEqual(PstFixtureBuilder.MaxBlockPayload * leaves.Length, actual.Length);
        CollectionAssert.AreEqual(leaves.SelectMany(static l => l).ToArray(), actual);
    }

    /// <summary>
    /// Verifies that an empty data-block identifier reads back as an empty payload rather than faulting, since a node
    /// is permitted to carry no data at all.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenNodeHasNoDataBlock_ShouldReturnEmpty()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, dataBlockId: 0);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        Assert.AreEqual(0, file.GetNode(new PstNodeId(NodeId)).ReadAllBytes().Length);
    }

    /// <summary>
    /// Verifies that the reported data length of a tree-backed node comes from the logical total the tree block
    /// records, not from the size of the tree block itself.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenNodeHasAnXBlock_ShouldReportTheLogicalPayloadLength()
    {
        var builder = new PstFixtureBuilder();
        ulong tree = builder.AddXBlock(
            3000,
            builder.AddDataBlock(Payload(2000, 1)),
            builder.AddDataBlock(Payload(1000, 2)));
        builder.AddNode(NodeId, tree);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        Assert.AreEqual(3000L, file.EnumerateNodes().Single(n => n.NodeId.Value == NodeId).DataLength);
    }

    /// <summary>
    /// Verifies that a data length is reported as zero when the identifier is internal but the block behind it is not
    /// a tree block, rather than a length read out of unrelated bytes.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenInternalBlockIsNotATree_ShouldReportZeroLength()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, builder.AddRawBlock([0x7F, 0x01, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00], isInternal: true));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        Assert.AreEqual(0L, file.EnumerateNodes().Single(n => n.NodeId.Value == NodeId).DataLength);
    }

    /// <summary>
    /// Verifies that a data length is reported as zero when the data-block identifier is not in the block B-tree,
    /// so enumeration surveys a damaged file rather than throwing partway through.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenDataBlockIsMissing_ShouldReportZeroLength()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, dataBlockId: 0x9999);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        Assert.AreEqual(0L, file.EnumerateNodes().Single(n => n.NodeId.Value == NodeId).DataLength);
    }

    /// <summary>
    /// Verifies that a data-block identifier absent from the block B-tree is rejected when the payload is actually
    /// read, rather than yielding a silently truncated result.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenDataBlockIsMissing_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, dataBlockId: 0x9999);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that an <c>XBLOCK</c> naming a child that is not in the block B-tree is rejected.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenTreeChildIsMissing_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, builder.AddXBlock(64, builder.AddDataBlock(Payload(64, 1)), 0x9999));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that an internal identifier whose block is not a well-formed tree block is rejected, covering a wrong
    /// block-type byte, an out-of-range level, a truncated header, and a child count that overruns the block.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="block">The malformed tree-block payload.</param>
    [TestMethod]
    [DataRow("wrong block type", new byte[] { 0x02, 0x01, 0x01, 0x00, 0x40, 0x00, 0x00, 0x00, 1, 0, 0, 0, 0, 0, 0, 0 })]
    [DataRow("level above two", new byte[] { 0x01, 0x03, 0x01, 0x00, 0x40, 0x00, 0x00, 0x00, 1, 0, 0, 0, 0, 0, 0, 0 })]
    [DataRow("level zero", new byte[] { 0x01, 0x00, 0x01, 0x00, 0x40, 0x00, 0x00, 0x00, 1, 0, 0, 0, 0, 0, 0, 0 })]
    [DataRow("header truncated", new byte[] { 0x01, 0x01, 0x01, 0x00 })]
    [DataRow("child count overruns the block", new byte[] { 0x01, 0x01, 0x08, 0x00, 0x40, 0x00, 0x00, 0x00, 1, 0, 0, 0, 0, 0, 0, 0 })]
    public void ReadAllBytes_WhenTreeBlockIsMalformed_ShouldThrowPstFileFormatException(string testName, byte[] block)
    {
        Assert.IsNotNull(testName);

        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, builder.AddRawBlock(block, isInternal: true));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that an <c>XXBLOCK</c> whose child is itself an <c>XXBLOCK</c> is rejected. The format allows exactly
    /// two levels, and following a third would recurse without bound on a crafted file.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenXXBlockChildIsNotAnXBlock_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        ulong inner = builder.AddXXBlock(64, builder.AddXBlock(64, builder.AddDataBlock(Payload(64, 1))));
        builder.AddNode(NodeId, builder.AddXXBlock(64, inner));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that segment resolution of a single-block node yields exactly one segment carrying the block's payload,
    /// so the heap-on-node reader sees the same boundary a one-block heap declares.
    /// </summary>
    [TestMethod]
    public void ResolveSegments_WhenNodeHasASingleBlock_ShouldReturnOneSegment()
    {
        var builder = new PstFixtureBuilder();
        byte[] expected = Payload(200, 1);
        builder.AddNode(NodeId, builder.AddDataBlock(expected));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNbtEntry entry = GetEntry(file, NodeId);

        List<byte[]> segments = PstDataTree.ResolveSegments(file.GetSource(), entry.DataBlockId);

        Assert.AreEqual(1, segments.Count);
        CollectionAssert.AreEqual(expected, segments[0]);
    }

    /// <summary>
    /// Verifies that segment resolution of an <c>XBLOCK</c>-backed node preserves each data block as its own segment,
    /// in list order — the boundary information the flattened payload discards and the heap-on-node requires.
    /// </summary>
    [TestMethod]
    public void ResolveSegments_WhenNodeHasAnXBlock_ShouldPreserveBlockBoundaries()
    {
        var builder = new PstFixtureBuilder();
        byte[][] leaves = [Payload(1000, 1), Payload(1000, 2), Payload(500, 3)];

        ulong tree = builder.AddXBlock(2500, [.. leaves.Select(builder.AddDataBlock)]);
        builder.AddNode(NodeId, tree);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNbtEntry entry = GetEntry(file, NodeId);

        List<byte[]> segments = PstDataTree.ResolveSegments(file.GetSource(), entry.DataBlockId);

        Assert.AreEqual(3, segments.Count);
        for (int i = 0; i < leaves.Length; i++)
            CollectionAssert.AreEqual(leaves[i], segments[i]);
    }

    /// <summary>
    /// Verifies that segment resolution of an <c>XXBLOCK</c>-backed node yields every leaf data block beneath each
    /// <c>XBLOCK</c> as its own segment, in order.
    /// </summary>
    [TestMethod]
    public void ResolveSegments_WhenNodeHasAnXXBlock_ShouldYieldEveryLeafInOrder()
    {
        var builder = new PstFixtureBuilder();
        byte[][] leaves = [.. Enumerable.Range(1, 6).Select(i => Payload(700, i))];

        ulong firstX = builder.AddXBlock(2100, builder.AddDataBlock(leaves[0]), builder.AddDataBlock(leaves[1]), builder.AddDataBlock(leaves[2]));
        ulong secondX = builder.AddXBlock(2100, builder.AddDataBlock(leaves[3]), builder.AddDataBlock(leaves[4]), builder.AddDataBlock(leaves[5]));
        builder.AddNode(NodeId, builder.AddXXBlock(4200, firstX, secondX));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNbtEntry entry = GetEntry(file, NodeId);

        List<byte[]> segments = PstDataTree.ResolveSegments(file.GetSource(), entry.DataBlockId);

        Assert.AreEqual(6, segments.Count);
        for (int i = 0; i < leaves.Length; i++)
            CollectionAssert.AreEqual(leaves[i], segments[i]);
    }

    /// <summary>
    /// Verifies that segment resolution of an empty data-block identifier yields no segments, matching the empty
    /// payload the flattened accessor reports.
    /// </summary>
    [TestMethod]
    public void ResolveSegments_WhenNodeHasNoDataBlock_ShouldReturnEmptyList()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, dataBlockId: 0);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNbtEntry entry = GetEntry(file, NodeId);

        Assert.AreEqual(0, PstDataTree.ResolveSegments(file.GetSource(), entry.DataBlockId).Count);
    }

    /// <summary>
    /// Finds the node B-tree entry for the given node identifier.
    /// </summary>
    /// <param name="file">The open file.</param>
    /// <param name="nodeId">The node identifier.</param>
    /// <returns>The node's B-tree entry.</returns>
    private static PstNbtEntry GetEntry(PstFile file, uint nodeId)
    {
        Assert.IsTrue(PstBTree.TryFindNode(file.GetSource(), nodeId, out PstNbtEntry entry));
        return entry;
    }

    /// <summary>
    /// Verifies that the data stream exposes the same bytes the array accessor returns for a tree-backed node, so a
    /// caller streaming a large payload sees no difference from one materializing it.
    /// </summary>
    [TestMethod]
    public void OpenDataStream_WhenNodeHasAnXBlock_ShouldMatchReadAllBytes()
    {
        var builder = new PstFixtureBuilder();
        ulong tree = builder.AddXBlock(
            1500,
            builder.AddDataBlock(Payload(1000, 1)),
            builder.AddDataBlock(Payload(500, 2)));
        builder.AddNode(NodeId, tree);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        using Stream stream = node.OpenDataStream();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        CollectionAssert.AreEqual(node.ReadAllBytes(), buffer.ToArray());
    }
}
