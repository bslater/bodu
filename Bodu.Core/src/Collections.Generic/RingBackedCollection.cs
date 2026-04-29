// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides the shared low-level mechanics for ring-buffer-backed collections — a contiguous backing array,
/// head/tail indices with modulo wrap, a live element count, and a structural-version counter — as a
/// reusable base type for <see cref="CircularBuffer{T}"/> and <see cref="Deque{T}"/> (via
/// <see cref="DequeBase{T}"/>).
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the collection.</typeparam>
/// <remarks>
/// <para>
/// <see cref="RingBackedCollection{T}"/> is intended as an extension point for new ring-backed collection
/// types. It owns the storage and exposes:
/// </para>
/// <list type="bullet">
/// <item><description>A read-only public surface for consumers — <see cref="Capacity"/>, <see cref="Count"/>, <see cref="IsEmpty"/>, the head-relative indexer, <see cref="Clear"/>, <see cref="Contains(T)"/>, <see cref="CopyTo(T[], int)"/>, <see cref="ToArray"/>, and <see cref="TrimExcess"/>.</description></item>
/// <item><description>The framework collection interfaces — <see cref="System.Collections.ICollection"/>, <see cref="System.Collections.Generic.IEnumerable{T}"/>, and <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>.</description></item>
/// <item><description>A single shared <see cref="Enumerator"/> nested struct that walks the live region in head-to-tail logical order and uses a structural-version token to detect concurrent modification.</description></item>
/// <item><description>Protected primitives for derived types — <see cref="AddTail(T)"/>, <see cref="AddHead(T)"/>, <see cref="RemoveHead"/>, <see cref="RemoveTail"/>, <see cref="PeekHead"/>, <see cref="PeekTail"/>, <see cref="OverwriteTail(T)"/>, and <see cref="Resize(int)"/>.</description></item>
/// </list>
/// <para>
/// The protected mutators perform no capacity or emptiness validation: derived classes must enforce those
/// contracts before calling. This keeps the hot path branch-free in the common cases. Each mutator bumps
/// the internal structural-version counter so any in-flight enumerators are correctly invalidated.
/// </para>
/// <para>
/// Concrete derived types layer their own public API on top of these primitives:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="CircularBuffer{T}"/> adds the single-ended <c>Enqueue</c> / <c>Dequeue</c> / <c>Peek</c> surface with an <c>AllowOverwrite</c> toggle and eviction events.</description></item>
/// <item><description><see cref="DequeBase{T}"/> (and its sealed <see cref="Deque{T}"/> implementation) adds the double-ended <c>AddFirst</c> / <c>AddLast</c> / <c>RemoveFirst</c> / <c>RemoveLast</c> / <c>PeekFirst</c> / <c>PeekLast</c> surface, with an <c>AllowGrow</c> toggle for fixed-vs-growable behaviour.</description></item>
/// </list>
/// <para>
/// This type is not thread-safe. For thread-safe single-ended FIFO access, use
/// <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}"/> — its lock-free Vyukov
/// implementation does not share storage with this hierarchy.
/// </para>
/// </remarks>
[Serializable]
public abstract partial class RingBackedCollection<T>
{
    /// <summary>The backing array. The active region wraps from <see cref="_head"/> through <see cref="_tail"/>.</summary>
    private T[] _array;

    /// <summary>The index of the first (oldest / head) element. Undefined when <see cref="_count"/> is zero.</summary>
    private int _head;

    /// <summary>The index at which the next tail-side write occurs (one past the last element, modulo capacity).</summary>
    private int _tail;

    /// <summary>The number of live elements currently held.</summary>
    private int _count;

    /// <summary>A monotonic counter incremented on every structural mutation; consumed by enumerators for invalidation.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="RingBackedCollection{T}"/> class with a backing array of
    /// the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial backing-array capacity. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    protected RingBackedCollection(int capacity)
    {
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, Array.MaxLength);

