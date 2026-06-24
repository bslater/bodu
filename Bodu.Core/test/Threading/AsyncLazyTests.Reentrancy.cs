// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazyTests.Reentrancy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLazyTests
{
    /// <summary>
    /// Verifies that a factory which awaits the value of its own instance fails fast with
    /// <see cref="InvalidOperationException" /> rather than deadlocking.
    /// </summary>
    [TestMethod]
    public async Task GetAwaiter_WhenFactoryAwaitsOwnValue_ShouldThrowInvalidOperationException()
    {
        AsyncLazy<int> sut = null!;
        sut = new AsyncLazy<int>(async () => await sut.GetValueAsync(CancellationToken.None));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sut);
    }

    /// <summary>
    /// Verifies that a factory which reads <see cref="AsyncLazy{T}.Value" /> of its own instance fails fast with
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public async Task Value_WhenFactoryReadsOwnValue_ShouldThrowInvalidOperationException()
    {
        AsyncLazy<int> sut = null!;
        sut = new AsyncLazy<int>(async () => await sut.Value);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sut);
    }
}
