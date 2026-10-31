// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Flush.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies that <see cref="CompoundFile.Flush" /> and <see cref="CompoundFile.FlushAsync(CancellationToken)" /> are
/// true aliases for <see cref="CompoundFile.Commit" /> and
/// <see cref="CompoundFile.CommitAsync(CancellationToken)" />.
/// </summary>
/// <remarks>
/// The aliases exist so a caller holding a compound file behind a stream-shaped abstraction can use the vocabulary it
/// already has. That only holds if they write the container and clear the dirty state exactly as the members they
/// forward to, which is what these tests assert rather than merely that the call returns.
/// </remarks>
public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundFile.Flush" /> writes the staged tree to the destination and clears the dirty
    /// state, so the committed bytes reopen as a compound file carrying the staged stream.
    /// </summary>
    [TestMethod]
    public void Flush_WhenTreeIsStaged_ShouldWriteContainerAndClearDirty()
    {
        using var destination = new MemoryStream();
        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            using (CompoundStream cursor = file.RootStorage.CreateStream("Data"))
                cursor.Write(new byte[] { 1, 2, 3 });

            Assert.IsTrue(file.IsDirty);

            file.Flush();

            Assert.IsFalse(file.IsDirty);
        }

        using var reopened = CompoundFile.Open(new MemoryStream(destination.ToArray()));
        using CompoundStream committed = reopened.RootStorage.OpenStream("Data");
        Assert.AreEqual(3L, committed.Length);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.FlushAsync(CancellationToken)" /> writes the staged tree and clears the
    /// dirty state, matching its synchronous twin.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task FlushAsync_WhenTreeIsStaged_ShouldWriteContainerAndClearDirty()
    {
        using var destination = new MemoryStream();
        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            using (CompoundStream cursor = file.RootStorage.CreateStream("Data"))
                cursor.Write(new byte[] { 1, 2, 3, 4 });

            await file.FlushAsync();

            Assert.IsFalse(file.IsDirty);
        }

        using var reopened = CompoundFile.Open(new MemoryStream(destination.ToArray()));
        using CompoundStream committed = reopened.RootStorage.OpenStream("Data");
        Assert.AreEqual(4L, committed.Length);
    }

    /// <summary>
    /// Verifies that flushing a file with nothing staged since the last commit is a no-op rather than an error.
    /// </summary>
    [TestMethod]
    public void Flush_WhenNothingIsDirty_ShouldSucceed()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        file.Flush();
        long committed = destination.Length;

        file.Flush();

        Assert.AreEqual(committed, destination.Length);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.Flush" /> presents the surface of the <see cref="CompoundFile.Commit" />
    /// it forwards to when the destination throws from its flush: the <see cref="IOException" /> propagates after every
    /// sector has been written and the file stays dirty, and a retry once the destination flushes again writes the
    /// whole container a second time, flushes it, and clears the dirty state.
    /// </summary>
    [TestMethod]
    public void Flush_WhenDestinationFlushThrows_ShouldPropagateAndStayDirtyUntilRetried()
    {
        byte[] seed = CreateSeedContainer();
        byte[] full = CommitSampleEditsToMemory(seed);
        using var destination = new FaultingWriteStream(seed, long.MaxValue, throwOnFlush: true);
        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        StageSampleEdits(file);

        Assert.ThrowsExactly<IOException>(() =>
        {
            file.Flush();
        });

        Assert.IsTrue(file.IsDirty);
        Assert.AreEqual(1, destination.FlushCount);
        Assert.AreEqual(full.LongLength, destination.BytesWritten);

        destination.Disarm();
        file.Flush();

        Assert.IsFalse(file.IsDirty);
        Assert.AreEqual(2, destination.FlushCount);
        Assert.AreEqual(2 * full.LongLength, destination.BytesWritten);
        CollectionAssert.AreEqual(full, destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.FlushAsync(CancellationToken)" /> presents the surface of the
    /// <see cref="CompoundFile.CommitAsync(CancellationToken)" /> it forwards to when the destination throws from its
    /// flush: the <see cref="IOException" /> propagates after every sector has been written and the file stays dirty,
    /// and a retry once the destination flushes again writes the whole container a second time, flushes it, and clears
    /// the dirty state.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task FlushAsync_WhenDestinationFlushThrows_ShouldPropagateAndStayDirtyUntilRetried()
    {
        byte[] seed = CreateSeedContainer();
        byte[] full = CommitSampleEditsToMemory(seed);
        using var destination = new FaultingWriteStream(seed, long.MaxValue, throwOnFlush: true);
        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        StageSampleEdits(file);

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
        {
            await file.FlushAsync();
        });

        Assert.IsTrue(file.IsDirty);
        Assert.AreEqual(1, destination.FlushCount);
        Assert.AreEqual(full.LongLength, destination.BytesWritten);

        destination.Disarm();
        await file.FlushAsync();

        Assert.IsFalse(file.IsDirty);
        Assert.AreEqual(2, destination.FlushCount);
        Assert.AreEqual(2 * full.LongLength, destination.BytesWritten);
        CollectionAssert.AreEqual(full, destination.ToArray());
    }
}
