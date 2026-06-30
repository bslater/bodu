// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncCountdownEventTests.Signal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncCountdownEventTests
{
    /// <summary>
    /// Verifies that signaling the count down to zero releases waiters.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task Signal_WhenCountReachesZero_ShouldReleaseWaiters()
    {
        var sut = new AsyncCountdownEvent(2);
        ValueTask pending = sut.WaitAsync();

        Assert.IsFalse(sut.Signal());
        Assert.IsFalse(pending.IsCompleted);

        Assert.IsTrue(sut.Signal());
        await pending;

        Assert.IsTrue(sut.IsSet);
    }

    /// <summary>
    /// Verifies that signaling above zero returns <see langword="false" /> and leaves the event unsignaled.
    /// </summary>
    [TestMethod]
    public void Signal_WhenCountRemainsAboveZero_ShouldReturnFalse()
    {
        var sut = new AsyncCountdownEvent(3);

        Assert.IsFalse(sut.Signal());
        Assert.AreEqual(2, sut.CurrentCount);
        Assert.IsFalse(sut.IsSet);
    }

    /// <summary>
    /// Verifies that signaling exactly the remaining count in one call sets the event.
    /// </summary>
    [TestMethod]
    public void Signal_WhenSignalCountEqualsRemaining_ShouldSetEvent()
    {
        var sut = new AsyncCountdownEvent(3);

        Assert.IsTrue(sut.Signal(3));
        Assert.IsTrue(sut.IsSet);
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
    /// Verifies that a non-positive signal count throws <see cref="ArgumentOutOfRangeException" /> naming <c>signalCount</c>.
    /// </summary>
    [TestMethod]
    public void Signal_WhenSignalCountIsZero_ShouldThrowForSignalCount()
    {
        var sut = new AsyncCountdownEvent(1);

        Assert.AreEqual(
            "signalCount",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sut.Signal(0)).ParamName);
    }
}
