// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.IO.Compound.Builders;

/// <summary>
/// Verifies the behavior of <see cref="CompoundStorageBuilder" />.
/// </summary>
[TestClass]
public class CompoundStorageBuilderTests
{
    /// <summary>
    /// Verifies that adding a node that already belongs to a storage throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void AddStorage_WhenNodeAlreadyHasParent_ShouldThrowInvalidOperationException()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Stream 1", new byte[] { 1 });
        CompoundStorageBuilder other = CompoundStorageBuilder.CreateRoot();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => other.Add("Stream 1", stream));
    }

    /// <summary>
    /// Verifies that adding a second child with an existing name throws <see cref="CompoundFileSerializationException" />.
    /// </summary>
    [TestMethod]
    public void AddStream_WhenNameDuplicated_ShouldThrowCompoundFileSerializationException()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Stream 1", new byte[] { 1 });

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => root.AddStream("Stream 1", new byte[] { 2 }));
    }

    /// <summary>
    /// Verifies that names are compared case-insensitively by default, so differing case collides.
    /// </summary>
    [TestMethod]
    public void AddStream_WhenNameDiffersOnlyByCase_ShouldCollideByDefault()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Workbook", new byte[] { 1 });

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => root.AddStream("WORKBOOK", new byte[] { 2 }));
    }

    /// <summary>
    /// Verifies that an over-length name is rejected with <see cref="CompoundFileSerializationException" />.
    /// </summary>
    [TestMethod]
    public void AddStorage_WhenNameTooLong_ShouldThrowCompoundFileSerializationException()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => root.AddStorage(new string('x', 32)));
    }

    /// <summary>
    /// Verifies that a name containing a reserved character is rejected.
    /// </summary>
    [TestMethod]
    public void AddStorage_WhenNameContainsReservedCharacter_ShouldThrowCompoundFileSerializationException()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => root.AddStorage("a/b"));
    }

    /// <summary>
    /// Verifies that a control-prefixed name (as used by OLE property streams) is permitted.
    /// </summary>
    [TestMethod]
    public void AddStream_WhenNameIsControlPrefixed_ShouldBeAllowed()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();

        CompoundStreamBuilder stream = root.AddStream("SummaryInformation", new byte[] { 1 });

        Assert.IsTrue(root.ContainsName("SummaryInformation"));
        Assert.AreEqual("SummaryInformation", stream.Name);
    }

    /// <summary>
    /// Verifies that removing a child detaches it from its parent.
    /// </summary>
    [TestMethod]
    public void Remove_WhenChildExists_ShouldDetachFromParent()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Stream 1", new byte[] { 1 });

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
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder a = root.AddStream("a", new byte[] { 1 });
        CompoundStreamBuilder b = root.AddStream("b", new byte[] { 2 });

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
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Stream 1", new byte[] { 1 });
        _ = root.Remove("Stream 1");

        CompoundStorageBuilder other = CompoundStorageBuilder.CreateRoot();
        other.Add("Renamed", stream);

        Assert.AreSame(other, stream.Parent);
        Assert.AreEqual("Renamed", stream.Name);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorageBuilder.Rename(string, string)" /> updates the key and the node name.
    /// </summary>
    [TestMethod]
    public void Rename_WhenChildExists_ShouldUpdateNameAndKey()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Old", new byte[] { 1 });

        root.Rename("Old", "New");

        Assert.IsFalse(root.ContainsName("Old"));
        Assert.IsTrue(root.TryGetStream("New", out CompoundStreamBuilder? found));
        Assert.AreSame(stream, found);
        Assert.AreEqual("New", stream.Name);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorageBuilder.EnumerateStorages" /> and
    /// <see cref="CompoundStorageBuilder.EnumerateStreams" /> partition the children by kind.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenMixedChildren_ShouldPartitionByKind()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStorage("S");
        _ = root.AddStream("a", new byte[] { 1 });
        _ = root.AddStream("b", new byte[] { 2 });

        Assert.HasCount(1, root.EnumerateStorages());
        Assert.HasCount(2, root.EnumerateStreams());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundEntryBuilder.DeepClone" /> produces an independent, detached subtree.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenCalled_ShouldCopySubtreeWithoutParent()
    {
        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        CompoundStorageBuilder storage = root.AddStorage("Storage 1");
        _ = storage.AddStream("Stream 1", new byte[] { 1, 2, 3 });

        var clone = (CompoundStorageBuilder)root.DeepClone();

        Assert.IsNull(clone.Parent);
        Assert.IsTrue(clone.TryGetStorage("Storage 1", out CompoundStorageBuilder? clonedStorage));
        Assert.IsTrue(clonedStorage.TryGetStream("Stream 1", out CompoundStreamBuilder? clonedStream));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, clonedStream.Content.ToArray());
    }
}
