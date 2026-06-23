// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazyTests.IsValueCreated.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLazyTests
{
    /// <summary>
    /// Verifies that <see cref="AsyncLazy{T}.IsValueCreated" /> reports initialization state.
    /// </summary>
    [TestMethod]
    public async Task IsValueCreated_WhenNotYetAccessed_ShouldReportFalseThenTrue()
    {
        var sut = new AsyncLazy<int>(() => Task.FromResult(1));

        Assert.IsFalse(sut.IsValueCreated);

        _ = await sut;

        Assert.IsTrue(sut.IsValueCreated);
    }
}
