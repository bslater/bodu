// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryEnumeratorContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="EnumeratorContractTests{TEnumerable, TItem}" /> against
/// <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> with key/value pair items. Instances are sized well
/// above the seeded item count so capacity eviction never interferes with the contract's expectations.
/// </summary>
[TestClass]
public sealed class ConcurrentEvictingDictionaryEnumeratorContractTests
    : EnumeratorContractTests<ConcurrentEvictingDictionary<string, int>, KeyValuePair<string, int>>
{
    /// <inheritdoc />
    protected override ConcurrentEvictingDictionary<string, int> Create(params KeyValuePair<string, int>[] items)
    {
        ConcurrentEvictingDictionary<string, int> dictionary = new(capacity: Math.Max(64, items.Length));
        foreach (KeyValuePair<string, int> item in items)
            dictionary.Add(item.Key, item.Value);
        return dictionary;
    }

    /// <inheritdoc />
    protected override KeyValuePair<string, int> CreateItem(int index) => new($"enum-{index}", index);
}
