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
/// element and raises the <see cref="ItemEvicted"/> event (after removal). Handler exceptions are caught and
/// suppressed; this differs intentionally from the non-concurrent <see cref="CircularBuffer{T}"/>, which
/// propagates handler exceptions. Under MPMC the eviction has already been committed by the time the event
/// fires (the head counter has advanced and the slot has been freed by another consumer or producer running in
/// parallel), so propagating a handler exception cannot abort the eviction and would only obscure the cause of
/// failure for unrelated callers. Subscribers that need to react to a throwing handler should perform their own
/// catch/log in the handler body.
/// </para>
/// <para>
/// The <see cref="Count"/> property is approximate under concurrency. For a stable point-in-time view of
/// contents, use <see cref="ToArray"/>.
/// </para>
/// <para>
/// Enumeration iterates over a true snapshot captured at the moment the enumerator is created and does not
/// reflect subsequent changes.
/// </para>
/// <para>
/// Slots are padded to 64 bytes — the standard cache-line size on x86/x64 hardware — to prevent false sharing
/// between adjacent producer- and consumer-touched slots. Targets with larger cache lines (notably Apple Silicon
/// and some ARM SoCs at 128 bytes) remain correct; the padding is conservative on those platforms but does not
/// degrade throughput.
/// </para>
/// <para>
/// The generic type parameter is constrained to <c>class?</c> because the slot value is published and cleared
/// through <see cref="System.Threading.Volatile"/> overloads that target reference types. Value-type element
/// support would require a different publication mechanism and is intentionally out of scope for this type.
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
[DebuggerTypeProxy(typeof(ConcurrentCircularBufferDebugView<>))]
[Serializable]
public sealed partial class ConcurrentCircularBuffer<T>
    where T : class?
{
    private const int DefaultCapacity = 16;
    private const int MinCapacity = 2;

    /// <summary>
    /// Maximum number of complete snapshot/index/contains attempts before falling back to a best-effort or
    /// failing read. Sized for sustained-contention scenarios; under typical load a snapshot stabilises on
    /// the first attempt.
    /// </summary>
    private const int SnapshotOuterRetryBudget = 64;

    /// <summary>
    /// Maximum number of seqlock pre/post sequence-read retries on a single slot before treating the slot
    /// as unstable and aborting the surrounding snapshot or index read.
    /// </summary>
    private const int SlotReadRetryBudget = 8;

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
        for (var i = 0; i < capacity; i++)
            _buffer[i].Sequence = i;

        _head = 0;
        _tail = 0;
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
        T[] items = collection as T[] ?? collection.ToArray();

        if (!allowOverwrite && items.Length > capacity)
            throw new InvalidOperationException(ResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity);

        InitialFill(items);
    }

    /// <summary>
    /// Populates the freshly-constructed buffer directly from <paramref name="items"/>, retaining the trailing
    /// <see cref="Capacity"/> elements when the source is larger. The instance is not yet observable by other
    /// threads at this point, so the lock-free producer protocol is bypassed.
    /// </summary>
    /// <param name="items">The materialised source elements.</param>
    /// <remarks>
    /// <para>
    /// Writes occur in published-state form: each populated slot's <c>Sequence</c> is set to its publication
    /// mark (<c>tail + 1</c>) and <see cref="_tail"/> is advanced once at the end. <see cref="_head"/> remains
    /// zero, so the live region is <c>[0, tail)</c>.
    /// </para>
    /// </remarks>
    private void InitialFill(T[] items)
    {
        var capacity = _capacity;
        var sourceLength = items.Length;
        var copyLength = sourceLength <= capacity ? sourceLength : capacity;
        var sourceOffset = sourceLength - copyLength;

        for (var i = 0; i < copyLength; i++)
        {
            _buffer[i].Value = items[sourceOffset + i];
            _buffer[i].Sequence = i + 1;
        }

        _tail = copyLength;
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an item has been evicted because a new item was enqueued into a full buffer while
    /// <see cref="AllowOverwrite"/> is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exceptions thrown by handlers are caught and suppressed. Each subscriber's invocation is guarded
    /// independently so that a throwing handler cannot prevent later handlers from receiving the notification.
    /// </para>
    /// <para>
    /// This differs from the non-concurrent <see cref="CircularBuffer{T}.ItemEvicted"/>, which propagates
    /// handler exceptions to the caller of <see cref="Enqueue"/>. Under MPMC the eviction has already been
    /// committed by the time this event fires, so propagating the exception would block the caller for a
    /// failure unrelated to their own write and could not undo the eviction.
    /// </para>
    /// </remarks>
    public event Action<T>? ItemEvicted;

    /// <summary>
    /// Gets or sets a value indicating whether enqueuing into a full buffer evicts the oldest element.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to evict the oldest element when full; <see langword="false"/> to throw or return
    /// <see langword="false"/> from the producer-side methods.
    /// </value>
    /// <returns>The current overwrite policy.</returns>
    /// <remarks>
    /// Toggling this property concurrently with an in-flight <see cref="Enqueue"/> or <see cref="TryEnqueue"/>
    /// is safe but may have a benign window where a producer that observed the previous value commits its
    /// behaviour (eviction or rejection) before the new value takes effect. The window does not corrupt buffer
    /// state.
    /// </remarks>
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
    /// Gets the element at the specified zero-based logical index relative to the oldest element.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the element to retrieve. Must be non-negative and less than <see cref="Count"/>
    /// observed at the moment of the call.
    /// </param>
    /// <returns>The element that was at the specified logical position during the call.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is negative, or is greater than or equal to the number of elements in the
    /// buffer observed at the time of the call.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The buffer is under sustained concurrent modification and a stable single-slot read could not be
    /// obtained within the retry budget.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The accessor performs a sequence-validated single-slot read; it does not allocate a snapshot. Two
    /// consecutive index reads (for example, <c>buffer[i]</c> followed by <c>buffer[i + 1]</c>) are not
    /// jointly atomic — concurrent producers or consumers may modify the buffer between the two reads.
    /// Callers that require joint atomicity across multiple positions should call <see cref="ToArray"/> once
    /// and index the resulting array.
    /// </para>
    /// </remarks>
    public T this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfLessThan(index, 0);

            SpinWait spinner = default;
            for (var outerAttempt = 0; outerAttempt < SnapshotOuterRetryBudget; outerAttempt++)
            {
                var head = Volatile.Read(ref _head);
                var tail = Volatile.Read(ref _tail);
                var count = tail - head;
                if (count < 0) count = 0;
                if (count > _capacity) count = _capacity;

                ThrowHelper.ThrowIfGreaterThanOrEqual(index, count);

                var position = head + index;
                if (TryReadStableSlot(SlotIndex(position), position + 1, out T? value)
                    && Volatile.Read(ref _head) == head)
                {
                    return value!;
                }

                spinner.SpinOnce();
            }

            throw new InvalidOperationException(ResourceStrings.Concurrent_SnapshotUnstable);
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
        var head = Volatile.Read(ref _head);
        var tail = Volatile.Read(ref _tail);
        var count = Math.Clamp(tail - head, 0, _capacity);

        T? _;
        for (var i = 0; i < count && TryDequeue(out _); i++) { }
    }

    /// <summary>
    /// Determines whether the buffer contains the specified element using
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    /// <param name="item">The element to locate. May be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if a sequence-stable read found a match within the live region during the call;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Walks the live region using a sequence-validated direct slot scan; no array allocation occurs in the
    /// common case. Under sustained concurrent modification the scan restarts; if the retry budget is
    /// exhausted, a single coherent <see cref="ToArray"/> snapshot is used as a fallback.
    /// </para>
    /// </remarks>
    public bool Contains(T? item)
    {
        EqualityComparer<T?> comparer = EqualityComparer<T?>.Default;
        SpinWait spinner = default;

        for (var outerAttempt = 0; outerAttempt < SnapshotOuterRetryBudget; outerAttempt++)
        {
            var head = Volatile.Read(ref _head);
            var tail = Volatile.Read(ref _tail);
            var count = tail - head;
            if (count <= 0) return false;
            if (count > _capacity) count = _capacity;

            var slotFailure = false;
            for (var i = 0; i < count; i++)
            {
                var position = head + i;
                if (!TryReadStableSlot(SlotIndex(position), position + 1, out T? value))
                {
                    slotFailure = true;
                    break;
                }

                if (comparer.Equals(value, item))
                    return true;
            }

            // No match in this pass: only conclude false if the head has not advanced (otherwise our window
            // shifted under us and the missing element may have been retroactively reclaimed before we read it).
            if (!slotFailure && Volatile.Read(ref _head) == head)
                return false;

            spinner.SpinOnce();
        }

        // Fallback after exhausting the retry budget: a single coherent snapshot pass.
        foreach (T? x in ToArray())
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
    /// An array containing the elements observed in the buffer, ordered from oldest to newest. Returns an empty
    /// array if the buffer is empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Each slot in the snapshot is read using a sequence-validated seqlock pattern: the slot's coordination
    /// sequence is read both before and after the value, and the read is committed only when both sequence
    /// observations match the expected published mark. This guarantees the value, when committed, was the
    /// element published at that logical position — never a value from an earlier or later generation.
    /// </para>
    /// <para>
    /// If a slot cannot be stabilised within its retry budget, the entire snapshot is restarted. After the
    /// outer retry budget is exhausted under sustained churn, a best-effort snapshot is returned in which
    /// individual slots that still cannot be stabilised contribute the default value of <typeparamref name="T"/>;
    /// every committed slot in that fallback path is still sequence-validated, so a torn or stale-generation
    /// reference is never returned.
    /// </para>
    /// </remarks>
    public T[] ToArray()
    {
        SpinWait spinner = default;

        for (var outerAttempt = 0; outerAttempt < SnapshotOuterRetryBudget; outerAttempt++)
        {
            var head = Volatile.Read(ref _head);
            var tail = Volatile.Read(ref _tail);

            var count = tail - head;
            if (count <= 0) return Array.Empty<T>();
            if (count > _capacity) count = _capacity;

            var result = new T[count];
            var slotFailure = false;

            for (var i = 0; i < count; i++)
            {
                var position = head + i;
                if (!TryReadStableSlot(SlotIndex(position), position + 1, out T? value))
                {
                    slotFailure = true;
                    break;
                }

                result[i] = value!;
            }

            // Final coherence check: confirm the head has not advanced past the position we used to lay the
            // window out, otherwise our slots may have been retroactively reclaimed and re-published into a
            // different generation that we just happened to read consistently.
            if (!slotFailure && Volatile.Read(ref _head) == head)
                return result;

            spinner.SpinOnce();
        }

        return BestEffortSnapshot();
    }

    /// <summary>
    /// Produces a best-effort snapshot when the standard <see cref="ToArray"/> retry budget is exhausted.
    /// Each slot is still sequence-validated; slots that cannot be stabilised are written as
    /// <see langword="default"/> rather than as a torn or stale-generation value.
    /// </summary>
    /// <returns>The best-effort snapshot array.</returns>
    private T[] BestEffortSnapshot()
    {
        var head = Volatile.Read(ref _head);
        var tail = Volatile.Read(ref _tail);
        var count = Math.Clamp(tail - head, 0, _capacity);
        if (count == 0) return Array.Empty<T>();

        var result = new T[count];
        for (var i = 0; i < count; i++)
        {
            var position = head + i;
            if (TryReadStableSlot(SlotIndex(position), position + 1, out T? value))
                result[i] = value!;

            // Otherwise leave result[i] at default(T): null for the class? constraint on this type.
        }

        return result;
    }

    /// <summary>
    /// Attempts a sequence-validated read of the slot at the given physical index, expecting the slot to be
    /// in the published state for the supplied logical position.
    /// </summary>
    /// <param name="slotIndex">The physical slot index; must be in the range <c>[0, _capacity)</c>.</param>
    /// <param name="expectedSequence">
    /// The publication sequence the slot is expected to carry (<c>position + 1</c> for the slot's logical
    /// head-relative position).
    /// </param>
    /// <param name="value">
    /// On <see langword="true"/>, contains the committed value read from the slot. On <see langword="false"/>,
    /// contains <see langword="default"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the slot was observed in the expected published state both before and
    /// after the value read; <see langword="false"/> when the slot has been reclaimed, has not yet been
    /// published, or could not be stabilised within the inner retry budget.
    /// </returns>
    /// <remarks>
    /// Implements the seqlock read protocol: read sequence, read value, re-read sequence; commit when both
    /// sequence reads equal <paramref name="expectedSequence"/>. A divergence between the two sequence reads
    /// indicates the slot has been touched by a concurrent producer or consumer between the value read and
    /// the post-check.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryReadStableSlot(int slotIndex, int expectedSequence, out T? value)
    {
        SpinWait spinner = default;
        for (var attempt = 0; attempt < SlotReadRetryBudget; attempt++)
        {
            var seqPre = Volatile.Read(ref _buffer[slotIndex].Sequence);
            if (seqPre - expectedSequence != 0)
            {
                value = default;
                return false;
            }

            T? candidate = Volatile.Read(ref _buffer[slotIndex].Value);
            var seqPost = Volatile.Read(ref _buffer[slotIndex].Sequence);
            if (seqPost == seqPre)
            {
                value = candidate;
                return true;
            }

            spinner.SpinOnce();
        }

        value = default;
        return false;
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
            var head = Volatile.Read(ref _head);
            ref var slot = ref _buffer[SlotIndex(head)];

            var seq = Volatile.Read(ref slot.Sequence);
            var diff = seq - (head + 1);

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
            var head = Volatile.Read(ref _head);
            ref var slot = ref _buffer[SlotIndex(head)];

            var seq = Volatile.Read(ref slot.Sequence);
            var diff = seq - (head + 1);

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
            var head = Volatile.Read(ref _head);
            ref var slot = ref _buffer[SlotIndex(head)];

            var seq = Volatile.Read(ref slot.Sequence);
            var diff = seq - (head + 1);

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
            var tail = Volatile.Read(ref _tail);
            ref var slot = ref _buffer[SlotIndex(tail)];

            var seq = Volatile.Read(ref slot.Sequence);
            var diff = seq - tail;

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

#pragma warning disable CS0649 // Padding fields are deliberately unassigned; they exist only to enforce layout.

        /// <summary>Aligns <see cref="Value"/> to an 8-byte boundary on all supported platforms.</summary>
        private readonly int _sequencePadding;

#pragma warning restore CS0649

        /// <summary>The stored element. Written by the producer, cleared by the consumer.</summary>
        public T? Value;

#pragma warning disable CS0649 // Padding fields are deliberately unassigned; they exist only to enforce layout.

        // Pads the struct to 64 bytes: 4 (Sequence) + 4 (pad) + 8 (Value ref) + 6×8 (pad) = 64.
        private readonly long _pad0;
        private readonly long _pad1;
        private readonly long _pad2;
        private readonly long _pad3;
        private readonly long _pad4;
        private readonly long _pad5;

#pragma warning restore CS0649
    }
}
