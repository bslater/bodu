// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Deque.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents an unbounded, double-ended queue (deque) backed by a contiguous array that grows automatically as
/// elements are added. Elements may be added or removed from either end in amortised O(1) time.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the deque.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Deque{T}"/> stores its elements in a circular array. The capacity expands automatically when more
/// elements are added than the current backing array can hold; growth doubles the existing capacity (with a
/// small minimum) and is capped at <see cref="Array.MaxLength"/>. Use <see cref="ArrayDeque{T}"/> if a fixed
/// capacity is required, or <see cref="CircularBuffer{T}"/> for fixed-capacity FIFO with eviction-on-full
/// semantics.
/// </para>
/// <para>
/// This type is not thread-safe. Concurrent reads and writes from multiple threads require external synchronization.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DequeDebugView<>))]
[Serializable]
public sealed class Deque<T> : DequeBase<T>
{
    private const int DefaultCapacity = 16;
    private const int MinGrowCapacity = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class with the default initial capacity.
    /// </summary>
    public Deque()
        : base(DefaultCapacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity hint. Must be greater than zero. The deque will still grow beyond this when needed.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    public Deque(int capacity)
        : base(capacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class containing elements copied from
    /// <paramref name="collection"/>, sized to fit them.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public Deque(IEnumerable<T> collection)
        : this(Materialize(collection)) { }

    /// <summary>
    /// Single private ctor that drives the IEnumerable-only public overload from a single materialised array,
    /// avoiding a second enumeration of one-shot sources.
    /// </summary>
    /// <param name="items">The materialised source elements; never <see langword="null"/>.</param>
    private Deque(T[] items)
        : base(items, Math.Max(items.Length, DefaultCapacity)) { }

    /// <summary>
    /// Materialises <paramref name="collection"/> into an array exactly once, performing the null-check.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null"/>.</param>
    /// <returns>The materialised array; never <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    private static T[] Materialize(IEnumerable<T> collection)
    {
        ThrowHelper.ThrowIfNull(collection);
        return collection as T[] ?? collection.ToArray();
    }

    /// <inheritdoc />
    public override void AddFirst(T item)
    {
        if (Count == Capacity)
            Grow(Count + 1);

        AddHead(item);
    }

    /// <inheritdoc />
    public override void AddLast(T item)
    {
        if (Count == Capacity)
            Grow(Count + 1);

        AddTail(item);
    }

    /// <inheritdoc />
    /// <remarks>Always returns <see langword="true"/>; <see cref="Deque{T}"/> grows on demand instead of failing.</remarks>
    public override bool TryAddFirst(T item)
    {
        AddFirst(item);
        return true;
    }

    /// <inheritdoc />
    /// <remarks>Always returns <see langword="true"/>; <see cref="Deque{T}"/> grows on demand instead of failing.</remarks>
    public override bool TryAddLast(T item)
    {
        AddLast(item);
        return true;
    }

    /// <summary>
    /// Ensures that the deque can hold at least <paramref name="capacity"/> elements without further growth,
    /// expanding the backing array if necessary.
    /// </summary>
    /// <param name="capacity">The minimum capacity required. Must be non-negative.</param>
    /// <returns>The new capacity of the backing array (which may exceed <paramref name="capacity"/>).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public int EnsureCapacity(int capacity)
    {
        ThrowHelper.ThrowIfNegative(capacity, nameof(capacity));

        if (capacity > Capacity)
            Grow(capacity);

        return Capacity;
    }

    /// <summary>
    /// Expands the backing array so that it holds at least <paramref name="minCapacity"/> elements. The new
    /// capacity is at least double the current capacity (with a small floor) and is capped at
    /// <see cref="Array.MaxLength"/>.
    /// </summary>
    /// <param name="minCapacity">The minimum capacity that the new backing array must satisfy.</param>
    private void Grow(int minCapacity)
    {
        int doubled = Math.Max(MinGrowCapacity, Capacity * 2);
        int newCapacity = Math.Max(minCapacity, doubled);

        if ((uint)newCapacity > (uint)Array.MaxLength)
            newCapacity = Array.MaxLength;

        Resize(newCapacity);
    }
}
