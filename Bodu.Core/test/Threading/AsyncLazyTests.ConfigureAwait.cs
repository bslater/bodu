// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazyTests.ConfigureAwait.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLazyTests
{
    /// <summary>
    /// Verifies that awaiting through <see cref="AsyncLazy{T}.ConfigureAwait(bool)" /> with
    /// <see langword="false" /> yields the factory result.
    /// </summary>
    [TestMethod]
    public async Task ConfigureAwait_WhenContinueOnCapturedContextFalse_ShouldReturnFactoryResult()
    {
        var sut = new AsyncLazy<int>(() => Task.FromResult(99));

        Assert.AreEqual(99, await sut.ConfigureAwait(false));
    }

    /// <summary>
    /// Verifies that awaiting through <see cref="AsyncLazy{T}.ConfigureAwait(bool)" /> with
    /// <see langword="true" /> yields the factory result.
    /// </summary>
    [TestMethod]
    public async Task ConfigureAwait_WhenContinueOnCapturedContextTrue_ShouldReturnFactoryResult()
    {
        var sut = new AsyncLazy<int>(() => Task.FromResult(99));

        Assert.AreEqual(99, await sut.ConfigureAwait(true));
    }

    /// <summary>
    /// Verifies that <see cref="AsyncLazy{T}.ConfigureAwait(bool)" /> triggers initialization, surfacing a faulting
    /// factory's exception.
    /// </summary>
    [TestMethod]
    public async Task ConfigureAwait_WhenFactoryThrows_ShouldRethrow()
    {
        var sut = new AsyncLazy<int>((Func<Task<int>>)(() => throw new InvalidOperationException("boom")));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sut.ConfigureAwait(false));
    }
}
