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
            _ = sut.WriterAsync();
        });
    }
}
