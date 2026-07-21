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

        ValueTask<AsyncReaderWriterLock.Releaser> reader = sut.ReaderAsync(cts.Token);

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

        ValueTask<AsyncReaderWriterLock.Releaser> writer = sut.WriterAsync(cts.Token);

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
    /// Verifies that a reader queued behind a writer is admitted when that writer cancels while other readers are
    /// still active and the last active reader then releases, rather than being stranded until a future writer cycle.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenCanceledWhileReadersActive_ShouldAdmitQueuedReadersOnLastRelease()
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

        // The reader queue cannot drain yet: a reader is still active, so the cancellation path defers the grant.
        Assert.IsFalse(deferringReader.IsCompleted);

        activeReader.Dispose();

        // Releasing the last active reader must admit the queued reader; a stranded queue would hang here.
        (await deferringReader.AsTask().WaitAsync(TimeSpan.FromSeconds(10))).Dispose();
    }
}
