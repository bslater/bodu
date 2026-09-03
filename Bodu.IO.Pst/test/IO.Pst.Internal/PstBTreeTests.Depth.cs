// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTreeTests.Depth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstBTreeTests
{
    /// <summary>
    /// Builds a container whose node B-tree root is a branch page whose first child reference points back at the
    /// root itself — the cheapest crafted cycle, reachable from a single 512-byte page.
    /// </summary>
    /// <returns>The container bytes.</returns>
    private static byte[] BuildSelfReferencingNodeTree()
    {
        byte[] file = BuildMultiPageTree();
        long root = PstFixtureBuilder.ReadNodeTreeRootOffset(file);
        ulong rootBlockId = BinaryPrimitives.ReadUInt64LittleEndian(file.AsSpan((int)root + 504));

        // A branch entry is key(8) + BREF(bid 8, ib 8); redirect the first child at the root page itself.
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan((int)root + 8), rootBlockId);
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan((int)root + 16), (ulong)root);

        return file;
    }

    /// <summary>
    /// Verifies that enumerating a node B-tree whose branch page references itself fails with
    /// <see cref="PstFileFormatException" /> (<see cref="PstFileError.InvalidPage" />) instead of recursing without
    /// bound and terminating the process.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenBranchPageReferencesItself_ShouldThrowPstFileFormatException()
    {
        using PstFile file = PstFile.Open(new MemoryStream(BuildSelfReferencingNodeTree(), writable: false), PstFileOptions.Default);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = file.EnumerateNodes().ToList();
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }

    /// <summary>
    /// Verifies that a keyed lookup descending into a branch page that references itself fails with
    /// <see cref="PstFileFormatException" /> instead of recursing without bound.
    /// </summary>
    [TestMethod]
    public void TryGetNode_WhenBranchPageReferencesItself_ShouldThrowPstFileFormatException()
    {
        using PstFile file = PstFile.Open(new MemoryStream(BuildSelfReferencingNodeTree(), writable: false), PstFileOptions.Default);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = file.TryGetNode(new PstNodeId(0x21), out _);
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }
}
