// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeEnumeratorContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="EnumeratorContractTests{TEnumerable, TItem}" /> against <see cref="Deque{T}" />.
/// </summary>
[TestClass]
public sealed class DequeEnumeratorContractTests
    : EnumeratorContractTests<Deque<int>, int>
{
    /// <inheritdoc />
    protected override Deque<int> Create(params int[] items)
    {
        Deque<int> deque = new();
        foreach (int item in items)
            deque.AddLast(item);
        return deque;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 400 + index;
}
