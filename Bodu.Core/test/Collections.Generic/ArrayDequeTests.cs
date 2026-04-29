// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class ArrayDequeTests
    : DoubleEndedRingCollectionTestsBase<ArrayDequeTests, ArrayDeque<int>>
{
    private const int DefaultCapacity = 16;

    /// <inheritdoc />
    protected override ArrayDeque<int> CreateCollection(int capacity) =>
        new(capacity);

    /// <inheritdoc />
    protected override void AddToTail(ArrayDeque<int> collection, int item) =>
        collection.AddLast(item);

    /// <inheritdoc />
    protected override bool TryAddToTail(ArrayDeque<int> collection, int item) =>
        collection.TryAddLast(item);

    /// <inheritdoc />
    protected override int RemoveFromHead(ArrayDeque<int> collection) =>
        collection.RemoveFirst();

    /// <inheritdoc />
    protected override bool TryRemoveFromHead(ArrayDeque<int> collection, out int item) =>
        collection.TryRemoveFirst(out item);

    /// <inheritdoc />
    protected override int PeekHead(ArrayDeque<int> collection) =>
        collection.PeekFirst();

    /// <inheritdoc />
    protected override bool TryPeekHead(ArrayDeque<int> collection, out int item) =>
        collection.TryPeekFirst(out item);

    /// <inheritdoc />
    protected override void AddToHead(ArrayDeque<int> collection, int item) =>
        collection.AddFirst(item);

    /// <inheritdoc />
    protected override bool TryAddToHead(ArrayDeque<int> collection, int item) =>
        collection.TryAddFirst(item);

    /// <inheritdoc />
    protected override int RemoveFromTail(ArrayDeque<int> collection) =>
        collection.RemoveLast();

    /// <inheritdoc />
    protected override bool TryRemoveFromTail(ArrayDeque<int> collection, out int item) =>
        collection.TryRemoveLast(out item);

    /// <inheritdoc />
    protected override int PeekTail(ArrayDeque<int> collection) =>
        collection.PeekLast();

    /// <inheritdoc />
    protected override bool TryPeekTail(ArrayDeque<int> collection, out int item) =>
        collection.TryPeekLast(out item);

    /// <inheritdoc />
    protected override int GetCapacity(ArrayDeque<int> collection) =>
        collection.Capacity;

    /// <inheritdoc />
    protected override bool GetIsEmpty(ArrayDeque<int> collection) =>
        collection.IsEmpty;

    /// <inheritdoc />
    protected override void Clear(ArrayDeque<int> collection) =>
        collection.Clear();

    /// <inheritdoc />
    protected override bool Contains(ArrayDeque<int> collection, int item) =>
        collection.Contains(item);

    /// <inheritdoc />
    protected override void CopyTo(ArrayDeque<int> collection, int[] array, int index) =>
        collection.CopyTo(array, index);

    /// <inheritdoc />
    protected override int[] ToArray(ArrayDeque<int> collection) =>
        collection.ToArray();

    /// <inheritdoc />
    protected override int GetAt(ArrayDeque<int> collection, int index) =>
        collection[index];

    /// <inheritdoc />
    protected override void TrimExcess(ArrayDeque<int> collection) =>
        collection.TrimExcess();
}
