// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSet{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents an insertion-ordered set — a <see cref="ISet{T}" /> that preserves the order in which elements were first
/// added and exposes that order through <see cref="IReadOnlyList{T}" />.
/// </summary>
/// <typeparam name="T">The type of elements in the set. Elements must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// <see cref="OrderedSet{T}" /> shares its backing storage with <see cref="IndexedSet{T}" /> via the internal
/// <see cref="OrderedSetStorage{T}" /> engine: a contiguous element array for deterministic insertion order plus an
/// open-addressing hash table for O(1) <see cref="Contains" />. No BCL collection types are used as backing storage.
/// </para>
/// <para>
/// The contract is strictly set-shaped: <see cref="ISet{T}" /> for mutation and set algebra,
/// <see cref="IReadOnlyList{T}" /> for ordered iteration and positional read access. Positional mutation (<c>Insert</c>
/// , <c>RemoveAt</c>, <c>Move</c>, indexer setter) is intentionally not exposed — use <see cref="IndexedSet{T}" /> when
/// those operations are required.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // De-duplicate a stream of tags while keeping the order in which each was first seen.
/// var tags = new OrderedSet<string>(StringComparer.OrdinalIgnoreCase);
/// tags.Add("alpha");
/// tags.Add("beta");
/// tags.Add("ALPHA"); // ignored — already present under the case-insensitive comparer
/// tags.Add("gamma");
///
/// Console.WriteLine(string.Join(", ", tags)); // alpha, beta, gamma
/// Console.WriteLine(tags[0]);                 // "alpha" — positional read via IReadOnlyList<T>
///
/// // Set algebra returns a new OrderedSet preserving the left operand's order.
/// var diff = new OrderedSet<string>(tags);
/// diff.ExceptWith(new[] { "beta" });
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(OrderedSetStorageDebugView<>))]
[Serializable]
public sealed partial class OrderedSet<T>
    : ISet<T>, IReadOnlyList<T>
    where T : notnull
{
    /// <summary>The shared ordered-set storage engine that preserves insertion order and enforces uniqueness.</summary>
    private readonly OrderedSetStorage<T> _storage;

    /// <summary>
    /// Gets the backing storage exposed to debugger proxy views.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal OrderedSetStorage<T> DebuggerStorage => _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class using the default capacity and comparer.
    /// </summary>
    public OrderedSet()
        : this(0, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    public OrderedSet(IEqualityComparer<T>? comparer)
        : this(0, comparer)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial element capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    public OrderedSet(int capacity)
        : this(capacity, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class with the specified initial capacity and
    /// comparer.
    /// </summary>
    /// <param name="capacity">The initial element capacity.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    public OrderedSet(int capacity, IEqualityComparer<T>? comparer)
    {
        _storage = new OrderedSetStorage<T>(capacity, comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class containing the unique elements from
    /// <paramref name="collection" />.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public OrderedSet(IEnumerable<T> collection)
        : this(collection, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class containing the unique elements from
    /// <paramref name="collection" />.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public OrderedSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
        : this(GetCapacityHint(collection), comparer)
    {
        ThrowHelper.ThrowIfNull(collection);

        foreach (T item in collection)
            _storage.Add(item);
    }

    /// <summary>
    /// Gets the equality comparer used to compare elements.
    /// </summary>
    /// <value>The active equality comparer.</value>
    public IEqualityComparer<T> Comparer => _storage.Comparer;

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    /// <value>The number of elements currently stored in the set.</value>
    public int Count => _storage.Count;

    /// <summary>
    /// Gets the allocated element capacity.
    /// </summary>
    /// <value>The current allocated capacity of the underlying element storage.</value>
    public int Capacity => _storage.Capacity;

    /// <summary>
    /// Gets a value indicating whether the set is read-only.
    /// </summary>
    /// <value>Always <see langword="false" />.</value>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets the element at the specified zero-based index in insertion order.
    /// </summary>
    /// <param name="index">The zero-based index of the element to access.</param>
    /// <returns>The element at <paramref name="index" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    public T this[int index] => _storage.GetAt(index);

    /// <summary>
    /// Adds the specified item to the set.
    /// </summary>
    /// <param name="item">The item to add. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the item was added; otherwise, <see langword="false" /> when the set already
    /// contained it.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool Add(T item) =>
        _storage.Add(item);

    /// <summary>
    /// Adds each unique item from <paramref name="collection" />.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <returns>The number of items added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public int AddRange(IEnumerable<T> collection) =>
        _storage.AddRange(collection);

    /// <summary>
    /// Removes the specified item from the set.
    /// </summary>
    /// <param name="item">The item to remove. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the item was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool Remove(T item) =>
        _storage.Remove(item);

    /// <summary>
    /// Removes all elements from the set.
    /// </summary>
    public void Clear() =>
        _storage.Clear();

    /// <summary>
    /// Determines whether the set contains the specified item.
    /// </summary>
    /// <param name="item">The item to locate. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the item exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool Contains(T item) =>
        _storage.Contains(item);

    /// <summary>
    /// Returns the zero-based index of <paramref name="item" /> in insertion order.
    /// </summary>
    /// <param name="item">The item to locate. Must not be <see langword="null" />.</param>
    /// <returns>The zero-based index of the item, or <c>-1</c> if it is not present.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public int IndexOf(T item) =>
        _storage.IndexOf(item);

    /// <summary>
    /// Copies the elements to the specified array starting at <paramref name="arrayIndex" />.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The destination start index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> does not have enough space starting at <paramref name="arrayIndex" />.
    /// </exception>
    public void CopyTo(T[] array, int arrayIndex) =>
        _storage.CopyTo(array, arrayIndex);

    /// <summary>
    /// Ensures that the set can hold at least the specified number of items without reallocating storage.
    /// </summary>
    /// <param name="capacity">The desired item capacity.</param>
    /// <returns>The resulting item capacity.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    public int EnsureCapacity(int capacity) =>
        _storage.EnsureCapacity(capacity);

    /// <summary>
    /// Shrinks the underlying storage to the current element count.
    /// </summary>
    public void TrimExcess() =>
        _storage.TrimExcess();

    /// <summary>
    /// Copies the elements to a new array in insertion order.
    /// </summary>
    /// <returns>A new array containing the set elements.</returns>
    public T[] ToArray() =>
        _storage.ToArray();

    /// <summary>
    /// Returns an enumerator that iterates through the set in insertion order.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the set elements.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <summary>
    /// Returns a capacity hint suitable for sizing storage from <paramref name="collection" />, preferring the fast
    /// paths exposed by <see cref="ICollection{T}" /> and <see cref="IReadOnlyCollection{T}" />.
    /// </summary>
    /// <param name="collection">The source enumerable, which may be <see langword="null" />.</param>
    /// <returns>The hinted capacity, or <c>0</c> when the count cannot be determined cheaply.</returns>
    private static int GetCapacityHint(IEnumerable<T>? collection)
    {
        return collection is null
            ? 0
            : collection is ICollection<T> genericCollection
            ? genericCollection.Count
            : collection is IReadOnlyCollection<T> readOnlyCollection
                ? readOnlyCollection.Count
                : 0;
    }
}
