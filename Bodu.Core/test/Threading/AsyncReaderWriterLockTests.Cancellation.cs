// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLockTests.Cancellation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLockTests
{
    /// <summary>
    /// Verifies that requesting read access with an already-canceled token cancels synchronously and leaves the lock
    /// free for the next caller.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenTokenAlreadyCanceled_ShouldCancelSynchronously()
    {
        var sut = new AsyncReaderWriterLock();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var reader = sut.ReaderAsync(cts.Token);

        Assert.IsTrue(reader.IsCanceled);
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await reader);

        using (await sut.ReaderAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that requesting write access with an already-canceled token cancels synchronously and leaves the lock
    /// free for the next caller.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenTokenAlreadyCanceled_ShouldCancelSynchronously()
    {
        var sut = new AsyncReaderWriterLock();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var writer = sut.WriterAsync(cts.Token);

        Assert.IsTrue(writer.IsCanceled);
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await writer);

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
        var writer = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();

        var canceledReader = sut.ReaderAsync(cts.Token);
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
        var activeWriter = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();

        var canceledWriter = sut.WriterAsync(cts.Token);
        var liveWriter = sut.WriterAsync();
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
        var activeWriter = await sut.WriterAsync();
        using var cts = new CancellationTokenSource();

        var canceledWriter = sut.WriterAsync(cts.Token);
        var deferringReader = sut.ReaderAsync();
        Assert.IsFalse(deferringReader.IsCompleted);

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceledWriter);

        activeWriter.Dispose();

        // With no writer queued, the deferring reader is now admitted.
        (await deferringReader).Dispose();
    }
}
