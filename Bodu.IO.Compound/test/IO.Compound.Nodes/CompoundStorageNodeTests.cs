// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.IO.Compound.Nodes;

/// <summary>
/// Verifies the behavior of <see cref="CompoundStorageNode" />.
/// </summary>
[TestClass]
public class CompoundStorageNodeTests
{
    /// <summary>
    /// Verifies that adding a node that already belongs to a storage throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void AddStorage_WhenNodeAlreadyHasParent_ShouldThrowInvalidOperationException()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        CompoundStreamNode stream = root.AddStream("Stream 1", new byte[] { 1 });
        CompoundStorageNode other = CompoundStorageNode.CreateRoot();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => other.Add("Stream 1", stream));
    }

    /// <summary>
    /// Verifies that adding a second child with an existing name throws <see cref="CompoundFileSerializationException" />.
    /// </summary>
    [TestMethod]
    public void AddStream_WhenNameDuplicated_ShouldThrowCompoundFileSerializationException()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStream("Stream 1", new byte[] { 1 });

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => root.AddStream("Stream 1", new byte[] { 2 }));
    }

    /// <summary>
    /// Verifies that names are compared case-insensitively by default, so differing case collides.
    /// </summary>
    [TestMethod]
    public void AddStream_WhenNameDiffersOnlyByCase_ShouldCollideByDefault()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStream("Workbook", new byte[] { 1 });

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => root.AddStream("WORKBOOK", new byte[] { 2 }));
    }

    /// <summary>
    /// Verifies that an over-length name is rejected with <see cref="CompoundFileSerializationException" />.
    /// </summary>
    [TestMethod]
    public void AddStorage_WhenNameTooLong_ShouldThrowCompoundFileSerializationException()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => root.AddStorage(new string('x', 32)));
    }

    /// <summary>
    /// Verifies that a name containing a reserved character is rejected.
    /// </summary>
    [TestMethod]
    public void AddStorage_WhenNameContainsReservedCharacter_ShouldThrowCompoundFileSerializationException()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => root.AddStorage("a/b"));
    }

    /// <summary>
    /// Verifies that a control-prefixed name (as used by OLE property streams) is permitted.
    /// </summary>
    [TestMethod]
    public void AddStream_WhenNameIsControlPrefixed_ShouldBeAllowed()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();

        CompoundStreamNode stream = root.AddStream("SummaryInformation", new byte[] { 1 });

        Assert.IsTrue(root.ContainsName("SummaryInformation"));
        Assert.AreEqual("SummaryInformation", stream.Name);
    }

    /// <summary>
    /// Verifies that removing a child detaches it from its parent.
    /// </summary>
    [TestMethod]
    public void Remove_WhenChildExists_ShouldDetachFromParent()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        CompoundStreamNode stream = root.AddStream("Stream 1", new byte[] { 1 });

        bool removed = root.Remove("Stream 1");

        Assert.IsTrue(removed);
        Assert.IsNull(stream.Parent);
        Assert.IsFalse(root.ContainsName("Stream 1"));
    }

    /// <summary>
    /// Verifies that clearing a storage detaches every child.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldDetachAllChildren()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        CompoundStreamNode a = root.AddStream("a", new byte[] { 1 });
        CompoundStreamNode b = root.AddStream("b", new byte[] { 2 });

        root.Clear();

        Assert.AreEqual(0, root.Count);
        Assert.IsNull(a.Parent);
        Assert.IsNull(b.Parent);
    }

    /// <summary>
    /// Verifies that a removed node can be re-added to another storage (single-parent invariant released on removal).
    /// </summary>
    [TestMethod]
    public void Remove_WhenChildReadded_ShouldAllowReparenting()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        CompoundStreamNode stream = root.AddStream("Stream 1", new byte[] { 1 });
        _ = root.Remove("Stream 1");

        CompoundStorageNode other = CompoundStorageNode.CreateRoot();
        other.Add("Renamed", stream);

        Assert.AreSame(other, stream.Parent);
        Assert.AreEqual("Renamed", stream.Name);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorageNode.Rename(string, string)" /> updates the key and the node name.
    /// </summary>
    [TestMethod]
    public void Rename_WhenChildExists_ShouldUpdateNameAndKey()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        CompoundStreamNode stream = root.AddStream("Old", new byte[] { 1 });

        root.Rename("Old", "New");

        Assert.IsFalse(root.ContainsName("Old"));
        Assert.IsTrue(root.TryGetStream("New", out CompoundStreamNode? found));
        Assert.AreSame(stream, found);
        Assert.AreEqual("New", stream.Name);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorageNode.EnumerateStorages" /> and
    /// <see cref="CompoundStorageNode.EnumerateStreams" /> partition the children by kind.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenMixedChildren_ShouldPartitionByKind()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStorage("S");
        _ = root.AddStream("a", new byte[] { 1 });
        _ = root.AddStream("b", new byte[] { 2 });

        Assert.AreEqual(1, root.EnumerateStorages().Count());
        Assert.AreEqual(2, root.EnumerateStreams().Count());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundNode.DeepClone" /> produces an independent, detached subtree.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenCalled_ShouldCopySubtreeWithoutParent()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        CompoundStorageNode storage = root.AddStorage("Storage 1");
        _ = storage.AddStream("Stream 1", new byte[] { 1, 2, 3 });

        var clone = (CompoundStorageNode)root.DeepClone();

        Assert.IsNull(clone.Parent);
        Assert.IsTrue(clone.TryGetStorage("Storage 1", out CompoundStorageNode? clonedStorage));
        Assert.IsTrue(clonedStorage.TryGetStream("Stream 1", out CompoundStreamNode? clonedStream));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, clonedStream.Content.ToArray());
    }
}
