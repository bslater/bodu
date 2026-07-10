// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheDebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="DebugViewContractTests{TCollection}" /> against <see cref="ConcurrentLruCache{TKey, TValue}" />.
/// Asserts the standard Bodu debugger-display contract — DebuggerDisplay, DebuggerTypeProxy, and an
/// instance-constructible proxy — is present and wired up correctly.
/// </summary>
[TestClass]
public sealed class ConcurrentLruCacheDebugViewContractTests
    : DebugViewContractTests<ConcurrentLruCache<string, int>>
{
    /// <inheritdoc />
    protected override ConcurrentLruCache<string, int> Create()
    {
        ConcurrentLruCache<string, int> cache = new(capacity: 16);
        cache.Add("a", 1);
        cache.Add("b", 2);
        cache.Add("c", 3);
        return cache;
    }
}
