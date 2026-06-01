// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetEnumeratorContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="EnumeratorContractTests{TEnumerable, TItem}" /> against <see cref="IndexedSet{T}" />.
/// </summary>
[TestClass]
public sealed class IndexedSetEnumeratorContractTests
    : EnumeratorContractTests<IndexedSet<int>, int>
{
    /// <inheritdoc />
    protected override IndexedSet<int> Create(params int[] items)
    {
        IndexedSet<int> set = new();
        foreach (var item in items)
            set.Add(item);
        return set;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 7000 + index;
}
