// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetReadOnlyCollectionContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="ReadOnlyCollectionContractTests{TCollection, TItem}" /> against
/// <see cref="IndexedSet{T}" /> via its <see cref="IReadOnlyList{T}" /> implementation.
/// </summary>
[TestClass]
public sealed class IndexedSetReadOnlyCollectionContractTests
    : ReadOnlyCollectionContractTests<IndexedSet<int>, int>
{
    /// <inheritdoc />
    protected override IndexedSet<int> CreateEmpty() => new();

    /// <inheritdoc />
    protected override IndexedSet<int> Create(params int[] items)
    {
        IndexedSet<int> set = new();
        foreach (var item in items)
            set.Add(item);
        return set;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 6000 + index;
}
