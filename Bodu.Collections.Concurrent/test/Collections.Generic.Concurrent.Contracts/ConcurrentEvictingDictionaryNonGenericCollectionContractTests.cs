// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryNonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="NonGenericCollectionContractTests{TCollection}" /> against
/// <see cref="ConcurrentEvictingDictionary{TKey, TValue}" />, exercising the non-generic
/// <see cref="System.Collections.ICollection" /> surface (<c>Count</c>, <c>SyncRoot</c>, <c>IsSynchronized</c>,
/// <c>CopyTo</c>). Eviction semantics are covered by <c>ConcurrentEvictingDictionaryTests</c>; concurrency stress
/// lives in <c>ConcurrentEvictingDictionaryTests._Stress.cs</c>.
/// </summary>
[TestClass]
public sealed class ConcurrentEvictingDictionaryNonGenericCollectionContractTests
    : NonGenericCollectionContractTests<ConcurrentEvictingDictionary<string, int>>
{
    /// <inheritdoc />
    /// <remarks>ConcurrentEvictingDictionary intentionally throws <see cref="NotSupportedException" /> from <c>SyncRoot</c> to prevent callers taking the same locks used internally.</remarks>
    protected override bool SyncRootSupported => false;

    /// <inheritdoc />
    protected override ConcurrentEvictingDictionary<string, int> CreateEmpty() => new(capacity: 16);

    /// <inheritdoc />
    protected override ConcurrentEvictingDictionary<string, int> Create()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = new(capacity: 16);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        return dictionary;
    }
}
