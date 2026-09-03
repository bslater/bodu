// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstHeapNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;
using Bodu.Test.Kat;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Verifies the heap-on-node reader: header and page-map parsing across single- and multi-block heaps, HID resolution,
/// and rejection of malformed heap geometry.
/// </summary>
[TestClass]
public partial class PstHeapNodeTests
{
    /// <summary>The node identifier the fixtures use for the node under test.</summary>
    private const uint NodeId = 0x21;

    /// <summary>
    /// Builds an item whose bytes vary with position and seed, so a misresolved HID surfaces as a content mismatch.
    /// </summary>
    /// <param name="length">The item length.</param>
    /// <param name="seed">A value mixed into each byte.</param>
    /// <returns>The item bytes.</returns>
    private static byte[] Item(int length, int seed) =>
        [.. Enumerable.Range(0, length).Select(i => (byte)((i * 17) + seed))];

    /// <summary>
    /// Opens a container and parses the heap of the node under test.
    /// </summary>
    /// <param name="builder">The container builder holding the heap node.</param>
    /// <returns>The open file and the parsed heap; the caller disposes the file.</returns>
    private static (PstFile File, PstHeapNode Heap) Parse(PstFixtureBuilder builder)
    {
        PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        Assert.IsTrue(PstBTree.TryFindNode(file.GetSource(), NodeId, out PstNbtEntry entry));
        return (file, PstHeapNode.Parse(file.GetSource(), entry));
    }

    /// <summary>
    /// Verifies that a single-block heap parses its header and resolves each item under its HID.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSingleBlockHeap_ShouldResolveEveryItem()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xBC, UserRootHid = 0x20 };
        byte[][] items = [Item(16, 1), Item(40, 2), Item(3, 3)];
        uint[] hids = [.. items.Select(ltp.AddItem)];

        var builder = new PstFixtureBuilder();
        ltp.AddHeapNode(builder, NodeId);

