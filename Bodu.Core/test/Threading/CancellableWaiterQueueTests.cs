// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellableWaiterQueueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the internal <see cref="CancellableWaiterQueue{TResult}" /> type that the asynchronous
/// primitives share for their FIFO waiters and cancellation.
/// </summary>
[TestClass]
public sealed partial class CancellableWaiterQueueTests
{
    /// <summary>The time a test waits for an asynchronous completion before failing.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
