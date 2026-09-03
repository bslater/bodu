// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSourceTests.Cache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstSourceTests
{
    /// <summary>
    /// Verifies that a data block whose identifier collides with a cached page's cache key — the page's identifier
    /// with the top bit set — is read and validated as a block rather than served from the page cache: the two key
    /// spaces must be independent, because the identifier is read verbatim from the file.
    /// </summary>
    [TestMethod]
    public void ReadBlock_WhenBlockIdentifierCollidesWithCachedPageKey_ShouldReadTheBlock()
    {
        byte[] expected = [1, 2, 3];
        byte[] file = BuildSingleNode(out PstFixtureBuilder builder, expected, out ulong blockId);

        long nodeRoot = PstFixtureBuilder.ReadNodeTreeRootOffset(file);
        ulong pageBlockId = BinaryPrimitives.ReadUInt64LittleEndian(file.AsSpan((int)nodeRoot + 504));
        ulong collidingId = pageBlockId | (1UL << 63);

        // Rename the data block everywhere it is referenced: its block-tree entry key, the node entry's data
        // identifier, and its trailer.
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan((int)FindBlockTreeEntry(file, blockId)), collidingId);
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan((int)nodeRoot + 8), collidingId);
        long trailer = builder.BlockOffsets[blockId] + PstFixtureBuilder.BlockDiskLength(expected.Length) - 16;
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan((int)trailer + 8), collidingId);

        // The node lookup caches the node-tree root page first; the block read must not be satisfied by it.
        CollectionAssert.AreEqual(expected, ReadNodePayload(file, PstValidationLevel.Compatible));
    }
}
