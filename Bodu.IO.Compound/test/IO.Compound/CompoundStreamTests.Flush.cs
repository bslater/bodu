// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.Flush.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStreamTests
{
    /// <summary>
    /// Reopens the committed destination and returns the payload of the named root stream.
    /// </summary>
    /// <param name="destination">The committed destination bytes.</param>
    /// <param name="name">The stream name to read.</param>
    /// <returns>The committed payload.</returns>
    private static byte[] ReadCommitted(MemoryStream destination, string name)
    {
        using var reopened = CompoundFile.Open(new MemoryStream(destination.ToArray(), writable: false));
        using CompoundStream stream = reopened.RootStorage.OpenStream(name);
        return stream.ReadAllBytes();
    }

    /// <summary>
    /// Verifies that an explicit flush persists the written bytes into the staging tree so a commit made while the
    /// cursor is still open carries them.
    /// </summary>
    [TestMethod]
    public void Flush_WhenWritten_ShouldPersistToCommittedFile()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        using CompoundStream cursor = file.RootStorage.CreateStream("Data");
        cursor.Write([1, 2, 3]);
        cursor.Flush();
        file.Commit();

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, ReadCommitted(destination, "Data"));
    }

    /// <summary>
    /// Verifies that cursor writes made after a flush do not mutate the already-flushed staging snapshot until the
    /// next flush.
    /// </summary>
    [TestMethod]
    public void Flush_WhenWrittenAfterFlush_ShouldNotMutatePreviouslyCommittedSnapshot()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        using CompoundStream cursor = file.RootStorage.CreateStream("Data");
        cursor.Write([1, 2, 3]);
        cursor.Flush();

        cursor.Position = 0;
        cursor.Write([9]);
        file.Commit();

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, ReadCommitted(destination, "Data"));
    }

    /// <summary>
    /// Verifies that disposing after an explicit flush with no further writes preserves the flushed content
    /// without re-flushing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenFlushedWithNoFurtherWrites_ShouldPreserveFlushedContent()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        using (CompoundStream cursor = file.RootStorage.CreateStream("Data"))
        {
            cursor.Write([10, 20]);
            cursor.Flush();
        }

        file.Commit();

        CollectionAssert.AreEqual(new byte[] { 10, 20 }, ReadCommitted(destination, "Data"));
    }

    /// <summary>
    /// Verifies that writes made after a flush are persisted by the dispose-time flush.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenWrittenAfterFlush_ShouldPersistLatestBytes()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        using (CompoundStream cursor = file.RootStorage.CreateStream("Data"))
        {
            cursor.Write([1, 2, 3]);
            cursor.Flush();
            cursor.Position = 0;
            cursor.Write([9]);
        }

        file.Commit();

        CollectionAssert.AreEqual(new byte[] { 9, 2, 3 }, ReadCommitted(destination, "Data"));
    }

    /// <summary>
    /// Verifies that a truncating reopen (<see cref="FileMode.Create" /> over an existing stream) persists the
    /// truncation even when nothing is written before dispose.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenModeCreateOverExistingAndNoWrites_ShouldTruncateStream()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        file.RootStorage.CreateStream("Data", new byte[] { 1, 2, 3, 4 });
        using (file.RootStorage.OpenStream("Data", FileMode.Create, FileAccess.Write))
        {
        }

        file.Commit();

        Assert.IsEmpty(ReadCommitted(destination, "Data"));
    }
}
