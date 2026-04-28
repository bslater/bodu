// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingStorage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides the shared low-level ring-buffer storage primitives used by <see cref="CircularBuffer{T}"/>,
/// <see cref="ArrayDeque{T}"/>, and <see cref="Deque{T}"/>.
/// </summary>
/// <typeparam name="T">Specifies the type of elements held by the storage.</typeparam>
/// <remarks>
/// <para>
/// This is an internal implementation helper. It encapsulates the contiguous backing array, the head/tail indices
/// with modulo wrap, the live element count, and a structural-version counter used by enumerators to detect
/// concurrent modification.
/// </para>
/// <para>
/// The struct is mutated through its owning class field (for example <c>private RingStorage&lt;T&gt; _storage</c>);
/// because the owners are reference types, in-place mutation of the field works correctly.
/// </para>
/// <para>
/// Methods on this struct intentionally perform no argument validation — owners are expected to validate inputs
/// using <see cref="Bodu.ThrowHelper"/> before delegating. Mutation methods that grow the count (<see cref="AddTail"/>,
/// <see cref="AddHead"/>) require the caller to ensure spare capacity is available; removal methods
/// (<see cref="RemoveHead"/>, <see cref="RemoveTail"/>) require a non-empty buffer.
/// </para>
/// </remarks>
[Serializable]
internal struct RingStorage<T>
{
    /// <summary>
    /// The contiguous backing array. The active region wraps from <see cref="_head"/> through <see cref="_tail"/>.
    /// </summary>
    internal T[] _array;

    /// <summary>
    /// The index of the first (oldest) element. Undefined when <see cref="_count"/> is zero.
    /// </summary>
    internal int _head;

    /// <summary>
    /// The index at which the next tail-side write occurs (one past the last element, modulo capacity).
    /// </summary>
    internal int _tail;

    /// <summary>
    /// The number of live elements currently held in the storage.
    /// </summary>
    internal int _count;

