// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLockTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncLock" /> type.
/// </summary>
[TestClass]
public sealed partial class AsyncLockTests
{
    /// <summary>
    /// Atomically raises <paramref name="target" /> to <paramref name="value" /> if the value is greater.
    /// </summary>
    /// <param name="target">The location to raise.</param>
    /// <param name="value">The candidate maximum.</param>
    private static void InterlockedMax(ref int target, int value)
    {
        int current = Volatile.Read(ref target);
        while (value > current)
        {
            int prior = Interlocked.CompareExchange(ref target, value, current);
            if (prior == current)
                return;

            current = prior;
        }
    }
}
