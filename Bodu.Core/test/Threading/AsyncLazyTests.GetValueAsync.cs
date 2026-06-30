// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazyTests.GetValueAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLazyTests
{
    /// <summary>
    /// Verifies that <see cref="AsyncLazy{T}.GetValueAsync(CancellationToken)" /> returns the shared value.
    /// </summary>
    [TestMethod]
    public async Task GetValueAsync_WhenAwaited_ShouldReturnFactoryResult()
    {
        var sut = new AsyncLazy<int>(() => Task.FromResult(13));

        Assert.AreEqual(13, await sut.GetValueAsync(CancellationToken.None));
    }

    /// <summary>
    /// Verifies that canceling the caller's token cancels only that caller's wait, leaving the shared factory running
    /// so that a later caller still observes the value.
    /// </summary>
    [TestMethod]
    public async Task GetValueAsync_WhenCallerTokenCanceled_ShouldCancelOnlyThisWait()
    {
        var gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sut = new AsyncLazy<int>(() => gate.Task);
        using var cts = new CancellationTokenSource();

        Task<int> caller = sut.GetValueAsync(cts.Token);
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await caller);

        // The shared factory was not canceled; completing it satisfies a second caller.
        gate.SetResult(99);
        Assert.AreEqual(99, await sut.GetValueAsync(CancellationToken.None));
    }
}