    /// <summary>
    /// A monotonic counter incremented on every structural mutation; consumed by enumerators for invalidation.
    /// </summary>
    internal int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="RingStorage{T}"/> struct with a backing array of the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial backing-array capacity. Must be greater than zero.</param>
    public RingStorage(int capacity)
    {
        _array = new T[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
        _version = 0;
    }

    /// <summary>
    /// Gets the current capacity of the backing array.
    /// </summary>
    /// <returns>The length of the underlying array.</returns>
    public int Capacity => _array.Length;

    /// <summary>
    /// Gets the number of live elements currently held in the storage.
    /// </summary>
    /// <returns>The element count.</returns>
    public int Count => _count;

    /// <summary>
    /// Gets the current structural-version counter.
    /// </summary>
    /// <returns>An integer that increases whenever the storage is mutated.</returns>
    public int Version => _version;

    /// <summary>
    /// Gets the head index of the storage (index of the oldest element).
    /// </summary>
    /// <returns>The current head index.</returns>
    public int Head => _head;

    /// <summary>
    /// Gets the underlying backing array. Exposed for direct read access by enumerators.
    /// </summary>
    /// <returns>The backing array; never <see langword="null"/>.</returns>
    public T[] Array => _array;

    /// <summary>
    /// Returns the element at the specified zero-based logical index, where index 0 is the head.
    /// </summary>
    /// <param name="logicalIndex">A logical, zero-based offset from the head. Must be in <c>[0, Count)</c>.</param>
    /// <returns>The element at the given logical position.</returns>
    public T GetAt(int logicalIndex) =>
        _array[(_head + logicalIndex) % _array.Length];

    /// <summary>
    /// Returns the head element without removing it.
    /// </summary>
    /// <returns>The current head element.</returns>
    public T PeekHead() =>
        _array[_head];

    /// <summary>
    /// Returns the tail element (the most-recently inserted at the tail end) without removing it.
    /// </summary>
    /// <returns>The element immediately preceding <c>_tail</c> in modulo order.</returns>
    public T PeekTail()
    {
        int capacity = _array.Length;
        int tailIndex = (_tail - 1 + capacity) % capacity;
        return _array[tailIndex];
    }

    /// <summary>
    /// Writes <paramref name="item"/> at the tail position and advances the tail.
    /// </summary>
    /// <param name="item">The element to add at the tail.</param>
    /// <remarks>The caller must ensure <c>Count &lt; Capacity</c> before invoking.</remarks>
    public void AddTail(T item)
    {
        _array[_tail] = item;
        _tail = (_tail + 1) % _array.Length;
        _count++;
        _version++;
    }

    /// <summary>
    /// Writes <paramref name="item"/> at the position immediately before the head and retreats the head.
    /// </summary>
    /// <param name="item">The element to add at the head.</param>
    /// <remarks>The caller must ensure <c>Count &lt; Capacity</c> before invoking.</remarks>
    public void AddHead(T item)
    {
        int capacity = _array.Length;
        _head = (_head - 1 + capacity) % capacity;
        _array[_head] = item;
        _count++;
        _version++;
    }

    /// <summary>
    /// Removes and returns the head element, clearing its slot in the backing array.
    /// </summary>
    /// <returns>The element that was at the head.</returns>
    /// <remarks>The caller must ensure <c>Count &gt; 0</c> before invoking.</remarks>
    public T RemoveHead()
    {
        T item = _array[_head];
        _array[_head] = default!;
        _head = (_head + 1) % _array.Length;
        _count--;
        _version++;
        return item;
    }

    /// <summary>
    /// Removes and returns the tail element, clearing its slot in the backing array.
    /// </summary>
    /// <returns>The element that was at the tail.</returns>
    /// <remarks>The caller must ensure <c>Count &gt; 0</c> before invoking.</remarks>
    public T RemoveTail()
    {
        int capacity = _array.Length;
        _tail = (_tail - 1 + capacity) % capacity;
        T item = _array[_tail];
        _array[_tail] = default!;
        _count--;
        _version++;
        return item;
    }

    /// <summary>
    /// Writes <paramref name="item"/> at the tail position when the storage is full, advancing both head and tail
    /// so that the oldest element is overwritten in place. Used by the eviction path of <see cref="CircularBuffer{T}"/>.
    /// </summary>
    /// <param name="item">The element to write into the slot occupied by the tail.</param>
    /// <remarks>The caller must ensure <c>Count == Capacity</c> before invoking. Count is unchanged by this operation.</remarks>
    public void OverwriteTail(T item)
    {
        _array[_tail] = item;
        int next = (_tail + 1) % _array.Length;
        _head = next;
        _tail = next;
        _version++;
    }

    /// <summary>
    /// Removes all elements from the storage and resets head, tail and count to zero. Bumps the version counter
    /// only when something was cleared.
    /// </summary>
    public void Clear()
    {
        if (_count > 0)
        {
            int capacity = _array.Length;
            if (_head < _tail)
            {
                System.Array.Clear(_array, _head, _count);
            }
            else
            {
                System.Array.Clear(_array, _head, capacity - _head);
                System.Array.Clear(_array, 0, _tail);
            }

            _head = 0;
            _tail = 0;
            _count = 0;
            _version++;
        }
    }

    /// <summary>
    /// Determines whether the storage contains the specified element using <see cref="System.Array.IndexOf{T}(T[], T, int, int)"/>.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T item)
    {
        if (_count == 0)
            return false;

        if (_head < _tail)
        {
            return System.Array.IndexOf(_array, item, _head, _count) >= 0;
        }

        int firstSegmentLength = _array.Length - _head;
        if (System.Array.IndexOf(_array, item, _head, firstSegmentLength) >= 0)
            return true;

        return _tail > 0 && System.Array.IndexOf(_array, item, 0, _tail) >= 0;
    }

    /// <summary>
    /// Copies the live region into <paramref name="destination"/> starting at <paramref name="destinationIndex"/>,
    /// in head-to-tail logical order. Handles wrapped layouts using two segment copies.
    /// </summary>
    /// <param name="destination">The destination array. Must have sufficient space starting at <paramref name="destinationIndex"/>.</param>
    /// <param name="destinationIndex">The zero-based starting index in <paramref name="destination"/>.</param>
    public void CopyToInternal(System.Array destination, int destinationIndex)
    {
        if (_count == 0)
            return;

        if (_head < _tail)
        {
            System.Array.Copy(_array, _head, destination, destinationIndex, _count);
        }
        else
        {
            int firstSegmentLength = _array.Length - _head;
            System.Array.Copy(_array, _head, destination, destinationIndex, firstSegmentLength);
            System.Array.Copy(_array, 0, destination, destinationIndex + firstSegmentLength, _tail);
        }
    }

    /// <summary>
    /// Allocates a new <typeparamref name="T"/> array containing the live region in logical order.
    /// </summary>
    /// <returns>A freshly allocated array of length <see cref="Count"/>.</returns>
    public T[] ToArray()
    {
        T[] result = new T[_count];
        if (_count > 0)
            CopyToInternal(result, 0);

        return result;
    }

    /// <summary>
    /// Replaces the backing array with one of size <paramref name="newCapacity"/>, copying the live region into
    /// the new array starting at index zero. Resets <see cref="_head"/> to zero and <see cref="_tail"/> to the
    /// position just after the last element.
    /// </summary>
    /// <param name="newCapacity">The new capacity. Must satisfy <c>newCapacity &gt;= Count</c> and <c>newCapacity &gt;= 1</c>.</param>
    public void Resize(int newCapacity)
    {
        T[] newArray = new T[newCapacity];
        if (_count > 0)
            CopyToInternal(newArray, 0);

        _array = newArray;
        _head = 0;
        _tail = _count == newCapacity ? 0 : _count;
        _version++;
    }

    /// <summary>
    /// Initializes the backing array from a source <typeparamref name="T"/> array of the specified logical length.
    /// Retains the most recent <paramref name="capacity"/> elements when the source contains more than fits.
    /// </summary>
    /// <param name="source">The source array to copy from. Must not be <see langword="null"/>.</param>
    /// <param name="sourceLength">The number of elements in <paramref name="source"/> to consider.</param>
    /// <param name="capacity">The capacity of the new backing array. Must be greater than zero.</param>
    /// <remarks>Does not bump <see cref="_version"/>; intended for one-shot construction from a constructor.</remarks>
    public void InitializeFrom(T[] source, int sourceLength, int capacity)
    {
        _array = new T[capacity];

        if (sourceLength > capacity)
        {
            System.Array.Copy(source, sourceLength - capacity, _array, 0, capacity);
            _count = capacity;
        }
        else
        {
            System.Array.Copy(source, _array, sourceLength);
            _count = sourceLength;
        }

        _head = 0;
        _tail = _count == capacity ? 0 : _count;
    }
}
