// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class DequeTests
    : DoubleEndedRingCollectionTestsBase<DequeTests, Deque<int>>
{
    private const int DefaultCapacity = 16;

    /// <inheritdoc />
    protected override bool IsFixedCapacity => false;

    /// <inheritdoc />
    protected override bool ReportsExactCapacity => false;

    /// <inheritdoc />
    protected override Deque<int> CreateCollection(int capacity) =>
        new(capacity);

    /// <inheritdoc />
    protected override void AddToTail(Deque<int> collection, int item) =>
        collection.AddLast(item);

    /// <inheritdoc />
    protected override bool TryAddToTail(Deque<int> collection, int item)
    {
        // Deque grows on demand, so adds always succeed; mirror the API contract for the hook.
        collection.AddLast(item);
        return true;
    }

    /// <inheritdoc />
    protected override int RemoveFromHead(Deque<int> collection) =>
        collection.RemoveFirst();

    /// <inheritdoc />
    protected override bool TryRemoveFromHead(Deque<int> collection, out int item) =>
        collection.TryRemoveFirst(out item);

    /// <inheritdoc />
    protected override int PeekHead(Deque<int> collection) =>
        collection.PeekFirst();

    /// <inheritdoc />
    protected override bool TryPeekHead(Deque<int> collection, out int item) =>
        collection.TryPeekFirst(out item);

    /// <inheritdoc />
    protected override void AddToHead(Deque<int> collection, int item) =>
        collection.AddFirst(item);

    /// <inheritdoc />
    protected override bool TryAddToHead(Deque<int> collection, int item)
    {
        collection.AddFirst(item);
        return true;
    }

    /// <inheritdoc />
    protected override int RemoveFromTail(Deque<int> collection) =>
        collection.RemoveLast();

    /// <inheritdoc />
    protected override bool TryRemoveFromTail(Deque<int> collection, out int item) =>
        collection.TryRemoveLast(out item);

    /// <inheritdoc />
    protected override int PeekTail(Deque<int> collection) =>
        collection.PeekLast();

    /// <inheritdoc />
    protected override bool TryPeekTail(Deque<int> collection, out int item) =>
        collection.TryPeekLast(out item);

    /// <inheritdoc />
    protected override int GetCapacity(Deque<int> collection) =>
        collection.Capacity;

    /// <inheritdoc />
    protected override bool GetIsEmpty(Deque<int> collection) =>
        collection.IsEmpty;

    /// <inheritdoc />
    protected override void Clear(Deque<int> collection) =>
        collection.Clear();

    /// <inheritdoc />
    protected override bool Contains(Deque<int> collection, int item) =>
        collection.Contains(item);

    /// <inheritdoc />
    protected override void CopyTo(Deque<int> collection, int[] array, int index) =>
        collection.CopyTo(array, index);

    /// <inheritdoc />
    protected override int[] ToArray(Deque<int> collection) =>
        collection.ToArray();

    /// <inheritdoc />
    protected override int GetAt(Deque<int> collection, int index) =>
        collection[index];

    /// <inheritdoc />
    protected override void TrimExcess(Deque<int> collection) =>
        collection.TrimExcess();
}
