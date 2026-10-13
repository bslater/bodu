// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLockTests.WriterAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLockTests
{
    /// <summary>
    /// Verifies that a writer waits while a reader holds the lock and proceeds once it is released.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenReaderActive_ShouldWaitUntilReaderReleases()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser reader = await sut.ReaderAsync();

        ValueTask<AsyncReaderWriterLock.Releaser> writer = sut.WriterAsync();
        Assert.IsFalse(writer.IsCompleted, "A writer must wait while a reader is active.");

        reader.Dispose();
        (await writer).Dispose();
    }

    /// <summary>
    /// Verifies that a writer waits while another writer holds the lock and proceeds once it is released.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenWriterActive_ShouldWaitUntilWriterReleases()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser first = await sut.WriterAsync();

        ValueTask<AsyncReaderWriterLock.Releaser> second = sut.WriterAsync();
        Assert.IsFalse(second.IsCompleted, "A writer must wait while another writer is active.");

        first.Dispose();
        (await second).Dispose();
    }

    /// <summary>
    /// Verifies that the lock admits all waiting readers together once a writer releases and no writer is queued.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenReleasedWithReadersQueued_ShouldAdmitAllReaders()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();

        ValueTask<AsyncReaderWriterLock.Releaser> firstReader = sut.ReaderAsync();
        ValueTask<AsyncReaderWriterLock.Releaser> secondReader = sut.ReaderAsync();
        Assert.IsFalse(firstReader.IsCompleted);
        Assert.IsFalse(secondReader.IsCompleted);

        writer.Dispose();

        (await firstReader).Dispose();
        (await secondReader).Dispose();
    }

    /// <summary>
    /// Verifies that acquiring write access on a disposed lock throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void WriterAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var sut = new AsyncReaderWriterLock();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.WriterAsync().AsTask();
        });
    }

    /// <summary>
    /// Verifies that a writer granted the lock before its token is canceled keeps it, and that a reader acquires the
    /// lock once the writer releases it.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenGrantPrecedesCancellation_ShouldReturnWriter()
    {
        var sut = new AsyncReaderWriterLock();
        using var cts = new CancellationTokenSource();
        AsyncReaderWriterLock.Releaser active = await sut.WriterAsync();
        ValueTask<AsyncReaderWriterLock.Releaser> waiting = sut.WriterAsync(cts.Token);

        active.Dispose();
        cts.Cancel();

        (await waiting).Dispose();
        Assert.AreEqual(0, sut.WaitingWriterCount);
        using (await sut.ReaderAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that a write acquisition that must queue, with a token that cannot be canceled, allocates no more than
    /// its queued waiter: no cancellation registration, no cancellation callback and no asynchronous state machine.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenQueuedWithoutCancellationToken_ShouldAllocateOnlyTheWaiter()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser holder = await sut.WriterAsync();

        (long bytes, Task<AsyncReaderWriterLock.Releaser>[] pending) = ContendedWaitAllocation.BytesPerQueuedCall(() => sut.WriterAsync().AsTask());

        holder.Dispose();
        foreach (Task<AsyncReaderWriterLock.Releaser> waiter in pending)
            (await waiter.WaitAsync(TimeSpan.FromSeconds(30))).Dispose();

        Assert.IsLessThanOrEqualTo(ContendedWaitAllocation.QueuedWaiterBytes, bytes, $"A queued write acquisition allocated {bytes} bytes.");
    }

    /// <summary>
    /// Verifies that a write acquisition that must queue with a cancellable token does not allocate a new delegate for
    /// the lock's after-cancellation callback on every call.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenQueuedWithCancellationToken_ShouldNotAllocateACancellationCallback()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser holder = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();

        (long bytes, Task<AsyncReaderWriterLock.Releaser>[] pending) = ContendedWaitAllocation.BytesPerQueuedCall(() => sut.WriterAsync(cts.Token).AsTask());

        await cts.CancelAsync();
        holder.Dispose();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(30)));
        Assert.IsLessThanOrEqualTo(ContendedWaitAllocation.CancellableQueuedWriterBytes, bytes, $"A queued write acquisition allocated {bytes} bytes.");
    }
}