        _array = new T[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
        _version = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RingBackedCollection{T}"/> class containing elements
    /// copied from <paramref name="collection"/>, with the specified capacity. When the source is larger
    /// than the capacity, only the most recent <paramref name="capacity"/> elements are retained.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <param name="capacity">The backing-array capacity. Must be greater than zero.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    protected RingBackedCollection(IEnumerable<T> collection, int capacity)
    {
        ThrowHelper.ThrowIfNull(collection);
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, Array.MaxLength);

        T[] items = collection as T[] ?? collection.ToArray();

        _array = new T[capacity];
        if (items.Length > capacity)
        {
            Array.Copy(items, items.Length - capacity, _array, 0, capacity);
            _count = capacity;
        }
        else
        {
            Array.Copy(items, _array, items.Length);
            _count = items.Length;
        }

        _head = 0;
        _tail = _count == capacity ? 0 : _count;
        _version = 0;
    }

    /// <summary>
    /// Gets the maximum number of elements that the collection can currently hold without resizing the
    /// backing array.
    /// </summary>
    /// <value>The length of the underlying array.</value>
    /// <returns>The capacity.</returns>
    public int Capacity => _array.Length;

    /// <summary>
    /// Gets a value indicating whether the collection contains no elements.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="Count"/> is zero; otherwise, <see langword="false"/>.</value>
    /// <returns><see langword="true"/> when the collection is empty.</returns>
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// Gets a value indicating whether the collection has reached the current backing-array capacity. On a
    /// fixed-capacity buffer this signals that the next add will throw or be rejected; on a growable
    /// collection it is a transient state that the next add resolves by resizing.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="Count"/> equals <see cref="Capacity"/>.</value>
    /// <returns><see langword="true"/> when the collection is at capacity.</returns>
    public bool IsFull => _count == _array.Length;

    /// <summary>
    /// Gets the element at the specified zero-based logical index, where <c>0</c> refers to the head element.
    /// </summary>
    /// <param name="index">The zero-based, head-relative index. Must be in <c>[0, Count)</c>.</param>
    /// <returns>The element at the given logical position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is negative or not less than <see cref="Count"/>.
    /// </exception>
    public T this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfLessThan(index, 0);
            ThrowHelper.ThrowIfGreaterThanOrEqual(index, _count);

