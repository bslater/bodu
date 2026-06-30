// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazyTests.IsValueFactoryCompleted.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLazyTests
{
    /// <summary>
    /// Verifies that <see cref="AsyncLazy{T}.IsValueFactoryCompleted" /> reports <see langword="false" /> before the
    /// factory has been invoked.
    /// </summary>
    [TestMethod]
    public void IsValueFactoryCompleted_WhenNotStarted_ShouldReportFalse()
    {
        var sut = new AsyncLazy<int>(() => Task.FromResult(1));

        Assert.IsFalse(sut.IsValueFactoryCompleted);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncLazy{T}.IsValueFactoryCompleted" /> reports <see langword="false" /> while the
    /// factory is running and <see langword="true" /> once it completes.
    /// </summary>
    [TestMethod]
    public async Task IsValueFactoryCompleted_WhenRunningThenCompleted_ShouldReportFalseThenTrue()
    {
        var gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sut = new AsyncLazy<int>(() => gate.Task);

        Task<int> pending = sut.GetValueAsync(CancellationToken.None);
        Assert.IsFalse(sut.IsValueFactoryCompleted);

        gate.SetResult(5);
        Assert.AreEqual(5, await pending);

        Assert.IsTrue(sut.IsValueFactoryCompleted);
    }
}
