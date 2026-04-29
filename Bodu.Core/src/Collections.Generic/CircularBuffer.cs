// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBuffer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
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
/// <see cref="CircularBuffer{T}"/> is a high-performance collection for storing a bounded number of elements in a circular manner.
/// Elements are inserted at the tail and removed from the head. When the buffer reaches capacity, new elements overwrite the oldest
/// entries if <see cref="AllowOverwrite"/> is <see langword="true"/>.
/// </para>
/// <para>
/// This type is not thread-safe. If concurrent access is required, use
/// <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}"/> instead.
/// </para>
/// <para>Key operations include:</para>
/// <list type="bullet">
/// <item>
/// <description><see cref="Enqueue"/> and <see cref="TryEnqueue"/> - Add elements to the buffer.</description>
/// </item>
/// <item>
/// <description><see cref="Dequeue"/> and <see cref="TryDequeue"/> - Remove and return the oldest element.</description>
/// </item>
/// <item>
/// <description><see cref="Peek"/> and <see cref="TryPeek"/> - View the oldest element without removing it.</description>
/// </item>
/// </list>
/// <para>
/// The <see cref="RingBackedCollection{T}.Capacity"/> property defines the maximum number of elements the buffer can hold. If the
/// buffer is full and <see cref="AllowOverwrite"/> is <see langword="false"/>, attempts to enqueue additional elements will throw an
/// <see cref="InvalidOperationException"/>.
/// </para>
/// <para><see cref="CircularBuffer{T}"/> accepts <see langword="null"/> values (for reference types) and allows duplicate elements.</para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(CircularBufferDebugView<>))]
[Serializable]
public class CircularBuffer<T> : RingBackedCollection<T>
{
    private const int DefaultCapacity = 16;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class using the default capacity and allowing overwrites by default.
    /// </summary>
    public CircularBuffer()
        : this(DefaultCapacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class with the specified capacity, allowing overwrites when full.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than or equal to zero.</exception>
    public CircularBuffer(int capacity)
        : this(capacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class with the specified capacity and overwrite behaviour.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <param name="allowOverwrite">
    /// <see langword="true"/> to allow the buffer to automatically overwrite the oldest elements when full; <see langword="false"/> to
    /// prevent adding new elements when the buffer has reached capacity, which will cause an exception to be thrown during insertion.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than or equal to zero.</exception>
    public CircularBuffer(int capacity, bool allowOverwrite)
        : base(capacity)
    {
        AllowOverwrite = allowOverwrite;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class by copying elements from the specified collection, using
    /// the default capacity and allowing overwriting if needed.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <see langword="null"/>.</exception>
    public CircularBuffer(IEnumerable<T> collection)
        : this(collection, DefaultCapacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class by copying elements from the specified collection and
    /// applying the specified capacity. Overwriting is enabled by default.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than or equal to zero.</exception>
    public CircularBuffer(IEnumerable<T> collection, int capacity)
        : this(collection, capacity, allowOverwrite: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class by copying elements from the specified collection, applying
    /// the specified capacity and overwrite behaviour.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <param name="capacity">The maximum number of elements the buffer can contain. Must be greater than zero.</param>
    /// <param name="allowOverwrite">
    /// If <see langword="true"/>, the most recent elements from the collection are retained if its size exceeds
    /// <paramref name="capacity"/>. If <see langword="false"/>, the collection size must not exceed <paramref name="capacity"/>, or an
    /// exception is thrown.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="allowOverwrite"/> is <see langword="false"/> and the collection contains more elements than the buffer capacity.
    /// </exception>
    public CircularBuffer(IEnumerable<T> collection, int capacity, bool allowOverwrite)
        : base(MaterializeWithOverflowPolicy(collection, capacity, allowOverwrite), capacity)
    {
        AllowOverwrite = allowOverwrite;
    }

    /// <summary>
    /// Materialises <paramref name="collection"/> into an array exactly once and enforces the no-overwrite
    /// overflow policy before the base constructor sees the source. Returning a <c>T[]</c> avoids a second
    /// enumeration in the base ctor.
    /// </summary>
    /// <param name="collection">The collection from which elements will be copied.</param>
    /// <param name="capacity">The maximum number of elements the buffer can contain.</param>
    /// <param name="allowOverwrite">Whether eviction is permitted; controls whether the size check is enforced.</param>
    /// <returns>The materialised array; never <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="allowOverwrite"/> is <see langword="false"/> and the collection size exceeds <paramref name="capacity"/>.
    /// </exception>
    private static T[] MaterializeWithOverflowPolicy(IEnumerable<T> collection, int capacity, bool allowOverwrite)
    {
        ThrowHelper.ThrowIfNull(collection);
        T[] items = collection as T[] ?? collection.ToArray();

        if (!allowOverwrite && items.Length > capacity)
            throw new InvalidOperationException(ResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity);

        return items;
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an item has been evicted from the <see cref="CircularBuffer{T}"/> due to capacity limits.
    /// </summary>
    /// <remarks>
    /// <para>This event is raised only when the buffer reaches capacity and <see cref="AllowOverwrite"/> is <see langword="true"/>.</para>
    /// <para>
    /// <b>Important:</b> Exceptions thrown by event handlers are not caught and will propagate to the caller of <see cref="Enqueue"/>
    /// or <see cref="TryEnqueue"/>. Consumers should ensure event handlers are exception-safe.
    /// </para>
    /// </remarks>
    public event Action<T>? ItemEvicted;

    /// <summary>
    /// Occurs immediately <b>before</b> an item is evicted from the <see cref="CircularBuffer{T}"/> due to capacity limits.
    /// </summary>
    /// <remarks>
    /// <para>This event is raised only when the buffer has reached its capacity and <see cref="AllowOverwrite"/> is <see langword="true"/>.</para>
    /// <para>
    /// <b>Important:</b> If the event handler throws an exception, the item will not be evicted, and the <see cref="Enqueue"/> or
    /// <see cref="TryEnqueue"/> operation will propagate that exception. Event handlers should therefore avoid throwing unless intentional.
    /// </para>
    /// </remarks>
    public event Action<T>? ItemEvicting;

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="CircularBuffer{T}"/> will automatically overwrite the oldest element when
    /// capacity is reached.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to overwrite the oldest item when the buffer is full; <see langword="false"/> to throw an exception instead
    /// of overwriting.
    /// </value>
    public bool AllowOverwrite { get; set; }

    /// <summary>
    /// Removes and returns the oldest element from the buffer.
    /// </summary>
    /// <returns>The oldest element in the buffer.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the buffer is empty when <see cref="Dequeue"/> is called.</exception>
    public T Dequeue()
    {
        if (Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_EmptySequence);

        return RemoveHead();
    }

    /// <summary>
    /// Adds an element to the end of the buffer.
    /// </summary>
    /// <param name="item">The element to add. Can be <see langword="null"/> for reference types.</param>
    /// <exception cref="InvalidOperationException">Thrown if the buffer is at full capacity and <see cref="AllowOverwrite"/> is <see langword="false"/>.</exception>
    public void Enqueue(T item) => _ = TryEnqueueInternal(item, throwIfFull: true);

    /// <summary>
    /// Returns the oldest element in the <see cref="CircularBuffer{T}"/> without removing it.
    /// </summary>
    /// <returns>The element at the front of the buffer (the oldest item).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the buffer is empty (i.e., <see cref="RingBackedCollection{T}.Count"/> equals <c>0</c>).</exception>
    public T Peek()
    {
        if (Count == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);

        return PeekHead();
    }

    /// <summary>
    /// Attempts to remove and return the oldest element from the <see cref="CircularBuffer{T}"/> without throwing an exception.
    /// </summary>
    /// <param name="item">
    /// When this method returns, contains the dequeued element if one was available; otherwise, the default value of <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an item was successfully dequeued; otherwise, <see langword="false"/> if the buffer was empty.
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
    /// Attempts to add an element to the end of the <see cref="CircularBuffer{T}"/> without throwing an exception.
    /// </summary>
    /// <param name="item">The element to add. Can be <see langword="null"/> for reference types.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully enqueued; <see langword="false"/> if the buffer is full and
    /// <see cref="AllowOverwrite"/> is <see langword="false"/>.
    /// </returns>
    public bool TryEnqueue(T item) => TryEnqueueInternal(item, throwIfFull: false);

    /// <summary>
    /// Attempts to retrieve the oldest element from the <see cref="CircularBuffer{T}"/> without removing it.
    /// </summary>
    /// <param name="item">
    /// When this method returns, contains the oldest element if the buffer is not empty; otherwise, contains the default value for <typeparamref name="T"/>.
    /// </param>
    /// <returns><see langword="true"/> if an item was successfully retrieved; <see langword="false"/> if the buffer is empty.</returns>
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
    /// Single internal Enqueue implementation shared by <see cref="Enqueue"/> and <see cref="TryEnqueue"/>.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <param name="throwIfFull">
    /// If <see langword="true"/>, throws when the buffer is full and <see cref="AllowOverwrite"/> is <see langword="false"/>;
    /// otherwise returns <see langword="false"/>.
    /// </param>
    /// <returns><see langword="true"/> when the item was added (or evicted-and-replaced).</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="throwIfFull"/> is <see langword="true"/> and the buffer is full while
    /// <see cref="AllowOverwrite"/> is <see langword="false"/>.
    /// </exception>
    private bool TryEnqueueInternal(T item, bool throwIfFull)
    {
        if (Count == Capacity)
        {
            if (!AllowOverwrite)
            {
                if (throwIfFull)
                    throw new InvalidOperationException(ResourceStrings.InvalidOperation_CapacityExhausted);

                return false;
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
