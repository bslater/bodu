// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundWriterTests.Guards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Nodes;

namespace Bodu.IO.Compound.Writer;

public partial class CompoundWriterTests
{
    /// <summary>
    /// Verifies that serializing a non-root storage throws <see cref="CompoundFileSerializationException" />.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenNodeIsNotRoot_ShouldThrowCompoundFileSerializationException()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        CompoundStorageNode storage = root.AddStorage("Storage 1");

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => storage.ToArray());
    }

    /// <summary>
    /// Verifies that exceeding the configured maximum nesting depth throws
    /// <see cref="CompoundFileSerializationException" /> during serialization.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenNestingExceedsMaxDepth_ShouldThrowCompoundFileSerializationException()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        CompoundStorageNode a = root.AddStorage("A");
        CompoundStorageNode b = a.AddStorage("B");
        _ = b.AddStorage("C");

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(
            () => root.ToArray(new CompoundWriterOptions { MaxDepth = 2 }));
    }

    /// <summary>
    /// Verifies that the imperative writer rejects closing a storage when only the root is open.
    /// </summary>
    [TestMethod]
    public void WriteEndStorage_WhenNoStorageOpen_ShouldThrowInvalidOperationException()
    {
        using MemoryStream destination = new();
        using var writer = new CompoundWriter(destination);

        _ = Assert.ThrowsExactly<InvalidOperationException>(writer.WriteEndStorage);
    }

    /// <summary>
    /// Verifies that the imperative writer produces a container the reader can open.
    /// </summary>
    [TestMethod]
    public void CompoundWriter_WhenAuthoringImperatively_ShouldProduceReadableContainer()
    {
        using MemoryStream destination = new();
        using (var writer = new CompoundWriter(destination))
        {
            writer.WriteStream("Top", new byte[] { 1, 2 });
            writer.WriteStartStorage("Storage 1");
            writer.WriteStream("Nested", new byte[] { 3, 4, 5 });
            writer.WriteEndStorage();
        }

        using CompoundFile file = CompoundFile.Open(new MemoryStream(destination.ToArray()));
        Assert.IsTrue(file.RootStorage.TryOpenStream("Top", out _));
        Assert.IsTrue(file.RootStorage.OpenStorage("Storage 1").TryOpenStream("Nested", out _));
    }
}
