// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeBase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides the shared public surface for double-ended ring-backed collections — the remove-side and
/// peek-side operations of a deque — leaving capacity policy (throw vs grow when full) to derived types.
/// Inherited by <see cref="Deque{T}"/>.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the deque.</typeparam>
/// <remarks>
/// <para>
/// <see cref="DequeBase{T}"/> exists as an extension point for additional double-ended collection
/// implementations. The current shipping implementation is the sealed <see cref="Deque{T}"/>, which
/// selects fixed-capacity or growable behaviour at runtime via its <see cref="Deque{T}.AllowGrow"/>
/// property; future variants (for example a linked-list-backed deque) could share this contract.
/// </para>
/// <para>
/// The remove and peek operations — <see cref="RemoveFirst"/>, <see cref="RemoveLast"/>,
/// <see cref="PeekFirst"/>, <see cref="PeekLast"/>, and their <c>Try*</c> variants — have identical
/// semantics regardless of capacity policy and are implemented once on this base. The add operations
/// (<see cref="AddFirst(T)"/>, <see cref="AddLast(T)"/>, <see cref="TryAddFirst(T)"/>,
/// <see cref="TryAddLast(T)"/>) are abstract because their behaviour on full differs by derived type:
/// fixed-capacity deques throw, growable deques resize.
/// </para>
/// <para>
/// Inherits from <see cref="RingBackedCollection{T}"/>, so derived types automatically pick up
/// <see cref="RingBackedCollection{T}.Capacity"/>, <see cref="RingBackedCollection{T}.Count"/>,
/// <see cref="RingBackedCollection{T}.IsEmpty"/>, the indexer, <see cref="RingBackedCollection{T}.Clear"/>,
/// <see cref="RingBackedCollection{T}.Contains(T)"/>, <see cref="RingBackedCollection{T}.CopyTo(T[], int)"/>,
/// <see cref="RingBackedCollection{T}.ToArray"/>, <see cref="RingBackedCollection{T}.TrimExcess"/>, the
/// shared <see cref="RingBackedCollection{T}.Enumerator"/>, and the framework collection interfaces.
/// </para>
/// </remarks>
[Serializable]
public abstract class DequeBase<T> : RingBackedCollection<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DequeBase{T}"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial backing-array capacity. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    protected DequeBase(int capacity)
        : base(capacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DequeBase{T}"/> class containing elements copied from
    /// <paramref name="collection"/>, with the specified capacity.
    /// </summary>
    /// <param name="collection">The collection from which elements are copied. Must not be <see langword="null"/>.</param>
    /// <param name="capacity">The backing-array capacity. Must be greater than zero.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    protected DequeBase(IEnumerable<T> collection, int capacity)
        : base(collection, capacity) { }

    /// <summary>
    /// Adds <paramref name="item"/> to the head of the deque.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <exception cref="InvalidOperationException">The deque is fixed-capacity and full.</exception>
    public abstract void AddFirst(T item);

    /// <summary>
    /// Adds <paramref name="item"/> to the tail of the deque.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <exception cref="InvalidOperationException">The deque is fixed-capacity and full.</exception>
    public abstract void AddLast(T item);

    /// <summary>
    /// Attempts to add <paramref name="item"/> to the head of the deque without throwing.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <returns>
    /// <see langword="true"/> if the item was added; <see langword="false"/> if the deque was fixed-capacity
    /// and full.
    /// </returns>
    public abstract bool TryAddFirst(T item);

    /// <summary>
    /// Attempts to add <paramref name="item"/> to the tail of the deque without throwing.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null"/> for reference types.</param>
    /// <returns>
    /// <see langword="true"/> if the item was added; <see langword="false"/> if the deque was fixed-capacity
    /// and full.
    /// </returns>
    public abstract bool TryAddLast(T item);

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
}
