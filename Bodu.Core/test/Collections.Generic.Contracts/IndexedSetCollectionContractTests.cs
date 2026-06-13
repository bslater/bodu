// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetCollectionContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="CollectionContractTests{TCollection, TItem}" /> against
/// <see cref="IndexedSet{T}" /> with <see cref="int" /> elements. <see cref="IndexedSet{T}" /> implements
/// <see cref="IList{T}" /> (and therefore <see cref="ICollection{T}" />) plus
/// <see cref="IReadOnlyList{T}" /> — despite the "Set" in the name it is positional, not value-only, so
/// it does not implement <see cref="ISet{T}" />. The shared <see cref="ICollection{T}" /> contract
/// applies; bespoke index / Move / list-specific tests live in the existing <c>IndexedSetTests.*</c>
/// partials.
/// </summary>
[TestClass]
public sealed class IndexedSetCollectionContractTests
    : CollectionContractTests<IndexedSet<int>, int>
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
    protected override int CreateItem(int index) => 5000 + index;
}
