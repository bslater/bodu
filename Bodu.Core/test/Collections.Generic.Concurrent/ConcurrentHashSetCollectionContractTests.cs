// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Concurrent;
using Bodu.Collections.Generic.Contracts;

namespace Bodu.Collections.Generic.Concurrent.Contracts;

/// <summary>
/// Drives <see cref="CollectionContractTests{TCollection, TItem}" /> against
/// <see cref="ConcurrentHashSet{T}" /> with <see cref="int" /> elements. The base class exercises the
/// shared <see cref="ICollection{T}" /> contract; bespoke concurrency / sharding tests live in the other
/// <c>ConcurrentHashSetTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class ConcurrentHashSetCollectionContractTests
    : CollectionContractTests<ConcurrentHashSet<int>, int>
{
    /// <inheritdoc />
    protected override ConcurrentHashSet<int> CreateEmpty() => new();

    /// <inheritdoc />
    protected override ConcurrentHashSet<int> Create(params int[] items)
    {
        ConcurrentHashSet<int> set = new();
        foreach (var item in items)
            set.Add(item);
        return set;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 1000 + index;
}
