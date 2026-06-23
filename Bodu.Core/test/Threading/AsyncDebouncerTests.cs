// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncDebouncer" /> type.
/// </summary>
[TestClass]
public sealed class AsyncDebouncerTests
{
    /// <summary>
    /// Verifies that a single trigger runs the callback once the quiet period elapses.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Invoke_WhenQuietPeriodElapses_ShouldRunCallbackOnce()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, time);

        sut.Invoke();
        Assert.AreEqual(0, runs);

        time.Advance(TimeSpan.FromMilliseconds(100));

        Assert.AreEqual(1, runs);
    }

    /// <summary>
    /// Verifies that a burst of triggers within the quiet period results in a single callback invocation.
    /// </summary>
    [TestMethod]
    public void Invoke_WhenTriggeredRepeatedly_ShouldCoalesceIntoOneRun()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(50));
        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(50));
        sut.Invoke();

        Assert.AreEqual(0, runs);

        time.Advance(TimeSpan.FromMilliseconds(100));

        Assert.AreEqual(1, runs);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncDebouncer.Cancel" /> discards a pending invocation.
    /// </summary>
    [TestMethod]
    public void Cancel_WhenPending_ShouldNotRunCallback()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, time);

        sut.Invoke();
        sut.Cancel();
        time.Advance(TimeSpan.FromMilliseconds(200));

        Assert.AreEqual(0, runs);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncDebouncer.FlushAsync" /> runs a pending invocation immediately.
    /// </summary>
    [TestMethod]
    public async Task FlushAsync_WhenPending_ShouldRunCallbackImmediately()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, time);

        sut.Invoke();
        await sut.FlushAsync();

        Assert.AreEqual(1, runs);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> callback throws <see cref="ArgumentNullException" /> naming <c>callback</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCallbackIsNull_ShouldThrowForCallback()
    {
        Assert.AreEqual(
            "callback",
            Assert.ThrowsExactly<ArgumentNullException>(() => new AsyncDebouncer(TimeSpan.FromSeconds(1), null!)).ParamName);
    }

    /// <summary>
    /// Verifies that triggering a disposed debouncer throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Invoke_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var sut = new AsyncDebouncer(TimeSpan.FromSeconds(1), _ => ValueTask.CompletedTask);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => sut.Invoke());
    }
}
