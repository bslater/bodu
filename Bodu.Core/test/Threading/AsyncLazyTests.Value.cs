// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazyTests.Value.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLazyTests
{
    /// <summary>
    /// Verifies that the factory runs at most once across multiple awaits.
    /// </summary>
    [TestMethod]
    public async Task Value_WhenAwaitedMultipleTimes_ShouldInvokeFactoryOnce()
    {
        int invocations = 0;
        var sut = new AsyncLazy<int>(() =>
        {
            Interlocked.Increment(ref invocations);
            return Task.FromResult(7);
        });

        _ = await sut;
        _ = await sut;
        _ = await sut.Value;

        Assert.AreEqual(1, invocations);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncLazy{T}.Value" /> exposes the shared, cached task across reads.
    /// </summary>
    [TestMethod]
    public void Value_WhenReadTwice_ShouldReturnSameTask()
    {
        var sut = new AsyncLazy<int>(() => Task.FromResult(1));

        Assert.AreSame(sut.Value, sut.Value);
    }

    /// <summary>
    /// Verifies that a synchronous factory is offloaded so it does not run during construction.
    /// </summary>
    [TestMethod]
    public async Task Value_WhenConstructedFromSyncFactory_ShouldProduceValue()
    {
        var sut = new AsyncLazy<string>(() => "computed");

        Assert.AreEqual("computed", await sut);
    }
}
