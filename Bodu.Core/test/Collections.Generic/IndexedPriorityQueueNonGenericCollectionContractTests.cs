// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueueNonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Drives <see cref="NonGenericCollectionContractTests{TCollection}" /> against
/// <see cref="IndexedPriorityQueue{TElement, TPriority}" />, exercising the non-generic
/// <see cref="System.Collections.ICollection" /> surface (<c>Count</c>, <c>SyncRoot</c>,
/// <c>IsSynchronized</c>, <c>CopyTo</c>).
/// </summary>
[TestClass]
public sealed class IndexedPriorityQueueNonGenericCollectionContractTests
    : NonGenericCollectionContractTests<IndexedPriorityQueue<string, int>>
{
    /// <inheritdoc />
    protected override IndexedPriorityQueue<string, int> CreateEmpty() => new();

    /// <inheritdoc />
    protected override IndexedPriorityQueue<string, int> Create()
    {
        IndexedPriorityQueue<string, int> queue = new();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 2);
        queue.Enqueue("c", 3);
        return queue;
    }
}
