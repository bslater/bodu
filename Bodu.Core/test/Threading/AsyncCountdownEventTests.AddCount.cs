// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncCountdownEventTests.AddCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncCountdownEventTests
{
    /// <summary>
    /// Verifies that <see cref="AsyncCountdownEvent.AddCount()" /> raises the remaining count by one.
    /// </summary>
    [TestMethod]
    public void AddCount_WhenNotYetSignaled_ShouldRaiseCountByOne()
    {
        var sut = new AsyncCountdownEvent(1);

        sut.AddCount();

        Assert.AreEqual(2, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncCountdownEvent.AddCount(int)" /> raises the remaining count by the given amount.
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

        Assert.ThrowsExactly<InvalidOperationException>(sut.AddCount);
    }

    /// <summary>
    /// Verifies that a non-positive add count throws <see cref="ArgumentOutOfRangeException" /> naming <c>count</c>.
    /// </summary>
    [TestMethod]
    public void AddCount_WhenCountIsZero_ShouldThrowForCount()
    {
        var sut = new AsyncCountdownEvent(1);

        Assert.AreEqual(
            "count",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sut.AddCount(0)).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncCountdownEvent.TryAddCount()" /> raises the count and returns <see langword="true" />
    /// while the event is unsignaled.
    /// </summary>
    [TestMethod]
    public void TryAddCount_WhenNotYetSignaled_ShouldRaiseCountAndReturnTrue()
    {
        var sut = new AsyncCountdownEvent(1);

        Assert.IsTrue(sut.TryAddCount());
        Assert.AreEqual(2, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncCountdownEvent.TryAddCount()" /> returns <see langword="false" /> once the event
    /// is signaled.
    /// </summary>
    [TestMethod]
    public void TryAddCount_WhenAlreadyZero_ShouldReturnFalse()
    {
        var sut = new AsyncCountdownEvent(1);
        sut.Signal();

        Assert.IsFalse(sut.TryAddCount());
    }

    /// <summary>
    /// Verifies that an addition that would overflow the count throws <see cref="InvalidOperationException" /> and
    /// leaves the count unchanged.
    /// </summary>
    [TestMethod]
    public void TryAddCount_WhenWouldOverflow_ShouldThrowAndNotMutateState()
    {
        var sut = new AsyncCountdownEvent(int.MaxValue - 1);

        Assert.ThrowsExactly<InvalidOperationException>(() => sut.TryAddCount(2));
        Assert.AreEqual(int.MaxValue - 1, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncCountdownEvent.AddCount(int)" /> rejects an overflowing addition with
    /// <see cref="InvalidOperationException" /> and leaves the count unchanged.
    /// </summary>
    [TestMethod]
    public void AddCount_WhenWouldOverflow_ShouldThrowAndNotMutateState()
    {
        var sut = new AsyncCountdownEvent(int.MaxValue);

        Assert.ThrowsExactly<InvalidOperationException>(() => sut.AddCount(1));
        Assert.AreEqual(int.MaxValue, sut.CurrentCount);
    }
}
