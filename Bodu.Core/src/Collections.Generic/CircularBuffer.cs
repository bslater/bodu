// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="CircularBuffer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a first-in, first-out (FIFO) collection of elements using a fixed-size circular buffer with optional overwrite support.
/// </summary>
/// <typeparam name="T">Specifies the type of elements in the collection.</typeparam>
/// <remarks>
/// <para>
/// <see cref="CircularBuffer{T}" /> is a high-performance collection for storing a bounded number of elements in a circular manner.
/// Elements are inserted at the tail and removed from the head. When the buffer reaches capacity, new elements overwrite the oldest
/// entries if <see cref="AllowOverwrite" /> is <see langword="true" />.
/// </para>
/// <para>
/// This type is not thread-safe. If concurrent access is required, use
/// <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}" /> instead.
/// </para>
/// <para>Key operations include:</para>
/// <list type="bullet">
/// <item>
/// <description><see cref="Enqueue" /> and <see cref="TryEnqueue" /> - Add elements to the buffer.</description>
/// </item>
/// <item>
/// <description><see cref="Dequeue" /> and <see cref="TryDequeue" /> - Remove and return the oldest element.</description>
/// </item>
/// <item>
/// <description><see cref="Peek" /> and <see cref="TryPeek" /> - View the oldest element without removing it.</description>
/// </item>
/// </list>
/// <para>
/// The <see cref="Capacity" /> property defines the maximum number of elements the buffer can hold. If the buffer is full and
/// <see cref="AllowOverwrite" /> is <see langword="false" />, attempts to enqueue additional elements will throw an <see cref="InvalidOperationException" />.
/// </para>
/// <para><see cref="CircularBuffer{T}" /> accepts <see langword="null" /> values (for reference types) and allows duplicate elements.</para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// Create a circular buffer with capacity for 3 items
/// var buffer = new CircularBuffer<int>(capacity: 3);
///
/// Enqueue three items
/// buffer.Enqueue(1);
/// buffer.Enqueue(2);
/// buffer.Enqueue(3);                // Buffer now contains 1, 2, 3
/// buffer.TryEnqueue(4);            // false
///
/// Attempt to add a fourth item (will throw if AllowOverwrite is false)
/// try
/// {
///     buffer.Enqueue(4);            // InvalidOperationException
/// }
/// catch (InvalidOperationException ex)
/// {
///     Console.WriteLine("Buffer full: " + ex.Message);
/// }
///
/// Enable overwrite mode to allow overwriting the oldest element
/// buffer.AllowOverwrite = true;
///
/// Now enqueuing will overwrite the oldest item (1)
/// buffer.Enqueue(4);                // Buffer now contains 2, 3, 4
///
/// Dequeue all items to verify the order
/// while (buffer.TryDequeue(out int value))
/// {
///     Console.WriteLine(value);    // Outputs: 2, 3, 4
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(CircularBufferDebugView<>))]
[Serializable]
public partial class CircularBuffer<T>
{
    private const int DefaultCapacity = 16;
#if !NET6_0_OR_GREATER
    private const int MaxArrayLength = 0x7FFFFFC7; // 2,147,483,647 - 1
#endif

