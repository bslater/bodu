// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheReadOnlyCollectionContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="ReadOnlyCollectionContractTests{TCollection, TItem}" /> against
/// <see cref="ConcurrentLruCache{TKey, TValue}" />, exercising the <see cref="IReadOnlyCollection{T}" /> surface over
/// key/value pair items. Instances are sized well above the seeded item count so capacity eviction never interferes
/// with the contract's expectations.
/// </summary>
[TestClass]
public sealed class ConcurrentLruCacheReadOnlyCollectionContractTests
    : ReadOnlyCollectionContractTests<ConcurrentLruCache<string, int>, KeyValuePair<string, int>>
{
    /// <inheritdoc />
    protected override ConcurrentLruCache<string, int> CreateEmpty() => new(capacity: 64);

    /// <inheritdoc />
    protected override ConcurrentLruCache<string, int> Create(params KeyValuePair<string, int>[] items)
    {
        ConcurrentLruCache<string, int> cache = new(capacity: Math.Max(64, items.Length * 3));
        foreach (KeyValuePair<string, int> item in items)
            cache.Add(item.Key, item.Value);
        return cache;
    }

    /// <inheritdoc />
    protected override KeyValuePair<string, int> CreateItem(int index) => new($"item-{index}", index);
}
