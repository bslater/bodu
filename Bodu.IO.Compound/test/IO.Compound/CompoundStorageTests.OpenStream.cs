// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.OpenStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies how <see cref="CompoundStorage.OpenStream(string, FileMode, FileAccess)" /> translates the two
/// recoverable-absence results of its Try-core into exceptions.
/// </summary>
/// <remarks>
/// The Try-core reports <see langword="false" /> for both "the stream does not exist" and
/// "<see cref="FileMode.CreateNew" /> and it does" - two different failures behind one result. The throwing overload
/// has to tell them apart after the fact, so these tests pin that each maps to its own exception type rather than
/// collapsing to a single one.
/// </remarks>
public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that <see cref="FileMode.CreateNew" /> over an existing staged stream throws
    /// <see cref="IOException" /> - the BCL's signal for "the file is already there" - rather than the
    /// not-found exception.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenCreateNewAndStreamExists_ShouldThrowIOException()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);

        _ = Assert.ThrowsExactly<IOException>(() =>
        {
            using CompoundStream _ = file.RootStorage.OpenStream("Stream 1", FileMode.CreateNew, FileAccess.Write);
        });
    }

    /// <summary>
    /// Verifies that opening an absent staged stream throws <see cref="CompoundStreamNotFoundException" /> naming the
    /// stream, so the caller can report which entry was missing.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenOpenAndStreamIsAbsent_ShouldThrowStreamNotFound()
    {
        using var file = CompoundFile.Create(new MemoryStream());

        var ex = Assert.ThrowsExactly<CompoundStreamNotFoundException>(() =>
        {
            using CompoundStream _ = file.RootStorage.OpenStream("Missing", FileMode.Open, FileAccess.Write);
        });

        Assert.IsTrue(ex.Message.Contains("Missing", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a read-only request for an absent staged stream throws the not-found exception rather than the
    /// <see cref="IOException" /> reserved for the create-new collision.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenAccessIsReadAndStreamIsAbsent_ShouldThrowStreamNotFound()
    {
        using var file = CompoundFile.Create(new MemoryStream());

        _ = Assert.ThrowsExactly<CompoundStreamNotFoundException>(() =>
        {
            using CompoundStream _ = file.RootStorage.OpenStream("Missing", FileMode.Open, FileAccess.Read);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.TryOpenStorage(string, out CompoundStorage)" /> resolves a staged
    /// child storage on a writable file, and reports <see langword="false" /> for an absent one.
    /// </summary>
    [TestMethod]
    public void TryOpenStorage_WhenFileIsWritable_ShouldResolveStagedChildStorage()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        _ = file.RootStorage.CreateStorage("Storage 1");

        Assert.IsTrue(file.RootStorage.TryOpenStorage("Storage 1", out CompoundStorage? storage));
        Assert.AreEqual("Storage 1", storage!.Name);

        Assert.IsFalse(file.RootStorage.TryOpenStorage("Storage 2", out CompoundStorage? missing));
        Assert.IsNull(missing);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.EnumerateStorages" /> walks the staging tree on a writable file,
    /// returning only storages and not the sibling stream.
    /// </summary>
    [TestMethod]
    public void EnumerateStorages_WhenFileIsWritable_ShouldReturnStagedStoragesOnly()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        _ = file.RootStorage.CreateStorage("Storage 1");
        _ = file.RootStorage.CreateStorage("Storage 2");
        using (CompoundStream seed = file.RootStorage.OpenStream("Stream 1", FileMode.Create, FileAccess.Write))
            seed.Write(new byte[] { 1 });

        var names = file.RootStorage.EnumerateStorages().Select(s => s.Name).ToList();

        CollectionAssert.AreEquivalent(new[] { "Storage 1", "Storage 2" }, names);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.EnumerateStreams" /> walks the staging tree on a writable file,
    /// reporting each staged stream's length from the staging node rather than from a committed container.
    /// </summary>
    [TestMethod]
    public void EnumerateStreams_WhenFileIsWritable_ShouldReturnStagedStreamsWithLengths()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);
        _ = file.RootStorage.CreateStorage("Storage 1");

        var streams = file.RootStorage.EnumerateStreams().ToList();

        Assert.HasCount(1, streams);
        Assert.AreEqual("Stream 1", streams[0].Name);
        Assert.AreEqual(3L, streams[0].Length);
    }
}
