// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetEnumeratorContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="EnumeratorContractTests{TEnumerable, TItem}" /> against <see cref="NavigableSet{T}" />.
/// </summary>
[TestClass]
public sealed class NavigableSetEnumeratorContractTests
    : EnumeratorContractTests<NavigableSet<int>, int>
{
    /// <inheritdoc />
    protected override NavigableSet<int> Create(params int[] items)
    {
        NavigableSet<int> set = new();
        foreach (int item in items)
            set.Add(item);
        return set;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 9100 + index;
}
