// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLockTests.Releaser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLockTests
{
    /// <summary>
    /// Verifies that disposing a default <see cref="AsyncLock.Releaser" /> (one with no owner) is a harmless no-op.
    /// </summary>
    [TestMethod]
    public void Releaser_WhenDefault_ShouldDisposeWithoutThrowing()
    {
        var releaser = default(AsyncLock.Releaser);

        releaser.Dispose();
    }

    /// <summary>
    /// Verifies that disposing the releaser returned by <see cref="AsyncLock.LockAsync()" /> releases the lock so it
    /// can be reacquired.
    /// </summary>
    [TestMethod]
    public async Task Releaser_WhenDisposed_ShouldReleaseLock()
    {
        var sut = new AsyncLock();

        var releaser = await sut.LockAsync();
        var contended = sut.LockAsync();
        Assert.IsFalse(contended.IsCompleted);

        releaser.Dispose();

        using (await contended)
        {
        }
    }
}
