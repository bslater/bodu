// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2MatrixPoolTests.TrimIdle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Argon2MatrixPoolTests
{
    /// <summary>
    /// Verifies that a buffer left unused for the idle timeout is freed, so a process that stops deriving gives the
    /// memory back.
    /// </summary>
    [TestMethod]
    public void TrimIdle_WhenABufferHasBeenIdleForTheTimeout_ShouldFreeIt()
    {
        var clock = new ManualTimeProvider();
        Argon2MatrixPool pool = CreatePool(clock);
        Argon2Matrix.Rent(16, pool).Dispose();

        clock.Advance(LongIdleTimeout);
        pool.TrimIdle();

        Assert.AreEqual(0, pool.RetainedCount);
    }

    /// <summary>
    /// Verifies that a buffer returned within the idle timeout is kept for reuse.
    /// </summary>
    [TestMethod]
    public void TrimIdle_WhenABufferWasReturnedRecently_ShouldKeepIt()
    {
        var clock = new ManualTimeProvider();
        Argon2MatrixPool pool = CreatePool(clock);
        Argon2Matrix.Rent(16, pool).Dispose();

        clock.Advance(LongIdleTimeout - TimeSpan.FromSeconds(1));
        pool.TrimIdle();

        Assert.AreEqual(1, pool.RetainedCount);
    }

    /// <summary>
    /// Verifies that trimming frees only the buffers that have been idle for the timeout, keeping those returned since.
    /// </summary>
    [TestMethod]
    public void TrimIdle_WhenBuffersWereReturnedAtDifferentTimes_ShouldFreeOnlyTheIdleOnes()
    {
        var clock = new ManualTimeProvider();
        Argon2MatrixPool pool = CreatePool(clock);
        Argon2Matrix older = Argon2Matrix.Rent(16, pool);
        Argon2Matrix newer = Argon2Matrix.Rent(16, pool);
        older.Dispose();

        clock.Advance(LongIdleTimeout / 2);
        newer.Dispose();
        clock.Advance(LongIdleTimeout / 2);
        pool.TrimIdle();

        Assert.AreEqual(1, pool.RetainedCount);
    }
}
