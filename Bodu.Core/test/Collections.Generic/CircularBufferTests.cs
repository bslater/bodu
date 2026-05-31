// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class CircularBufferTests
    : RingBackedCollectionTestsBase<CircularBufferTests, CircularBuffer<int>>
{

    private const int DefaultCapacity = 16;

    /// <inheritdoc />
    protected override void AddToTail(CircularBuffer<int> collection, int item) =>
        collection.Enqueue(item);

    /// <inheritdoc />
    protected override void Clear(CircularBuffer<int> collection) =>
        collection.Clear();

    /// <inheritdoc />
    protected override bool Contains(CircularBuffer<int> collection, int item) =>
        collection.Contains(item);

    /// <inheritdoc />
    protected override void CopyTo(CircularBuffer<int> collection, int[] array, int index) =>
        collection.CopyTo(array, index);

    /// <inheritdoc />
    protected override CircularBuffer<int> CreateCollection(int capacity) =>
        new(capacity);

    /// <inheritdoc />
    protected override int GetAt(CircularBuffer<int> collection, int index) =>
        collection[index];

    /// <inheritdoc />
    protected override int GetCapacity(CircularBuffer<int> collection) =>
        collection.Capacity;

    /// <inheritdoc />
    protected override int PeekHead(CircularBuffer<int> collection) =>
        collection.Peek();

    /// <inheritdoc />
    protected override int RemoveFromHead(CircularBuffer<int> collection) =>
        collection.Dequeue();

    /// <inheritdoc />
    protected override int[] ToArray(CircularBuffer<int> collection) =>
        collection.ToArray();

    /// <inheritdoc />
    protected override void TrimExcess(CircularBuffer<int> collection) =>
        collection.TrimExcess();

    /// <inheritdoc />
    protected override bool TryAddToTail(CircularBuffer<int> collection, int item) =>
        collection.TryEnqueue(item);

    /// <inheritdoc />
    protected override bool TryPeekHead(CircularBuffer<int> collection, out int item) =>
        collection.TryPeek(out item);

    /// <inheritdoc />
    protected override bool TryRemoveFromHead(CircularBuffer<int> collection, out int item) =>
        collection.TryDequeue(out item);

}
