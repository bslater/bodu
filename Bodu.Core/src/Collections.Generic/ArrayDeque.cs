// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDeque.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a fixed-capacity, double-ended queue (deque) backed by a contiguous array. Elements may be added
/// or removed from either end in amortised O(1) time.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the deque.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ArrayDeque{T}"/> stores its elements in a circular array. The capacity is fixed at construction
/// time; attempting to add an element while <see cref="IsFull"/> is <see langword="true"/> throws an
/// <see cref="InvalidOperationException"/> from <see cref="AddFirst(T)"/> / <see cref="AddLast(T)"/>, or returns
/// <see langword="false"/> from the corresponding <c>Try*</c> overloads. There is no overwrite mode — use
/// <see cref="CircularBuffer{T}"/> if eviction-on-full semantics are required, or <see cref="Deque{T}"/> for an
/// unbounded growable double-ended queue.
/// </para>
/// <para>
/// This type is not thread-safe. Concurrent reads and writes from multiple threads require external synchronization.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
[DebuggerTypeProxy(typeof(ArrayDequeDebugView<>))]
[Serializable]
public sealed class ArrayDeque<T> : DequeBase<T>
{
    private const int DefaultCapacity = 16;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDeque{T}"/> class with the default capacity.
    /// </summary>
    public ArrayDeque()
        : base(DefaultCapacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDeque{T}"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the deque can contain. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    public ArrayDeque(int capacity)
        : base(capacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDeque{T}"/> class containing elements copied from
    /// <paramref name="collection"/>, using the larger of the collection size or the default capacity.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public ArrayDeque(IEnumerable<T> collection)
        : this(MaterializeUpTo(collection, capacity: int.MaxValue, throwOnOverflow: false),
               UseFloorCapacity)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDeque{T}"/> class containing elements copied from
    /// <paramref name="collection"/>, with the specified capacity.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <param name="capacity">The maximum number of elements the deque can contain. Must be greater than zero, and at least <c>collection.Count</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">The number of elements in <paramref name="collection"/> exceeds <paramref name="capacity"/>.</exception>
    public ArrayDeque(IEnumerable<T> collection, int capacity)
        : this(MaterializeUpTo(collection, capacity, throwOnOverflow: true), capacity)
    { }

    /// <summary>
    /// Sentinel marker passed to the private ctor to indicate the (IEnumerable) overload should derive its
    /// capacity as <c>max(items.Length, DefaultCapacity)</c>.
    /// </summary>
    private const int UseFloorCapacity = -1;

    /// <summary>
    /// Single private ctor that drives both the IEnumerable-only and (IEnumerable, capacity) public overloads
    /// from a single materialised array.
    /// </summary>
    /// <param name="items">The materialised source elements; never <see langword="null"/>.</param>
    /// <param name="capacity">
    /// The requested capacity, or <see cref="UseFloorCapacity"/> when invoked from the IEnumerable-only
    /// overload (in which case the capacity becomes <c>max(items.Length, DefaultCapacity)</c>).
    /// </param>
    private ArrayDeque(T[] items, int capacity)
        : base(items, capacity == UseFloorCapacity ? Math.Max(items.Length, DefaultCapacity) : capacity)
    { }

    /// <summary>
    /// Materialises <paramref name="collection"/> into an array exactly once, optionally enforcing a
    /// capacity bound. Performs the null-check and the size check before construction so the base
    /// constructor never sees a truncated source.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null"/>.</param>
    /// <param name="capacity">The maximum allowed source length when <paramref name="throwOnOverflow"/> is set.</param>
    /// <param name="throwOnOverflow">
    /// When <see langword="true"/>, throws <see cref="InvalidOperationException"/> if the materialised array
    /// has more elements than <paramref name="capacity"/>.
    /// </param>
    /// <returns>The materialised array; never <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The source exceeds <paramref name="capacity"/> when <paramref name="throwOnOverflow"/> is <see langword="true"/>.</exception>
    private static T[] MaterializeUpTo(IEnumerable<T> collection, int capacity, bool throwOnOverflow)
    {
        ThrowHelper.ThrowIfNull(collection);
        T[] items = collection as T[] ?? collection.ToArray();

        if (throwOnOverflow && items.Length > capacity)
            throw new InvalidOperationException(ResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity);

        return items;
    }

    /// <summary>
    /// Gets a value indicating whether the deque has reached its capacity.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="RingBackedCollection{T}.Count"/> equals <see cref="RingBackedCollection{T}.Capacity"/>.</value>
    /// <returns><see langword="true"/> when full.</returns>
    public bool IsFull => Count == Capacity;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The deque is full.</exception>
    public override void AddFirst(T item)
    {
        if (Count == Capacity)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

        AddHead(item);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The deque is full.</exception>
    public override void AddLast(T item)
    {
        if (Count == Capacity)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

        AddTail(item);
    }

    /// <inheritdoc />
    public override bool TryAddFirst(T item)
    {
        if (Count == Capacity)
            return false;

        AddHead(item);
        return true;
    }

    /// <inheritdoc />
    public override bool TryAddLast(T item)
    {
        if (Count == Capacity)
            return false;

        AddTail(item);
        return true;
    }
}