    private int _capacity;
    private int _count;
    private int _head;
    private T[] _internalBuffer;
    private int _tail;
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class using the default capacity and allowing overwrites by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This constructor initializes the buffer with a default capacity defined by the internal <c>DefaultCapacity</c> constant. When the
    /// buffer becomes full, new items will overwrite the oldest elements.
    /// </para>
    /// <para>
    /// To customize capacity or overwrite behavior, use an overloaded constructor such as
    /// <see cref="CircularBuffer{T}.CircularBuffer(int)" /> or <see cref="CircularBuffer{T}.CircularBuffer(int, bool)" />.
    /// </para>
    /// </remarks>
    public CircularBuffer()
        : this(DefaultCapacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class with the specified capacity, allowing overwrites when full.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// <para>
    /// When the number of items exceeds <paramref name="capacity" />, older elements are automatically overwritten. To disable overwrites,
    /// use the <see cref="CircularBuffer{T}.CircularBuffer(int, bool)" /> constructor with <c>allowOverwrite: false</c>.
    /// </para>
    /// </remarks>
    public CircularBuffer(int capacity)
        : this(capacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class with the specified capacity and overwrite behavior.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <param name="allowOverwrite">
    /// <see langword="true" /> to allow the buffer to automatically overwrite the oldest elements when full; <see langword="false" /> to
    /// prevent adding new elements when the buffer has reached capacity, which will cause an exception to be thrown during insertion.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// Use this constructor when you need to control whether the buffer should overwrite old data once full or strictly enforce capacity.
    /// </remarks>
    public CircularBuffer(int capacity, bool allowOverwrite)
    {
#if NET6_0_OR_GREATER
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, Array.MaxLength);
#else
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, MaxArrayLength);
#endif
        this._internalBuffer = new T[capacity];
        this._capacity = capacity;
        this.AllowOverwrite = allowOverwrite;
        this._count = 0;
        this._head = 0;
        this._tail = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class by copying elements from the specified collection, using
    /// the default capacity and allowing overwriting if needed.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// The default capacity is defined by <c>DefaultCapacity</c>. If the collection contains more elements than the buffer can hold, only
    /// the most recent items up to the buffer's capacity are retained. Older items are discarded during construction.
    /// </remarks>
    public CircularBuffer(IEnumerable<T> collection)
        : this(collection, DefaultCapacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class by copying elements from the specified collection and
    /// applying the specified capacity. Overwriting is enabled by default.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null" />.</param>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// If the number of elements in <paramref name="collection" /> exceeds <paramref name="capacity" />, only the most recent items are
    /// retained. Older items are discarded during construction.
    /// </remarks>
    public CircularBuffer(IEnumerable<T> collection, int capacity)
        : this(collection, capacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}" /> class by copying elements from the specified collection, applying
    /// the specified capacity and overwrite behavior.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null" />.</param>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <param name="allowOverwrite">
    /// If <see langword="true" />, the most recent elements from the collection are retained if its size exceeds
    /// <paramref name="capacity" />. If <see langword="false" />, the collection size must not exceed <paramref name="capacity" />, or an
    /// exception is thrown.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="allowOverwrite" /> is <see langword="false" /> and the collection contains more elements than the buffer capacity.
    /// </exception>
    /// <remarks>
    /// When the number of items in the collection exceeds the capacity, only the most recent items are copied into the buffer (if
    /// overwriting is allowed).
    /// </remarks>
    public CircularBuffer(IEnumerable<T> collection, int capacity, bool allowOverwrite)
    {
        ThrowHelper.ThrowIfNull(collection);
#if NET6_0_OR_GREATER
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, Array.MaxLength);
#else
        ThrowHelper.ThrowIfOutOfRange(capacity, 1, MaxArrayLength);
#endif

        T[] items = collection as T[] ?? collection.ToArray();

        if (items.Length > capacity && !allowOverwrite)
            throw new InvalidOperationException(ResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity);

        this._internalBuffer = new T[capacity];
        this._capacity = capacity;
        this.AllowOverwrite = allowOverwrite;

        if (items.Length > capacity)
        {
            // Retain the most recent elements that fit in the buffer.
            Array.Copy(items, items.Length - capacity, this._internalBuffer, 0, capacity);
            this._count = capacity;
        }
        else
        {
            Array.Copy(items, this._internalBuffer, items.Length);
            this._count = items.Length;
        }

        this._head = 0;
        this._tail = this._count % capacity;
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an item has been evicted from the <see cref="CircularBuffer{T}" /> due to capacity limits.
    /// </summary>
    /// <remarks>
    /// <para>This event is raised only when the buffer reaches capacity and <see cref="AllowOverwrite" /> is <see langword="true" />.</para>
    /// <para>
    /// It provides access to the evicted item, which has been removed from the buffer. You can use this event to log history, trigger
    /// cleanup actions, or propagate notifications.
    /// </para>
    /// <para>
    /// <b>Important:</b> Exceptions thrown by event handlers are not caught and will propagate to the caller of <see cref="Enqueue" />
    /// or <see cref="TryEnqueue" />. Consumers should ensure event handlers are exception-safe.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var buffer = new CircularBuffer<string>(capacity: 2, allowOverwrite: true);
    /// buffer.ItemEvicted += item => Console.WriteLine($"Evicted: {item}");
    ///
    /// buffer.Enqueue("A");
    /// buffer.Enqueue("B");
    /// buffer.Enqueue("C"); // Triggers ItemEvicted for "A"
    ///]]>
    /// </code>
    /// </example>
    public event Action<T>? ItemEvicted;

    /// <summary>
    /// Occurs immediately <b>before</b> an item is evicted from the <see cref="CircularBuffer{T}" /> due to capacity limits.
    /// </summary>
    /// <remarks>
    /// <para>This event is raised only when the buffer has reached its capacity and <see cref="AllowOverwrite" /> is <see langword="true" />.</para>
    /// <para>
    /// It allows consumers to inspect the item that is about to be removed from the buffer. This can be useful for pre-eviction
    /// validation, logging, synchronization with external systems, or veto logic.
    /// </para>
    /// <para>
    /// <b>Important:</b> If the event handler throws an exception, the item will not be evicted, and the <see cref="Enqueue" /> or
    /// <see cref="TryEnqueue" /> operation will propagate that exception. Event handlers should therefore avoid throwing unless intentional.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var buffer = new CircularBuffer<string>(capacity: 2, allowOverwrite: true);
    /// buffer.ItemEvicting += item => Console.WriteLine($"Evicting: {item}");
    ///
    /// buffer.Enqueue("A");
    /// buffer.Enqueue("B");
    /// buffer.Enqueue("C"); // Triggers ItemEvicting for "A"
    ///]]>
    /// </code>
    /// </example>
    public event Action<T>? ItemEvicting;

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="CircularBuffer{T}" /> will automatically overwrite the oldest element when
    /// capacity is reached.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to overwrite the oldest item when the buffer is full; <see langword="false" /> to throw an exception instead
    /// of overwriting.
    /// </value>
    /// <remarks>
    /// <para>
    /// When <see cref="AllowOverwrite" /> is set to <see langword="true" />, the buffer permits new items to overwrite the oldest item once
    /// <see cref="Capacity" /> is reached.
    /// </para>
    /// <para>
    /// When set to <see langword="false" />, attempting to <see cref="Enqueue(T)" /> or <see cref="TryEnqueue(T)" /> into a full buffer
    /// will throw an <see cref="InvalidOperationException" /> or return <see langword="false" />, respectively.
    /// </para>
    /// <para>This property can be toggled at runtime to change eviction behavior dynamically.</para>
    /// </remarks>
    public bool AllowOverwrite { get; set; }

    /// <summary>
    /// Gets the maximum number of elements that the <see cref="CircularBuffer{T}" /> can contain.
    /// </summary>
    /// <value>The total capacity of the buffer, which determines how many elements it can hold before reaching its limit.</value>
    /// <remarks>
    /// <para>
    /// The <see cref="Capacity" /> is fixed at construction and defines the maximum number of items that can be stored in the buffer at
    /// once. If <see cref="AllowOverwrite" /> is <see langword="true" />, adding an item to a full buffer will evict the oldest item.
    /// Otherwise, an exception is thrown or the addition fails depending on the method used.
    /// </para>
    /// <para>To reduce memory usage after elements are removed, use <see cref="TrimExcess" /> to shrink the buffer.</para>
    /// </remarks>
    public int Capacity => this._capacity;

    /// <summary>
    /// Gets the element at the specified zero-based index from the oldest to the newest element.
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve (0 refers to the oldest element).</param>
    /// <returns>The element stored at the specified index within the buffer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.</exception>
    /// <remarks>
    /// <para>
    /// The indexer provides read-only access to the elements in the buffer in logical order (from oldest to newest). Internally, the
    /// circular structure is resolved to return the correct element corresponding to the requested index.
    /// </para>
    /// <para>
    /// The index is relative to the logical start of the buffer. That is, <c>buffer[0]</c> returns the oldest element, and <c>buffer[Count
    /// - 1]</c> returns the most recently added item.
    /// </para>
    /// </remarks>
    public T this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfLessThan(index, 0);
            ThrowHelper.ThrowIfGreaterThanOrEqual(index, this._count);

            int actualIndex = (this._head + index) % this._capacity;
            return this._internalBuffer[actualIndex];
        }
    }