        (PstFile file, PstHeapNode heap) = Parse(builder);
        using (file)
        {
            Assert.AreEqual(NodeId, heap.NodeId);
            Assert.AreEqual(0xBC, heap.ClientSignature);
            Assert.AreEqual(0x20u, heap.UserRootHid);

            for (int i = 0; i < items.Length; i++)
                CollectionAssert.AreEqual(items[i], heap.GetItem(hids[i]).ToArray());
        }
    }

    /// <summary>
    /// Verifies that a multi-block heap resolves items in later blocks through the HID's block-index bits, including
    /// past the bitmap-header block at index 8.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultiBlockHeap_ShouldResolveItemsAcrossBlocks()
    {
        var ltp = new PstLtpFixtureBuilder();
        var expected = new Dictionary<uint, byte[]>();

        // Ten blocks so the walk crosses the HNBITMAPHDR shape at block index 8.
        for (int block = 0; block < 10; block++)
        {
            if (block > 0)
                ltp.StartBlock();

            byte[] item = Item(24, block + 1);
            expected[ltp.AddItem(item)] = item;
        }

        var builder = new PstFixtureBuilder();
        ltp.AddHeapNode(builder, NodeId);

        (PstFile file, PstHeapNode heap) = Parse(builder);
        using (file)
        {
            foreach ((uint hid, byte[] item) in expected)
                CollectionAssert.AreEqual(item, heap.GetItem(hid).ToArray());
        }
    }

    /// <summary>
    /// Verifies that <see cref="PstHeapNode.TryGetItem" /> reports failure — and <see cref="PstHeapNode.GetItem" />
    /// throws <see cref="PstFileFormatException" /> — for identifiers outside the heap: the null HID, nonzero type
    /// bits, an out-of-range item index, and an out-of-range block index.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="hid">The identifier that must not resolve.</param>
    [TestMethod]
    [DataRow("null hid", 0u)]
    [DataRow("type bits set", 0x21u)]
    [DataRow("item index beyond the block", 0x40u)]
    [DataRow("block index beyond the heap", 0x10020u)]
    public void GetItem_WhenHidDoesNotResolve_ShouldThrowPstFileFormatException(string testName, uint hid)
    {
        Assert.IsNotNull(testName);

        var ltp = new PstLtpFixtureBuilder();
        _ = ltp.AddItem(Item(8, 1));

        var builder = new PstFixtureBuilder();
        ltp.AddHeapNode(builder, NodeId);

        (PstFile file, PstHeapNode heap) = Parse(builder);
        using (file)
        {
            Assert.IsFalse(heap.TryGetItem(hid, out _));
            _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = heap.GetItem(hid));
        }
    }

    /// <summary>
    /// Verifies that a node with no data at all is rejected as a heap, since every heap carries at least its header
    /// block.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNodeHasNoData_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, dataBlockId: 0);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        Assert.IsTrue(PstBTree.TryFindNode(file.GetSource(), NodeId, out PstNbtEntry entry));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = PstHeapNode.Parse(file.GetSource(), entry));
    }

    /// <summary>
    /// Verifies that malformed heap geometry is rejected: a wrong heap signature, a truncated header, a page map
    /// beyond the block, a page-map count that overruns the block, and allocation offsets that regress or overrun.
    /// </summary>
    /// <param name="kat">The malformed-heap row.</param>
    [TestMethod]
    [DynamicData(
        nameof(MalformedHeapBlocks),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenHeapGeometryIsMalformed_ShouldThrowPstFileFormatException(InvalidKat<byte[]> kat)
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, builder.AddDataBlock(kat.Input));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        Assert.IsTrue(PstBTree.TryFindNode(file.GetSource(), NodeId, out PstNbtEntry entry));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = PstHeapNode.Parse(file.GetSource(), entry));
    }

    /// <summary>
    /// Gets the malformed heap-block rows for <see cref="Parse_WhenHeapGeometryIsMalformed_ShouldThrowPstFileFormatException" />.
    /// </summary>
    /// <value>The scenario rows.</value>
    private static IEnumerable<object[]> MalformedHeapBlocks
    {
        get
        {
            yield return [new InvalidKat<byte[]>("wrong heap signature", BuildBlock(signature: 0xEB, mapOffset: 12, allocations: [12, 12]), typeof(PstFileFormatException))];
            yield return [new InvalidKat<byte[]>("header truncated", [0x0C, 0x00, 0xEC, 0xBC], typeof(PstFileFormatException))];
            yield return [new InvalidKat<byte[]>("page map beyond the block", BuildBlock(signature: 0xEC, mapOffset: 500, allocations: [12, 12]), typeof(PstFileFormatException))];
            yield return [new InvalidKat<byte[]>("allocation regresses", BuildBlock(signature: 0xEC, mapOffset: 20, allocations: [18, 12]), typeof(PstFileFormatException))];
            yield return [new InvalidKat<byte[]>("allocation beyond the block", BuildBlock(signature: 0xEC, mapOffset: 12, allocations: [12, 600]), typeof(PstFileFormatException))];
            yield return [new InvalidKat<byte[]>("allocation count overruns the block", BuildCountOverrun(), typeof(PstFileFormatException))];
        }
    }

    /// <summary>
    /// Builds a heap block with an explicit signature, page-map offset, and allocation offsets.
    /// </summary>
    /// <param name="signature">The heap signature byte to write.</param>
    /// <param name="mapOffset">The page-map offset the header records.</param>
    /// <param name="allocations">The allocation offsets to write, the first being the content start.</param>
    /// <returns>The block payload.</returns>
    private static byte[] BuildBlock(byte signature, ushort mapOffset, ushort[] allocations)
    {
        int declaredEnd = mapOffset + 4 + (allocations.Length * 2);
        var block = new byte[Math.Min(Math.Max(declaredEnd, 24), 64)];
        BinaryPrimitives.WriteUInt16LittleEndian(block, mapOffset);
        block[2] = signature;
        block[3] = 0xBC;

        if (mapOffset + 4 <= block.Length)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(mapOffset), (ushort)(allocations.Length - 1));
            for (int i = 0; i < allocations.Length && mapOffset + 4 + (i * 2) + 2 <= block.Length; i++)
                BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(mapOffset + 4 + (i * 2)), allocations[i]);
        }

        return block;
    }

    /// <summary>
    /// Builds a heap block whose page-map allocation count overruns the block's end.
    /// </summary>
    /// <returns>The block payload.</returns>
    private static byte[] BuildCountOverrun()
    {
        var block = new byte[20];
        BinaryPrimitives.WriteUInt16LittleEndian(block, 12);
        block[2] = 0xEC;
        block[3] = 0xBC;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(12), 100);
        return block;
    }
}
