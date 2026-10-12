// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.CreateStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundStorage.CreateStream(string, ReadOnlyMemory{byte})" /> throws
    /// <see cref="ArgumentException" /> for a name a child of the storage already has, compared case-insensitively, and
    /// leaves that child as it was.
    /// </summary>
    /// <param name="existing">The name the storage's child already has.</param>
    /// <param name="requested">The name passed to the call.</param>
    /// <param name="existingIsStorage">Whether the existing child is a storage rather than a stream.</param>
    [TestMethod]
    [DataRow("A", "A", false)]
    [DataRow("A", "a", false)]
    [DataRow("S", "S", true)]
    public void CreateStream_WhenNameIsTaken_ShouldThrowArgumentException(string existing, string requested, bool existingIsStorage)
    {
        using CompoundFile file = CreateWritableWithChild(existing, existingIsStorage);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            file.RootStorage.CreateStream(requested, new byte[20]);
        });

        Assert.AreEqual("name", ex.ParamName);
        AssertChildUnchanged(file, existing, existingIsStorage);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.CreateStream(string)" /> throws <see cref="ArgumentException" /> for a
    /// name a child of the storage already has, compared case-insensitively, instead of truncating an existing stream.
    /// </summary>
    /// <param name="existing">The name the storage's child already has.</param>
    /// <param name="requested">The name passed to the call.</param>
    /// <param name="existingIsStorage">Whether the existing child is a storage rather than a stream.</param>
    [TestMethod]
    [DataRow("A", "A", false)]
    [DataRow("A", "a", false)]
    [DataRow("S", "S", true)]
    public void CreateStream_WhenCursorNameIsTaken_ShouldThrowArgumentException(string existing, string requested, bool existingIsStorage)
    {
        using CompoundFile file = CreateWritableWithChild(existing, existingIsStorage);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using CompoundStream cursor = file.RootStorage.CreateStream(requested);
        });

        Assert.AreEqual("name", ex.ParamName);
        AssertChildUnchanged(file, existing, existingIsStorage);
    }

    /// <summary>
    /// Verifies that a file opened for update rejects a duplicate stream name at the call, as a created file does.
    /// </summary>
    [TestMethod]
    public void CreateStream_WhenNameIsTakenInUpdateMode_ShouldThrowArgumentException()
    {
        var destination = new MemoryStream();
        using (CompoundFile seed = CreateWritableWithChild("A", existingIsStorage: false, destination))
            seed.Commit();

        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            file.RootStorage.CreateStream("A", new byte[20]);
        });
    }

    /// <summary>
    /// Creates a writable file whose root holds one child, a stream of the bytes 1, 2 and 3 or an empty storage.
    /// </summary>
    /// <param name="name">The name of the child.</param>
    /// <param name="existingIsStorage">Whether the child is a storage rather than a stream.</param>
    /// <param name="destination">The destination of the file, or <see langword="null" /> for a new one.</param>
    /// <returns>The open writable file; dispose it when finished.</returns>
    private static CompoundFile CreateWritableWithChild(string name, bool existingIsStorage, MemoryStream? destination = null)
    {
        var file = CompoundFile.Create(destination ?? new MemoryStream(), leaveOpen: destination is not null);
        if (existingIsStorage)
            _ = file.RootStorage.CreateStorage(name);
        else
            file.RootStorage.CreateStream(name, new byte[] { 1, 2, 3 });

        return file;
    }

    /// <summary>
    /// Asserts that the child a duplicate call named is still the only child, with its original kind and content.
    /// </summary>
    /// <param name="file">The writable file.</param>
    /// <param name="name">The name of the child.</param>
    /// <param name="isStorage">Whether the child is a storage rather than a stream.</param>
    private static void AssertChildUnchanged(CompoundFile file, string name, bool isStorage)
    {
        CompoundEntryInfo child = file.RootStorage.EnumerateEntries().Single();
        Assert.AreEqual(name, child.Name);
        Assert.AreEqual(isStorage ? CompoundEntryType.Storage : CompoundEntryType.Stream, child.EntryType);
        if (!isStorage)
        {
            using CompoundStream stream = file.RootStorage.OpenStream(name);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, stream.ReadAllBytes());
        }
    }
}
