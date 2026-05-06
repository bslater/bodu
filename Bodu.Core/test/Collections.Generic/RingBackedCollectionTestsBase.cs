// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides the shared test contract for ring-backed collections that expose at least a single-ended
/// FIFO surface (head-pop / tail-push). Inherited by <see cref="CircularBufferTests"/>,
/// <see cref="DequeTests"/>, and <see cref="DequeFixedCapacityTests"/>.
/// </summary>
/// <typeparam name="TTest">
/// The concrete test class (CRTP self-reference). Required so that <c>[DynamicData]</c> sources on the base can
/// instantiate the concrete subclass to obtain hook values.
/// </typeparam>
/// <typeparam name="TCollection">
/// The collection type under test. Constrained to <see cref="IEnumerable{Int32}"/> and the non-generic
/// <see cref="ICollection"/> so the base can directly exercise the framework collection interfaces.
/// </typeparam>
/// <remarks>
/// <para>
/// Concrete test classes implement the abstract hook methods (<see cref="CreateCollection(int)"/>,
/// <see cref="AddToTail(TCollection, int)"/>, <see cref="RemoveFromHead(TCollection)"/>, etc.) so the contract
/// tests defined in the base can exercise each implementation through its native public API.
/// </para>
/// <para>
/// All tests in this base operate on <see langword="int"/> elements to keep the contract simple and to avoid
/// adding a third generic parameter for the element type. Type-specific behaviours
/// (e.g. <c>CircularBuffer.AllowOverwrite</c>, <c>Deque.AllowGrow</c>, <c>Deque.IsFull</c>) belong on the
/// concrete test class, not here.
/// </para>
/// </remarks>
[TestClass]
public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
    where TTest : RingBackedCollectionTestsBase<TTest, TCollection>, new()
    where TCollection : class, IEnumerable<int>, ICollection
{
    /// <summary>The default capacity used when concrete tests do not specify a different one.</summary>
    protected const int DefaultTestCapacity = 16;

    /// <summary>
    /// Gets a value indicating whether the collection under test rejects (throws or returns false) further
    /// insertions once <c>Count == Capacity</c>. Fixed-capacity collections set this to <see langword="true"/>;
    /// growable ones (<see cref="Deque{T}"/>) set it to <see langword="false"/>.
    /// </summary>
    /// <returns><see langword="true"/> for fixed-capacity collections; <see langword="false"/> when adds auto-grow.</returns>
    protected virtual bool IsFixedCapacity => true;

    /// <summary>
    /// Gets a value indicating whether <see cref="GetCapacity(TCollection)"/> is guaranteed to return exactly
    /// the value passed to <see cref="CreateCollection(int)"/>. Growable collections may report a larger value.
    /// </summary>
    /// <returns><see langword="true"/> when capacity is exactly preserved.</returns>
    protected virtual bool ReportsExactCapacity => IsFixedCapacity;

    /// <summary>Creates a new collection instance with the specified initial capacity.</summary>
    /// <param name="capacity">The capacity (or capacity hint, for growable types) to use.</param>
    /// <returns>A freshly constructed empty collection.</returns>
    protected abstract TCollection CreateCollection(int capacity);

    /// <summary>Adds an item at the tail end of the collection.</summary>
    /// <param name="collection">The collection to mutate.</param>
    /// <param name="item">The item to append.</param>
    protected abstract void AddToTail(TCollection collection, int item);

    /// <summary>
    /// Attempts to add an item at the tail end. Should return <see langword="false"/> for fixed-capacity
    /// collections when full, or always succeed for growable types.
    /// </summary>
    /// <param name="collection">The collection to mutate.</param>
    /// <param name="item">The item to append.</param>
    /// <returns><see langword="true"/> if the item was added.</returns>
    protected abstract bool TryAddToTail(TCollection collection, int item);

    /// <summary>Removes and returns the head element. Throws when empty.</summary>
    /// <param name="collection">The collection to mutate.</param>
    /// <returns>The removed head element.</returns>
    protected abstract int RemoveFromHead(TCollection collection);

    /// <summary>Attempts to remove and return the head element without throwing when empty.</summary>
    /// <param name="collection">The collection to mutate.</param>
    /// <param name="item">When this method returns, contains the removed item if successful.</param>
    /// <returns><see langword="true"/> if an element was removed.</returns>
    protected abstract bool TryRemoveFromHead(TCollection collection, out int item);

    /// <summary>Returns the head element without removing it. Throws when empty.</summary>
    /// <param name="collection">The collection to inspect.</param>
    /// <returns>The current head element.</returns>
    protected abstract int PeekHead(TCollection collection);

    /// <summary>Attempts to read the head element without throwing when empty.</summary>
    /// <param name="collection">The collection to inspect.</param>
    /// <param name="item">When this method returns, contains the head element if available.</param>
    /// <returns><see langword="true"/> if the head was read.</returns>
    protected abstract bool TryPeekHead(TCollection collection, out int item);

    /// <summary>Returns the current capacity (length of the backing array).</summary>
    /// <param name="collection">The collection to inspect.</param>
    /// <returns>The capacity.</returns>
    protected abstract int GetCapacity(TCollection collection);

    /// <summary>
    /// Returns the count via the collection's strongly-typed property. Default delegates to
    /// <see cref="ICollection.Count"/>; concrete classes may override if a more specific path is required.
    /// </summary>
    /// <param name="collection">The collection to inspect.</param>
    /// <returns>The element count.</returns>
    protected virtual int GetCount(TCollection collection) =>
        collection.Count;

    /// <summary>
    /// Returns the value of the collection's <c>IsEmpty</c> property if it has one; otherwise returns
    /// <c>GetCount(collection) == 0</c>. Default implementation uses count comparison.
    /// </summary>
    /// <param name="collection">The collection to inspect.</param>
    /// <returns><see langword="true"/> when empty.</returns>
    protected virtual bool GetIsEmpty(TCollection collection) =>
        GetCount(collection) == 0;

    /// <summary>Removes all elements.</summary>
    /// <param name="collection">The collection to mutate.</param>
    protected abstract void Clear(TCollection collection);

    /// <summary>Determines whether <paramref name="item"/> is present.</summary>
    /// <param name="collection">The collection to search.</param>
    /// <param name="item">The item to find.</param>
    /// <returns><see langword="true"/> if found.</returns>
    protected abstract bool Contains(TCollection collection, int item);

    /// <summary>Copies elements to <paramref name="array"/> starting at <paramref name="index"/> using the strongly-typed CopyTo overload.</summary>
    /// <param name="collection">The source collection.</param>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based starting index in <paramref name="array"/>.</param>
    protected abstract void CopyTo(TCollection collection, int[] array, int index);

    /// <summary>Returns the elements as a freshly allocated array in head-to-tail order.</summary>
    /// <param name="collection">The source collection.</param>
    /// <returns>A new array.</returns>
    protected abstract int[] ToArray(TCollection collection);

    /// <summary>Reads the element at the specified zero-based logical index (head-relative).</summary>
    /// <param name="collection">The source collection.</param>
    /// <param name="index">The zero-based logical index.</param>
    /// <returns>The element at that index.</returns>
    protected abstract int GetAt(TCollection collection, int index);

    /// <summary>Reduces the backing-array capacity to match the current count.</summary>
    /// <param name="collection">The collection to trim.</param>
    protected abstract void TrimExcess(TCollection collection);
}
