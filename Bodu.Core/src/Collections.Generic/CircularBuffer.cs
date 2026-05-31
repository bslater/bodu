// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBuffer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a fixed-size, first-in first-out (FIFO) circular buffer with optional overwrite-on-full semantics.
/// Elements are inserted at the tail and removed from the head; once the buffer reaches
/// <see cref="RingBackedCollection{T}.Capacity" />, the <see cref="AllowOverwrite" /> property determines whether
/// further inserts evict the oldest element or are rejected.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the buffer.</typeparam>
/// <remarks>
/// <para>
/// <see cref="CircularBuffer{T}" /> is the single-ended member of the ring-backed collection family. Like
/// <see cref="Deque{T}" />, it stores its elements in a contiguous backing array using head and tail indices that wrap
/// around modulo the capacity, giving O(1) cost for adds, removes, and peeks.
/// </para>
/// <para>
/// The behavior on a full buffer is controlled by the mutable <see cref="AllowOverwrite" /> property:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>AllowOverwrite = true</c> (the default for the parameterless and capacity-only constructors) — adds to a full
/// buffer evict the oldest element to make room for the new one. The <see cref="ItemEvicting" /> event fires before the
/// eviction (and may veto it by throwing) and the <see cref="ItemEvicted" /> event fires after.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>AllowOverwrite = false</c> — <see cref="Enqueue" /> throws <see cref="InvalidOperationException" /> when the
/// buffer is full; <see cref="TryEnqueue" /> returns <see langword="false" /> without modifying state.
/// </description>
/// </item>
/// </list>
/// <para>
/// Key operations:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Enqueue(T)" /> / <see cref="TryEnqueue(T)" /> — add an element at the tail.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Dequeue" /> / <see cref="TryDequeue(out T)" /> — remove and return the oldest (head) element.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Peek" /> / <see cref="TryPeek(out T)" /> — read the oldest element without removing it.
/// </description>
/// </item>
/// <item>
/// <description>
/// Inherited <see cref="RingBackedCollection{T}.TrimExcess" /> — shrink the backing array to <c>Count</c>.
/// </description>
/// </item>
/// </list>
/// <para>
/// For a double-ended counterpart with the same fixed-vs-growable choice, see <see cref="Deque{T}" />. For thread-safe
/// concurrent FIFO access, see <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}" />;
/// <see cref="CircularBuffer{T}" /> itself is not thread-safe.
/// </para>
/// <para>
/// <see cref="CircularBuffer{T}" /> accepts <see langword="null" /> values for reference types and allows duplicate
/// elements.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Sliding window of the three most recent samples — overwrite-on-full is the default.
/// var window = new CircularBuffer<int>(capacity: 3);
/// window.Enqueue(1);
/// window.Enqueue(2);
/// window.Enqueue(3);
/// window.Enqueue(4); // evicts 1; ItemEvicted fires with value 1
/// Console.WriteLine(window.Peek()); // 2 (oldest remaining)
///
/// Fixed FIFO queue that rejects further pushes when full.
/// var bounded = new CircularBuffer<int>(capacity: 2, allowOverwrite: false);
/// bounded.Enqueue(10);
/// bounded.Enqueue(20);
/// bool added = bounded.TryEnqueue(30); // false — buffer is full
///]]>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(CircularBufferDebugView<>))]
[Serializable]
public sealed class CircularBuffer<T>
    : RingBackedCollection<T>
{
    private const int DefaultCapacity = 16;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class using the default capacity and allowing
    /// overwrites by default.
    /// </summary>
    public CircularBuffer()
        : this(DefaultCapacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class with the specified capacity, allowing
    /// overwrites when full.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    public CircularBuffer(int capacity)
        : this(capacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class with the specified capacity and
    /// overwrite behavior.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <param name="allowOverwrite">
    /// <see langword="true" /> to allow the buffer to automatically overwrite the oldest elements when full;
    /// <see langword="false" /> to prevent adding new elements when the buffer has reached capacity, which will cause
    /// an exception to be thrown during insertion.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    public CircularBuffer(int capacity, bool allowOverwrite)
        : base(capacity)
    {
        AllowOverwrite = allowOverwrite;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class by copying elements from the specified
    /// collection, using the default capacity and allowing overwriting if needed.
    /// </summary>
    /// <param name="collection">
    /// The collection from which elements are copied. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    public CircularBuffer(IEnumerable<T> collection)
        : this(collection, DefaultCapacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class by copying elements from the specified
    /// collection and applying the specified capacity. Overwriting is enabled by default.
    /// </summary>
    /// <param name="collection">
    /// The collection from which elements are copied. Must not be <see langword="null" />.
    /// </param>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    public CircularBuffer(IEnumerable<T> collection, int capacity)
        : this(collection, capacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class by copying elements from the specified
    /// collection, applying the specified capacity and overwrite behavior.
    /// </summary>
    /// <param name="collection">
    /// The collection from which elements are copied. Must not be <see langword="null" />.
    /// </param>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <param name="allowOverwrite">
    /// If <see langword="true" />, the most recent elements from the collection are retained if its size exceeds
    /// <paramref name="capacity" />. If <see langword="false" />, the collection size must not exceed
    /// <paramref name="capacity" />, or an exception is thrown.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="allowOverwrite" /> is <see langword="false" /> and the collection contains more
    /// elements than the buffer capacity.
    /// </exception>
    public CircularBuffer(IEnumerable<T> collection, int capacity, bool allowOverwrite)
        : base(MaterializeWithOverflowPolicy(collection, capacity, allowOverwrite), capacity)
    {
        AllowOverwrite = allowOverwrite;
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an item has been evicted from the <see cref="CircularBuffer{T}" /> due to
    /// capacity limits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised only when the buffer reaches capacity and <see cref="AllowOverwrite" /> is
    /// <see langword="true" />.
    /// </para>
    /// <para>
    /// <b>Important:</b> Exceptions thrown by event handlers are not caught and will propagate to the caller of
    /// <see cref="Enqueue" /> or <see cref="TryEnqueue" />. Consumers should ensure event handlers are exception-safe.
    /// </para>
    /// </remarks>
    public event Action<T>? ItemEvicted;

    /// <summary>
    /// Occurs immediately <b>before</b> an item is evicted from the <see cref="CircularBuffer{T}" /> due to capacity
    /// limits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised only when the buffer has reached its capacity and <see cref="AllowOverwrite" /> is
    /// <see langword="true" />.
    /// </para>
    /// <para>
    /// <b>Important:</b> Any exception thrown from a handler vetoes the eviction in place — the oldest element is not
    /// removed, the new element is not stored, the count, head, and tail indices are unchanged, and the exception
    /// propagates to the caller of <see cref="Enqueue" /> or <see cref="TryEnqueue" />. Event handlers should therefore
    /// avoid throwing unless the veto is intentional.
    /// </para>
    /// </remarks>
    public event Action<T>? ItemEvicting;

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="CircularBuffer{T}" /> will automatically overwrite the
    /// oldest element when capacity is reached.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to overwrite the oldest item when the buffer is full; <see langword="false" /> to throw
    /// an exception instead of overwriting.
    /// </value>
    public bool AllowOverwrite { get; set; }

    /// <summary>
    /// Materializes <paramref name="collection" /> into an array exactly once and enforces the no-overwrite overflow
    /// policy before the base constructor sees the source. Returning a <c>T[]</c> avoids a second enumeration in the
    /// base ctor.
    /// </summary>
    /// <param name="collection">The collection from which elements will be copied.</param>
    /// <param name="capacity">The maximum number of elements the buffer can contain.</param>
    /// <param name="allowOverwrite">Whether eviction is permitted; controls whether the size check is enforced.</param>
    /// <returns>The materialized array; never <see langword="null" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="allowOverwrite" /> is <see langword="false" /> and the collection size exceeds
    /// <paramref name="capacity" />.
    /// </exception>
    /// <remarks>
    /// When eviction is permitted, only the trailing <paramref name="capacity" /> elements would survive the base-ctor
    /// trim, so the source is materialized through <see cref="Enumerable.TakeLast{TSource}" /> for non-array inputs to
    /// bound the allocation. The no-overwrite path still requires a full enumeration to enforce the size contract.
    /// </remarks>
    private static T[] MaterializeWithOverflowPolicy(IEnumerable<T> collection, int capacity, bool allowOverwrite)
    {
        ThrowHelper.ThrowIfNull(collection);

        if (allowOverwrite)
            return collection as T[] ?? [.. collection.TakeLast(capacity)];

        T[] items = collection as T[] ?? [.. collection];
        return items.Length > capacity ? throw new InvalidOperationException(ResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity) : items;
    }

    /// <summary>
    /// Removes and returns the oldest element from the buffer.
    /// </summary>
    /// <returns>The oldest element in the buffer.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the buffer is empty when <see cref="Dequeue" /> is called.
    /// </exception>
    public T Dequeue() => Count == 0 ? throw new InvalidOperationException(ResourceStrings.Op_Invalid_EmptySequence) : RemoveHead();

    /// <summary>
    /// Adds an element to the end of the buffer.
    /// </summary>
    /// <param name="item">The element to add. Can be <see langword="null" /> for reference types.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the buffer is at full capacity and <see cref="AllowOverwrite" /> is <see langword="false" />.
    /// </exception>
    public void Enqueue(T item) => _ = TryEnqueueInternal(item, throwIfFull: true);

    /// <summary>
    /// Returns the oldest element in the <see cref="CircularBuffer{T}" /> without removing it.
    /// </summary>
    /// <returns>The element at the front of the buffer (the oldest item).</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the buffer is empty (i.e., <see cref="RingBackedCollection{T}.Count" /> equals <c>0</c>).
    /// </exception>
    public T Peek() => Count == 0 ? throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionEmpty) : PeekHead();

    /// <summary>
    /// Attempts to remove and return the oldest element from the <see cref="CircularBuffer{T}" /> without throwing an
    /// exception.
    /// </summary>
    /// <param name="item">
    /// When this method returns, contains the dequeued element if one was available; otherwise, the default value of
    /// <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an item was successfully dequeued; otherwise, <see langword="false" /> if the buffer
    /// was empty.
    /// </returns>
    public bool TryDequeue(out T item)
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
    /// Attempts to add an element to the end of the <see cref="CircularBuffer{T}" /> without throwing an exception.
    /// </summary>
    /// <param name="item">The element to add. Can be <see langword="null" /> for reference types.</param>
    /// <returns>
    /// <see langword="true" /> if the item was successfully enqueued; <see langword="false" /> if the buffer is full
    /// and <see cref="AllowOverwrite" /> is <see langword="false" />.
    /// </returns>
    public bool TryEnqueue(T item) => TryEnqueueInternal(item, throwIfFull: false);

    /// <summary>
    /// Attempts to retrieve the oldest element from the <see cref="CircularBuffer{T}" /> without removing it.
    /// </summary>
    /// <param name="item">
    /// When this method returns, contains the oldest element if the buffer is not empty; otherwise, contains the
    /// default value for <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an item was successfully retrieved; <see langword="false" /> if the buffer is empty.
    /// </returns>
    public bool TryPeek(out T item)
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
    /// Single internal Enqueue implementation shared by <see cref="Enqueue" /> and <see cref="TryEnqueue" />.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <param name="throwIfFull">
    /// If <see langword="true" />, throws when the buffer is full and <see cref="AllowOverwrite" /> is
    /// <see langword="false" />; otherwise returns <see langword="false" />.
    /// </param>
    /// <returns><see langword="true" /> when the item was added (or evicted-and-replaced).</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="throwIfFull" /> is <see langword="true" /> and the buffer is full while
    /// <see cref="AllowOverwrite" /> is <see langword="false" />.
    /// </exception>
    private bool TryEnqueueInternal(T item, bool throwIfFull)
    {
        if (Count == Capacity)
        {
            if (!AllowOverwrite)
            {
                return throwIfFull ? throw new InvalidOperationException(ResourceStrings.Op_Invalid_CapacityExhausted) : false;
            }

            // When full, head == tail, so PeekHead returns the slot the eviction will overwrite.
            // Capture before raising ItemEvicting so a handler exception vetoes the eviction in place.
            T overwritten = PeekHead();
            ItemEvicting?.Invoke(overwritten);

            OverwriteTail(item);

            ItemEvicted?.Invoke(overwritten);
        }
        else
        {
            AddTail(item);
        }

        return true;
    }
}
