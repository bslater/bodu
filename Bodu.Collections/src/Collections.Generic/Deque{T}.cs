// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Deque{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a double-ended queue (deque) backed by a contiguous circular array. Elements may be added or removed from
/// either end in amortized O(1) time. The <see cref="AllowGrow" /> property selects between growable and fixed-capacity
/// behavior at runtime.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the deque.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Deque{T}" /> stores its elements in a single backing array using head and tail indices that wrap around
/// modulo the capacity. This gives O(1) amortized cost for adds and removes at either end, plus O(1) random read access
/// through the indexer in head-to-tail logical order.
/// </para>
/// <para>
/// The growth policy is controlled by the mutable <see cref="AllowGrow" /> property:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>AllowGrow = true</c> (the default) — the backing array doubles automatically whenever <see cref="AddFirst(T)" />
/// or <see cref="AddLast(T)" /> would otherwise overflow, capped at <see cref="Array.MaxLength" />.
/// <see cref="TryAddFirst(T)" /> and <see cref="TryAddLast(T)" /> always return <see langword="true" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>AllowGrow = false</c> — the deque is fixed at its current capacity and <see cref="OverflowPolicy" /> selects the
/// behavior of adds on a full deque. Under <see cref="DequeOverflowPolicy.Reject" /> (the default),
/// <see cref="AddFirst(T)" /> and <see cref="AddLast(T)" /> throw <see cref="InvalidOperationException" /> when full
/// while <see cref="TryAddFirst(T)" /> and <see cref="TryAddLast(T)" /> return <see langword="false" /> without
/// modifying state. Under <see cref="DequeOverflowPolicy.EvictOpposite" />, adds silently discard the element at the
/// opposite end to make room — the Python <c>deque(maxlen=N)</c> semantics, analogous to
/// <see cref="CircularBuffer{T}.AllowOverwrite" /> — raising <see cref="ItemEvicting" /> before and
/// <see cref="ItemEvicted" /> after each eviction.
/// </description>
/// </item>
/// </list>
/// <para>
/// Key operations:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="AddFirst(T)" /> / <see cref="AddLast(T)" /> — push at either end (with <see cref="TryAddFirst(T)" /> /
/// <see cref="TryAddLast(T)" /> non-throwing variants).
/// </description>
/// </item>
/// <item>
/// <description>
/// Inherited <c>RemoveFirst</c> / <c>RemoveLast</c> — pop and return the head or tail element.
/// </description>
/// </item>
/// <item>
/// <description>
/// Inherited <c>PeekFirst</c> / <c>PeekLast</c> — read the head or tail element without removing it.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="EnsureCapacity(int)" /> — pre-grow the backing array even when <see cref="AllowGrow" /> is
/// <see langword="false" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// Inherited <see cref="RingBackedCollection{T}.TrimExcess" /> — shrink the backing array to <c>Count</c> after a burst
/// of removes.
/// </description>
/// </item>
/// </list>
/// <para>
/// <see cref="AllowGrow" /> can be toggled at runtime to switch the deque between modes. Switching from
/// <see langword="true" /> to <see langword="false" /> does not shrink the existing capacity — call
/// <see cref="RingBackedCollection{T}.TrimExcess" /> afterwards if a smaller footprint is wanted.
/// </para>
/// <para>
/// For a single-ended FIFO buffer with eviction-on-full semantics, see <see cref="CircularBuffer{T}" />. For
/// thread-safe concurrent FIFO access, see
/// <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}" />. <see cref="Deque{T}" /> itself is
/// not thread-safe; concurrent reads and writes require external synchronization.
/// </para>
/// <para>
/// <see cref="Deque{T}" /> accepts <see langword="null" /> values for reference types and allows duplicate elements.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Growable double-ended queue (the default).
/// var deque = new Deque<int>();
/// deque.AddLast(2);
/// deque.AddFirst(1);
/// deque.AddLast(3);                  // contents: 1, 2, 3
/// int head = deque.RemoveFirst();    // 1
///
/// // Fixed-capacity queue: rejects adds when full.
/// var bounded = new Deque<int>(capacity: 8, allowGrow: false);
/// for (int i = 0; i < 8; i++) bounded.AddLast(i);
/// bool added = bounded.TryAddLast(8); // false — bounded is full
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, AllowGrow = {AllowGrow}")]
[DebuggerTypeProxy(typeof(DequeDebugView<>))]
[Serializable]
public sealed class Deque<T>
    : RingBackedCollection<T>
{
    /// <summary>The capacity used when the deque is constructed without an explicit capacity.</summary>
    private const int DefaultCapacity = 16;

    /// <summary>The smallest capacity a growable deque expands to when its initial capacity is below this floor.</summary>
    private const int MinGrowCapacity = 4;

    /// <summary>Sentinel value passed to the private ctor to indicate that the (IEnumerable) overload should derive its capacity as <c>max(items.Length, DefaultCapacity)</c>.</summary>
    private const int UseFloorCapacity = -1;

    /// <summary>The policy applied when an add targets a full, fixed-capacity deque. Defaults to <see cref="DequeOverflowPolicy.Reject" />.</summary>
    private DequeOverflowPolicy _overflowPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}" /> class with the default initial capacity and auto-grow
    /// enabled.
    /// </summary>
    public Deque()
        : this(DefaultCapacity, allowGrow: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}" /> class with the specified initial capacity and
    /// auto-grow enabled.
    /// </summary>
    /// <param name="capacity">
    /// The initial capacity (or capacity hint when <see cref="AllowGrow" /> is <see langword="true" />). Must be
    /// greater than zero.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than 1.</exception>
    public Deque(int capacity)
        : this(capacity, allowGrow: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}" /> class with the specified capacity and growth policy.
    /// </summary>
    /// <param name="capacity">The initial backing-array capacity. Must be greater than zero.</param>
    /// <param name="allowGrow">
    /// <see langword="true" /> to allow the deque to expand its backing array when full; <see langword="false" /> to
    /// throw <see cref="InvalidOperationException" /> from <see cref="AddFirst(T)" /> and <see cref="AddLast(T)" />
    /// (and return <see langword="false" /> from their <c>Try*</c> variants) when full.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than 1.</exception>
    public Deque(int capacity, bool allowGrow)
        : base(capacity)
    {
        AllowGrow = allowGrow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}" /> class containing elements copied from
    /// <paramref name="collection" />, sized to fit them, with auto-grow enabled.
    /// </summary>
    /// <param name="collection">
    /// The collection from which elements are copied. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public Deque(IEnumerable<T> collection)
        : this(Materialize(collection), capacity: UseFloorCapacity, allowGrow: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}" /> class containing elements copied from
    /// <paramref name="collection" />, with the specified capacity. Auto-grow is enabled.
    /// </summary>
    /// <param name="collection">
    /// The collection from which elements are copied. Must not be <see langword="null" />.
    /// </param>
    /// <param name="capacity">
    /// The initial backing-array capacity. Must be greater than zero, and at least <c>collection.Count</c> when
    /// auto-grow is disabled.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than 1.</exception>
    public Deque(IEnumerable<T> collection, int capacity)
        : this(Materialize(collection), capacity, allowGrow: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}" /> class containing elements copied from
    /// <paramref name="collection" />, with the specified capacity and growth policy.
    /// </summary>
    /// <param name="collection">
    /// The collection from which elements are copied. Must not be <see langword="null" />.
    /// </param>
    /// <param name="capacity">
    /// The initial backing-array capacity. Must be greater than zero, and at least <c>collection.Count</c> when
    /// <paramref name="allowGrow" /> is <see langword="false" />.
    /// </param>
    /// <param name="allowGrow">
    /// <see langword="true" /> to allow the deque to expand its backing array when full; <see langword="false" /> for
    /// fixed-capacity behavior.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="allowGrow" /> is <see langword="false" /> and <paramref name="collection" /> contains more
    /// elements than <paramref name="capacity" />.
    /// </exception>
    public Deque(IEnumerable<T> collection, int capacity, bool allowGrow)
        : this(Materialize(collection), capacity, allowGrow) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}" /> class using a pre-materialized array of elements, with
    /// the specified capacity and growth policy.
    /// </summary>
    /// <param name="items">The source elements to populate the deque. Must not be <see langword="null" />.</param>
    /// <param name="capacity">
    /// The initial backing-array capacity, or <see cref="UseFloorCapacity" /> to derive the capacity from
    /// <paramref name="items" />.
    /// </param>
    /// <param name="allowGrow">
    /// <see langword="true" /> to allow the deque to expand its backing array when full; <see langword="false" /> for
    /// fixed-capacity behavior.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="allowGrow" /> is <see langword="false" />, an explicit capacity was supplied, and
    /// <paramref name="items" /> contains more elements than <paramref name="capacity" />.
    /// </exception>
    private Deque(T[] items, int capacity, bool allowGrow)
        : base(ValidateItems(items, capacity, allowGrow), ResolveCapacity(items, capacity, allowGrow))
    {
        AllowGrow = allowGrow;
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an item has been evicted from the opposite end of the <see cref="Deque{T}" /> to
    /// make room for a new element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised only when the deque is full, <see cref="AllowGrow" /> is <see langword="false" />, and
    /// <see cref="OverflowPolicy" /> is <see cref="DequeOverflowPolicy.EvictOpposite" />.
    /// </para>
    /// <para>
    /// <b>Important:</b> Exceptions thrown by event handlers are not caught and will propagate to the caller of
    /// <see cref="AddFirst(T)" />, <see cref="AddLast(T)" />, <see cref="TryAddFirst(T)" />, or
    /// <see cref="TryAddLast(T)" />. Consumers should ensure event handlers are exception-safe.
    /// </para>
    /// </remarks>
    public event Action<T>? ItemEvicted;

    /// <summary>
    /// Occurs immediately <b>before</b> an item is evicted from the opposite end of the <see cref="Deque{T}" /> to make
    /// room for a new element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised only when the deque is full, <see cref="AllowGrow" /> is <see langword="false" />, and
    /// <see cref="OverflowPolicy" /> is <see cref="DequeOverflowPolicy.EvictOpposite" />.
    /// </para>
    /// <para>
    /// <b>Important:</b> Any exception thrown from a handler vetoes the eviction in place — the opposite-end element is
    /// not removed, the new element is not stored, the count, head, and tail indices are unchanged, and the exception
    /// propagates to the caller of <see cref="AddFirst(T)" />, <see cref="AddLast(T)" />, <see cref="TryAddFirst(T)" />,
    /// or <see cref="TryAddLast(T)" />. Event handlers should therefore avoid throwing unless the veto is intentional.
    /// </para>
    /// </remarks>
    public event Action<T>? ItemEvicting;

    /// <summary>
    /// Gets or sets a value indicating whether the deque expands its backing array when an add operation would overflow
    /// the current capacity.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to grow on demand; <see langword="false" /> to apply <see cref="OverflowPolicy" /> once
    /// full — by default rejecting the add (throw from <see cref="AddFirst(T)" /> and <see cref="AddLast(T)" />,
    /// <see langword="false" /> from their <c>Try*</c> variants).
    /// </value>
    /// <remarks>
    /// <para>
    /// This property may be toggled at runtime to switch the deque between fixed and growable modes. Switching from
    /// <see langword="true" /> to <see langword="false" /> does not shrink the existing backing array; call
    /// <see cref="RingBackedCollection{T}.TrimExcess" /> if a smaller footprint is desired.
    /// </para>
    /// <para>
    /// While this property is <see langword="true" />, growth always wins and <see cref="OverflowPolicy" /> is never
    /// consulted — no element is evicted regardless of the configured policy.
    /// </para>
    /// </remarks>
    public bool AllowGrow { get; set; }

    /// <summary>
    /// Gets or sets the policy applied when an add operation targets a full, fixed-capacity deque.
    /// </summary>
    /// <value>
    /// One of the <see cref="DequeOverflowPolicy" /> values. The default is <see cref="DequeOverflowPolicy.Reject" />,
    /// which preserves the historical throw / return-<see langword="false" /> behavior.
    /// </value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The assigned value is not a defined <see cref="DequeOverflowPolicy" /> member.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The policy is consulted only when <see cref="AllowGrow" /> is <see langword="false" /> and the deque is full.
    /// When <see cref="AllowGrow" /> is <see langword="true" /> the backing array grows instead — growth always wins,
    /// rendering the policy irrelevant on a growable deque.
    /// </para>
    /// <para>
    /// Under <see cref="DequeOverflowPolicy.EvictOpposite" />, <see cref="AddFirst(T)" /> discards the tail element and
    /// <see cref="AddLast(T)" /> discards the head element before storing the new item, keeping
    /// <see cref="RingBackedCollection{T}.Count" /> at <see cref="RingBackedCollection{T}.Capacity" /> — the Python
    /// <c>deque(maxlen=N)</c> semantics. Each eviction raises <see cref="ItemEvicting" /> beforehand and
    /// <see cref="ItemEvicted" /> afterwards.
    /// </para>
    /// </remarks>
    public DequeOverflowPolicy OverflowPolicy
    {
        get => _overflowPolicy;
        set
        {
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);

            _overflowPolicy = value;
        }
    }

    /// <summary>
    /// Throws when <paramref name="allowGrow" /> is <see langword="false" /> and the supplied collection size exceeds
    /// the explicit <paramref name="capacity" />. Returns the items unchanged on success.
    /// </summary>
    /// <param name="items">The materialized source elements.</param>
    /// <param name="capacity">The requested capacity, or <see cref="UseFloorCapacity" />.</param>
    /// <param name="allowGrow">Whether the deque is permitted to grow.</param>
    /// <returns><paramref name="items" />, unchanged.</returns>
    /// <exception cref="InvalidOperationException">Source overflow on a fixed-capacity construction.</exception>
    private static T[] ValidateItems(T[] items, int capacity, bool allowGrow)
    {
        return !allowGrow && capacity != UseFloorCapacity && items.Length > capacity
            ? throw new InvalidOperationException(CollectionsResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity)
            : items;
    }

    /// <summary>
    /// Resolves the actual capacity passed to the base ctor. When growth is allowed and the source exceeds the
    /// requested capacity, returns the source length so the base never truncates; otherwise returns the requested
    /// capacity.
    /// </summary>
    /// <param name="items">The materialized source elements.</param>
    /// <param name="capacity">The requested capacity, or <see cref="UseFloorCapacity" />.</param>
    /// <param name="allowGrow">Whether the deque is permitted to grow.</param>
    /// <returns>The capacity to forward to the base constructor.</returns>
    private static int ResolveCapacity(T[] items, int capacity, bool allowGrow) =>
        capacity == UseFloorCapacity
            ? Math.Max(items.Length, DefaultCapacity)
            : allowGrow && items.Length > capacity ? items.Length : capacity;

    /// <summary>
    /// Materializes <paramref name="collection" /> into an array exactly once, performing the null-check.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <returns>The materialized array; never <see langword="null" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    private static T[] Materialize(IEnumerable<T> collection)
    {
        ThrowHelper.ThrowIfNull(collection);

        return collection as T[] ?? [.. collection];
    }

    /// <summary>
    /// Adds <paramref name="item" /> to the head of the deque.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null" /> for reference types.</param>
    /// <exception cref="InvalidOperationException">
    /// <see cref="AllowGrow" /> is <see langword="false" />, <see cref="OverflowPolicy" /> is
    /// <see cref="DequeOverflowPolicy.Reject" />, and the deque is already at capacity.
    /// </exception>
    /// <remarks>
    /// On a full, fixed-capacity deque with <see cref="OverflowPolicy" /> set to
    /// <see cref="DequeOverflowPolicy.EvictOpposite" />, the tail element is silently discarded (raising
    /// <see cref="ItemEvicting" /> and <see cref="ItemEvicted" />) and the new item is stored at the head, leaving
    /// <see cref="RingBackedCollection{T}.Count" /> unchanged.
    /// </remarks>
    public void AddFirst(T item) => _ = TryAddInternal(item, atHead: true, throwIfFull: true);

    /// <summary>
    /// Adds <paramref name="item" /> to the tail of the deque.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null" /> for reference types.</param>
    /// <exception cref="InvalidOperationException">
    /// <see cref="AllowGrow" /> is <see langword="false" />, <see cref="OverflowPolicy" /> is
    /// <see cref="DequeOverflowPolicy.Reject" />, and the deque is already at capacity.
    /// </exception>
    /// <remarks>
    /// On a full, fixed-capacity deque with <see cref="OverflowPolicy" /> set to
    /// <see cref="DequeOverflowPolicy.EvictOpposite" />, the head element is silently discarded (raising
    /// <see cref="ItemEvicting" /> and <see cref="ItemEvicted" />) and the new item is stored at the tail, leaving
    /// <see cref="RingBackedCollection{T}.Count" /> unchanged.
    /// </remarks>
    public void AddLast(T item) => _ = TryAddInternal(item, atHead: false, throwIfFull: true);

    /// <summary>
    /// Attempts to add <paramref name="item" /> to the head of the deque without throwing.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null" /> for reference types.</param>
    /// <returns>
    /// <see langword="true" /> if the item was added (including after growing or evicting the tail element);
    /// <see langword="false" /> if the deque was fixed-capacity, full, and <see cref="OverflowPolicy" /> is
    /// <see cref="DequeOverflowPolicy.Reject" />.
    /// </returns>
    /// <remarks>
    /// Returns <see langword="true" /> after auto-growing when <see cref="AllowGrow" /> is <see langword="true" />, and
    /// after evicting the tail element when <see cref="OverflowPolicy" /> is
    /// <see cref="DequeOverflowPolicy.EvictOpposite" /> on a full, fixed-capacity deque. Returns
    /// <see langword="false" /> without modifying state only when the deque is full, <see cref="AllowGrow" /> is
    /// <see langword="false" />, and <see cref="OverflowPolicy" /> is <see cref="DequeOverflowPolicy.Reject" />.
    /// </remarks>
    public bool TryAddFirst(T item) => TryAddInternal(item, atHead: true, throwIfFull: false);

    /// <summary>
    /// Attempts to add <paramref name="item" /> to the tail of the deque without throwing.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null" /> for reference types.</param>
    /// <returns>
    /// <see langword="true" /> if the item was added (including after growing or evicting the head element);
    /// <see langword="false" /> if the deque was fixed-capacity, full, and <see cref="OverflowPolicy" /> is
    /// <see cref="DequeOverflowPolicy.Reject" />.
    /// </returns>
    /// <remarks>
    /// Returns <see langword="true" /> after auto-growing when <see cref="AllowGrow" /> is <see langword="true" />, and
    /// after evicting the head element when <see cref="OverflowPolicy" /> is
    /// <see cref="DequeOverflowPolicy.EvictOpposite" /> on a full, fixed-capacity deque. Returns
    /// <see langword="false" /> without modifying state only when the deque is full, <see cref="AllowGrow" /> is
    /// <see langword="false" />, and <see cref="OverflowPolicy" /> is <see cref="DequeOverflowPolicy.Reject" />.
    /// </remarks>
    public bool TryAddLast(T item) => TryAddInternal(item, atHead: false, throwIfFull: false);

    /// <summary>
    /// Single internal add implementation shared by <see cref="AddFirst(T)" />, <see cref="AddLast(T)" />,
    /// <see cref="TryAddFirst(T)" />, and <see cref="TryAddLast(T)" />.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <param name="atHead">
    /// <see langword="true" /> to add at the head (evicting from the tail when applicable); <see langword="false" /> to
    /// add at the tail (evicting from the head when applicable).
    /// </param>
    /// <param name="throwIfFull">
    /// If <see langword="true" />, throws when the add is rejected on a full deque; otherwise returns
    /// <see langword="false" />.
    /// </param>
    /// <returns><see langword="true" /> when the item was added (including after growth or eviction).</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="throwIfFull" /> is <see langword="true" />, the deque is full, <see cref="AllowGrow" /> is
    /// <see langword="false" />, and <see cref="OverflowPolicy" /> is <see cref="DequeOverflowPolicy.Reject" />.
    /// </exception>
    private bool TryAddInternal(T item, bool atHead, bool throwIfFull)
    {
        if (Count == Capacity)
        {
            if (AllowGrow)
            {
                Grow(Count + 1);
            }
            else if (_overflowPolicy == DequeOverflowPolicy.EvictOpposite)
            {
                // Capture the opposite-end victim before raising ItemEvicting so a handler exception vetoes the
                // eviction in place — nothing is removed, the new element is not stored.
                T evicted = atHead ? PeekTail() : PeekHead();
                ItemEvicting?.Invoke(evicted);

                if (atHead)
                {
                    _ = RemoveTail();
                    AddHead(item);
                }
                else
                {
                    _ = RemoveHead();
                    AddTail(item);
                }

                ItemEvicted?.Invoke(evicted);
                return true;
            }
            else
            {
                return throwIfFull ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CapacityExhausted) : false;
            }
        }

        if (atHead)
        {
            AddHead(item);
        }
        else
        {
            AddTail(item);
        }

        return true;
    }

    /// <summary>
    /// Removes and returns the head element.
    /// </summary>
    /// <returns>The element that was at the head.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T RemoveFirst() => Count == 0 ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_EmptySequence) : RemoveHead();

    /// <summary>
    /// Removes and returns the tail element.
    /// </summary>
    /// <returns>The element that was at the tail.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T RemoveLast() => Count == 0 ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_EmptySequence) : RemoveTail();

    /// <summary>
    /// Attempts to remove and return the head element without throwing when empty.
    /// </summary>
    /// <param name="item">When this method returns, contains the removed element if successful.</param>
    /// <returns><see langword="true" /> if an element was removed; otherwise <see langword="false" />.</returns>
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
    /// <returns><see langword="true" /> if an element was removed; otherwise <see langword="false" />.</returns>
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
    public T PeekFirst() => Count == 0 ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionEmpty) : PeekHead();

    /// <summary>
    /// Returns the tail element without removing it.
    /// </summary>
    /// <returns>The current tail element.</returns>
    /// <exception cref="InvalidOperationException">The deque is empty.</exception>
    public T PeekLast() => Count == 0 ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionEmpty) : PeekTail();

    /// <summary>
    /// Attempts to read the head element without throwing when empty.
    /// </summary>
    /// <param name="item">When this method returns, contains the head element if available.</param>
    /// <returns><see langword="true" /> if the head was read; otherwise <see langword="false" />.</returns>
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
    /// <returns><see langword="true" /> if the tail was read; otherwise <see langword="false" />.</returns>
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
    /// Ensures that the deque can hold at least <paramref name="capacity" /> elements without further growth, expanding
    /// the backing array if necessary. Available regardless of <see cref="AllowGrow" />.
    /// </summary>
    /// <param name="capacity">The minimum capacity required. Must be non-negative.</param>
    /// <returns>The new capacity of the backing array (which may exceed <paramref name="capacity" />).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    /// <remarks>
    /// This method ignores <see cref="AllowGrow" /> — it is the explicit pre-grow hatch even on fixed-capacity deques.
    /// Use it to reserve space ahead of a known burst of inserts.
    /// </remarks>
    public int EnsureCapacity(int capacity)
    {
        ThrowHelper.ThrowIfNegative(capacity, nameof(capacity));

        if (capacity > Capacity)
            Grow(capacity);

        return Capacity;
    }

    /// <summary>
    /// Expands the backing array so that it holds at least <paramref name="minCapacity" /> elements. The new capacity
    /// is at least double the current capacity (with a small floor) and is capped at <see cref="Array.MaxLength" />.
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
