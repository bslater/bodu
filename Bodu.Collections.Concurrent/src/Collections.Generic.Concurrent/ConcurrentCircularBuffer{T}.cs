// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBuffer{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a lock-free, bounded first-in, first-out (FIFO) buffer with optional overwrite semantics.
/// </summary>
/// <typeparam name="T">The reference type stored in the buffer (constraint: <c>where T : class</c>).</typeparam>
/// <remarks>
/// <para>
/// This type implements a multi-producer/multi-consumer (MPMC) circular buffer using per-slot sequence numbers (Vyukov
/// pattern). The buffer capacity is fixed at construction time and must be at least 2.
/// </para>
/// <para>
/// The Vyukov MPMC sequence protocol uses two distinct sequence marks per slot: one written by the producer when data
/// is published (<c>tail + 1</c>), and one written by the consumer when the slot is released (<c>head + capacity</c>).
/// These marks must be numerically distinct so that concurrent producers can determine whether a slot is free or still
/// occupied. With a capacity of 1 they are always equal for every round, making the two states indistinguishable and
/// allowing a second concurrent producer to overwrite an occupied slot, permanently skipping a sequence number and
/// leaving consumers in an infinite spin. A minimum capacity of 2 is therefore required for the protocol to be correct.
/// </para>
/// <para>
/// When <see cref="AllowOverwrite" /> is <see langword="true" />, enqueuing into a full buffer evicts the oldest
/// element and raises the <see cref="ItemEvicted" /> event (after removal). Ordinary handler exceptions are caught and
/// suppressed (a process-fatal <see cref="OutOfMemoryException" /> still propagates); this differs intentionally from
/// the non-concurrent <see cref="CircularBuffer{T}" />, which propagates handler exceptions. Under MPMC the eviction
/// has already been committed by the time the event fires (the head counter has advanced and the slot has been freed by
/// another consumer or producer running in parallel), so propagating a handler exception cannot abort the eviction and
/// would only obscure the cause of failure for unrelated callers. Subscribers that need to react to a throwing handler
/// should perform their own catch/log in the handler body.
/// </para>
/// <para>
/// The <see cref="Count" /> property is approximate under concurrency. For a stable point-in-time view of contents, use
/// <see cref="ToArray" />.
/// </para>
/// <para>
/// Enumeration iterates over a true snapshot captured at the moment the enumerator is created and does not reflect
/// subsequent changes.
/// </para>
/// <para>
/// Slots are padded to 64 bytes — the standard cache-line size on x86/x64 hardware — to prevent false sharing between
/// adjacent producer- and consumer-touched slots. Targets with larger cache lines (notably Apple Silicon and some ARM
/// SoCs at 128 bytes) remain correct; the padding is conservative on those platforms but does not degrade throughput.
/// </para>
/// <para>
/// The generic type parameter is constrained to <c>class?</c> because the slot value is published and cleared through
/// <see cref="System.Threading.Volatile" /> overloads that target reference types. Value-type element support would
/// require a different publication mechanism and is intentionally out of scope for this type.
/// </para>
/// <para>
/// Any capacity of at least two is supported, including non-power-of-two values, and <see cref="Capacity" /> reports
/// the exact requested value. One caveat applies at extreme longevity: the head and tail counters are 32-bit, and the
/// physical slot index is their unsigned modulo the capacity. When the capacity is not a power of two, the counter's
/// wrap at <c>2^32</c> operations shifts that modulo by <c>2^32 mod capacity</c>, which could misalign a single slot
/// once every ~4.29 billion enqueue/dequeue operations. A power-of-two capacity divides <c>2^32</c> evenly and is
/// therefore free of the caveat entirely; callers whose buffers process on the order of billions of operations without
/// recreation should prefer one. Eliminating the caveat for arbitrary capacities would require widening the counters to
/// 64-bit and is intentionally out of scope for this type.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
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
/// </code>
/// </example>
[DebuggerDisplay("Count ≈ {Count}, Capacity = {Capacity}")]
[DebuggerTypeProxy(typeof(ConcurrentCircularBufferDebugView<>))]
[Serializable]
public sealed partial class ConcurrentCircularBuffer<T>
    where T : class?
{
    /// <summary>The capacity used when the buffer is constructed without an explicit capacity.</summary>
    private const int DefaultCapacity = 16;

    /// <summary>The smallest capacity the Vyukov MPMC protocol permits.</summary>
    private const int MinCapacity = 2;

    /// <summary>Maximum number of seqlock pre/post sequence-read retries on a single slot before treating the slot as unstable and aborting the surrounding snapshot or index read.</summary>
    private const int SlotReadRetryBudget = 8;

    /// <summary>Maximum number of complete snapshot/index/contains attempts before falling back to a best-effort or failing read. Sized for sustained-contention scenarios; under typical load a snapshot stabilizes on the first attempt.</summary>
    private const int SnapshotOuterRetryBudget = 64;

    /// <summary>The fixed array of slots backing the ring. Immutable after construction.</summary>
    private readonly Slot[] _buffer;

    /// <summary>The maximum number of elements the buffer can hold. Immutable after construction.</summary>
    private readonly int _capacity;

    /// <summary>Indicates whether the oldest element is evicted when the buffer is full.</summary>
    private bool _allowOverwrite;

    /// <summary>The consumer (head) position, incremented monotonically using unchecked signed arithmetic as elements are dequeued. Slot indices are derived from it via unsigned modulo so they stay non-negative across overflow.</summary>
    private int _head;

    /// <summary>The producer (tail) position, incremented monotonically as elements are enqueued.</summary>
    private int _tail;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCircularBuffer{T}" /> class with default capacity and
    /// overwriting enabled.
    /// </summary>
    public ConcurrentCircularBuffer()
        : this(DefaultCapacity, allowOverwrite: true)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCircularBuffer{T}" /> class with the specified capacity
    /// and overwriting enabled.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of elements the buffer can hold. Must be at least 2. See the class remarks for an explanation
    /// of why the Vyukov MPMC protocol requires a minimum capacity of 2.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> &lt; 2.</exception>
    public ConcurrentCircularBuffer(int capacity)
        : this(capacity, allowOverwrite: true)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCircularBuffer{T}" /> class with the specified capacity
    /// and overwrite behavior.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of elements the buffer can hold. Must be at least 2. See the class remarks for an explanation
    /// of why the Vyukov MPMC protocol requires a minimum capacity of 2.
    /// </param>
    /// <param name="allowOverwrite">
    /// <see langword="true" /> to evict the oldest element when the buffer is full; <see langword="false" /> to throw
    /// or return <see langword="false" /> instead.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> &lt; 2.</exception>
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
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCircularBuffer{T}" /> class. Initializes a new instance
    /// by copying from <paramref name="collection" />, using the specified capacity and overwrite behavior.
    /// </summary>
    /// <param name="collection">
    /// The collection whose elements are copied into the buffer. Must not be <see langword="null" />.
    /// </param>
    /// <param name="capacity">
    /// The maximum number of elements the buffer can hold. Must be at least 2. See the class remarks for an explanation
    /// of why the Vyukov MPMC protocol requires a minimum capacity of 2.
    /// </param>
    /// <param name="allowOverwrite">
    /// <see langword="true" /> to evict the oldest element when the buffer is full; <see langword="false" /> to throw
    /// on overflow. Defaults to <see langword="true" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> &lt; 2.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="allowOverwrite" /> is <see langword="false" /> and the number of items in
    /// <paramref name="collection" /> exceeds <paramref name="capacity" />.
    /// </exception>
    public ConcurrentCircularBuffer(IEnumerable<T> collection, int capacity, bool allowOverwrite = true)
        : this(capacity, allowOverwrite)
    {
        ThrowHelper.ThrowIfNull(collection);
        T[] items = collection as T[] ?? collection.ToArray();

        if (!allowOverwrite && items.Length > capacity)
            throw new InvalidOperationException(ConcurrentCollectionsResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity);

        InitialFill(items);
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an item has been evicted because a new item was enqueued into a full buffer
    /// while <see cref="AllowOverwrite" /> is <see langword="true" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ordinary exceptions thrown by handlers are caught and suppressed; a process-fatal
    /// <see cref="OutOfMemoryException" /> is allowed to propagate to the caller of <see cref="Enqueue" /> rather than
    /// being masked. Each subscriber's invocation is guarded independently so that a throwing handler cannot prevent
    /// later handlers from receiving the notification.
    /// </para>
    /// <para>
    /// This differs from the non-concurrent <see cref="CircularBuffer{T}.ItemEvicted" />, which propagates handler
    /// exceptions to the caller of <see cref="Enqueue" />. Under MPMC the eviction has already been committed by the
    /// time this event fires, so propagating the exception would block the caller for a failure unrelated to their own
    /// write and could not undo the eviction.
    /// </para>
    /// <para>
    /// <strong>Count under contention.</strong> When multiple producers overwrite a full buffer concurrently, a single
    /// logical "enqueue into a full buffer" can trigger more than one eviction: a producer may evict the oldest element
    /// to free a slot, lose that freed slot to another producer before it can claim it, and evict again. Each eviction
    /// still removes a distinct real element in FIFO order and fires this event exactly once, so data is never lost or
    /// duplicated — but the total number of firings is an <em>upper bound</em> on the number of admissions, not a
    /// one-to-one signal. A subscriber counting evictions against enqueues will see them diverge under write pressure.
    /// In the uncontended (single-producer) case the ratio is exactly one eviction per overflow admission. Handlers run
    /// inside the lock-free dequeue path, so they must not perform heavy work.
    /// </para>
    /// </remarks>
    public event Action<T>? ItemEvicted;

    /// <summary>
    /// Gets or sets a value indicating whether enqueuing into a full buffer evicts the oldest element.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to evict the oldest element when full; <see langword="false" /> to throw or return
    /// <see langword="false" /> from the producer-side methods.
    /// </value>
    /// <remarks>
    /// Toggling this property concurrently with an in-flight <see cref="Enqueue" /> or <see cref="TryEnqueue" /> is
    /// safe but may have a benign window where a producer that observed the previous value commits its behavior
    /// (eviction or rejection) before the new value takes effect. The window does not corrupt buffer state.
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
    /// The zero-based index of the element to retrieve. Must be non-negative and less than <see cref="Count" />
    /// observed at the moment of the call.
    /// </param>
    /// <returns>The element that was at the specified logical position during the call.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative, or is greater than or equal to the number of elements in the buffer
    /// observed at the time of the call.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The buffer is under sustained concurrent modification and a stable single-slot read could not be obtained within
    /// the retry budget.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The accessor performs a sequence-validated single-slot read; it does not allocate a snapshot. Two consecutive
    /// index reads (for example, <c>buffer[i]</c> followed by <c>buffer[i + 1]</c>) are not jointly atomic — concurrent
    /// producers or consumers may modify the buffer between the two reads. Callers that require joint atomicity across
    /// multiple positions should call <see cref="ToArray" /> once and index the resulting array.
    /// </para>
    /// </remarks>
    public T this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfLessThan(index, 0);

            SpinWait spinner = default;
            for (int outerAttempt = 0; outerAttempt < SnapshotOuterRetryBudget; outerAttempt++)
            {
                int head = Volatile.Read(ref _head);
                int tail = Volatile.Read(ref _tail);
                int count = tail - head;
                if (count < 0)
                    count = 0;
                if (count > _capacity)
                    count = _capacity;

                ThrowHelper.ThrowIfGreaterThanOrEqual(index, count);

                int position = head + index;
                if (TryReadStableSlot(SlotIndex(position), position + 1, out T? value)
                    && Volatile.Read(ref _head) == head)
                {
                    return value!;
                }

                spinner.SpinOnce();
            }

            throw new InvalidOperationException(ConcurrentCollectionsResourceStrings.Op_Invalid_ConcurrentSnapshotUnstable);
        }
    }

    /// <summary>
    /// Removes elements from the buffer up to the number present at the time of the call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method drains at most the number of elements observed when <see cref="Clear" /> is called. Elements added
    /// by concurrent producers after this point are not removed. This bounding prevents an indefinite loop when
    /// <see cref="AllowOverwrite" /> is <see langword="true" /> and producers are continuously enqueueing.
    /// </para>
    /// <para>
    /// The <see cref="ItemEvicted" /> event is not raised by this operation, as clearing the buffer is not considered
    /// an eviction.
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
        for (int i = 0; i < count && TryDequeue(out _); i++)
        {
        }
    }

    /// <summary>
    /// Determines whether the buffer contains the specified element using <see cref="EqualityComparer{T}.Default" />.
    /// </summary>
    /// <param name="item">The element to locate. May be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if a sequence-stable read found a match within the live region during the call;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Walks the live region using a sequence-validated direct slot scan; no array allocation occurs in the common
    /// case. Under sustained concurrent modification the scan restarts; if the retry budget is exhausted, a single
    /// coherent <see cref="ToArray" /> snapshot is used as a fallback.
    /// </para>
    /// </remarks>
    public bool Contains(T? item)
    {
        EqualityComparer<T?> comparer = EqualityComparer<T?>.Default;
        SpinWait spinner = default;

        for (int outerAttempt = 0; outerAttempt < SnapshotOuterRetryBudget; outerAttempt++)
        {
            int head = Volatile.Read(ref _head);
            int tail = Volatile.Read(ref _tail);
            int count = tail - head;
            if (count <= 0)
                return false;
            if (count > _capacity)
                count = _capacity;

            bool slotFailure = false;
            for (int i = 0; i < count; i++)
            {
                int position = head + i;
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
    /// Copies a snapshot of the buffer to <paramref name="array" /> starting at <paramref name="index" />.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.</exception>
    /// <exception cref="ArgumentException">
    /// The number of elements in the buffer exceeds the available space in <paramref name="array" /> from
    /// <paramref name="index" /> onward.
    /// </exception>
    public void CopyTo(T[] array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(index, nameof(index));
        T[] snap = ToArray();
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + snap.Length);

        Array.Copy(snap, 0, array, index, snap.Length);
    }

    /// <summary>
    /// Removes and returns the oldest element.
    /// </summary>
    /// <returns>The oldest element in the buffer.</returns>
    /// <exception cref="InvalidOperationException">The buffer is empty.</exception>
    public T Dequeue() =>
        TryDequeue(out T? item)
            ? item!
            : throw new InvalidOperationException(ConcurrentCollectionsResourceStrings.Op_Invalid_CollectionEmpty);

    /// <summary>
    /// Adds an element to the end of the buffer, throwing when full if overwriting is disabled.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null" />.</param>
    /// <exception cref="InvalidOperationException">
    /// The buffer is full and <see cref="AllowOverwrite" /> is <see langword="false" />.
    /// </exception>
    public void Enqueue(T item) =>
        InternalEnqueue(item, throwIfFull: true);

    /// <summary>
    /// Returns the oldest element without removing it.
    /// </summary>
    /// <returns>The oldest element in the buffer.</returns>
    /// <exception cref="InvalidOperationException">The buffer is empty.</exception>
    public T Peek() =>
        TryPeek(out T? item)
            ? item!
            : throw new InvalidOperationException(ConcurrentCollectionsResourceStrings.Op_Invalid_CollectionEmpty);

    /// <summary>
    /// Returns a snapshot of the buffer's contents in FIFO order.
    /// </summary>
    /// <returns>
    /// An array containing the elements observed in the buffer, ordered from oldest to newest. Returns an empty array
    /// if the buffer is empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Each slot in the snapshot is read using a sequence-validated seqlock pattern: the slot's coordination sequence
    /// is read both before and after the value, and the read is committed only when both sequence observations match
    /// the expected published mark. This guarantees the value, when committed, was the element published at that
    /// logical position — never a value from an earlier or later generation.
    /// </para>
    /// <para>
    /// If a slot cannot be stabilized within its retry budget, the entire snapshot is restarted. After the outer retry
    /// budget is exhausted under sustained churn, a best-effort snapshot is returned in which individual slots that
    /// still cannot be stabilized contribute the default value of <typeparamref name="T" />; every committed slot in
    /// that fallback path is still sequence-validated, so a torn or stale-generation reference is never returned.
    /// </para>
    /// </remarks>
    public T[] ToArray()
    {
        SpinWait spinner = default;

        for (int outerAttempt = 0; outerAttempt < SnapshotOuterRetryBudget; outerAttempt++)
        {
            int head = Volatile.Read(ref _head);
            int tail = Volatile.Read(ref _tail);

            int count = tail - head;
            if (count <= 0)
                return [];
            if (count > _capacity)
                count = _capacity;

            var result = new T[count];
            bool slotFailure = false;

            for (int i = 0; i < count; i++)
            {
                int position = head + i;
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
    /// Attempts to remove and return the oldest element.
    /// </summary>
    /// <param name="item">
    /// When this method returns <see langword="true" />, contains the removed element; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an element was successfully removed; <see langword="false" /> if the buffer was
    /// empty.
    /// </returns>
    public bool TryDequeue(out T? item) =>
        InternalDequeue(out item, throwIfEmpty: false);

    /// <summary>
    /// Attempts to add an element to the end of the buffer without throwing when full.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the element was enqueued; <see langword="false" /> if the buffer is full and
    /// <see cref="AllowOverwrite" /> is <see langword="false" />.
    /// </returns>
    public bool TryEnqueue(T item) =>
        InternalEnqueue(item, throwIfFull: false);

    /// <summary>
    /// Attempts to return the oldest element without removing it.
    /// </summary>
    /// <param name="item">
    /// When this method returns <see langword="true" />, contains the oldest element; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an element was found; <see langword="false" /> if the buffer was empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method retries on transient races where another thread concurrently dequeues the head element between the
    /// head position read and the slot sequence check. It returns <see langword="false" /> only when the buffer is
    /// observed to be empty.
    /// </para>
    /// </remarks>
    public bool TryPeek(out T? item)
    {
        var spinner = default(SpinWait);

        while (true)
        {
            int head = Volatile.Read(ref _head);
            ref Slot slot = ref _buffer[SlotIndex(head)];

            int seq = Volatile.Read(ref slot.Sequence);
            int diff = seq - (head + 1);

            if (diff == 0)
            {
                // The slot looked published for the observed head. Read the value, then validate
                // that neither the logical head nor the slot sequence changed while the value was
                // being read. If either changed, a concurrent dequeue/overwrite may have cleared or
                // republished the slot, so retry rather than returning a torn observation.
                T? value = Volatile.Read(ref slot.Value);

                if (Volatile.Read(ref _head) == head &&
                    Volatile.Read(ref slot.Sequence) == seq)
                {
                    item = value;
                    return true;
                }

                spinner.SpinOnce();
                continue;
            }

            if (diff < 0)
            {
                // The observed head slot has not yet been published, so the buffer is empty relative
                // to this head observation.
                item = default;
                return false;
            }

            // diff > 0: stale head read — another thread dequeued or advanced past this slot; retry.
            spinner.SpinOnce();
        }
    }

    /// <summary>
    /// Produces a best-effort snapshot when the standard <see cref="ToArray" /> retry budget is exhausted. Each slot is
    /// still sequence-validated; slots that cannot be stabilized are written as <see langword="default" /> rather than
    /// as a torn or stale-generation value.
    /// </summary>
    /// <returns>The best-effort snapshot array.</returns>
    private T[] BestEffortSnapshot()
    {
        int head = Volatile.Read(ref _head);
        int tail = Volatile.Read(ref _tail);
        int count = Math.Clamp(tail - head, 0, _capacity);
        if (count == 0)
            return [];

        var result = new T[count];
        for (int i = 0; i < count; i++)
        {
            int position = head + i;
            if (TryReadStableSlot(SlotIndex(position), position + 1, out T? value))
                result[i] = value!;

            // Otherwise leave result[i] at default(T): null for the class? constraint on this type.
        }

        return result;
    }

    /// <summary>
    /// Evicts exactly one item from the head (used when overwriting). Fires <see cref="ItemEvicted" /> after removal.
    /// Ordinary handler exceptions are isolated per handler and suppressed; a process-fatal
    /// <see cref="OutOfMemoryException" /> is allowed to propagate.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if an item was evicted; <see langword="false" /> if the buffer was empty.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvictOne()
    {
        var spinner = default(SpinWait);

        while (true)
        {
            int head = Volatile.Read(ref _head);
            ref Slot slot = ref _buffer[SlotIndex(head)];

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
                Volatile.Write(ref slot.Value, default);
                Volatile.Write(ref slot.Sequence, head + _capacity);

                // AFTER removal: fire event; each handler is guarded independently so a throwing
                // subscriber cannot prevent subsequent subscribers from receiving the notification.
                // The invocation list is materialized at most once per subscription change: the cache
                // pairs the delegate instance with its handler array in one immutable object, so a
                // racing refresh publishes a coherent pair and the steady-state eviction path no longer
                // allocates a Delegate[] per evicted item inside the producer's enqueue loop.
                Action<T>? onEvicted = ItemEvicted;
                if (onEvicted != null)
                {
                    EvictionHandlers? cache = _evictionHandlers;
                    if (cache is null || !ReferenceEquals(cache.Source, onEvicted))
                        _evictionHandlers = cache = new EvictionHandlers(onEvicted);

                    foreach (Action<T> handler in cache.Handlers)
                    {
                        try
                        {
                            handler(value!);
                        }
                        catch (Exception ex) when (ex is not OutOfMemoryException)
                        { /* isolate ordinary handler failures; let process-fatal exceptions propagate */
                        }
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
    /// Populates the freshly-constructed buffer directly from <paramref name="items" />, retaining the trailing
    /// <see cref="Capacity" /> elements when the source is larger. The instance is not yet observable by other threads
    /// at this point, so the lock-free producer protocol is bypassed.
    /// </summary>
    /// <param name="items">The materialized source elements.</param>
    /// <remarks>
    /// <para>
    /// Writes occur in published-state form: each populated slot's <c>Sequence</c> is set to its publication mark (
    /// <c>tail + 1</c>) and <see cref="_tail" /> is advanced once at the end. <see cref="_head" /> remains zero, so the
    /// live region is <c>[0, tail)</c>.
    /// </para>
    /// </remarks>
    private void InitialFill(T[] items)
    {
        int capacity = _capacity;
        int sourceLength = items.Length;
        int copyLength = sourceLength <= capacity ? sourceLength : capacity;
        int sourceOffset = sourceLength - copyLength;

        for (int i = 0; i < copyLength; i++)
        {
            _buffer[i].Value = items[sourceOffset + i];
            _buffer[i].Sequence = i + 1;
        }

        _tail = copyLength;
    }

    /// <summary>
    /// Removes the oldest element using the lock-free consumer protocol.
    /// </summary>
    /// <param name="item">
    /// When this method returns <see langword="true" />, contains the removed element; otherwise the default value of
    /// <typeparamref name="T" />.
    /// </param>
    /// <param name="throwIfEmpty">
    /// If <see langword="true" />, throws <see cref="InvalidOperationException" /> when the buffer is empty; if
    /// <see langword="false" />, returns <see langword="false" /> on empty.
    /// </param>
    /// <returns><see langword="true" /> if an element was removed; otherwise <see langword="false" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// The buffer is empty and <paramref name="throwIfEmpty" /> is <see langword="true" />.
    /// </exception>
    private bool InternalDequeue(out T? item, bool throwIfEmpty)
    {
        var spinner = default(SpinWait);

        while (true)
        {
            int head = Volatile.Read(ref _head);
            ref Slot slot = ref _buffer[SlotIndex(head)];

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
                Volatile.Write(ref slot.Value, default);
                Volatile.Write(ref slot.Sequence, head + _capacity);
                return true;
            }
            else if (diff < 0)
            {
                // empty
                item = default;
                return throwIfEmpty
                    ? throw new InvalidOperationException(ConcurrentCollectionsResourceStrings.Op_Invalid_CollectionEmpty)
                    : false;
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
    /// <param name="item">The element to add. The value may be <see langword="null" />.</param>
    /// <param name="throwIfFull">
    /// When <see langword="true" /> and <see cref="AllowOverwrite" /> is <see langword="false" />, a full buffer
    /// throws; when <see langword="false" /> the method returns <see langword="false" /> instead.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the element was enqueued; otherwise <see langword="false" /> when the buffer is full
    /// and overwriting is disabled.
    /// </returns>
    private bool InternalEnqueue(T item, bool throwIfFull)
    {
        var spinner = default(SpinWait);

        while (true)
        {
            int tail = Volatile.Read(ref _tail);
            ref Slot slot = ref _buffer[SlotIndex(tail)];

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
                return true;
            }
            else if (diff < 0)
            {
                // looks full wrt this slot
                if (!AllowOverwrite)
                {
                    return throwIfFull
                        ? throw new InvalidOperationException(ConcurrentCollectionsResourceStrings.Op_Invalid_CapacityExhausted)
                        : false;
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
    /// Computes a non-negative slot index from a monotonically increasing counter position.
    /// </summary>
    /// <param name="position">The monotonically-increasing producer or consumer counter value.</param>
    /// <returns>The wrapped slot index in the range <c>[0, _capacity)</c>.</returns>
    /// <remarks>
    /// Uses unsigned modulo to guarantee a non-negative result even after <see cref="int" /> counter overflow, where
    /// plain <c>position % _capacity</c> would yield a negative index in C#. The mapping is a clean permutation across
    /// the counter's <c>2^32</c> wrap only when the capacity is a power of two; for a non-power-of-two capacity a
    /// single slot may misalign once per <c>2^32</c> operations (see the class remarks).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SlotIndex(int position) => (int)((uint)position % (uint)_capacity);

    /// <summary>
    /// Attempts a sequence-validated read of the slot at the given physical index, expecting the slot to be in the
    /// published state for the supplied logical position.
    /// </summary>
    /// <param name="slotIndex">The physical slot index; must be in the range <c>[0, _capacity)</c>.</param>
    /// <param name="expectedSequence">
    /// The publication sequence the slot is expected to carry (<c>position + 1</c> for the slot's logical head-relative
    /// position).
    /// </param>
    /// <param name="value">
    /// On <see langword="true" />, contains the committed value read from the slot. On <see langword="false" />,
    /// contains <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the slot was observed in the expected published state both before and after the
    /// value read; <see langword="false" /> when the slot has been reclaimed, has not yet been published, or could not
    /// be stabilized within the inner retry budget.
    /// </returns>
    /// <remarks>
    /// Implements the seqlock read protocol: read sequence, read value, re-read sequence; commit when both sequence
    /// reads equal <paramref name="expectedSequence" />. A divergence between the two sequence reads indicates the slot
    /// has been touched by a concurrent producer or consumer between the value read and the post-check.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryReadStableSlot(int slotIndex, int expectedSequence, out T? value)
    {
        SpinWait spinner = default;
        for (int attempt = 0; attempt < SlotReadRetryBudget; attempt++)
        {
            int seqPre = Volatile.Read(ref _buffer[slotIndex].Sequence);
            if (seqPre - expectedSequence != 0)
            {
                value = default;
                return false;
            }

            T? candidate = Volatile.Read(ref _buffer[slotIndex].Value);
            int seqPost = Volatile.Read(ref _buffer[slotIndex].Sequence);
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
    /// Ring slot holding a coordination sequence number and a stored value, with trailing padding to prevent false
    /// sharing between adjacent slots on multi-core hardware.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two hot fields — <see cref="Sequence" /> (written on every enqueue and dequeue) and <see cref="Value" />
    /// (written on every enqueue) — are declared first, followed by seven <see cref="long" /> padding fields. Under
    /// <see cref="LayoutKind.Sequential" /> the declaration order is the memory order, so the padding trails the hot
    /// fields and isolates them from the <em>next</em> slot's hot fields, pushing the struct past a 64-byte cache line.
    /// Padding placed ahead of the hot fields would not achieve this — it would only separate them from the slot's own
    /// cold prefix while leaving them adjacent to the following slot.
    /// </para>
    /// <para>
    /// <see cref="LayoutKind.Explicit" /> cannot be used here because the CLR prohibits explicit layout on structs
    /// nested within generic types. <see cref="LayoutKind.Sequential" /> with trailing padding fields is the correct
    /// alternative.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    private struct Slot
    {
        /// <summary>The Vyukov sequence number used to coordinate producers and consumers for this slot.</summary>
        public int Sequence;

        /// <summary>The stored element. Written by the producer, cleared by the consumer.</summary>
        public T? Value;

        /// <summary>Trailing padding to isolate this slot's hot fields from the next slot's, avoiding false sharing.</summary>
        private readonly long _pad0;

        /// <summary>Trailing padding to isolate this slot's hot fields from the next slot's, avoiding false sharing.</summary>
        private readonly long _pad1;

        /// <summary>Trailing padding to isolate this slot's hot fields from the next slot's, avoiding false sharing.</summary>
        private readonly long _pad2;

        /// <summary>Trailing padding to isolate this slot's hot fields from the next slot's, avoiding false sharing.</summary>
        private readonly long _pad3;

        /// <summary>Trailing padding to isolate this slot's hot fields from the next slot's, avoiding false sharing.</summary>
        private readonly long _pad4;

        /// <summary>Trailing padding to isolate this slot's hot fields from the next slot's, avoiding false sharing.</summary>
        private readonly long _pad5;

        /// <summary>Trailing padding to isolate this slot's hot fields from the next slot's, avoiding false sharing.</summary>
        private readonly long _pad6;
    }
}
