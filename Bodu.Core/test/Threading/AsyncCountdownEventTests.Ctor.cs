// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncCountdownEventTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncCountdownEventTests
{
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
    /// Verifies that an event constructed with a positive count starts unsignaled and reports that count.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    public void Ctor_WhenInitialCountIsPositive_ShouldStartUnsignaled(int initialCount)
    {
        var sut = new AsyncCountdownEvent(initialCount);

        Assert.IsFalse(sut.IsSet);
        Assert.AreEqual(initialCount, sut.CurrentCount);
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
}
