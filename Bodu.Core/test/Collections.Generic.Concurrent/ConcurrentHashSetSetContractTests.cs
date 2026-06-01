// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetSetContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Contracts;

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="SetContractTests{TSet, TItem}" /> against <see cref="ConcurrentHashSet{T}" /> with
/// <see cref="int" /> elements. Covers the set-specific surface (<c>UnionWith</c>, <c>IntersectWith</c>,
/// <c>ExceptWith</c>, <c>SymmetricExceptWith</c>, <c>IsSubsetOf</c>, <c>IsSupersetOf</c>, <c>Overlaps</c>,
/// <c>SetEquals</c>) on top of the basic collection contract.
/// </summary>
[TestClass]
public sealed class ConcurrentHashSetSetContractTests
    : SetContractTests<ConcurrentHashSet<int>, int>
{
    /// <inheritdoc />
    protected override ConcurrentHashSet<int> CreateSet(params int[] items)
    {
        ConcurrentHashSet<int> set = new();
        foreach (var item in items)
            set.Add(item);
        return set;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 2000 + index;
}
