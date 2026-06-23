// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLockTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncReaderWriterLock" /> type.
/// </summary>
[TestClass]
public sealed class AsyncReaderWriterLockTests
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

        var first = await sut.ReaderAsync();
        var second = sut.ReaderAsync();

        Assert.IsTrue(second.IsCompleted, "A second reader must not wait behind an active reader.");

        first.Dispose();
        (await second).Dispose();
    }

    /// <summary>
    /// Verifies that a writer waits while a reader holds the lock and proceeds once it is released.
    /// </summary>
    [TestMethod]
    public async Task WriterAsync_WhenReaderActive_ShouldWaitUntilReaderReleases()
    {
        var sut = new AsyncReaderWriterLock();
        var reader = await sut.ReaderAsync();

        var writer = sut.WriterAsync();
        Assert.IsFalse(writer.IsCompleted, "A writer must wait while a reader is active.");

        reader.Dispose();
        (await writer).Dispose();
    }

    /// <summary>
    /// Verifies that a new reader waits while a writer is active and proceeds once it is released.
    /// </summary>
    [TestMethod]
    public async Task ReaderAsync_WhenWriterActive_ShouldWaitUntilWriterReleases()
    {
        var sut = new AsyncReaderWriterLock();
        var writer = await sut.WriterAsync();

        var reader = sut.ReaderAsync();
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
        var reader = await sut.ReaderAsync();

        var queuedWriter = sut.WriterAsync();
        var laterReader = sut.ReaderAsync();

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

    /// <summary>
    /// Verifies that disposing the lock faults any waiter with <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhenWaiterPending_ShouldFaultWaiter()
    {
        var sut = new AsyncReaderWriterLock();
        using var writer = await sut.WriterAsync();

        var pending = sut.ReaderAsync();
        sut.Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await pending);
    }

    /// <summary>
    /// Verifies that no reader is ever active at the same time as a writer under contention.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public async Task ReaderWriter_WhenContended_ShouldNeverOverlap()
    {
        var sut = new AsyncReaderWriterLock();
        var readers = 0;
        var writers = 0;
        var violations = 0;

        async Task Reader()
        {
            for (var i = 0; i < 200; i++)
            {
                using (await sut.ReaderAsync())
                {
                    Interlocked.Increment(ref readers);
                    if (Volatile.Read(ref writers) != 0)
                        Interlocked.Increment(ref violations);
                    await Task.Yield();
                    Interlocked.Decrement(ref readers);
                }
            }
        }

        async Task Writer()
        {
            for (var i = 0; i < 200; i++)
            {
                using (await sut.WriterAsync())
                {
                    Interlocked.Increment(ref writers);
                    if (Volatile.Read(ref readers) != 0 || Volatile.Read(ref writers) != 1)
                        Interlocked.Increment(ref violations);
                    await Task.Yield();
                    Interlocked.Decrement(ref writers);
                }
            }
        }

        var tasks = new List<Task>();
        for (var i = 0; i < 4; i++)
            tasks.Add(Task.Run(Reader));
        for (var i = 0; i < 2; i++)
            tasks.Add(Task.Run(Writer));

        await Task.WhenAll(tasks);

        Assert.AreEqual(0, violations, "Readers and writers must never overlap.");
    }
}
