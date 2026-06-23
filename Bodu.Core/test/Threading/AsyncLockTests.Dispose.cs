// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLockTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLockTests
{
    /// <summary>
    /// Verifies that disposing the lock twice does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var sut = new AsyncLock();

        sut.Dispose();
        sut.Dispose();
    }

    /// <summary>
    /// Verifies that disposing the lock while a caller is waiting faults that caller with
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhenWaiterPending_ShouldFaultWaiter()
    {
        var sut = new AsyncLock();
        using var held = await sut.LockAsync();

        var pending = sut.LockAsync();
        Assert.IsFalse(pending.IsCompleted);

        sut.Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await pending);
    }

    /// <summary>
    /// Verifies that disposing the lock while it is held, then disposing the holder's releaser, does not throw.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhenHeld_ShouldAllowHolderReleaseAsNoOp()
    {
        var sut = new AsyncLock();
        var held = await sut.LockAsync();

        sut.Dispose();

        held.Dispose();
    }
}
