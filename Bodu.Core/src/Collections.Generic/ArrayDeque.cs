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
/// Represents a fixed-capacity, double-ended queue (deque) backed by a contiguous array. Elements may be added or
/// removed from either end in amortised O(1) time.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the deque.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ArrayDeque{T}"/> stores its elements in a circular array. The capacity is fixed at construction time;
/// attempting to add an element while <see cref="IsFull"/> is <see langword="true"/> throws an
/// <see cref="InvalidOperationException"/> from <see cref="AddFirst(T)"/> / <see cref="AddLast(T)"/>, or returns
/// <see langword="false"/> from the corresponding <c>Try*</c> overloads. There is no overwrite mode — use
/// <see cref="CircularBuffer{T}"/> if eviction-on-full semantics are required, or <see cref="Deque{T}"/> for an
/// unbounded growable double-ended queue.
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
[DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
[DebuggerTypeProxy(typeof(ArrayDequeDebugView<>))]
[Serializable]
public partial class ArrayDeque<T>
{
    private const int DefaultCapacity = 16;
#if !NET6_0_OR_GREATER
    private const int MaxArrayLength = 0x7FFFFFC7;
#endif

    private RingStorage<T> _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDeque{T}"/> class with the default capacity.
    /// </summary>
    public ArrayDeque()
        : this(DefaultCapacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDeque{T}"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the deque can contain. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    public ArrayDeque(int capacity)
    {
#if NET6_0_OR_GREATER
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, Array.MaxLength);
#else
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, MaxArrayLength);
#endif
        _storage = new RingStorage<T>(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDeque{T}"/> class containing elements copied from
    /// <paramref name="collection"/>, using the larger of the collection size or <see cref="DefaultCapacity"/> as the capacity.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public ArrayDeque(IEnumerable<T> collection)
    {
        ThrowHelper.ThrowIfNull(collection);

        T[] items = collection as T[] ?? collection.ToArray();
        int capacity = Math.Max(items.Length, DefaultCapacity);
        _storage.InitializeFrom(items, items.Length, capacity);
    }

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
    {
        ThrowHelper.ThrowIfNull(collection);
#if NET6_0_OR_GREATER
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, Array.MaxLength);
#else
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, MaxArrayLength);
#endif

        T[] items = collection as T[] ?? collection.ToArray();
        if (items.Length > capacity)
            throw new InvalidOperationException(ResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity);

        _storage.InitializeFrom(items, items.Length, capacity);
    }

    /// <summary>
    /// Gets the maximum number of elements that the <see cref="ArrayDeque{T}"/> can contain.
    /// </summary>
    /// <value>The fixed capacity established at construction.</value>
    /// <returns>The capacity of the deque.</returns>
    public int Capacity => _storage.Capacity;

    /// <summary>
    /// Gets a value indicating whether the deque contains no elements.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="Count"/> is zero; otherwise, <see langword="false"/>.</value>
    /// <returns><see langword="true"/> when empty.</returns>
    public bool IsEmpty => _storage.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the deque has reached its capacity.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="Count"/> equals <see cref="Capacity"/>; otherwise, <see langword="false"/>.</value>
    /// <returns><see langword="true"/> when full.</returns>
    public bool IsFull => _storage.Count == _storage.Capacity;

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
    /// Adds <paramref name="item"/> to the head of the deque.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <exception cref="InvalidOperationException">The deque is at full capacity.</exception>
    public void AddFirst(T item)
    {
        if (_storage.Count == _storage.Capacity)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

        _storage.AddHead(item);
    }

    /// <summary>
    /// Adds <paramref name="item"/> to the tail of the deque.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <exception cref="InvalidOperationException">The deque is at full capacity.</exception>
    public void AddLast(T item)
    {
        if (_storage.Count == _storage.Capacity)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

        _storage.AddTail(item);
    }

    /// <summary>
    /// Attempts to add <paramref name="item"/> to the head of the deque without throwing if the deque is full.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <returns><see langword="true"/> if the item was added; <see langword="false"/> if the deque was full.</returns>
    public bool TryAddFirst(T item)
    {
        if (_storage.Count == _storage.Capacity)
            return false;

        _storage.AddHead(item);
        return true;
    }

    /// <summary>
    /// Attempts to add <paramref name="item"/> to the tail of the deque without throwing if the deque is full.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <returns><see langword="true"/> if the item was added; <see langword="false"/> if the deque was full.</returns>
    public bool TryAddLast(T item)
    {
        if (_storage.Count == _storage.Capacity)
            return false;

        _storage.AddTail(item);
        return true;
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
    /// the capacity is reduced to one slot to keep the deque operational.
    /// </summary>
    public void TrimExcess()
    {
        int newCapacity = Math.Max(_storage.Count, 1);
        if (newCapacity == _storage.Capacity)
            return;

        _storage.Resize(newCapacity);
    }
}
