// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncThrottlerTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncThrottlerTests
{
    /// <summary>
    /// Verifies that a zero interval throws <see cref="ArgumentOutOfRangeException" /> naming <c>interval</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIntervalIsZero_ShouldThrowForInterval()
    {
        Assert.AreEqual(
            "interval",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AsyncThrottler(TimeSpan.Zero)).ParamName);
    }

    /// <summary>
    /// Verifies that a negative interval throws <see cref="ArgumentOutOfRangeException" /> naming <c>interval</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIntervalIsNegative_ShouldThrowForInterval()
    {
        Assert.AreEqual(
            "interval",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AsyncThrottler(TimeSpan.FromTicks(-1))).ParamName);
    }
}
