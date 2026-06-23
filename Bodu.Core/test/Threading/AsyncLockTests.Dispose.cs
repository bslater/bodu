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
}
