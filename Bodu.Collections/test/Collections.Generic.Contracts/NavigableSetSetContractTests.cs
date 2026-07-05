// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetSetContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="SetContractTests{TSet, TItem}" /> against <see cref="NavigableSet{T}" />.
/// </summary>
[TestClass]
public sealed class NavigableSetSetContractTests
    : SetContractTests<NavigableSet<int>, int>
{
    /// <inheritdoc />
    protected override NavigableSet<int> CreateSet(params int[] items)
    {
        NavigableSet<int> set = new();
        foreach (int item in items)
            set.Add(item);
        return set;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 9000 + index;
}
