// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedDictionaryNonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="NonGenericCollectionContractTests{TCollection}" /> against
/// <see cref="LinkedDictionary{TKey, TValue}" />, exercising the non-generic
/// <see cref="System.Collections.ICollection" /> surface (<c>Count</c>, <c>SyncRoot</c>, <c>IsSynchronized</c>,
/// <c>CopyTo</c>).
/// </summary>
[TestClass]
public sealed class LinkedDictionaryNonGenericCollectionContractTests
    : NonGenericCollectionContractTests<LinkedDictionary<int, string>>
{
    /// <inheritdoc />
    protected override LinkedDictionary<int, string> CreateEmpty() => new();

    /// <inheritdoc />
    protected override LinkedDictionary<int, string> Create()
    {
        LinkedDictionary<int, string> map = new();
        map[1] = "one";
        map[2] = "two";
        map[3] = "three";
        return map;
    }
}
