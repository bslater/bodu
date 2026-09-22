// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheNonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="NonGenericCollectionContractTests{TCollection}" /> against
/// <see cref="ConcurrentLruCache{TKey, TValue}" />, exercising the non-generic
/// <see cref="System.Collections.ICollection" /> surface (<c>Count</c>, <c>SyncRoot</c>, <c>IsSynchronized</c>,
/// <c>CopyTo</c>). Pseudo-LRU eviction semantics are covered by <c>ConcurrentLruCacheTests</c>; concurrency stress
/// lives in <c>ConcurrentLruCacheTests._Stress.cs</c>.
/// </summary>
[TestClass]
public sealed class ConcurrentLruCacheNonGenericCollectionContractTests
    : NonGenericCollectionContractTests<ConcurrentLruCache<string, int>>
{
    /// <inheritdoc />
    /// <remarks>ConcurrentLruCache intentionally throws <see cref="NotSupportedException" /> from <c>SyncRoot</c> because it manages its own internal coordination.</remarks>
    protected override bool SyncRootSupported => false;

    /// <inheritdoc />
    protected override ConcurrentLruCache<string, int> CreateEmpty() => new(capacity: 16);

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
