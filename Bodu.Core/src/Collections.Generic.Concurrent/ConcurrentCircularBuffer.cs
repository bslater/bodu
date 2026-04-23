// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBuffer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a lock-free, bounded first-in, first-out (FIFO) buffer with optional overwrite semantics.
/// </summary>
/// <typeparam name="T">
/// The reference type stored in the buffer (constraint: <c>where T : class</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// This type implements a multi-producer/multi-consumer (MPMC) circular buffer using per-slot sequence numbers
/// (Vyukov pattern). The buffer capacity is fixed at construction time and must be at least 2.
/// </para>
/// <para>
/// The Vyukov MPMC sequence protocol uses two distinct sequence marks per slot: one written by the producer
/// when data is published (<c>tail + 1</c>), and one written by the consumer when the slot is released
/// (<c>head + capacity</c>). These marks must be numerically distinct so that concurrent producers can
/// determine whether a slot is free or still occupied. With a capacity of 1 they are always equal for every
/// round, making the two states indistinguishable and allowing a second concurrent producer to overwrite an
/// occupied slot, permanently skipping a sequence number and leaving consumers in an infinite spin. A minimum
/// capacity of 2 is therefore required for the protocol to be correct.
/// </para>
/// <para>
/// When <see cref="AllowOverwrite"/> is <see langword="true"/>, enqueuing into a full buffer evicts the oldest
/// element and raises the <see cref="ItemEvicted"/> event (after removal). Handler exceptions are swallowed.
/// </para>
/// <para>
/// The <see cref="Count"/> property is approximate under concurrency. For a stable point-in-time view of
/// contents, use <see cref="ToArray"/>.
/// </para>
/// <para>
/// Enumeration iterates over a true snapshot captured at the moment the enumerator is created and does not
/// reflect subsequent changes.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var buffer = new ConcurrentCircularBuffer<string>(capacity: 3, allowOverwrite: true);
/// buffer.Enqueue("A");
/// buffer.Enqueue("B");
/// buffer.Enqueue("C");
/// buffer.Enqueue("D"); // "A" is evicted
///
/// if (buffer.TryPeek(out var head))
///     Console.WriteLine(head); // "B"
///
/// Console.WriteLine(buffer.Dequeue()); // "B"
///]]>
/// </example>
[DebuggerDisplay("Count ≈ {Count}, Capacity = {Capacity}")]
[DebuggerTypeProxy(typeof(CircularBufferDebugView<>))]
[Serializable]
public sealed partial class ConcurrentCircularBuffer<T>
    where T : class?
{
    private const int DefaultCapacity = 16;
    private const int MinCapacity = 2;

    // Immutable after construction
    private readonly Slot[] _buffer;

    private readonly int _capacity;

    // Mode flag
    private bool _allowOverwrite;

    // Head (consumer) and tail (producer) positions; increase monotonically using unchecked signed arithmetic.
    // Slot indices are derived via SlotIndex(), which uses unsigned modulo to ensure non-negative results even
    // after int overflow. The signed diff arithmetic (seq - tail, seq - head) remains valid across wrapping
    // because capacity is always much smaller than int.MaxValue / 2.
    private int _head;

    private int _tail;

    // Version for snapshot readers
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCircularBuffer{T}"/> class with default capacity and overwriting enabled.
    /// </summary>
    public ConcurrentCircularBuffer()
        : this(DefaultCapacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCircularBuffer{T}"/> class with the specified capacity and overwriting enabled.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of elements the buffer can hold. Must be at least 2. See the class remarks for an
    /// explanation of why the Vyukov MPMC protocol requires a minimum capacity of 2.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> &lt; 2.</exception>
    public ConcurrentCircularBuffer(int capacity)
        : this(capacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCircularBuffer{T}"/> class with the specified capacity and overwrite behaviour.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of elements the buffer can hold. Must be at least 2. See the class remarks for an
    /// explanation of why the Vyukov MPMC protocol requires a minimum capacity of 2.
    /// </param>
    /// <param name="allowOverwrite">
    /// <see langword="true"/> to evict the oldest element when the buffer is full; <see langword="false"/> to
    /// throw or return <see langword="false"/> instead.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> &lt; 2.</exception>
    public ConcurrentCircularBuffer(int capacity, bool allowOverwrite)
    {
        // The Vyukov MPMC sequence protocol requires at least two slots to be correct. With a single slot the
        // "data published" sequence mark (tail + 1) and the "slot released" sequence mark (head + capacity)
        // are always numerically identical, making the two states indistinguishable to concurrent producers and
        // causing a permanent consumer livelock. See the class remarks for a full explanation.
        ThrowHelper.ThrowIfLessThan(capacity, MinCapacity);

        _buffer = new Slot[capacity];
        _capacity = capacity;
        _allowOverwrite = allowOverwrite;

        // Initialize slot sequences so that slot i is initially "free" for tail == i.
        for (int i = 0; i < capacity; i++)
            _buffer[i].Sequence = i;

        _head = 0;
        _tail = 0;
        _version = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCircularBuffer{T}"/> class.
    /// Initializes a new instance by copying from <paramref name="collection"/>, using the specified capacity
    /// and overwrite behaviour.
    /// </summary>
    /// <param name="collection">
    /// The collection whose elements are copied into the buffer. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="capacity">
    /// The maximum number of elements the buffer can hold. Must be at least 2. See the class remarks for an
    /// explanation of why the Vyukov MPMC protocol requires a minimum capacity of 2.
    /// </param>
    /// <param name="allowOverwrite">
    /// <see langword="true"/> to evict the oldest element when the buffer is full; <see langword="false"/> to
    /// throw on overflow. Defaults to <see langword="true"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> &lt; 2.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="allowOverwrite"/> is <see langword="false"/> and the number of items in
    /// <paramref name="collection"/> exceeds <paramref name="capacity"/>.
    /// </exception>
    public ConcurrentCircularBuffer(IEnumerable<T> collection, int capacity, bool allowOverwrite = true)
        : this(capacity, allowOverwrite)
    {
        ThrowHelper.ThrowIfNull(collection);
        var items = collection as T[] ?? collection.ToArray();

        if (!allowOverwrite && items.Length > capacity)
            throw new InvalidOperationException(ResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity);

        // Enqueue in order using the lock-free path (no concurrency during construction).
        foreach (var item in items.Skip(Math.Max(0, items.Length - capacity)))
            InternalEnqueue(item, throwIfFull: !allowOverwrite);
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an item has been evicted because a new item was enqueued into a full buffer while
    /// <see cref="AllowOverwrite"/> is <see langword="true"/>.
    /// </summary>
    /// <remarks>Exceptions thrown by handlers are caught and ignored.</remarks>
    public event Action<T>? ItemEvicted;

    /// <summary>
    /// Gets or sets a value indicating whether enqueuing into a full buffer evicts the oldest element.
    /// </summary>
    public bool AllowOverwrite
    {
        get => Volatile.Read(ref _allowOverwrite);
        set => Volatile.Write(ref _allowOverwrite, value);
    }

    /// <summary>
    /// Gets the fixed capacity of the buffer.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the element at the specified zero-based logical index relative to the oldest element (snapshot-based).
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve. Must be non-negative and less than <see cref="Count"/>.</param>
    /// <returns>The element at the specified logical index within the snapshot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than zero, or greater than or equal to the number of elements in the buffer at the time of the call.
    /// </exception>
    public T this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfLessThan(index, 0);
            var snapshot = ToArray();
            ThrowHelper.ThrowIfGreaterThanOrEqual(index, snapshot.Length);
            return snapshot[index];
        }
    }

    /// <summary>
    /// Removes elements from the buffer up to the number present at the time of the call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method drains at most the number of elements observed when <see cref="Clear"/> is called. Elements added by concurrent
    /// producers after this point are not removed. This bounding prevents an indefinite loop when <see cref="AllowOverwrite"/> is
    /// <see langword="true"/> and producers are continuously enqueueing.
    /// </para>
    /// <para>
    /// The <see cref="ItemEvicted"/> event is not raised by this operation, as clearing the buffer is not considered an eviction.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        // Bound the drain to the element count observed at call time. This prevents
        // unbounded spinning when producers are continuously enqueueing into the buffer.
        int head = Volatile.Read(ref _head);
        int tail = Volatile.Read(ref _tail);
        int count = Math.Clamp(tail - head, 0, _capacity);

        T? _;
        for (int i = 0; i < count && TryDequeue(out _); i++) { }
    }

    /// <summary>
    /// Determines whether a snapshot of the buffer contains the specified element.
    /// </summary>
    /// <param name="item">The element to locate. May be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the element was found in the snapshot; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T? item)
    {
        var comparer = EqualityComparer<T?>.Default;
        foreach (var x in ToArray())
        {
            if (comparer.Equals(x, item))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Copies a snapshot of the buffer to <paramref name="array"/> starting at <paramref name="index"/>.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null"/>.</param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
    /// <exception cref="ArgumentException">
    /// The number of elements in the buffer exceeds the available space in <paramref name="array"/> from <paramref name="index"/> onward.
    /// </exception>
    public void CopyTo(T[] array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(index, nameof(index));
        var snap = ToArray();
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + snap.Length);

        Array.Copy(snap, 0, array, index, snap.Length);
    }

    /// <summary>
    /// Removes and returns the oldest element.
    /// </summary>
    /// <returns>The oldest element in the buffer.</returns>
    /// <exception cref="InvalidOperationException">The buffer is empty.</exception>
    public T Dequeue()
    {
        if (TryDequeue(out var item))
            return item!;
        throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);
    }

    /// <summary>
    /// Adds an element to the end of the buffer, throwing when full if overwriting is disabled.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/>.</param>
    /// <exception cref="InvalidOperationException">The buffer is full and <see cref="AllowOverwrite"/> is <see langword="false"/>.</exception>
    public void Enqueue(T item) => InternalEnqueue(item, throwIfFull: true);

    /// <summary>
    /// Returns the oldest element without removing it.
    /// </summary>
    /// <returns>The oldest element in the buffer.</returns>
    /// <exception cref="InvalidOperationException">The buffer is empty.</exception>
    public T Peek()
    {
        if (TryPeek(out var item))
            return item!;
        throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);
    }

    /// <summary>
    /// Returns a snapshot of the buffer's contents in FIFO order.
    /// </summary>
    /// <returns>
    /// An array containing the elements observed in the buffer, ordered from oldest to newest. Returns an empty array if the buffer
    /// is empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses a versioned retry loop to obtain a consistent snapshot. Up to 64 attempts are made to read a stable slice.
    /// If the buffer is under sustained write pressure and a stable snapshot cannot be obtained, a best-effort read is returned.
    /// </para>
    /// </remarks>
    public T[] ToArray()
    {
        var spinner = default(SpinWait);

        for (int attempt = 0; attempt < 64; attempt++)
        {
            int v1 = Volatile.Read(ref _version);
            int head = Volatile.Read(ref _head);
            int tail = Volatile.Read(ref _tail);

            int count = tail - head;
            if (count <= 0) return Array.Empty<T>();
            if (count > _capacity) count = _capacity; // defensive clamp

            var result = new T[count];
            for (int i = 0; i < count; i++)
                result[i] = Volatile.Read(ref _buffer[SlotIndex(head + i)].Value)!;

            int v2 = Volatile.Read(ref _version);
            if (v1 == v2) return result;
            spinner.SpinOnce();
        }

        // Fallback best-effort snapshot after exhausting retry budget
        int h = Volatile.Read(ref _head);
        int t = Volatile.Read(ref _tail);
        int c = Math.Clamp(t - h, 0, _capacity);
        var res = new T[c];
        for (int i = 0; i < c; i++)
            res[i] = Volatile.Read(ref _buffer[SlotIndex(h + i)].Value)!;
        return res;
    }

    /// <summary>
    /// Attempts to remove and return the oldest element.
    /// </summary>
    /// <param name="item">
    /// When this method returns <see langword="true"/>, contains the removed element; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if an element was successfully removed; <see langword="false"/> if the buffer was empty.</returns>
    public bool TryDequeue(out T? item) => InternalDequeue(out item, throwIfEmpty: false);

    /// <summary>
    /// Attempts to add an element to the end of the buffer without throwing when full.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the element was enqueued; <see langword="false"/> if the buffer is full and
    /// <see cref="AllowOverwrite"/> is <see langword="false"/>.
    /// </returns>
    public bool TryEnqueue(T item) => InternalEnqueue(item, throwIfFull: false);

    /// <summary>
    /// Attempts to return the oldest element without removing it.
    /// </summary>
    /// <param name="item">
    /// When this method returns <see langword="true"/>, contains the oldest element; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if an element was found; <see langword="false"/> if the buffer was empty.</returns>
    /// <remarks>
    /// <para>
    /// This method retries on transient races where another thread concurrently dequeues the head element between the head position
    /// read and the slot sequence check. It returns <see langword="false"/> only when the buffer is observed to be empty.
    /// </para>
    /// </remarks>
    public bool TryPeek(out T? item)
    {
        var spinner = default(SpinWait);

        while (true)
        {
            int head = Volatile.Read(ref _head);
            ref var slot = ref _buffer[SlotIndex(head)];

            int seq = Volatile.Read(ref slot.Sequence);
            int diff = seq - (head + 1);

            if (diff == 0)
            {
                // slot is published and still at head — safe to read
                item = Volatile.Read(ref slot.Value);
                return true;
            }

            if (diff < 0)
            {
                // empty
                item = default;
                return false;
            }

            // diff > 0: stale head read — another thread dequeued this slot; retry
            spinner.SpinOnce();
        }
    }

    /// <summary>
    /// Computes a non-negative slot index from a monotonically increasing counter position.
    /// </summary>
    /// <param name="position">The monotonically-increasing producer or consumer counter value.</param>
    /// <returns>The wrapped slot index in the range <c>[0, _capacity)</c>.</returns>
    /// <remarks>
    /// Uses unsigned modulo to guarantee a non-negative result even after <see cref="int"/> counter overflow,
    /// where plain <c>position % _capacity</c> would yield a negative index in C#.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SlotIndex(int position) => (int)((uint)position % (uint)_capacity);

    /// <summary>
    /// Evicts exactly one item from the head (used when overwriting). Fires <see cref="ItemEvicted"/> after removal. Handler exceptions
    /// are swallowed.
    /// </summary>
    /// <returns><see langword="true" /> if an item was evicted; <see langword="false" /> if the
    /// buffer was empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvictOne()
    {
        var spinner = default(SpinWait);

        while (true)
        {
            int head = Volatile.Read(ref _head);
            ref var slot = ref _buffer[SlotIndex(head)];

            int seq = Volatile.Read(ref slot.Sequence);
            int diff = seq - (head + 1);

            if (diff == 0)
            {
                // claim head
                if (Interlocked.CompareExchange(ref _head, head + 1, head) != head)
                {
                    spinner.SpinOnce();
                    continue;
                }

                // read value, clear, publish free
                T? value = Volatile.Read(ref slot.Value);
                Volatile.Write(ref slot.Value, default!);
                Volatile.Write(ref slot.Sequence, head + _capacity);
                Interlocked.Increment(ref _version);

                // AFTER removal: fire event; each handler is guarded independently so a throwing
                // subscriber cannot prevent subsequent subscribers from receiving the notification.
                var onEvicted = ItemEvicted;
                if (onEvicted != null)
                {
                    foreach (Action<T> handler in onEvicted.GetInvocationList())
                    {
                        try { handler(value!); }
                        catch { /* swallow */ }
                    }
                }

                return true;
            }
            else if (diff < 0)
            {
                // empty — nothing to evict
                return false;
            }
            else
            {
                spinner.SpinOnce();
            }
        }
    }

    /// <summary>
    /// Removes the oldest element using the lock-free consumer protocol.
    /// </summary>
    /// <param name="item">When this method returns <see langword="true" />, contains the removed
    /// element; otherwise the default value of <typeparamref name="T" />.</param>
    /// <param name="throwIfEmpty">If <see langword="true" />, throws
    /// <see cref="InvalidOperationException" /> when the buffer is empty; if
    /// <see langword="false" />, returns <see langword="false" /> on empty.</param>
    /// <returns><see langword="true" /> if an element was removed; otherwise <see langword="false" />.</returns>
    /// <exception cref="InvalidOperationException">The buffer is empty and
    /// <paramref name="throwIfEmpty" /> is <see langword="true" />.</exception>
    private bool InternalDequeue(out T? item, bool throwIfEmpty)
    {
        var spinner = default(SpinWait);

        while (true)
        {
            int head = Volatile.Read(ref _head);
            ref var slot = ref _buffer[SlotIndex(head)];

            int seq = Volatile.Read(ref slot.Sequence);
            int diff = seq - (head + 1);

            if (diff == 0)
            {
                // claim head
                if (Interlocked.CompareExchange(ref _head, head + 1, head) != head)
                {
                    spinner.SpinOnce();
                    continue;
                }

                // read value, clear, publish free
                item = Volatile.Read(ref slot.Value);
                Volatile.Write(ref slot.Value, default!);
                Volatile.Write(ref slot.Sequence, head + _capacity);

                Interlocked.Increment(ref _version);
                return true;
            }
            else if (diff < 0)
            {
                // empty
                item = default;
                if (throwIfEmpty)
                    throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);
                return false;
            }
            else
            {
                // another consumer ahead / slot not yet published
                spinner.SpinOnce();
            }
        }
    }

    /// <summary>
    /// Adds an element using the lock-free producer protocol.
    /// </summary>
    /// <param name="item">The element to add. The value may be <see langword="null"/>.</param>
    /// <param name="throwIfFull">
    /// When <see langword="true"/> and <see cref="AllowOverwrite"/> is <see langword="false"/>, a full buffer throws; when
    /// <see langword="false"/> the method returns <see langword="false"/> instead.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the element was enqueued; otherwise <see langword="false"/> when the buffer is full and overwriting is disabled.
    /// </returns>
    private bool InternalEnqueue(T item, bool throwIfFull)
    {
        var spinner = default(SpinWait);

        while (true)
        {
            int tail = Volatile.Read(ref _tail);
            ref var slot = ref _buffer[SlotIndex(tail)];

            int seq = Volatile.Read(ref slot.Sequence);
            int diff = seq - tail;

            if (diff == 0)
            {
                // claim tail
                if (Interlocked.CompareExchange(ref _tail, tail + 1, tail) != tail)
                {
                    spinner.SpinOnce();
                    continue;
                }

                // write and publish
                Volatile.Write(ref slot.Value, item);
                Volatile.Write(ref slot.Sequence, tail + 1);
                Interlocked.Increment(ref _version);
                return true;
            }
            else if (diff < 0)
            {
                // looks full wrt this slot
                if (!AllowOverwrite)
                {
                    if (throwIfFull)
                        throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);
                    return false;
                }

                // overwrite mode: evict oldest, then retry
                if (!EvictOne())
                {
                    // briefly empty / race
                    spinner.SpinOnce();
                }

                // retry claim & publish
                continue;
            }
            else
            {
                // another producer ahead / slot not yet free
                spinner.SpinOnce();
            }
        }
    }

    /// <summary>
    /// Ring slot holding a coordination sequence number and a stored value, padded to a full cache line to prevent false sharing
    /// between adjacent slots on multi-core hardware.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each <see cref="Slot"/> is sized to 64 bytes — a typical CPU cache line — via sequential layout with explicit padding fields.
    /// This eliminates false sharing between concurrently accessed producer and consumer slots, which would otherwise cause unnecessary
    /// cache coherence traffic and reduce throughput on multi-core systems.
    /// </para>
    /// <para>
    /// <see cref="LayoutKind.Explicit"/> cannot be used here because the CLR prohibits explicit layout on structs nested within
    /// generic types. <see cref="LayoutKind.Sequential"/> with padding fields is the correct alternative and produces an equivalent
    /// in-memory footprint.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    private struct Slot
    {
        /// <summary>The Vyukov sequence number used to coordinate producers and consumers for this slot.</summary>
        public int Sequence;

        // Aligns Value to an 8-byte boundary on all supported platforms.
        private readonly int _sequencePadding;

        /// <summary>The stored element. Written by the producer, cleared by the consumer.</summary>
        public T? Value;

        // Pads the struct to 64 bytes: 4 (Sequence) + 4 (pad) + 8 (Value ref) + 6×8 (pad) = 64.
        private readonly long _pad0;
        private readonly long _pad1;
        private readonly long _pad2;
        private readonly long _pad3;
        private readonly long _pad4;
        private readonly long _pad5;
    }
}
