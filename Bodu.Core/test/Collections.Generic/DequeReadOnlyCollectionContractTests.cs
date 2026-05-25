// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeReadOnlyCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="ReadOnlyCollectionContractTests{TCollection, TItem}" /> against
/// <see cref="Deque{T}" /> with <see cref="int" /> elements. Deque is a double-ended ring collection so
/// the bulk of its semantic coverage lives in <c>DoubleEndedRingCollectionTestsBase</c>; this contract
/// subclass adds the shared read-only-collection assertions on top.
/// </summary>
[TestClass]
public sealed class DequeReadOnlyCollectionContractTests
    : ReadOnlyCollectionContractTests<Deque<int>, int>
{
    /// <inheritdoc />
    protected override Deque<int> CreateEmpty() => new();

    /// <inheritdoc />
    protected override Deque<int> Create(params int[] items)
    {
        Deque<int> deque = new();
        foreach (int item in items)
            deque.AddLast(item);
        return deque;
    }

    /// <inheritdoc />
    protected override int CreateItem(int index) => 300 + index;
}
