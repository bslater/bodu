// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryNonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="NonGenericCollectionContractTests{TCollection}" /> against
/// <see cref="EvictingDictionary{TKey, TValue}" />, exercising the non-generic
/// <see cref="System.Collections.ICollection" /> surface (<c>Count</c>, <c>SyncRoot</c>,
/// <c>IsSynchronized</c>, <c>CopyTo</c>).
/// </summary>
[TestClass]
public sealed class EvictingDictionaryNonGenericCollectionContractTests
    : NonGenericCollectionContractTests<EvictingDictionary<int, string>>
{
    /// <inheritdoc />
    protected override EvictingDictionary<int, string> CreateEmpty() => new(capacity: 4);

    /// <inheritdoc />
    protected override EvictingDictionary<int, string> Create()
    {
        EvictingDictionary<int, string> map = new(capacity: 4);
        map[1] = "one";
        map[2] = "two";
        map[3] = "three";
        return map;
    }
}
