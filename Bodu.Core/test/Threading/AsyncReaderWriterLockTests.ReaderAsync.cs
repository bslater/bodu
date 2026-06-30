// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLockTests.ReaderAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLockTests
{
    /// <summary>
    /// Verifies that read and write access can be acquired and released in sequence.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task ReaderAndWriter_WhenUncontended_ShouldAcquireAndRelease()
    {
        var sut = new AsyncReaderWriterLock();

        using (await sut.ReaderAsync())
        {
        }

        using (await sut.WriterAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that multiple readers may hold the lock concurrently.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenMultipleReaders_ShouldAllowConcurrentAccess()
    {
        var sut = new AsyncReaderWriterLock();

        AsyncReaderWriterLock.Releaser first = await sut.ReaderAsync();
        ValueTask<AsyncReaderWriterLock.Releaser> second = sut.ReaderAsync();

        Assert.IsTrue(second.IsCompleted, "A second reader must not wait behind an active reader.");

        first.Dispose();
        (await second).Dispose();
    }

    /// <summary>
    /// Verifies that a new reader waits while a writer is active and proceeds once it is released.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenWriterActive_ShouldWaitUntilWriterReleases()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();

        ValueTask<AsyncReaderWriterLock.Releaser> reader = sut.ReaderAsync();
        Assert.IsFalse(reader.IsCompleted, "A reader must wait while a writer is active.");

        writer.Dispose();
        (await reader).Dispose();
    }

    /// <summary>
    /// Verifies that a reader arriving while a writer is queued waits, demonstrating writer preference.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenWriterQueued_ShouldWaitForWriterPreference()
    {
        var sut = new AsyncReaderWriterLock();
        AsyncReaderWriterLock.Releaser reader = await sut.ReaderAsync();

        ValueTask<AsyncReaderWriterLock.Releaser> queuedWriter = sut.WriterAsync();
        ValueTask<AsyncReaderWriterLock.Releaser> laterReader = sut.ReaderAsync();

        Assert.IsFalse(laterReader.IsCompleted, "A reader must defer to a queued writer.");

        reader.Dispose();
        (await queuedWriter).Dispose();
        (await laterReader).Dispose();
    }

    /// <summary>
    /// Verifies that acquiring read access on a disposed lock throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void ReaderAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var sut = new AsyncReaderWriterLock();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.ReaderAsync();
        });
    }
}