    /// <summary>
    /// Removes all elements from the <see cref="CircularBuffer{T}" />, resetting its state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method clears the buffer by resetting internal state and clearing array contents. After calling <see cref="Clear" />, the
    /// buffer’s <see cref="Count" /> becomes <c>0</c>, but the <see cref="Capacity" /> remains unchanged.
    /// </para>
    /// <para>
    /// Any subscribed <see cref="ItemEvicting" /> or <see cref="ItemEvicted" /> handlers are not invoked by this operation, as
    /// <see cref="Clear" /> does not count as eviction.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        if (this._count > 0)
        {
            if (this._head < this._tail)
            {
                Array.Clear(this._internalBuffer, this._head, this._count);
            }
            else
            {
                Array.Clear(this._internalBuffer, this._head, this._capacity - this._head);
                Array.Clear(this._internalBuffer, 0, this._tail);
            }

            this._head = this._tail = this._count = 0;
            this._version++;
        }
    }

    /// <summary>
    /// Determines whether the buffer contains a specific element.
    /// </summary>
    /// <param name="item">The element to locate in the buffer. Can be <see langword="null" /> for reference types.</param>
    /// <returns><see langword="true" /> if <paramref name="item" /> exists in the buffer; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// <para>This method performs a linear scan from the oldest to the newest element using the default equality comparer for <typeparamref name="T" />.</para>
    /// <para>The search is read-only and does not modify the buffer's state.</para>
    /// </remarks>
    public bool Contains(T item)
    {
        if (this._count == 0)
            return false;

        if (this._head < this._tail)
        {
            // Contiguous segment — delegate to Array.IndexOf which uses EqualityComparer<T>.Default
            // and can be JIT-vectorised on supported runtimes.
            return Array.IndexOf(this._internalBuffer, item, this._head, this._count) >= 0;
        }

        // Wrapped layout: [_head .. end) then [0 .. _tail)
        int firstSegmentLength = this._capacity - this._head;
        if (Array.IndexOf(this._internalBuffer, item, this._head, firstSegmentLength) >= 0)
            return true;

        return this._tail > 0 && Array.IndexOf(this._internalBuffer, item, 0, this._tail) >= 0;
    }

    /// <summary>
    /// Copies elements from the buffer to the specified target array, starting at the given array index.
    /// </summary>
    /// <param name="array">The destination array to copy elements into. Must not be <see langword="null" />.</param>
    /// <param name="index">The zero-based index in the destination array at which copying begins.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the number of elements in the buffer exceeds the available space in the target array starting at the specified <paramref name="index" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Elements are copied in logical FIFO order-from the oldest to the newest. If the buffer is wrapped internally, this method handles
    /// segmenting the copy operation appropriately.
    /// </para>
    /// <para>The target array must be large enough to accommodate the number of elements in the buffer.</para>
    /// </remarks>
    public void CopyTo(T[] array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index, this._count);

        this.CopyToInternal(array, index);
    }

    /// <summary>
    /// Removes and returns the oldest element from the buffer.
    /// </summary>
    /// <returns>The oldest element in the buffer.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the buffer is empty when <see cref="Dequeue" /> is called.</exception>
    /// <remarks>
    /// <para>
    /// This method removes the element that has been in the buffer the longest (FIFO behavior). If the buffer is empty, an
    /// <see cref="InvalidOperationException" /> is thrown.
    /// </para>
    /// <para>Use <see cref="TryDequeue(out T)" /> to avoid exceptions when the buffer may be empty.</para>
    /// </remarks>
    public T Dequeue()
    {
        this.TryDequeueInternal(out T? item, throwIfEmpty: true);
        return item;
    }

    /// <summary>
    /// Adds an element to the end of the buffer.
    /// </summary>
    /// <param name="item">The element to add. Can be <see langword="null" /> for reference types.</param>
    /// <exception cref="InvalidOperationException">Thrown if the buffer is at full capacity and <see cref="AllowOverwrite" /> is <see langword="false" />.</exception>
    /// <remarks>
    /// <para>
    /// If <see cref="AllowOverwrite" /> is <see langword="true" />, the oldest element in the buffer is evicted to make room for the new
    /// item. If <see cref="AllowOverwrite" /> is <see langword="false" />, an exception is thrown when the buffer is full.
    /// </para>
    /// <para>To avoid exceptions when the buffer may be full, use <see cref="TryEnqueue(T)" /> instead.</para>
    /// </remarks>
    public void Enqueue(T item) => _ = this.TryEnqueueInternal(item, throwIfFull: true);

    /// <summary>
    /// Returns the oldest element in the <see cref="CircularBuffer{T}" /> without removing it.
    /// </summary>
    /// <returns>The element at the front of the buffer (the oldest item).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the buffer is empty (i.e., <see cref="Count" /> equals <c>0</c>).</exception>
    /// <remarks>
    /// Use <see cref="Peek" /> to inspect the current front of the buffer without modifying its contents. If you need to remove the item as
    /// well, use <see cref="Dequeue" />.
    /// </remarks>
    public T Peek()
    {
        if (this._count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);

        return this._internalBuffer[this._head];
    }

    /// <summary>
    /// Copies the contents of the <see cref="CircularBuffer{T}" /> to a new array.
    /// </summary>
    /// <returns>A new one-dimensional array containing the buffer's elements, ordered from oldest to newest.</returns>
    /// <remarks>
    /// This method creates a shallow copy of the buffer's contents. It is useful for inspection, snapshotting, or passing the elements
    /// to APIs that require array input.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// <![CDATA[
    /// var buffer = new CircularBuffer<int>(3);
    /// buffer.Enqueue(1);
    /// buffer.Enqueue(2);
    /// buffer.Enqueue(3);
    /// int[] copy = buffer.ToArray(); // copy = [1, 2, 3]
    ///]]>
    /// </code>
    /// </example>
    public T[] ToArray()
    {
        T[] result = new T[this._count];
        if (this._count > 0)
            this.CopyToInternal(result, 0);

        return result;
    }

    /// <summary>
    /// Reduces the internal capacity of the <see cref="CircularBuffer{T}" /> to match the current number of elements, freeing unused memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is useful in scenarios where the buffer previously had a large capacity but no longer needs to retain that size. It
    /// creates a new internal array sized to the current <see cref="Count" /> and copies the existing elements into it.
    /// </para>
    /// <para>
    /// If the buffer is empty, the internal storage is reduced to a minimal size (at least one element) to ensure the buffer remains operational.
    /// </para>
    /// <para>
    /// After trimming, the <see cref="Capacity" /> will equal the current <see cref="Count" />, and the buffer will be reset to a
    /// zero-based internal index layout.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// <![CDATA[
    /// var buffer = new CircularBuffer<string>(100);
    /// buffer.Enqueue("A");
    /// buffer.Enqueue("B");
    /// buffer.TrimExcess(); // Reduces capacity to 2
    ///]]>
    ///</code>
    /// </example>
    public void TrimExcess()
    {
        int newCapacity = Math.Max(this._count, 1);
        if (newCapacity == this._capacity)
            return;
        T[] trimmed = new T[newCapacity];
        this.CopyTo(trimmed, 0);

        this._internalBuffer = trimmed;
        this._capacity = newCapacity;
        this._head = this._tail = 0;
        this._version++;
    }

    /// <summary>
    /// Attempts to remove and return the oldest element from the <see cref="CircularBuffer{T}" /> without throwing an exception.
    /// </summary>
    /// <param name="item">
    /// When this method returns, contains the dequeued element if one was available; otherwise, the default value of <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an item was successfully dequeued; otherwise, <see langword="false" /> if the buffer was empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call when the buffer may be empty. It avoids throwing <see cref="InvalidOperationException" /> and
    /// instead returns <see langword="false" /> if no items are available.
    /// </para>
    /// <para>Use this method when performance is critical or when you want to avoid exception-based control flow in empty-buffer scenarios.</para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var buffer = new CircularBuffer<string>(2);
    /// buffer.Enqueue("A");
    /// if (buffer.TryDequeue(out var value))
    ///     Console.WriteLine($"Removed: {value}");
    ///]]>
    /// </code>
    /// </example>
    public bool TryDequeue(out T item) => this.TryDequeueInternal(out item, throwIfEmpty: false);

    /// <summary>
    /// Attempts to add an element to the end of the <see cref="CircularBuffer{T}" /> without throwing an exception.
    /// </summary>
    /// <param name="item">The element to add. Can be <see langword="null" /> for reference types.</param>
    /// <returns>
    /// <see langword="true" /> if the item was successfully enqueued; <see langword="false" /> if the buffer is full and
    /// <see cref="AllowOverwrite" /> is <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call when the buffer may be at capacity. It avoids throwing <see cref="InvalidOperationException" /> and
    /// instead returns <see langword="false" /> if the item could not be enqueued.
    /// </para>
    /// <para>Use this method when performance is critical or when you want to avoid exception-based control flow in full-buffer scenarios.</para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var buffer = new CircularBuffer<string>(2, allowOverwrite: false);
    /// if (!buffer.TryEnqueue("X"))
    ///     Console.WriteLine("Item could not be added.");
    ///]]>
    /// </code>
    /// </example>
    public bool TryEnqueue(T item) => this.TryEnqueueInternal(item, throwIfFull: false);

    /// <summary>
    /// Attempts to retrieve the oldest element from the <see cref="CircularBuffer{T}" /> without removing it.
    /// </summary>
    /// <param name="item">
    /// When this method returns, contains the oldest element if the buffer is not empty; otherwise, contains the default value for <typeparamref name="T" />.
    /// </param>
    /// <returns><see langword="true" /> if an item was successfully retrieved; <see langword="false" /> if the buffer is empty.</returns>
    /// <remarks>
    /// <para>Use this method to inspect the oldest item in the buffer without modifying the buffer's contents.</para>
    /// <para>
    /// This method avoids throwing exceptions when the buffer is empty, making it suitable for high-throughput or exception-sensitive
    /// code paths.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var buffer = new CircularBuffer<int>(2);
    /// buffer.Enqueue(10);
    /// if (buffer.TryPeek(out int value))
    ///     Console.WriteLine($"Peeked: {value}");
    ///]]>
    /// </code>
    /// </example>
    public bool TryPeek(out T item)
    {
        if (this._count == 0)
        {
            item = default!;
            return false;
        }

        item = this._internalBuffer[this._head];
        return true;
    }

    /// <summary>
    /// Copies the contents of the circular buffer to the specified <see cref="Array" /> starting at the given index.
    /// </summary>
    /// <param name="destination">
    /// The destination array to which elements from the buffer will be copied. Must not be <see langword="null" /> and must have sufficient space.
    /// </param>
    /// <param name="destinationIndex">The zero-based index in the destination array at which copying begins.</param>
    /// <remarks>
    /// <para>
    /// This method performs the core logic for copying elements from the buffer to an external array, handling both contiguous and wrapped
    /// buffer layouts.
    /// </para>
    /// <para>
    /// If the buffer is empty, no operation is performed. If the buffer wraps around its internal array boundary, copying occurs in two
    /// segments. Type compatibility between the buffer element type and the destination array element type is enforced by
    /// <see cref="Array.Copy(Array, int, Array, int, int)" />, which throws <see cref="ArrayTypeMismatchException" /> on mismatch.
    /// </para>
    /// </remarks>
    private void CopyToInternal(Array destination, int destinationIndex)
    {
        if (this._count == 0)
            return;

        if (this._head < this._tail)
        {
            Array.Copy(this._internalBuffer, this._head, destination, destinationIndex, this._count);
        }
        else
        {
            int firstSegmentLength = this._capacity - this._head;
            Array.Copy(this._internalBuffer, this._head, destination, destinationIndex, firstSegmentLength);
            Array.Copy(this._internalBuffer, 0, destination, destinationIndex + firstSegmentLength, this._tail);
        }
    }

    /// <summary>
    /// Attempts to remove and return the oldest element from the <see cref="CircularBuffer{T}" />.
    /// </summary>
    /// <param name="item">When this method returns, contains the dequeued item if successful; otherwise, the default value of <typeparamref name="T" />.</param>
    /// <param name="throwIfEmpty">
    /// If <see langword="true" />, throws an <see cref="InvalidOperationException" /> when the buffer is empty; if
    /// <see langword="false" />, returns <see langword="false" /> without throwing.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an element was successfully dequeued; otherwise, <see langword="false" /> if the buffer was empty and
    /// <paramref name="throwIfEmpty" /> was <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="throwIfEmpty" /> is <see langword="true" /> and the buffer is empty.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This internal method underpins both <see cref="Dequeue" /> and <see cref="TryDequeue" />, and provides a shared path for controlled
    /// exception handling.
    /// </para>
    /// </remarks>
    private bool TryDequeueInternal(out T item, bool throwIfEmpty)
    {
        if (this._count == 0)
        {
            if (throwIfEmpty)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_EmptySequence);

            item = default!;
            return false;
        }

        item = this._internalBuffer[this._head];
        this._internalBuffer[this._head] = default!;
        this._head = (this._head + 1) % this._capacity;
        this._count--;
        this._version++;

        return true;
    }

    /// <summary>
    /// Attempts to enqueue an item into the internal <see cref="CircularBuffer{T}" />.
    /// </summary>
    /// <param name="item">The element to add. Can be <see langword="null" /> for reference types.</param>
    /// <param name="throwIfFull">
    /// If <see langword="true" />, throws an <see cref="InvalidOperationException" /> when the buffer is full and overwriting is
    /// disallowed; otherwise, returns <see langword="false" /> without throwing.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the item was enqueued successfully; <see langword="false" /> if the buffer was full and overwriting was
    /// disallowed and <paramref name="throwIfFull" /> was <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="throwIfFull" /> is <see langword="true" /> and the buffer is full while <see cref="AllowOverwrite" /> is
    /// <see langword="false" />, or the buffer’s capacity is zero.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is used by both <see cref="Enqueue" /> and <see cref="TryEnqueue" /> to centralize the enqueue logic and exception control.
    /// </para>
    /// <para>
    /// If <see cref="AllowOverwrite" /> is enabled, the oldest element is evicted to make room for the new item, and the
    /// <see cref="ItemEvicting" /> and <see cref="ItemEvicted" /> events are raised.
    /// </para>
    /// </remarks>
    private bool TryEnqueueInternal(T item, bool throwIfFull)
    {
        if (this._count == this._internalBuffer.Length)
        {
            if (!this.AllowOverwrite)
            {
                if (throwIfFull)
                    throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

                return false;
            }

            T overwritten = this._internalBuffer[this._tail];
            this.ItemEvicting?.Invoke(overwritten);

            this._internalBuffer[this._tail] = item;
            this._head = this._tail = (this._tail + 1) % this._capacity;

            this.ItemEvicted?.Invoke(overwritten);
        }
        else
        {
            this._internalBuffer[this._tail] = item;
            this._tail = (this._tail + 1) % this._capacity;
            this._count++;
        }

        this._version++;

        return true;
    }
}
