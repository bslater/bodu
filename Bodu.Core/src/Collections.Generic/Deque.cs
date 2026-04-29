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
/// capacity is required, or <see cref="CircularBuffer{T}"/> for fixed-capacity FIFO with eviction-on-full semantics.
/// </para>
/// <para>
/// This type is not thread-safe. Concurrent reads and writes from multiple threads require external synchronization.
/// </para>
/// <para>
/// Logical order runs from the head (the element returned by <see cref="PeekFirst"/>) to the tail (the element
/// returned by <see cref="PeekLast"/>). The indexer <see cref="this[int]"/>, <see cref="ToArray"/>,
/// <see cref="CopyTo(T[], int)"/>, and enumeration all operate in this head-to-tail order.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DequeDebugView<>))]
[Serializable]
public partial class Deque<T>
{
    private const int DefaultCapacity = 16;
    private const int MinGrowCapacity = 4;
#if !NET6_0_OR_GREATER
    private const int MaxArrayLength = 0x7FFFFFC7;
#endif

    private RingStorage<T> _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class with the default initial capacity.
    /// </summary>
    public Deque()
        : this(DefaultCapacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity hint. Must be greater than zero. The deque will still grow beyond this when needed.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    public Deque(int capacity)
    {
#if NET6_0_OR_GREATER
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, Array.MaxLength);
#else
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, MaxArrayLength);
#endif
        _storage = new RingStorage<T>(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class containing elements copied from
    /// <paramref name="collection"/>, sized to fit them.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public Deque(IEnumerable<T> collection)
    {
        ThrowHelper.ThrowIfNull(collection);

        T[] items = collection as T[] ?? collection.ToArray();
        int capacity = Math.Max(items.Length, DefaultCapacity);
        _storage.InitializeFrom(items, items.Length, capacity);
    }

    /// <summary>
    /// Gets the current capacity of the backing array. The deque will grow this automatically when needed.
    /// </summary>
    /// <value>The current backing-array length.</value>
    /// <returns>The current capacity of the deque.</returns>
    public int Capacity => _storage.Capacity;

    /// <summary>
    /// Gets a value indicating whether the deque contains no elements.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="Count"/> is zero; otherwise, <see langword="false"/>.</value>
    /// <returns><see langword="true"/> when empty.</returns>
    public bool IsEmpty => _storage.Count == 0;

    /// <summary>
    /// Gets the element at the specified zero-based logical position, where index 0 is the head.
    /// </summary>
    /// <param name="index">The zero-based index in head-to-tail order.</param>
    /// <returns>The element at the specified position.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
    public T this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfLessThan(index, 0);
            ThrowHelper.ThrowIfGreaterThanOrEqual(index, _storage.Count);

            return _storage.GetAt(index);
        }
    }

    /// <summary>
    /// Adds <paramref name="item"/> to the head of the deque, growing the backing array if necessary.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    public void AddFirst(T item)
    {
        if (_storage.Count == _storage.Capacity)
            Grow(_storage.Count + 1);

        _storage.AddHead(item);
    }

    /// <summary>
    /// Adds <paramref name="item"/> to the tail of the deque, growing the backing array if necessary.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    public void AddLast(T item)
    {
        if (_storage.Count == _storage.Capacity)
            Grow(_storage.Count + 1);

        _storage.AddTail(item);
    }

    /// <summary>
    /// Removes and returns the element at the head of the deque.
    /// </summary>
    /// <returns>The element that was at the head.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T RemoveFirst()
    {
        if (_storage.Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_EmptySequence);

        return _storage.RemoveHead();
    }

    /// <summary>
    /// Removes and returns the element at the tail of the deque.
    /// </summary>
    /// <returns>The element that was at the tail.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T RemoveLast()
    {
        if (_storage.Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_EmptySequence);

        return _storage.RemoveTail();
    }

    /// <summary>
    /// Attempts to remove and return the element at the head of the deque without throwing if the deque is empty.
    /// </summary>
    /// <param name="item">When this method returns, the removed head element if successful; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if an element was removed; <see langword="false"/> if the deque was empty.</returns>
    public bool TryRemoveFirst(out T item)
    {
        if (_storage.Count == 0)
        {
            item = default!;
            return false;
        }

        item = _storage.RemoveHead();
        return true;
    }

    /// <summary>
    /// Attempts to remove and return the element at the tail of the deque without throwing if the deque is empty.
    /// </summary>
    /// <param name="item">When this method returns, the removed tail element if successful; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if an element was removed; <see langword="false"/> if the deque was empty.</returns>
    public bool TryRemoveLast(out T item)
    {
        if (_storage.Count == 0)
        {
            item = default!;
            return false;
        }

        item = _storage.RemoveTail();
        return true;
    }

    /// <summary>
    /// Returns the head element without removing it.
    /// </summary>
    /// <returns>The element at the head of the deque.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T PeekFirst()
    {
        if (_storage.Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);

        return _storage.PeekHead();
    }

    /// <summary>
    /// Returns the tail element without removing it.
    /// </summary>
    /// <returns>The element at the tail of the deque.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T PeekLast()
    {
        if (_storage.Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);

        return _storage.PeekTail();
    }

    /// <summary>
    /// Attempts to read the head element without removing it.
    /// </summary>
    /// <param name="item">When this method returns, the head element if available; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if the head was read; <see langword="false"/> if the deque was empty.</returns>
    public bool TryPeekFirst(out T item)
    {
        if (_storage.Count == 0)
        {
            item = default!;
            return false;
        }

        item = _storage.PeekHead();
        return true;
    }

    /// <summary>
    /// Attempts to read the tail element without removing it.
    /// </summary>
    /// <param name="item">When this method returns, the tail element if available; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if the tail was read; <see langword="false"/> if the deque was empty.</returns>
    public bool TryPeekLast(out T item)
    {
        if (_storage.Count == 0)
        {
            item = default!;
            return false;
        }

        item = _storage.PeekTail();
        return true;
    }

    /// <summary>
    /// Removes all elements from the deque, resetting <see cref="Count"/> to zero. <see cref="Capacity"/> is unchanged.
    /// </summary>
    public void Clear() =>
        _storage.Clear();

    /// <summary>
    /// Determines whether the deque contains the specified element using <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    /// <param name="item">The element to locate. May be <see langword="null"/> for reference types.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T item) =>
        _storage.Contains(item);

    /// <summary>
    /// Copies the deque's elements to <paramref name="array"/> in head-to-tail order, starting at <paramref name="index"/>.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null"/>.</param>
    /// <param name="index">The zero-based starting index in <paramref name="array"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">The destination is too small to hold the deque's contents starting at <paramref name="index"/>.</exception>
    public void CopyTo(T[] array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(index, nameof(index));
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + _storage.Count);

        _storage.CopyToInternal(array, index);
    }

    /// <summary>
    /// Returns a new array containing the deque's elements in head-to-tail order.
    /// </summary>
    /// <returns>A freshly allocated array of length <see cref="Count"/>.</returns>
    public T[] ToArray() =>
        _storage.ToArray();

    /// <summary>
    /// Reduces the backing array's capacity to match <see cref="Count"/>, freeing unused memory. If the deque is empty,
    /// the capacity is reduced to one slot.
    /// </summary>
    public void TrimExcess()
    {
        int newCapacity = Math.Max(_storage.Count, 1);
        if (newCapacity == _storage.Capacity)
            return;

        _storage.Resize(newCapacity);
    }

    /// <summary>
    /// Ensures that the deque can hold at least <paramref name="capacity"/> elements without further growth, expanding
    /// the backing array if necessary.
    /// </summary>
    /// <param name="capacity">The minimum capacity required. Must be non-negative.</param>
    /// <returns>The new capacity of the backing array (which may exceed <paramref name="capacity"/>).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public int EnsureCapacity(int capacity)
    {
        ThrowHelper.ThrowIfNegative(capacity, nameof(capacity));

        if (capacity > _storage.Capacity)
            Grow(capacity);

        return _storage.Capacity;
    }

    /// <summary>
    /// Expands the backing array so that it holds at least <paramref name="minCapacity"/> elements. The new capacity
    /// is at least double the current capacity (with a small floor) and is capped at <see cref="Array.MaxLength"/>.
    /// </summary>
    /// <param name="minCapacity">The minimum capacity that the new backing array must satisfy.</param>
    private void Grow(int minCapacity)
    {
#if NET6_0_OR_GREATER
        int max = Array.MaxLength;
#else
        int max = MaxArrayLength;
#endif
        int doubled = Math.Max(MinGrowCapacity, _storage.Capacity * 2);
        int newCapacity = Math.Max(minCapacity, doubled);

        if ((uint)newCapacity > (uint)max)
            newCapacity = max;

        _storage.Resize(newCapacity);
    }
}
