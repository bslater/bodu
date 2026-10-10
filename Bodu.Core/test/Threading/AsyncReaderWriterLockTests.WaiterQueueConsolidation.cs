// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLockTests.WaiterQueueConsolidation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLockTests
{
    /// <summary>Committed writer ownership is not revoked by cancellation after handoff.</summary>
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
}
