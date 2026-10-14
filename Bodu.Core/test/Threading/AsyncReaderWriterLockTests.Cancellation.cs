// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLockTests.Cancellation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLockTests
{
    /// <summary>
    /// Verifies that immediately available read access is granted even when the token is already canceled, because
    /// success wins when the acquisition can complete immediately.
    /// </summary>
    [TestMethod]
    public void ReaderAsync_WhenTokenAlreadyCanceledAndFree_ShouldGrantAccess()
    {
        var sut = new AsyncReaderWriterLock();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Task<AsyncReaderWriterLock.Releaser> reader = sut.ReaderAsync(cts.Token).AsTask();

        Assert.IsTrue(reader.IsCompletedSuccessfully);
        reader.Result.Dispose();
    }

    /// <summary>
    /// Verifies that read access requested with an already-canceled token cancels synchronously when a writer is
    /// active, and that the lock remains usable afterward.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenTokenAlreadyCanceledAndWriterActive_ShouldCancelSynchronously()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ValueTask<AsyncReaderWriterLock.Releaser> reader = sut.ReaderAsync(cts.Token);

        Assert.IsTrue(reader.IsCanceled);
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await reader);

        writer.Dispose();
        using (await sut.ReaderAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that immediately available write access is granted even when the token is already canceled, because
    /// success wins when the acquisition can complete immediately.
    /// </summary>
    [TestMethod]
    public void WriterAsync_WhenTokenAlreadyCanceledAndFree_ShouldGrantAccess()
    {
        var sut = new AsyncReaderWriterLock();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Task<AsyncReaderWriterLock.Releaser> writer = sut.WriterAsync(cts.Token).AsTask();

        Assert.IsTrue(writer.IsCompletedSuccessfully);
        writer.Result.Dispose();
    }

    /// <summary>
    /// Verifies that write access requested with an already-canceled token cancels synchronously when a writer is
    /// active, and that the lock remains usable afterward.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenTokenAlreadyCanceledAndWriterActive_ShouldCancelSynchronously()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser active = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ValueTask<AsyncReaderWriterLock.Releaser> writer = sut.WriterAsync(cts.Token);

        Assert.IsTrue(writer.IsCanceled);
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await writer);

        active.Dispose();
        using (await sut.WriterAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that canceling a reader queued behind an active writer cancels only that reader and frees the queue,
    /// so a later reader still acquires once the writer releases.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenCanceledWhileWaiting_ShouldCancelOnlyThatReader()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();

        ValueTask<AsyncReaderWriterLock.Releaser> canceledReader = sut.ReaderAsync(cts.Token);
        Assert.IsFalse(canceledReader.IsCompleted);

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceledReader);

        writer.Dispose();

        using (await sut.ReaderAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that canceling a writer queued behind an active writer cancels only that writer, so the next writer is
    /// granted once the active writer releases.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenCanceledWhileWaiting_ShouldCancelOnlyThatWriter()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser activeWriter = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();

        ValueTask<AsyncReaderWriterLock.Releaser> canceledWriter = sut.WriterAsync(cts.Token);
        ValueTask<AsyncReaderWriterLock.Releaser> liveWriter = sut.WriterAsync();
        Assert.IsFalse(canceledWriter.IsCompleted);
        Assert.IsFalse(liveWriter.IsCompleted);

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceledWriter);

        activeWriter.Dispose();

        // The live writer is granted, proving the canceled writer was skipped.
        (await liveWriter).Dispose();
    }

    /// <summary>
    /// Verifies that canceling a queued writer releases the readers that were deferring to it once the active writer
    /// releases.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenCanceled_ShouldReleaseDeferringReaders()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser activeWriter = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();

        ValueTask<AsyncReaderWriterLock.Releaser> canceledWriter = sut.WriterAsync(cts.Token);
        ValueTask<AsyncReaderWriterLock.Releaser> deferringReader = sut.ReaderAsync();
        Assert.IsFalse(deferringReader.IsCompleted);

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceledWriter);

        activeWriter.Dispose();

        // With no writer queued, the deferring reader is now admitted.
        (await deferringReader).Dispose();
    }

    /// <summary>
    /// Verifies that a reader queued behind a writer is admitted, alongside the reader still active, when that writer
    /// cancels, rather than being stranded until the active reader releases or a future writer cycle.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenCanceledWhileReadersActive_ShouldAdmitQueuedReadersAlongsideActiveReaders()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser activeReader = await sut.ReaderAsync();
        using var cts = new CancellationTokenSource();

        ValueTask<AsyncReaderWriterLock.Releaser> canceledWriter = sut.WriterAsync(cts.Token);
        ValueTask<AsyncReaderWriterLock.Releaser> deferringReader = sut.ReaderAsync();
        Assert.IsFalse(canceledWriter.IsCompleted);
        Assert.IsFalse(deferringReader.IsCompleted);

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceledWriter);

        // With no writer left, the queued reader shares the lock with the active one; a stranded queue would hang here.
        (await deferringReader.AsTask().WaitAsync(TimeSpan.FromSeconds(10))).Dispose();

        activeReader.Dispose();
    }

    /// <summary>
    /// Verifies that when the only queued writer cancels while a reader is active, the reader queued behind it is
    /// admitted at once, so readers that arrive later never overtake it.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenCanceledWhileReadersActive_ShouldAdmitQueuedReadersBeforeLaterReaders()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser activeReader = await sut.ReaderAsync();
        using var cts = new CancellationTokenSource();

        ValueTask<AsyncReaderWriterLock.Releaser> canceledWriter = sut.WriterAsync(cts.Token);
        ValueTask<AsyncReaderWriterLock.Releaser> queuedReader = sut.ReaderAsync();
        Assert.IsFalse(queuedReader.IsCompleted);

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceledWriter);

        // Two later readers, overlapping, are admitted immediately because no writer is active or queued.
        AsyncReaderWriterLock.Releaser laterReader1 = await sut.ReaderAsync();
        AsyncReaderWriterLock.Releaser laterReader2 = await sut.ReaderAsync();

        // The reader that queued first must not still be waiting behind them.
        Assert.AreEqual(0, sut.WaitingReaderCount);
        (await queuedReader.AsTask().WaitAsync(TimeSpan.FromSeconds(10))).Dispose();

        laterReader2.Dispose();
        laterReader1.Dispose();
        activeReader.Dispose();
    }

    /// <summary>
    /// Verifies that canceling one token shared by many readers queued behind a writer cancels every one of them,
    /// empties the reader queue, and leaves the lock usable.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenQueuedReadersShareACanceledToken_ShouldCancelThemAll()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();

        var readers = new Task<AsyncReaderWriterLock.Releaser>[50];
        for (int i = 0; i < readers.Length; i++)
            readers[i] = sut.ReaderAsync(cts.Token).AsTask();

        Assert.AreEqual(readers.Length, sut.WaitingReaderCount);

        cts.Cancel();

        foreach (Task<AsyncReaderWriterLock.Releaser> reader in readers)
            await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await reader);

        Assert.AreEqual(0, sut.WaitingReaderCount);

        writer.Dispose();
        ValueTask<AsyncReaderWriterLock.Releaser> next = sut.ReaderAsync();
        Assert.IsTrue(next.IsCompletedSuccessfully);
        (await next).Dispose();
    }

    /// <summary>
    /// Verifies that when one of several readers queued behind a writer cancels, the others stay queued, in order, and
    /// are all admitted together when the writer releases.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenOneOfSeveralQueuedReadersCancels_ShouldAdmitTheOthersWhenTheWriterReleases()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();

        ValueTask<AsyncReaderWriterLock.Releaser> first = sut.ReaderAsync();
        ValueTask<AsyncReaderWriterLock.Releaser> canceled = sut.ReaderAsync(cts.Token);
        ValueTask<AsyncReaderWriterLock.Releaser> last = sut.ReaderAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);
        Assert.AreEqual(2, sut.WaitingReaderCount);

        writer.Dispose();

        AsyncReaderWriterLock.Releaser firstReader = await first.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        AsyncReaderWriterLock.Releaser lastReader = await last.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(0, sut.WaitingReaderCount);

        // Both readers hold the lock together, so a writer must wait for both.
        ValueTask<AsyncReaderWriterLock.Releaser> nextWriter = sut.WriterAsync();
        firstReader.Dispose();
        Assert.IsFalse(nextWriter.IsCompleted);
        lastReader.Dispose();
        (await nextWriter.AsTask().WaitAsync(TimeSpan.FromSeconds(10))).Dispose();
    }

    /// <summary>
    /// Verifies that a reader granted the lock before its token is canceled keeps it, and that a writer acquires the
    /// lock once the reader releases it.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenGrantPrecedesCancellation_ShouldReturnReader()
    {
        var sut = new AsyncReaderWriterLock();
        using var cts = new CancellationTokenSource();
        AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();
        ValueTask<AsyncReaderWriterLock.Releaser> waiting = sut.ReaderAsync(cts.Token);

        writer.Dispose();
        cts.Cancel();

        (await waiting).Dispose();
        Assert.AreEqual(0, sut.WaitingReaderCount);
        using (await sut.WriterAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that a reader queued with a non-cancelable token behind a writer is admitted when the writer releases.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenQueuedWithoutCancellationToken_ShouldBeAdmittedWhenTheWriterReleases()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();

        ValueTask<AsyncReaderWriterLock.Releaser> reader = sut.ReaderAsync(CancellationToken.None);
        Assert.AreEqual(1, sut.WaitingReaderCount);

        writer.Dispose();

        (await reader.AsTask().WaitAsync(TimeSpan.FromSeconds(10))).Dispose();
        Assert.AreEqual(0, sut.WaitingReaderCount);
    }
}