            return _array[(_head + index) % _array.Length];
        }
    }

    /// <summary>
    /// Removes all elements from the collection, clearing the live region of the backing array and resetting
    /// head, tail, and count. Bumps the structural version when something was cleared.
    /// </summary>
    public void Clear()
    {
        if (_count > 0)
        {
            int capacity = _array.Length;
            if (_head < _tail)
            {
                Array.Clear(_array, _head, _count);
            }
            else
            {
                Array.Clear(_array, _head, capacity - _head);
                Array.Clear(_array, 0, _tail);
            }

            _head = 0;
            _tail = 0;
            _count = 0;
            _version++;
        }
    }

    /// <summary>
    /// Determines whether the collection contains the specified element using
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    /// <param name="item">The element to locate. May be <see langword="null"/> for reference types.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T item)
    {
        if (_count == 0)
            return false;

        if (_head < _tail)
        {
            return Array.IndexOf(_array, item, _head, _count) >= 0;
        }

        int firstSegmentLength = _array.Length - _head;
        if (Array.IndexOf(_array, item, _head, firstSegmentLength) >= 0)
            return true;

        return _tail > 0 && Array.IndexOf(_array, item, 0, _tail) >= 0;
    }

    /// <summary>
    /// Copies the collection's elements to <paramref name="array"/> in head-to-tail logical order, starting
    /// at <paramref name="index"/>.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null"/>.</param>
    /// <param name="index">The zero-based starting index in <paramref name="array"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// The destination is too small to hold the contents starting at <paramref name="index"/>.
    /// </exception>
    public void CopyTo(T[] array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(index, nameof(index));
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + _count);

        CopyToInternal(array, index);
    }

    /// <summary>
    /// Returns a new array containing the collection's elements in head-to-tail logical order.
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
    /// Reduces the backing-array capacity to match <see cref="Count"/>, freeing unused memory. If the
    /// collection is empty, the capacity is reduced to one slot to keep operations valid.
    /// </summary>
    public void TrimExcess()
    {
        int newCapacity = Math.Max(_count, 1);
        if (newCapacity == _array.Length)
            return;

        Resize(newCapacity);
    }

    /// <summary>
    /// Adds <paramref name="item"/> at the tail position and advances the tail. The caller must ensure
    /// <c>Count &lt; Capacity</c> before invoking.
    /// </summary>
    /// <param name="item">The element to append.</param>
    protected void AddTail(T item)
    {
        _array[_tail] = item;
        _tail = (_tail + 1) % _array.Length;
        _count++;
        _version++;
    }

    /// <summary>
    /// Adds <paramref name="item"/> at the position immediately before the head and retreats the head. The
    /// caller must ensure <c>Count &lt; Capacity</c> before invoking.
    /// </summary>
    /// <param name="item">The element to prepend.</param>
    protected void AddHead(T item)
    {
        int capacity = _array.Length;
        _head = (_head - 1 + capacity) % capacity;
        _array[_head] = item;
        _count++;
        _version++;
    }

    /// <summary>
    /// Removes and returns the head element, clearing its slot. The caller must ensure
    /// <c>Count &gt; 0</c> before invoking.
    /// </summary>
    /// <returns>The element that was at the head.</returns>
    protected T RemoveHead()
    {
        T item = _array[_head];
        _array[_head] = default!;
        _head = (_head + 1) % _array.Length;
        _count--;
        _version++;
        return item;
    }

    /// <summary>
    /// Removes and returns the tail element, clearing its slot. The caller must ensure <c>Count &gt; 0</c>
    /// before invoking.
    /// </summary>
    /// <returns>The element that was at the tail.</returns>
    protected T RemoveTail()
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
    /// Returns the head element without removing it. The caller must ensure <c>Count &gt; 0</c>.
    /// </summary>
    /// <returns>The current head element.</returns>
    protected T PeekHead() =>
        _array[_head];

    /// <summary>
    /// Returns the tail element without removing it. The caller must ensure <c>Count &gt; 0</c>.
    /// </summary>
    /// <returns>The element immediately preceding <c>_tail</c> in modulo order.</returns>
    protected T PeekTail()
    {
        int capacity = _array.Length;
        int tailIndex = (_tail - 1 + capacity) % capacity;
        return _array[tailIndex];
    }

    /// <summary>
    /// Writes <paramref name="item"/> at the slot occupied by the tail (which equals the head when the
    /// collection is full) and advances both head and tail by one slot. Used by the eviction path of
    /// <see cref="CircularBuffer{T}"/> when overwriting the oldest element. <see cref="Count"/> is unchanged.
    /// </summary>
    /// <param name="item">The element to write into the slot occupied by the tail.</param>
    /// <remarks>The caller must ensure <c>Count == Capacity</c> before invoking.</remarks>
    protected void OverwriteTail(T item)
    {
        _array[_tail] = item;
        int next = (_tail + 1) % _array.Length;
        _head = next;
        _tail = next;
        _version++;
    }

    /// <summary>
    /// Replaces the backing array with one of size <paramref name="newCapacity"/>, copying the live region
    /// into the new array starting at index zero. Resets <see cref="_head"/> to zero and <see cref="_tail"/>
    /// to the position just after the last element.
    /// </summary>
    /// <param name="newCapacity">
    /// The new capacity. Must satisfy <c>newCapacity &gt;= Count</c> and <c>newCapacity &gt;= 1</c>.
    /// </param>
    protected void Resize(int newCapacity)
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
    /// Copies the live region into <paramref name="destination"/> starting at <paramref name="destinationIndex"/>,
    /// in head-to-tail logical order. Handles wrapped layouts using two segment copies. Internal so the
    /// non-generic <see cref="System.Collections.ICollection.CopyTo"/> implementation can reuse it.
    /// </summary>
    /// <param name="destination">The destination array. Must have sufficient space.</param>
    /// <param name="destinationIndex">The zero-based starting index in <paramref name="destination"/>.</param>
    private protected void CopyToInternal(Array destination, int destinationIndex)
    {
        if (_count == 0)
            return;

        if (_head < _tail)
        {
            Array.Copy(_array, _head, destination, destinationIndex, _count);
        }
        else
        {
            int firstSegmentLength = _array.Length - _head;
            Array.Copy(_array, _head, destination, destinationIndex, firstSegmentLength);
            Array.Copy(_array, 0, destination, destinationIndex + firstSegmentLength, _tail);
        }
    }
}
