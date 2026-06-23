// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncLazy{T}" /> type.
/// </summary>
[TestClass]
public sealed class AsyncLazyTests
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
    /// Verifies that the factory runs at most once across multiple awaits.
    /// </summary>
    [TestMethod]
    public async Task Value_WhenAwaitedMultipleTimes_ShouldInvokeFactoryOnce()
    {
        var invocations = 0;
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
    /// Verifies that <see cref="AsyncLazy{T}.IsValueCreated" /> reports initialization state.
    /// </summary>
    [TestMethod]
    public async Task IsValueCreated_WhenNotYetAccessed_ShouldReportFalseThenTrue()
    {
        var sut = new AsyncLazy<int>(() => Task.FromResult(1));

        Assert.IsFalse(sut.IsValueCreated);

        _ = await sut;

        Assert.IsTrue(sut.IsValueCreated);
    }

    /// <summary>
    /// Verifies that a faulting factory caches the failure and surfaces the same exception to every awaiter.
    /// </summary>
    [TestMethod]
    public async Task GetAwaiter_WhenFactoryThrows_ShouldCacheAndRethrow()
    {
        var invocations = 0;
        var sut = new AsyncLazy<int>((Func<Task<int>>)(() =>
        {
            Interlocked.Increment(ref invocations);
            throw new InvalidOperationException("boom");
        }));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sut);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sut);

        Assert.AreEqual(1, invocations);
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

    /// <summary>
    /// Verifies that a <see langword="null" /> value factory throws <see cref="ArgumentNullException" /> naming <c>valueFactory</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueFactoryIsNull_ShouldThrowForValueFactory()
    {
        Assert.AreEqual(
            "valueFactory",
            Assert.ThrowsExactly<ArgumentNullException>(() => new AsyncLazy<int>((Func<int>)null!)).ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> task factory throws <see cref="ArgumentNullException" /> naming <c>taskFactory</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTaskFactoryIsNull_ShouldThrowForTaskFactory()
    {
        Assert.AreEqual(
            "taskFactory",
            Assert.ThrowsExactly<ArgumentNullException>(() => new AsyncLazy<int>((Func<Task<int>>)null!)).ParamName);
    }
}
