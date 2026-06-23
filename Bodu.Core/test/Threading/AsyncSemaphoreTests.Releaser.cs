// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSemaphoreTests.Releaser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncSemaphoreTests
{
    /// <summary>
    /// Verifies that disposing a default <see cref="AsyncSemaphore.Releaser" /> (one with no owner) is a harmless no-op.
    /// </summary>
    [TestMethod]
    public void Releaser_WhenDefault_ShouldDisposeWithoutThrowing()
    {
        var releaser = default(AsyncSemaphore.Releaser);

        releaser.Dispose();
    }

    /// <summary>
    /// Verifies that disposing the releaser returned by <see cref="AsyncSemaphore.LockAsync()" /> returns the permit.
    /// </summary>
    [TestMethod]
    public async Task Releaser_WhenDisposed_ShouldReturnPermit()
    {
        var sut = new AsyncSemaphore(1);

        var releaser = await sut.LockAsync();
        Assert.AreEqual(0, sut.CurrentCount);

        releaser.Dispose();

        Assert.AreEqual(1, sut.CurrentCount);
    }
}
