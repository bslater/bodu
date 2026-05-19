// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents an index-addressable unique list — an insertion-ordered collection of unique elements that exposes the
/// full <see cref="IList{T}" /> contract.
/// </summary>
/// <typeparam name="T">The type of elements in the set. Elements must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IndexedSet{T}" /> shares its backing storage with <see cref="OrderedSet{T}" /> via the internal
/// <see cref="OrderedSetStorage{T}" /> engine: a contiguous element array for deterministic index order plus an
/// open-addressing hash table for O(1) <see cref="Contains" /> and <see cref="IndexOf" />. No BCL collection types are
/// used as backing storage.
/// </para>
/// <para>
/// Use <see cref="IndexedSet{T}" /> when callers need positional mutation — <see cref="Insert" />,
/// <see cref="RemoveAt" />, <see cref="Move" />, or the indexer setter. Use <see cref="OrderedSet{T}" /> when the
/// public surface is conceptually a set and indices exist only as a read-only view onto insertion order.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Build a deduplicated playlist that can still be reordered by index.
/// var playlist = new IndexedSet<string>(StringComparer.OrdinalIgnoreCase);
///
/// playlist.Add("Intro");
/// playlist.Add("Verse");
/// playlist.Add("Chorus");
/// bool added = playlist.Add("verse"); // false — already present under case-insensitive comparer
///
/// playlist.Insert(0, "Cold Open");    // positional insertion preserves uniqueness
/// playlist.Move(oldIndex: 1, newIndex: 3);
///
/// int chorusIndex = playlist.IndexOf("Chorus"); // O(1) lookup via the hash index
/// string second   = playlist[1];                // O(1) positional read
///]]>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(OrderedSetStorageDebugView<>))]
[Serializable]
public sealed partial class IndexedSet<T>
    : IList<T>, IReadOnlyList<T>
    where T : notnull
{
    /// <summary>
    /// The shared ordered-set storage engine that preserves insertion order and enforces uniqueness.
    /// </summary>
    private readonly OrderedSetStorage<T> _storage;

    /// <summary>
    /// Gets the backing storage exposed to debugger proxy views.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal OrderedSetStorage<T> DebuggerStorage => _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class using the default capacity and comparer.
    /// </summary>
    public IndexedSet()
        : this(0, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    public IndexedSet(IEqualityComparer<T>? comparer)
        : this(0, comparer)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial element capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    public IndexedSet(int capacity)
        : this(capacity, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class with the specified initial capacity and
    /// comparer.
    /// </summary>
    /// <param name="capacity">The initial element capacity.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    public IndexedSet(int capacity, IEqualityComparer<T>? comparer)
    {
        _storage = new OrderedSetStorage<T>(capacity, comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class containing the unique elements from
    /// <paramref name="collection" />.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public IndexedSet(IEnumerable<T> collection)
        : this(collection, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class containing the unique elements from
    /// <paramref name="collection" />.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public IndexedSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
        : this(GetCapacityHint(collection), comparer)
    {
        ThrowHelper.ThrowIfNull(collection);

        foreach (T item in collection)
            _storage.Add(item);
    }

    /// <summary>
    /// Gets the equality comparer used to compare elements.
    /// </summary>
    /// <returns>The active equality comparer.</returns>
    public IEqualityComparer<T> Comparer => _storage.Comparer;

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    /// <returns>The number of elements currently stored in the set.</returns>
    public int Count => _storage.Count;

    /// <summary>
    /// Gets the allocated element capacity.
    /// </summary>
    /// <returns>The current allocated capacity of the underlying element storage.</returns>
    public int Capacity => _storage.Capacity;

    /// <summary>
    /// Gets a value indicating whether the set is read-only.
    /// </summary>
    /// <returns>Always <see langword="false" />.</returns>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or replaces the element at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to access.</param>
    /// <returns>The element at <paramref name="index" />.</returns>
    /// <exception cref="ArgumentNullException">The replacement value is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    /// <exception cref="ArgumentException">The replacement value already exists at another index.</exception>
    public T this[int index]
    {
        get => _storage.GetAt(index);
        set => _storage.ReplaceAt(index, value);
    }

    /// <summary>
    /// Adds the specified item to the end of the set.
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
    /// Attempts to insert the specified item at the specified position.
    /// </summary>
    /// <param name="index">The insertion index in the range <c>[0, <see cref="Count" />]</c>.</param>
    /// <param name="item">The item to insert. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the item was inserted; otherwise, <see langword="false" /> when the set already
    /// contained it.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than <see cref="Count" />.
    /// </exception>
    public bool TryInsert(int index, T item) =>
        _storage.TryInsert(index, item);

    /// <summary>
    /// Inserts the specified item at the specified position.
    /// </summary>
    /// <param name="index">The insertion index in the range <c>[0, <see cref="Count" />]</c>.</param>
    /// <param name="item">The item to insert. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than <see cref="Count" />.
    /// </exception>
    /// <exception cref="ArgumentException">The item already exists in the set.</exception>
    public void Insert(int index, T item)
    {
        if (!_storage.TryInsert(index, item))
            throw new ArgumentException("The set already contains the specified value.", nameof(item)); //TODO: define a BCL-style exception message in the resx file and remove inline static text
    }

    /// <summary>
    /// Removes the specified item from the set.
    /// </summary>
    /// <param name="item">The item to remove. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the item was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool Remove(T item) =>
        _storage.Remove(item);

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    public void RemoveAt(int index) =>
        _storage.RemoveAt(index);

    /// <summary>
    /// Moves an existing element from one index to another.
    /// </summary>
    /// <param name="oldIndex">The current zero-based index.</param>
    /// <param name="newIndex">The target zero-based index.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="oldIndex" /> or <paramref name="newIndex" /> is negative or greater than or equal to
    /// <see cref="Count" />.
    /// </exception>
    public void Move(int oldIndex, int newIndex) =>
        _storage.Move(oldIndex, newIndex);

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
    /// Returns the zero-based index of the specified item.
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
    /// Adds <paramref name="item" /> via the <see cref="ICollection{T}.Add(T)" /> contract.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <remarks>
    /// Discards the boolean result of <see cref="Add(T)" />; callers that need to detect a duplicate-add should invoke
    /// the typed <see cref="Add(T)" /> overload directly.
    /// </remarks>
    void ICollection<T>.Add(T item) =>
        _storage.Add(item);

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
