// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Deque.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a double-ended queue (deque) backed by a contiguous circular array. Elements may be added or
/// removed from either end in amortized O(1) time. The <see cref="AllowGrow"/> property selects between
/// growable and fixed-capacity behavior at runtime.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the deque.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Deque{T}"/> stores its elements in a single backing array using head and tail indices that wrap
/// around modulo the capacity. This gives O(1) amortized cost for adds and removes at either end, plus O(1)
/// random read access through the indexer in head-to-tail logical order.
/// </para>
/// <para>The growth policy is controlled by the mutable <see cref="AllowGrow"/> property:</para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>AllowGrow = true</c> (the default) — the backing array doubles automatically whenever
/// <see cref="AddFirst(T)"/> or <see cref="AddLast(T)"/> would otherwise overflow, capped at
/// <see cref="Array.MaxLength"/>. <see cref="TryAddFirst(T)"/> and <see cref="TryAddLast(T)"/> always return
/// <see langword="true"/>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>AllowGrow = false</c> — the deque is fixed at its current capacity. <see cref="AddFirst(T)"/> and
/// <see cref="AddLast(T)"/> throw <see cref="InvalidOperationException"/> when full;
/// <see cref="TryAddFirst(T)"/> and <see cref="TryAddLast(T)"/> return <see langword="false"/> without
/// modifying state.
/// </description>
/// </item>
/// </list>
/// <para>Key operations:</para>
/// <list type="bullet">
/// <item><description><see cref="AddFirst(T)"/> / <see cref="AddLast(T)"/> — push at either end (with <see cref="TryAddFirst(T)"/> / <see cref="TryAddLast(T)"/> non-throwing variants).</description></item>
/// <item><description>Inherited <c>RemoveFirst</c> / <c>RemoveLast</c> — pop and return the head or tail element.</description></item>
/// <item><description>Inherited <c>PeekFirst</c> / <c>PeekLast</c> — read the head or tail element without removing it.</description></item>
/// <item><description><see cref="EnsureCapacity(int)"/> — pre-grow the backing array even when <see cref="AllowGrow"/> is <see langword="false"/>.</description></item>
/// <item><description>Inherited <see cref="RingBackedCollection{T}.TrimExcess"/> — shrink the backing array to <c>Count</c> after a burst of removes.</description></item>
/// </list>
/// <para>
/// <see cref="AllowGrow"/> can be toggled at runtime to switch the deque between modes. Switching from
/// <see langword="true"/> to <see langword="false"/> does not shrink the existing capacity — call
/// <see cref="RingBackedCollection{T}.TrimExcess"/> afterwards if a smaller footprint is wanted.
/// </para>
/// <para>
/// For a single-ended FIFO buffer with eviction-on-full semantics, see <see cref="CircularBuffer{T}"/>.
/// For thread-safe concurrent FIFO access, see
/// <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}"/>. <see cref="Deque{T}"/>
/// itself is not thread-safe; concurrent reads and writes require external synchronization.
/// </para>
/// <para>
/// <see cref="Deque{T}"/> accepts <see langword="null"/> values for reference types and allows duplicate elements.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Growable double-ended queue (the default)
/// var deque = new Deque<int>();
/// deque.AddLast(2);
/// deque.AddFirst(1);
/// deque.AddLast(3);          // contents: 1, 2, 3
/// int head = deque.RemoveFirst();   // 1
///
/// // Fixed-capacity queue: rejects adds when full
/// var bounded = new Deque<int>(capacity: 8, allowGrow: false);
/// for (int i = 0; i < 8; i++) bounded.AddLast(i);
/// bool added = bounded.TryAddLast(8); // false — bounded is full
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, AllowGrow = {AllowGrow}")]
[DebuggerTypeProxy(typeof(DequeDebugView<>))]
[Serializable]
public sealed class Deque<T> 
    : RingBackedCollection<T>
{
    private const int DefaultCapacity = 16;
    private const int MinGrowCapacity = 4;

    /// <summary>
    /// Sentinel value passed to the private ctor to indicate that the (IEnumerable) overload should derive its
    /// capacity as <c>max(items.Length, DefaultCapacity)</c>.
    /// </summary>
    private const int UseFloorCapacity = -1;

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
    /// <see langword="true"/> to allow the deque to expand its backing array when full; <see langword="false"/> for fixed-capacity behavior.
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
    /// Initializes a new instance of the <see cref="Deque{T}"/> class using a pre-materialized
    /// array of elements, with the specified capacity and growth policy.
    /// </summary>
    /// <param name="items">The source elements to populate the deque. Must not be <see langword="null"/>.</param>
    /// <param name="capacity">
    /// The initial backing-array capacity, or <see cref="UseFloorCapacity"/> to derive the capacity
    /// from <paramref name="items"/>.
    /// </param>
    /// <param name="allowGrow">
    /// <see langword="true"/> to allow the deque to expand its backing array when full;
    /// <see langword="false"/> for fixed-capacity behavior.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="allowGrow"/> is <see langword="false"/>, an explicit capacity was supplied, and
    /// <paramref name="items"/> contains more elements than <paramref name="capacity"/>.
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
    /// <param name="items">The materialized source elements.</param>
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
    /// <param name="items">The materialized source elements.</param>
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
    /// Materializes <paramref name="collection"/> into an array exactly once, performing the null-check.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null"/>.</param>
    /// <returns>The materialized array; never <see langword="null"/>.</returns>
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
    /// Adds <paramref name="item"/> to the head of the deque.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <exception cref="InvalidOperationException">
    /// <see cref="AllowGrow"/> is <see langword="false"/> and the deque is already at capacity.
    /// </exception>
    public void AddFirst(T item)
    {
        if (Count == Capacity)
        {
            if (!AllowGrow)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

            Grow(Count + 1);
        }

        AddHead(item);
    }

    /// <summary>
    /// Adds <paramref name="item"/> to the tail of the deque.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <exception cref="InvalidOperationException">
    /// <see cref="AllowGrow"/> is <see langword="false"/> and the deque is already at capacity.
    /// </exception>
    public void AddLast(T item)
    {
        if (Count == Capacity)
        {
            if (!AllowGrow)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

            Grow(Count + 1);
        }

        AddTail(item);
    }

    /// <summary>
    /// Attempts to add <paramref name="item"/> to the head of the deque without throwing.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <returns>
    /// <see langword="true"/> if the item was added; <see langword="false"/> if the deque was fixed-capacity
    /// and full.
    /// </returns>
    /// <remarks>
    /// Returns <see langword="true"/> after auto-growing when <see cref="AllowGrow"/> is <see langword="true"/>.
    /// Returns <see langword="false"/> without modifying state when <see cref="AllowGrow"/> is
    /// <see langword="false"/> and the deque is full.
    /// </remarks>
    public bool TryAddFirst(T item)
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

    /// <summary>
    /// Attempts to add <paramref name="item"/> to the tail of the deque without throwing.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <returns>
    /// <see langword="true"/> if the item was added; <see langword="false"/> if the deque was fixed-capacity
    /// and full.
    /// </returns>
    /// <remarks>
    /// Returns <see langword="true"/> after auto-growing when <see cref="AllowGrow"/> is <see langword="true"/>.
    /// Returns <see langword="false"/> without modifying state when <see cref="AllowGrow"/> is
    /// <see langword="false"/> and the deque is full.
    /// </remarks>
    public bool TryAddLast(T item)
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
    /// Removes and returns the head element.
    /// </summary>
    /// <returns>The element that was at the head.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T RemoveFirst()
    {
        if (Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_EmptySequence);

        return RemoveHead();
    }

    /// <summary>
    /// Removes and returns the tail element.
    /// </summary>
    /// <returns>The element that was at the tail.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T RemoveLast()
    {
        if (Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_EmptySequence);

        return RemoveTail();
    }

    /// <summary>
    /// Attempts to remove and return the head element without throwing when empty.
    /// </summary>
    /// <param name="item">When this method returns, contains the removed element if successful.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise <see langword="false"/>.</returns>
    public bool TryRemoveFirst(out T item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = RemoveHead();
        return true;
    }

    /// <summary>
    /// Attempts to remove and return the tail element without throwing when empty.
    /// </summary>
    /// <param name="item">When this method returns, contains the removed element if successful.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise <see langword="false"/>.</returns>
    public bool TryRemoveLast(out T item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = RemoveTail();
        return true;
    }

    /// <summary>
    /// Returns the head element without removing it.
    /// </summary>
    /// <returns>The current head element.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T PeekFirst()
    {
        if (Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);

        return PeekHead();
    }

    /// <summary>
    /// Returns the tail element without removing it.
    /// </summary>
    /// <returns>The current tail element.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T PeekLast()
    {
        if (Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);

        return PeekTail();
    }

    /// <summary>
    /// Attempts to read the head element without throwing when empty.
    /// </summary>
    /// <param name="item">When this method returns, contains the head element if available.</param>
    /// <returns><see langword="true"/> if the head was read; otherwise <see langword="false"/>.</returns>
    public bool TryPeekFirst(out T item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = PeekHead();
        return true;
    }

    /// <summary>
    /// Attempts to read the tail element without throwing when empty.
    /// </summary>
    /// <param name="item">When this method returns, contains the tail element if available.</param>
    /// <returns><see langword="true"/> if the tail was read; otherwise <see langword="false"/>.</returns>
    public bool TryPeekLast(out T item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = PeekTail();
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
        var doubled = Math.Max(MinGrowCapacity, Capacity * 2);
        var newCapacity = Math.Max(minCapacity, doubled);

        if ((uint)newCapacity > (uint)Array.MaxLength)
            newCapacity = Array.MaxLength;

        Resize(newCapacity);
    }
}
