// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that a file created with <see cref="CompoundFile.Create(System.IO.Stream, bool, Bodu.IO.Compound.Builders.CompoundBuildOptions)" />
    /// reports write access and an initially clean state.
    /// </summary>
    [TestMethod]
    public void Create_WhenStreamSupplied_ShouldReportWritableAndNotDirty()
    {
        using var destination = new MemoryStream();
        using CompoundFile file = CompoundFile.Create(destination, leaveOpen: true);

        Assert.IsTrue(file.CanWrite);
        Assert.IsTrue(file.RootStorage.CanWrite);
        Assert.IsFalse(file.IsDirty);
    }

    /// <summary>
    /// Verifies that creating a stream and writing to it marks the file dirty.
    /// </summary>
    [TestMethod]
    public void CreateStream_WhenContentWritten_ShouldMarkFileDirty()
    {
        using var destination = new MemoryStream();
        using CompoundFile file = CompoundFile.Create(destination, leaveOpen: true);

        using (CompoundStream stream = file.RootStorage.CreateStream("Data"))
            stream.Write([1, 2, 3, 4], 0, 4);

        Assert.IsTrue(file.IsDirty);
    }

    /// <summary>
    /// Verifies that a writable cursor round-trips its bytes through commit and reopen.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Commit_WhenStreamsStaged_ShouldRoundTripThroughReopen()
    {
        byte[] payload = [10, 20, 30, 40, 50];
        var destination = new MemoryStream();

        using (CompoundFile file = CompoundFile.Create(destination, leaveOpen: true))
        {
            using (CompoundStream stream = file.RootStorage.CreateStream("Workbook"))
                stream.Write(payload, 0, payload.Length);

            file.Commit();
            Assert.IsFalse(file.IsDirty);
        }

        destination.Position = 0;
        using CompoundFile reopened = CompoundFile.Open(destination);
        using CompoundStream read = reopened.RootStorage.OpenStream("Workbook");

        CollectionAssert.AreEqual(payload, read.ReadAllBytes().ToArray());
    }

    /// <summary>
    /// Verifies that nested storages and streams created through the unified API survive a commit and reopen.
    /// </summary>
    [TestMethod]
    public void Commit_WhenNestedHierarchyStaged_ShouldRoundTripStructure()
    {
        var destination = new MemoryStream();

        using (CompoundFile file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.CreateStream("Root", new byte[] { 1 });
            CompoundStorage child = file.RootStorage.CreateStorage("Storage 1");
            child.CreateStream("Nested", new byte[] { 2, 3 });
            file.Commit();
        }

        destination.Position = 0;
        using CompoundFile reopened = CompoundFile.Open(destination);

        using CompoundStream root = reopened.RootStorage.OpenStream("Root");
        CollectionAssert.AreEqual(new byte[] { 1 }, root.ReadAllBytes().ToArray());

        CompoundStorage storage = reopened.RootStorage.OpenStorage("Storage 1");
        using CompoundStream nested = storage.OpenStream("Nested");
        CollectionAssert.AreEqual(new byte[] { 2, 3 }, nested.ReadAllBytes().ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.Delete(string)" /> removes a staged stream so it is absent after commit.
    /// </summary>
    [TestMethod]
    public void Delete_WhenStreamStaged_ShouldRemoveFromCommittedFile()
    {
        var destination = new MemoryStream();

        using (CompoundFile file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.CreateStream("Keep", new byte[] { 1 });
            file.RootStorage.CreateStream("Remove", new byte[] { 2 });

            Assert.IsTrue(file.RootStorage.Delete("Remove"));
            file.Commit();
        }

        destination.Position = 0;
        using CompoundFile reopened = CompoundFile.Open(destination);

        Assert.IsTrue(reopened.RootStorage.TryOpenStream("Keep", out _));
        Assert.IsFalse(reopened.RootStorage.TryOpenStream("Remove", out _));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.Rename(string, string)" /> renames a staged stream.
    /// </summary>
    [TestMethod]
    public void Rename_WhenStreamStaged_ShouldRenameInCommittedFile()
    {
        var destination = new MemoryStream();

        using (CompoundFile file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.CreateStream("OldName", new byte[] { 9 });
            file.RootStorage.Rename("OldName", "NewName");
            file.Commit();
        }

        destination.Position = 0;
        using CompoundFile reopened = CompoundFile.Open(destination);

        Assert.IsFalse(reopened.RootStorage.TryOpenStream("OldName", out _));
        using CompoundStream renamed = reopened.RootStorage.OpenStream("NewName");
        CollectionAssert.AreEqual(new byte[] { 9 }, renamed.ReadAllBytes().ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.Revert" /> discards staged edits and clears the dirty state.
    /// </summary>
    [TestMethod]
    public void Revert_WhenEditsStaged_ShouldDiscardChanges()
    {
        using var destination = new MemoryStream();
        using CompoundFile file = CompoundFile.Create(destination, leaveOpen: true);

        file.RootStorage.CreateStream("Temp", new byte[] { 1, 2 });
        Assert.IsTrue(file.IsDirty);

        file.Revert();

        Assert.IsFalse(file.IsDirty);
        Assert.IsFalse(file.RootStorage.EnumerateStreams().Any());
    }

    /// <summary>
    /// Verifies that re-opening a writable stream with <see cref="System.IO.FileMode.Open" /> and write access appends
    /// to the previously staged content.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenReopenedForWriteWithModeOpen_ShouldPreserveStagedContent()
    {
        var destination = new MemoryStream();

        using (CompoundFile file = CompoundFile.Create(destination, leaveOpen: true))
        {
            using (CompoundStream first = file.RootStorage.CreateStream("Data"))
                first.Write([1, 2], 0, 2);

            using (CompoundStream second = file.RootStorage.OpenStream("Data", FileMode.Open, FileAccess.ReadWrite))
            {
                second.Seek(0, SeekOrigin.End);
                second.Write([3, 4], 0, 2);
            }

            file.Commit();
        }

        destination.Position = 0;
        using CompoundFile reopened = CompoundFile.Open(destination);
        using CompoundStream read = reopened.RootStorage.OpenStream("Data");

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, read.ReadAllBytes().ToArray());
    }

    /// <summary>
    /// Verifies that a mutation member throws <see cref="InvalidOperationException" /> on a read-only storage.
    /// </summary>
    [TestMethod]
    public void CreateStream_WhenFileReadOnly_ShouldThrowInvalidOperationException()
    {
        using CompoundFile file = OpenSample();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = file.RootStorage.CreateStream("Nope");
        });
    }

    /// <summary>
    /// Verifies that requesting write access on a read-only storage throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenWriteAccessRequestedOnReadOnlyFile_ShouldThrowNotSupportedException()
    {
        using CompoundFile file = OpenSample();
        string name = file.RootStorage.EnumerateStreams().First().Name;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = file.RootStorage.OpenStream(name, FileMode.Create, FileAccess.Write);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.Commit" /> throws <see cref="InvalidOperationException" /> on a read-only
    /// file.
    /// </summary>
    [TestMethod]
    public void Commit_WhenFileReadOnly_ShouldThrowInvalidOperationException()
    {
        using CompoundFile file = OpenSample();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            file.Commit();
        });
    }

    /// <summary>
    /// Verifies that disposing a writable file without committing discards the staged edits, leaving the destination
    /// untouched.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenNotCommitted_ShouldNotWriteToDestination()
    {
        using var destination = new MemoryStream();

        using (CompoundFile file = CompoundFile.Create(destination, leaveOpen: true))
            file.RootStorage.CreateStream("Data", new byte[] { 1, 2, 3 });

        Assert.AreEqual(0, destination.Length);
    }
}
