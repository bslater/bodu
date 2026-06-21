// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.IO.Compound.Nodes;

/// <summary>
/// Verifies the shared behavior of the mutable compound-file node model.
/// </summary>
[TestClass]
public class CompoundNodeTests
{
    /// <summary>
    /// Verifies that a small tree can be authored and reports the expected entry types and parent links.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Build_WhenAuthoringTree_ShouldReportTypesAndParents()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        CompoundStorageNode storage = root.AddStorage("Storage 1");
        CompoundStreamNode stream = storage.AddStream("Stream 1", new byte[] { 1, 2, 3 });

        Assert.AreEqual(CompoundEntryType.RootStorage, root.EntryType);
        Assert.AreEqual(CompoundEntryType.Storage, storage.EntryType);
        Assert.AreEqual(CompoundEntryType.Stream, stream.EntryType);
        Assert.AreSame(root, storage.Parent);
        Assert.AreSame(root, stream.Root);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundNode.AsStorage" /> returns the storage and <see cref="CompoundNode.AsStream" />
    /// throws for a storage node.
    /// </summary>
    [TestMethod]
    public void AsStorage_WhenNodeIsStorage_ShouldReturnStorageAndRejectStreamCast()
    {
        CompoundNode node = CompoundStorageNode.CreateRoot();

        Assert.IsNotNull(node.AsStorage());
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => node.AsStream());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundNode.AsStream" /> returns the stream and <see cref="CompoundNode.AsStorage" />
    /// throws for a stream node.
    /// </summary>
    [TestMethod]
    public void AsStream_WhenNodeIsStream_ShouldReturnStreamAndRejectStorageCast()
    {
        CompoundNode node = CompoundStreamNode.Create("Stream 1", new byte[] { 1 });

        Assert.IsNotNull(node.AsStream());
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => node.AsStorage());
    }
}
