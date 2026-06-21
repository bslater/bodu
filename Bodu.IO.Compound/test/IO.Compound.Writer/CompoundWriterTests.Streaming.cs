// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundWriterTests.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.IO.Compound.Nodes;
using Bodu.Test;

namespace Bodu.IO.Compound.Writer;

public partial class CompoundWriterTests
{
    /// <summary>
    /// Verifies that a file-backed deferred stream is serialized by streaming from disk and round-trips byte-for-byte.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Save_WhenStreamBackedByFile_ShouldRoundTrip()
    {
        byte[] payload = CreatePayload(12 * 1024 * 1024);
        string sourcePath = Path.Combine(Path.GetTempPath(), $"bodu-src-{Guid.NewGuid():N}.bin");
        string outputPath = Path.Combine(Path.GetTempPath(), $"bodu-out-{Guid.NewGuid():N}.cfb");
        try
        {
            File.WriteAllBytes(sourcePath, payload);

            CompoundStorageNode root = CompoundStorageNode.CreateRoot();
            _ = root.AddStreamFromFile("Big", sourcePath);
            using (FileStream output = File.Create(outputPath))
                root.Save(output);

            using FileStream reopen = File.OpenRead(outputPath);
            using CompoundFile file = CompoundFile.Open(reopen);
            Assert.IsTrue(file.RootStorage.TryOpenStream("Big", out CompoundStreamEntry? entry));
            Assert.AreEqual(payload.Length, entry.Length);
            Assert.AreEqual(Hash(payload), Hash(entry.ReadAllBytes().Span));
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Verifies that a deferred source is opened exactly once and only during serialization, and round-trips.
    /// </summary>
    [TestMethod]
    public void Save_WhenStreamBackedByFactory_ShouldOpenOnceDuringSave()
    {
        byte[] payload = CreatePayload(9000);
        int opens = 0;

        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStream("Data", () =>
        {
            opens++;
            return new MemoryStream(payload, writable: false);
        }, payload.Length);

        Assert.AreEqual(0, opens, "source opened before Save");

        byte[] bytes = root.ToArray();

        Assert.AreEqual(1, opens, "source not opened exactly once");
        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes));
        Assert.IsTrue(file.RootStorage.TryOpenStream("Data", out CompoundStreamEntry? entry));
        CollectionAssert.AreEqual(payload, entry.ReadAllBytes().ToArray());
    }

    /// <summary>
    /// Verifies that streaming to a destination produces exactly the same bytes as the buffered array path, for a tree
    /// mixing in-memory and deferred streams across both versions.
    /// </summary>
    /// <param name="version">The compound-file version under test.</param>
    [TestMethod]
    [DataRow(CompoundFileVersion.V3)]
    [DataRow(CompoundFileVersion.V4)]
    public void Save_WhenMixedTree_ShouldMatchArrayPathByteForByte(CompoundFileVersion version)
    {
        var options = new CompoundWriterOptions { Version = version };

        byte[] streamed;
        using (MemoryStream destination = new())
        {
            BuildMixedTree().Save(destination, options);
            streamed = destination.ToArray();
        }

        byte[] array = BuildMixedTree().ToArray(options);

        CollectionAssert.AreEqual(array, streamed);
    }

    /// <summary>
    /// Verifies that a tree mixing in-memory, deferred, mini, and empty-deferred streams round-trips.
    /// </summary>
    [TestMethod]
    public void Save_WhenMixedDeferredAndInline_ShouldRoundTrip()
    {
        using CompoundFile file = CompoundFile.Open(new MemoryStream(BuildMixedTree().ToArray()));

        Assert.IsTrue(file.RootStorage.TryOpenStream("Mini", out CompoundStreamEntry? mini));
        Assert.AreEqual(10, mini.Length);
        Assert.IsTrue(file.RootStorage.TryOpenStream("DeferredBig", out CompoundStreamEntry? big));
        Assert.AreEqual(Hash(CreatePayload(7000)), Hash(big.ReadAllBytes().Span));
        Assert.IsTrue(file.RootStorage.TryOpenStream("InlineBig", out CompoundStreamEntry? inline));
        Assert.AreEqual(Hash(CreatePayload(6000)), Hash(inline.ReadAllBytes().Span));
        Assert.IsTrue(file.RootStorage.TryOpenStream("Empty", out CompoundStreamEntry? empty));
        Assert.AreEqual(0, empty.Length);
    }

    /// <summary>
    /// Verifies that a deferred source shorter than its declared length throws
    /// <see cref="CompoundFileSerializationException" /> during serialization.
    /// </summary>
    [TestMethod]
    public void Save_WhenDeferredSourceShorterThanDeclared_ShouldThrow()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStream("Short", () => new MemoryStream(new byte[100]), 8000);

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => root.ToArray());
    }

    /// <summary>
    /// Verifies that the imperative writer can author deferred streams from a file and a factory.
    /// </summary>
    [TestMethod]
    public void CompoundWriter_WhenDeferredStreams_ShouldRoundTrip()
    {
        byte[] payload = CreatePayload(5000);
        string sourcePath = Path.Combine(Path.GetTempPath(), $"bodu-src-{Guid.NewGuid():N}.bin");
        try
        {
            File.WriteAllBytes(sourcePath, payload);

            using MemoryStream destination = new();
            using (var writer = new CompoundWriter(destination))
            {
                writer.WriteStreamFromFile("FromFile", sourcePath);
                writer.WriteStream("FromFactory", () => new MemoryStream(payload), payload.Length);
            }

            using CompoundFile file = CompoundFile.Open(new MemoryStream(destination.ToArray()));
            Assert.IsTrue(file.RootStorage.TryOpenStream("FromFile", out CompoundStreamEntry? a));
            Assert.IsTrue(file.RootStorage.TryOpenStream("FromFactory", out CompoundStreamEntry? b));
            Assert.AreEqual(Hash(payload), Hash(a.ReadAllBytes().Span));
            Assert.AreEqual(Hash(payload), Hash(b.ReadAllBytes().Span));
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    /// <summary>
    /// Builds a tree mixing an in-memory large stream, a deferred large stream, a mini stream, and an empty deferred stream.
    /// </summary>
    /// <returns>The root storage.</returns>
    private static CompoundStorageNode BuildMixedTree()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStream("InlineBig", CreatePayload(6000));
        _ = root.AddStream("DeferredBig", () => new MemoryStream(CreatePayload(7000)), 7000);
        _ = root.AddStream("Mini", CreatePayload(10));
        _ = root.AddStream("Empty", () => new MemoryStream(Array.Empty<byte>()), 0);
        return root;
    }
}
