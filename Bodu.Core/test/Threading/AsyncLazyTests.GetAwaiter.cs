// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazyTests.GetAwaiter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLazyTests
{
    /// <summary>
    /// Verifies that awaiting the instance yields the value produced by the asynchronous factory.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task GetAwaiter_WhenAwaited_ShouldReturnFactoryResult()
    {
        var sut = new AsyncLazy<int>(() => Task.FromResult(42));

        Assert.AreEqual(42, await sut);
    }

    /// <summary>
    /// Verifies that a faulting factory caches the failure and surfaces the same exception to every awaiter.
    /// </summary>
    [TestMethod]
    public async Task GetAwaiter_WhenFactoryThrows_ShouldCacheAndRethrow()
    {
        int invocations = 0;
        var sut = new AsyncLazy<int>((Func<Task<int>>)(() =>
        {
            Interlocked.Increment(ref invocations);
            throw new InvalidOperationException("boom");
        }));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sut);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sut);

        Assert.AreEqual(1, invocations);
    }
}
