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
/// Represents a double-ended queue (deque) backed by a contiguous array. Elements may be added or removed from
/// either end in amortised O(1) time. The <see cref="AllowGrow"/> property controls whether the deque expands
/// the backing array on demand or rejects further inserts once it reaches <see cref="RingBackedCollection{T}.Capacity"/>.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the deque.</typeparam>
/// <remarks>
/// <para>
/// When <see cref="AllowGrow"/> is <see langword="true"/> (the default), the backing array doubles automatically
/// whenever <see cref="AddFirst(T)"/> or <see cref="AddLast(T)"/> would otherwise overflow, capped at
/// <see cref="Array.MaxLength"/>. When <see cref="AllowGrow"/> is <see langword="false"/>, the deque behaves as a
/// fixed-capacity buffer: add operations throw <see cref="InvalidOperationException"/> when full, and
/// <see cref="TryAddFirst(T)"/> / <see cref="TryAddLast(T)"/> return <see langword="false"/>.
/// </para>
/// <para>
/// Use <see cref="CircularBuffer{T}"/> for a single-ended FIFO buffer with eviction-on-full semantics.
/// </para>
/// <para>
/// This type is not thread-safe. Concurrent reads and writes from multiple threads require external synchronization.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, AllowGrow = {AllowGrow}")]
[DebuggerTypeProxy(typeof(DequeDebugView<>))]
[Serializable]
public sealed class Deque<T> : DequeBase<T>
{
    private const int DefaultCapacity = 16;
    private const int MinGrowCapacity = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class with the default initial capacity and
    /// auto-grow enabled.
    /// </summary>
    public Deque()
        : this(DefaultCapacity, allowGrow: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class with the specified initial capacity and
    /// auto-grow enabled.
    /// </summary>
    /// <param name="capacity">The initial capacity (or capacity hint when <see cref="AllowGrow"/> is <see langword="true"/>). Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    public Deque(int capacity)
        : this(capacity, allowGrow: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class with the specified capacity and growth policy.
    /// </summary>
    /// <param name="capacity">The initial backing-array capacity. Must be greater than zero.</param>
    /// <param name="allowGrow">
    /// <see langword="true"/> to allow the deque to expand its backing array when full;
    /// <see langword="false"/> to throw <see cref="InvalidOperationException"/> from <see cref="AddFirst(T)"/>
    /// and <see cref="AddLast(T)"/> (and return <see langword="false"/> from their <c>Try*</c> variants) when full.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    public Deque(int capacity, bool allowGrow)
        : base(capacity)
    {
        AllowGrow = allowGrow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class containing elements copied from
    /// <paramref name="collection"/>, sized to fit them, with auto-grow enabled.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public Deque(IEnumerable<T> collection)
        : this(Materialize(collection), capacity: UseFloorCapacity, allowGrow: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class containing elements copied from
    /// <paramref name="collection"/>, with the specified capacity. Auto-grow is enabled.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <param name="capacity">The initial backing-array capacity. Must be greater than zero, and at least <c>collection.Count</c> when auto-grow is disabled.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    public Deque(IEnumerable<T> collection, int capacity)
        : this(Materialize(collection), capacity, allowGrow: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class containing elements copied from
    /// <paramref name="collection"/>, with the specified capacity and growth policy.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <param name="capacity">The initial backing-array capacity. Must be greater than zero, and at least <c>collection.Count</c> when <paramref name="allowGrow"/> is <see langword="false"/>.</param>
    /// <param name="allowGrow">
    /// <see langword="true"/> to allow the deque to expand its backing array when full; <see langword="false"/> for fixed-capacity behaviour.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="allowGrow"/> is <see langword="false"/> and <paramref name="collection"/> contains more
    /// elements than <paramref name="capacity"/>.
    /// </exception>
    public Deque(IEnumerable<T> collection, int capacity, bool allowGrow)
        : this(Materialize(collection), capacity, allowGrow) { }

    /// <summary>
    /// Sentinel value passed to the private ctor to indicate that the (IEnumerable) overload should derive its
    /// capacity as <c>max(items.Length, DefaultCapacity)</c>.
    /// </summary>
    private const int UseFloorCapacity = -1;

    /// <summary>
    /// Single private ctor that drives all IEnumerable-based public overloads from one materialised array.
    /// Validates the no-grow overflow contract before delegating to the base, and bumps the capacity to fit
    /// when growth is allowed so the base ctor never truncates.
    /// </summary>
    /// <param name="items">The materialised source elements; never <see langword="null"/>.</param>
    /// <param name="capacity">The requested capacity, or <see cref="UseFloorCapacity"/> for the bare-IEnumerable overload.</param>
    /// <param name="allowGrow">Whether subsequent adds should auto-grow.</param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="allowGrow"/> is <see langword="false"/>, an explicit capacity was supplied, and
    /// <paramref name="items"/> contains more elements than the capacity.
    /// </exception>
    private Deque(T[] items, int capacity, bool allowGrow)
        : base(ValidateItems(items, capacity, allowGrow), ResolveCapacity(items, capacity, allowGrow))
    {
        AllowGrow = allowGrow;
    }

    /// <summary>
    /// Throws when <paramref name="allowGrow"/> is <see langword="false"/> and the supplied collection size
    /// exceeds the explicit <paramref name="capacity"/>. Returns the items unchanged on success.
    /// </summary>
    /// <param name="items">The materialised source elements.</param>
    /// <param name="capacity">The requested capacity, or <see cref="UseFloorCapacity"/>.</param>
    /// <param name="allowGrow">Whether the deque is permitted to grow.</param>
    /// <returns><paramref name="items"/>, unchanged.</returns>
    /// <exception cref="InvalidOperationException">Source overflow on a fixed-capacity construction.</exception>
    private static T[] ValidateItems(T[] items, int capacity, bool allowGrow)
    {
        if (!allowGrow && capacity != UseFloorCapacity && items.Length > capacity)
            throw new InvalidOperationException(ResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity);

        return items;
    }

    /// <summary>
    /// Resolves the actual capacity passed to the base ctor. When growth is allowed and the source exceeds the
    /// requested capacity, returns the source length so the base never truncates; otherwise returns the
    /// requested capacity.
    /// </summary>
    /// <param name="items">The materialised source elements.</param>
    /// <param name="capacity">The requested capacity, or <see cref="UseFloorCapacity"/>.</param>
    /// <param name="allowGrow">Whether the deque is permitted to grow.</param>
    /// <returns>The capacity to forward to the base constructor.</returns>
    private static int ResolveCapacity(T[] items, int capacity, bool allowGrow)
    {
        if (capacity == UseFloorCapacity)
            return Math.Max(items.Length, DefaultCapacity);

        if (allowGrow && items.Length > capacity)
            return items.Length;

        return capacity;
    }

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

    /// <summary>
    /// Gets or sets a value indicating whether the deque expands its backing array when an add operation would
    /// overflow the current capacity.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to grow on demand; <see langword="false"/> to throw
    /// <see cref="InvalidOperationException"/> from <see cref="AddFirst(T)"/> and <see cref="AddLast(T)"/> and
    /// return <see langword="false"/> from their <c>Try*</c> variants once full.
    /// </value>
    /// <returns>The current growth policy.</returns>
    /// <remarks>
    /// This property may be toggled at runtime to switch the deque between fixed and growable modes. Switching
    /// from <see langword="true"/> to <see langword="false"/> does not shrink the existing backing array; call
    /// <see cref="RingBackedCollection{T}.TrimExcess"/> if a smaller footprint is desired.
    /// </remarks>
    public bool AllowGrow { get; set; }

    /// <summary>
    /// Gets a value indicating whether the deque has reached its current backing-array capacity.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="RingBackedCollection{T}.Count"/> equals
    /// <see cref="RingBackedCollection{T}.Capacity"/>.
    /// </value>
    /// <returns><see langword="true"/> when full at the moment of the call.</returns>
    /// <remarks>
    /// On a growable deque this is a transient state — the next <see cref="AddFirst(T)"/> or
    /// <see cref="AddLast(T)"/> triggers a resize. On a fixed-capacity deque it indicates that subsequent adds
    /// will throw or return <see langword="false"/>.
    /// </remarks>
    public bool IsFull => Count == Capacity;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// <see cref="AllowGrow"/> is <see langword="false"/> and the deque is already at capacity.
    /// </exception>
    public override void AddFirst(T item)
    {
        if (Count == Capacity)
        {
            if (!AllowGrow)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

            Grow(Count + 1);
        }

        AddHead(item);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// <see cref="AllowGrow"/> is <see langword="false"/> and the deque is already at capacity.
    /// </exception>
    public override void AddLast(T item)
    {
        if (Count == Capacity)
        {
            if (!AllowGrow)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

            Grow(Count + 1);
        }

        AddTail(item);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see langword="true"/> after auto-growing when <see cref="AllowGrow"/> is <see langword="true"/>.
    /// Returns <see langword="false"/> without modifying state when <see cref="AllowGrow"/> is
    /// <see langword="false"/> and the deque is full.
    /// </remarks>
    public override bool TryAddFirst(T item)
    {
        if (Count == Capacity)
        {
            if (!AllowGrow)
                return false;

            Grow(Count + 1);
        }

        AddHead(item);
        return true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see langword="true"/> after auto-growing when <see cref="AllowGrow"/> is <see langword="true"/>.
    /// Returns <see langword="false"/> without modifying state when <see cref="AllowGrow"/> is
    /// <see langword="false"/> and the deque is full.
    /// </remarks>
    public override bool TryAddLast(T item)
    {
        if (Count == Capacity)
        {
            if (!AllowGrow)
                return false;

            Grow(Count + 1);
        }

        AddTail(item);
        return true;
    }

    /// <summary>
    /// Ensures that the deque can hold at least <paramref name="capacity"/> elements without further growth,
    /// expanding the backing array if necessary. Available regardless of <see cref="AllowGrow"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity required. Must be non-negative.</param>
    /// <returns>The new capacity of the backing array (which may exceed <paramref name="capacity"/>).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    /// <remarks>
    /// This method ignores <see cref="AllowGrow"/> — it is the explicit pre-grow hatch even on fixed-capacity
    /// deques. Use it to reserve space ahead of a known burst of inserts.
    /// </remarks>
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
