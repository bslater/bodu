// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncCountdownEventTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncCountdownEvent" /> type.
/// </summary>
[TestClass]
public sealed class AsyncCountdownEventTests
{
    /// <summary>
    /// Verifies that signaling the count down to zero releases waiters.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task Signal_WhenCountReachesZero_ShouldReleaseWaiters()
    {
        var sut = new AsyncCountdownEvent(2);
        var pending = sut.WaitAsync();

        Assert.IsFalse(sut.Signal());
        Assert.IsFalse(pending.IsCompleted);

        Assert.IsTrue(sut.Signal());
        await pending;

        Assert.IsTrue(sut.IsSet);
    }

    /// <summary>
    /// Verifies that an event constructed with a zero count starts signaled.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCountIsZero_ShouldStartSignaled()
    {
        var sut = new AsyncCountdownEvent(0);

        Assert.IsTrue(sut.IsSet);
        Assert.IsTrue(sut.WaitAsync().IsCompleted);
    }

    /// <summary>
    /// Verifies that a negative initial count throws <see cref="ArgumentOutOfRangeException" /> naming <c>initialCount</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCountIsNegative_ShouldThrowForInitialCount()
    {
        Assert.AreEqual(
            "initialCount",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AsyncCountdownEvent(-1)).ParamName);
    }

    /// <summary>
    /// Verifies that signaling more than the remaining count throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Signal_WhenSignalCountExceedsRemaining_ShouldThrowInvalidOperationException()
    {
        var sut = new AsyncCountdownEvent(1);

        Assert.ThrowsExactly<InvalidOperationException>(() => sut.Signal(2));
    }

    /// <summary>
    /// Verifies that <see cref="AsyncCountdownEvent.AddCount()" /> raises the remaining count.
    /// </summary>
    [TestMethod]
    public void AddCount_WhenNotYetSignaled_ShouldRaiseCount()
    {
        var sut = new AsyncCountdownEvent(1);

        sut.AddCount(2);

        Assert.AreEqual(3, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that adding to an already-signaled event throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void AddCount_WhenAlreadyZero_ShouldThrowInvalidOperationException()
    {
        var sut = new AsyncCountdownEvent(1);
        sut.Signal();

        Assert.ThrowsExactly<InvalidOperationException>(() => sut.AddCount());
    }

    /// <summary>
    /// Verifies that <see cref="AsyncCountdownEvent.TryAddCount()" /> returns <see langword="false" /> once the event is signaled.
    /// </summary>
    [TestMethod]
    public void TryAddCount_WhenAlreadyZero_ShouldReturnFalse()
    {
        var sut = new AsyncCountdownEvent(1);
        sut.Signal();

        Assert.IsFalse(sut.TryAddCount());
    }
}
