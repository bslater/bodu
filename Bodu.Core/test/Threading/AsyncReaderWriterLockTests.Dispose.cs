// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLockTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLockTests
{
    /// <summary>
    /// Verifies that disposing the lock faults a pending reader with <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhenWaiterPending_ShouldFaultWaiter()
    {
        var sut = new AsyncReaderWriterLock();
        using AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();

        ValueTask<AsyncReaderWriterLock.Releaser> pending = sut.ReaderAsync();
        sut.Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await pending);
    }

    /// <summary>
    /// Verifies that disposing the lock faults every pending reader and writer with <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhenReaderAndWriterPending_ShouldFaultBoth()
    {
        var sut = new AsyncReaderWriterLock();
        using AsyncReaderWriterLock.Releaser writer = await sut.WriterAsync();

        ValueTask<AsyncReaderWriterLock.Releaser> pendingReader = sut.ReaderAsync();
        ValueTask<AsyncReaderWriterLock.Releaser> pendingWriter = sut.WriterAsync();

        sut.Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await pendingReader);
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await pendingWriter);
    }

    /// <summary>
    /// Verifies that disposing the lock twice does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var sut = new AsyncReaderWriterLock();

        sut.Dispose();
        sut.Dispose();
    }
}
