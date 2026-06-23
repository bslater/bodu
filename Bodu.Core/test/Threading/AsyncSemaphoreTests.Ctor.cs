// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSemaphoreTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncSemaphoreTests
{
    /// <summary>
    /// Verifies that a negative initial count throws <see cref="ArgumentOutOfRangeException" /> naming <c>initialCount</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCountIsNegative_ShouldThrowForInitialCount()
    {
        Assert.AreEqual(
            "initialCount",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AsyncSemaphore(-1)).ParamName);
    }

    /// <summary>
    /// Verifies that an initial count greater than the maximum throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCountExceedsMax_ShouldThrowForInitialCount()
    {
        Assert.AreEqual(
            "initialCount",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AsyncSemaphore(5, 2)).ParamName);
    }

    /// <summary>
    /// Verifies that a maximum count below one throws <see cref="ArgumentOutOfRangeException" /> naming <c>maxCount</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMaxCountIsZero_ShouldThrowForMaxCount()
    {
        Assert.AreEqual(
            "maxCount",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AsyncSemaphore(0, 0)).ParamName);
    }

    /// <summary>
    /// Verifies that the initial permit count is reported by <see cref="AsyncSemaphore.CurrentCount" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(5)]
    public void Ctor_WhenInitialCountProvided_ShouldReportCurrentCount(int initialCount)
    {
        var sut = new AsyncSemaphore(initialCount);

        Assert.AreEqual(initialCount, sut.CurrentCount);
    }
}
