// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstHeapNodeTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstHeapNodeTests
{
    /// <summary>The number of blocks the multi-block page-map fixtures write, so that block 8 carries an HNBITMAPHDR.</summary>
    private const int PageMapFixtureBlocks = 10;

    /// <summary>
    /// Verifies that a heap whose first allocation starts at offset 0, inside the HNHDR, is rejected as
    /// <see cref="PstFileError.InvalidHeap" /> at every validation level, rather than serving the header's bytes as the
    /// item.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="level">The validation level.</param>
    [TestMethod]
    [DataRow("Minimal", PstValidationLevel.Minimal)]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    public void Parse_WhenTheFirstAllocationStartsAtZero_ShouldThrowPstFileFormatException(string testName, PstValidationLevel level)
    {
        Assert.IsNotNull(testName);
        var ltp = new PstLtpFixtureBuilder();
        _ = ltp.AddItem(Item(10, 1));
        List<byte[]> blocks = ltp.BuildBlocks();
        SetAllocation(blocks[0], 0, 0);
        using PstFile file = PstFile.Open(BuildHeapStream(blocks), new PstFileOptions { ValidationLevel = level });
        Assert.IsTrue(PstBTree.TryFindNode(file.GetSource(), NodeId, out PstNbtEntry entry));

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = PstHeapNode.Parse(file.GetSource(), entry);
        });

        Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
    }

    /// <summary>
    /// Verifies that an allocation starting inside the header of its page is rejected as
    /// <see cref="PstFileError.InvalidHeap" /> for every page kind: the 12-byte HNHDR of block 0, the 2-byte HNPAGEHDR of
    /// an ordinary later block, and the 66-byte HNBITMAPHDR of block 8.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="blockIndex">The heap block whose first allocation is patched.</param>
    /// <param name="offset">The allocation's start offset.</param>
    [TestMethod]
    [DataRow("HNHDR, offset 0", 0, 0)]
    [DataRow("HNHDR, offset 11", 0, 11)]
    [DataRow("HNPAGEHDR, offset 0", 1, 0)]
    [DataRow("HNPAGEHDR, offset 1", 1, 1)]
    [DataRow("HNBITMAPHDR, offset 2", 8, 2)]
    [DataRow("HNBITMAPHDR, offset 65", 8, 65)]
    public void Parse_WhenAnAllocationStartsInsideThePageHeader_ShouldThrowPstFileFormatException(string testName, int blockIndex, int offset)
    {
        Assert.IsNotNull(testName);
        List<byte[]> blocks = BuildPageMapFixtureBlocks();
        SetAllocation(blocks[blockIndex], 0, (ushort)offset);
        using PstFile file = PstFile.Open(BuildHeapStream(blocks), PstFileOptions.Default);
        Assert.IsTrue(PstBTree.TryFindNode(file.GetSource(), NodeId, out PstNbtEntry entry));

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = PstHeapNode.Parse(file.GetSource(), entry);
        });

        Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
    }

    /// <summary>
    /// Verifies that an allocation ending past the page map, inside it but still within the block, is rejected as
    /// <see cref="PstFileError.InvalidHeap" /> for every page kind.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="blockIndex">The heap block whose last allocation is patched.</param>
    [TestMethod]
    [DataRow("HNHDR", 0)]
    [DataRow("HNPAGEHDR", 1)]
    [DataRow("HNBITMAPHDR", 8)]
    public void Parse_WhenAnAllocationRunsPastThePageMap_ShouldThrowPstFileFormatException(string testName, int blockIndex)
    {
        Assert.IsNotNull(testName);
        List<byte[]> blocks = BuildPageMapFixtureBlocks();
        byte[] block = blocks[blockIndex];
        int pageMap = BinaryPrimitives.ReadUInt16LittleEndian(block);
        SetAllocation(block, BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(pageMap)), (ushort)(pageMap + 2));
        using PstFile file = PstFile.Open(BuildHeapStream(blocks), PstFileOptions.Default);
        Assert.IsTrue(PstBTree.TryFindNode(file.GetSource(), NodeId, out PstNbtEntry entry));

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = PstHeapNode.Parse(file.GetSource(), entry);
        });

        Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
    }

    /// <summary>
    /// Verifies that a page map placed inside its page's header, with an empty allocation table that starts there too,
    /// is rejected as <see cref="PstFileError.InvalidHeap" />: the HNHDR of block 0 and the HNBITMAPHDR of block 8.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="blockIndex">The heap block replaced.</param>
    /// <param name="pageMap">The page map's offset, inside the header.</param>
    [TestMethod]
    [DataRow("HNHDR", 0, 6)]
    [DataRow("HNBITMAPHDR", 8, 10)]
    public void Parse_WhenThePageMapStartsInsideThePageHeader_ShouldThrowPstFileFormatException(string testName, int blockIndex, int pageMap)
    {
        Assert.IsNotNull(testName);
        List<byte[]> blocks = BuildPageMapFixtureBlocks();
        var block = new byte[80];
        blocks[blockIndex].AsSpan(0, 4).CopyTo(block);
        BinaryPrimitives.WriteUInt16LittleEndian(block, (ushort)pageMap);
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pageMap), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pageMap + 2), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pageMap + 4), (ushort)pageMap);
        blocks[blockIndex] = block;
        using PstFile file = PstFile.Open(BuildHeapStream(blocks), PstFileOptions.Default);
        Assert.IsTrue(PstBTree.TryFindNode(file.GetSource(), NodeId, out PstNbtEntry entry));

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = PstHeapNode.Parse(file.GetSource(), entry);
        });

        Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
    }

    /// <summary>
    /// Builds the heap blocks of a well-formed heap of <see cref="PageMapFixtureBlocks" /> blocks, one 24-byte item in
    /// each.
    /// </summary>
    /// <returns>The heap blocks.</returns>
    private static List<byte[]> BuildPageMapFixtureBlocks()
    {
        var ltp = new PstLtpFixtureBuilder();
        for (int block = 0; block < PageMapFixtureBlocks; block++)
        {
            if (block > 0)
                ltp.StartBlock();

            _ = ltp.AddItem(Item(24, block + 1));
        }

        return ltp.BuildBlocks();
    }

    /// <summary>
    /// Rewrites one allocation offset (<c>rgibAlloc[i]</c>) of a heap block's page map.
    /// </summary>
    /// <param name="block">The heap block.</param>
    /// <param name="index">The allocation offset's index.</param>
    /// <param name="offset">The offset to write.</param>
    private static void SetAllocation(byte[] block, int index, ushort offset)
    {
        int pageMap = BinaryPrimitives.ReadUInt16LittleEndian(block);
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pageMap + 4 + (index * 2)), offset);
    }

    /// <summary>
    /// Writes heap blocks as the data of the node under test: one data block, or data blocks under an XBLOCK.
    /// </summary>
    /// <param name="blocks">The heap blocks.</param>
    /// <returns>The container, as a read-only stream.</returns>
    private static MemoryStream BuildHeapStream(List<byte[]> blocks)
    {
        var builder = new PstFixtureBuilder();
        ulong data = blocks.Count == 1
            ? builder.AddDataBlock(blocks[0])
            : builder.AddXBlock((uint)blocks.Sum(block => block.Length), [.. blocks.Select(builder.AddDataBlock)]);
        builder.AddNode(NodeId, data);

        return builder.BuildStream();
    }
}
