// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTreeTests.KeyRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstBTreeTests
{
    /// <summary>The nodes the key-range fixtures declare, two to each leaf page of the node B-tree.</summary>
    private static readonly uint[] s_keyRangeNodeIds = [0x21, 0x61, 0x122, 0x8022];

    /// <summary>
    /// Verifies that enumerating a node B-tree in which two entries of the root page reference the same leaf page fails
    /// with <see cref="PstFileError.InvalidPage" /> at every validation level, instead of listing that page's nodes twice:
    /// the second entry's key is above every key on the page.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="level">The validation level.</param>
    [TestMethod]
    [DataRow("Minimal", PstValidationLevel.Minimal)]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    public void EnumerateNodes_WhenTwoEntriesReferenceOneLeafPage_ShouldThrowPstFileFormatException(string testName, PstValidationLevel level)
    {
        Assert.IsNotNull(testName);
        byte[] file = BuildTwoLeafTrees();
        RedirectBranchEntry(file, PstFixtureBuilder.ReadNodeTreeRootOffset(file), entry: 1, toEntry: 0);
        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), new PstFileOptions { ValidationLevel = level });

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = pst.EnumerateNodes().ToList();
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }

    /// <summary>
    /// Verifies that enumerating a node B-tree whose leaf page holds a key below the key of the entry that references
    /// it fails with <see cref="PstFileError.InvalidPage" />.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenALeafKeyIsBelowItsEntryKey_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildTwoLeafTrees();
        SetBranchKey(file, PstFixtureBuilder.ReadNodeTreeRootOffset(file), entry: 1, key: 0x123);
        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), PstFileOptions.Default);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = pst.EnumerateNodes().ToList();
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }

    /// <summary>
    /// Verifies that enumerating a node B-tree whose leaf page holds a key at or above the key of the next entry of its
    /// parent fails with <see cref="PstFileError.InvalidPage" />.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenALeafKeyReachesTheNextEntryKey_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildTwoLeafTrees();
        SetBranchKey(file, PstFixtureBuilder.ReadNodeTreeRootOffset(file), entry: 1, key: 0x61);
        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), PstFileOptions.Default);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = pst.EnumerateNodes().ToList();
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }

    /// <summary>
    /// Verifies that enumerating a three-level node B-tree in which both entries of the root reference the same
    /// intermediate page fails with <see cref="PstFileError.InvalidPage" />: the intermediate page's keys lie below the
    /// second entry's key.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenTwoEntriesReferenceOneIntermediatePage_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildTwoLeafTrees();
        PstBref intermediate = ReadNodeTreeRoot(file);
        file = AppendNodeTreeRoot(file, level: 2, [(0x21, intermediate), (0x10000, intermediate)]);
        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), PstFileOptions.Default);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = pst.EnumerateNodes().ToList();
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }

    /// <summary>
    /// Verifies that the range a page's entry gives its child is bounded by the entry after it in an ancestor: a
    /// three-level node B-tree whose last leaf under the first intermediate page holds a key the root's second entry
    /// claims fails with <see cref="PstFileError.InvalidPage" />, although every page is ordered on its own.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenALeafKeyReachesAnAncestorsNextKey_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildTwoLeafTrees();
        PstLayout layout = PstLayout.Unicode;
        PstBref intermediate = ReadNodeTreeRoot(file);
        long secondLeaf = (long)layout.ReadId(file.AsSpan((int)intermediate.Offset + layout.BranchEntryStride + (2 * layout.IdWidth)));
        byte[] lastEntry = file.AsSpan((int)secondLeaf + layout.NbtLeafStride, layout.NbtLeafStride).ToArray();
        file = AppendPage(file, 0x20000, level: 0, layout.NbtLeafStride, [lastEntry], out PstBref copiedLeaf);
        file = AppendPage(file, 0x20004, level: 1, layout.BranchEntryStride, [BranchEntry(0x8022, copiedLeaf)], out PstBref copiedIntermediate);
        file = AppendNodeTreeRoot(file, level: 2, [(0x21, intermediate), (0x8022, copiedIntermediate)]);
        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), PstFileOptions.Default);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = pst.EnumerateNodes().ToList();
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }

    /// <summary>
    /// Verifies that a lookup descending through an entry that references another entry's leaf page fails with
    /// <see cref="PstFileError.InvalidPage" />, instead of reporting the node missing.
    /// </summary>
    [TestMethod]
    public void TryGetNode_WhenTheEntryForTheKeyReferencesAnotherLeafPage_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildTwoLeafTrees();
        RedirectBranchEntry(file, PstFixtureBuilder.ReadNodeTreeRootOffset(file), entry: 1, toEntry: 0);
        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), PstFileOptions.Default);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = pst.TryGetNode(new PstNodeId(0x122), out _);
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }

    /// <summary>
    /// Verifies that the block B-tree is held to the same ranges: reading a node whose data block is looked up through
    /// a root entry that references another entry's leaf page fails with <see cref="PstFileError.InvalidPage" />.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenTwoBlockTreeEntriesReferenceOneLeafPage_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildTwoLeafTrees();
        RedirectBranchEntry(file, PstFixtureBuilder.ReadBlockTreeRootOffset(file), entry: 1, toEntry: 0);
        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), PstFileOptions.Default);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = pst.GetNode(new PstNodeId(0x8022)).ReadAllBytes();
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }

    /// <summary>
    /// Builds a Unicode container whose node and block B-trees each hold two leaf pages of two entries under one
    /// branch page: node leaves [0x21, 0x61] and [0x122, 0x8022], block leaves [4, 8] and [12, 16].
    /// </summary>
    /// <returns>The container bytes.</returns>
    private static byte[] BuildTwoLeafTrees()
    {
        var builder = new PstFixtureBuilder { MaxEntriesPerPage = 2 };
        for (int i = 0; i < s_keyRangeNodeIds.Length; i++)
            builder.AddNode(s_keyRangeNodeIds[i], builder.AddDataBlock([(byte)i]));

        return builder.Build();
    }

    /// <summary>
    /// Reads the reference to the node B-tree root from a Unicode header.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <returns>The root page reference.</returns>
    private static PstBref ReadNodeTreeRoot(byte[] file) =>
        PstLayout.Unicode.ReadBref(file.AsSpan(PstLayout.Unicode.NbtRootOffset));

    /// <summary>
    /// Points one entry of a branch page at the child page another entry references, and repairs the page checksum.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="page">The branch page's offset.</param>
    /// <param name="entry">The entry to redirect.</param>
    /// <param name="toEntry">The entry whose child it takes.</param>
    private static void RedirectBranchEntry(byte[] file, long page, int entry, int toEntry)
    {
        PstLayout layout = PstLayout.Unicode;
        int stride = layout.BranchEntryStride;
        file.AsSpan((int)page + (toEntry * stride) + layout.IdWidth, layout.BrefSize)
            .CopyTo(file.AsSpan((int)page + (entry * stride) + layout.IdWidth));
        RepairPageChecksum(file, page);
    }

    /// <summary>
    /// Rewrites the key of one entry of a branch page, and repairs the page checksum.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="page">The branch page's offset.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="key">The key to write.</param>
    private static void SetBranchKey(byte[] file, long page, int entry, ulong key)
    {
        PstLayout.Unicode.WriteId(file.AsSpan((int)page + (entry * PstLayout.Unicode.BranchEntryStride)), key);
        RepairPageChecksum(file, page);
    }

    /// <summary>
    /// Recomputes a Unicode page's checksum after a test patches its entries.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="page">The page's offset.</param>
    private static void RepairPageChecksum(byte[] file, long page)
    {
        PstLayout layout = PstLayout.Unicode;
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan((int)page + layout.PageCrcOffset), PstCrc.Compute(file.AsSpan((int)page, layout.PageCrcLength)));
    }

    /// <summary>
    /// Lays out one branch entry: a key and the child page's reference.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="child">The child page's reference.</param>
    /// <returns>The entry bytes.</returns>
    private static byte[] BranchEntry(ulong key, PstBref child)
    {
        PstLayout layout = PstLayout.Unicode;
        var entry = new byte[layout.BranchEntryStride];
        layout.WriteId(entry, key);
        layout.WriteBref(entry.AsSpan(layout.IdWidth), child);

        return entry;
    }

    /// <summary>
    /// Appends a branch page over the given entries and makes it the node B-tree root, repairing the header checksum.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="level">The page's level.</param>
    /// <param name="entries">The entries: each key and child page reference.</param>
    /// <returns>The container bytes with the page appended.</returns>
    private static byte[] AppendNodeTreeRoot(byte[] file, byte level, (ulong Key, PstBref Child)[] entries)
    {
        PstLayout layout = PstLayout.Unicode;
        byte[] result = AppendPage(file, 0x30000, level, layout.BranchEntryStride, [.. entries.Select(e => BranchEntry(e.Key, e.Child))], out PstBref root);
        layout.WriteBref(result.AsSpan(layout.NbtRootOffset), root);
        PstFixtureBuilder.RepairHeaderChecksum(result);

        return result;
    }

    /// <summary>
    /// Appends a node B-tree page at the next 512-byte boundary, with a trailer, signature and checksum a reader at
    /// every validation level accepts.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="pageId">The page's block identifier, unused elsewhere in the file.</param>
    /// <param name="level">The page's level.</param>
    /// <param name="stride">The entry stride.</param>
    /// <param name="entries">The entries, each <paramref name="stride" /> bytes.</param>
    /// <param name="reference">When this method returns, the new page's reference.</param>
    /// <returns>The container bytes with the page appended.</returns>
    private static byte[] AppendPage(byte[] file, ulong pageId, byte level, int stride, byte[][] entries, out PstBref reference)
    {
        PstLayout layout = PstLayout.Unicode;
        long offset = (file.Length + PstLayout.PageSize - 1) & ~(long)(PstLayout.PageSize - 1);
        var result = new byte[offset + PstLayout.PageSize];
        file.CopyTo(result, 0);

        Span<byte> page = result.AsSpan((int)offset, PstLayout.PageSize);
        for (int i = 0; i < entries.Length; i++)
            entries[i].CopyTo(page[(i * stride)..]);

        page[layout.PageEntryCountOffset] = (byte)entries.Length;
        page[layout.PageEntryCapacityOffset] = (byte)(layout.PageEntryArea / stride);
        page[layout.PageEntryStrideOffset] = (byte)stride;
        page[layout.PageLevelOffset] = level;
        page[layout.PageTrailerOffset] = PstBTree.NodePageType;
        page[layout.PageTrailerOffset + 1] = PstBTree.NodePageType;
        BinaryPrimitives.WriteUInt16LittleEndian(page[layout.PageSignatureOffset..], PstSource.ComputeSignature((ulong)offset, pageId));
        layout.WriteId(page[layout.PageBlockIdOffset..], pageId);
        BinaryPrimitives.WriteUInt32LittleEndian(page[layout.PageCrcOffset..], PstCrc.Compute(page[..layout.PageCrcLength]));

        reference = new PstBref(pageId, (ulong)offset);
        return result;
    }
}
