// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NativeBufferPoolTests.Return.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class NativeBufferPoolTests
{
    /// <summary>
    /// Verifies that a buffer comes back from the pool all zero after a matrix that wrote every block of it is disposed,
    /// so no password-derived word outlives its derivation.
    /// </summary>
    [TestMethod]
    public void Return_WhenMatrixWroteEveryBlock_ShouldLeaveTheBufferAllZero()
    {
        NativeBufferPool pool = CreatePool();
        using (Argon2Matrix written = Argon2Matrix.Rent(16, pool))
            FillWithPattern(written);

        using Argon2Matrix reused = Argon2Matrix.Rent(16, pool);

        Assert.IsTrue(IsAllZero(reused), "A returned buffer must be cleared before it is retained.");
    }

    /// <summary>
    /// Verifies that when a smaller derivation reuses a larger buffer, the buffer is still all zero afterwards - the
    /// blocks the smaller derivation wrote are cleared, and the rest were never touched.
    /// </summary>
    [TestMethod]
    public void Return_WhenSmallerMatrixReusedALargerBuffer_ShouldLeaveTheWholeBufferAllZero()
    {
        NativeBufferPool pool = CreatePool();
        using (Argon2Matrix first = Argon2Matrix.Rent(64, pool))
            FillWithPattern(first);

        using (Argon2Matrix smaller = Argon2Matrix.Rent(16, pool))
            FillWithPattern(smaller);

        using Argon2Matrix whole = Argon2Matrix.Rent(64, pool);

        Assert.IsTrue(IsAllZero(whole), "The whole buffer must be zero after any sequence of uses.");
    }

    /// <summary>
    /// Verifies that a buffer returned to a full pool is freed rather than retained.
    /// </summary>
    [TestMethod]
    public void Return_WhenThePoolIsFull_ShouldFreeTheBuffer()
    {
        var pool = new NativeBufferPool(1, 1024 * 1024, LongIdleTimeout, TimeProvider.System);
        Argon2Matrix first = Argon2Matrix.Rent(16, pool);
        Argon2Matrix second = Argon2Matrix.Rent(16, pool);

        first.Dispose();
        second.Dispose();

        Assert.AreEqual(1, pool.RetainedCount);
    }

    /// <summary>
    /// Verifies that a buffer larger than the pool retains is freed rather than retained.
    /// </summary>
    [TestMethod]
    public void Return_WhenTheBufferExceedsTheRetentionSize_ShouldFreeIt()
    {
        var pool = new NativeBufferPool(4, 8 * BlockBytes, LongIdleTimeout, TimeProvider.System);

        Argon2Matrix.Rent(16, pool).Dispose();

        Assert.AreEqual(0, pool.RetainedCount);
    }

    /// <summary>
    /// Verifies that a pool that retains nothing - the shared pool's behavior when the
    /// <see cref="NativeBufferPool.DisableReuseSwitchName" /> switch is set - frees every buffer.
    /// </summary>
    [TestMethod]
    public void Return_WhenRetentionIsDisabled_ShouldFreeTheBuffer()
    {
        var pool = new NativeBufferPool(0, 1024 * 1024, LongIdleTimeout, TimeProvider.System);

        Argon2Matrix.Rent(16, pool).Dispose();

        Assert.AreEqual(0, pool.RetainedCount);
    }

    /// <summary>
    /// Verifies that a buffer returned while the caller has already suppressed execution-context flow is retained and
    /// starts the idle timer, rather than failing on a second suppression.
    /// </summary>
    [TestMethod]
    public void Return_WhenExecutionContextFlowIsAlreadySuppressed_ShouldStartTheIdleTimer()
    {
        var clock = new ManualTimeProvider();
        NativeBufferPool pool = CreatePool(clock);
        Argon2Matrix matrix = Argon2Matrix.Rent(16, pool);

        using (ExecutionContext.SuppressFlow())
            matrix.Dispose();

        Assert.AreEqual(1, pool.RetainedCount);
        Assert.AreEqual(1, clock.TimersCreated);
    }
}
