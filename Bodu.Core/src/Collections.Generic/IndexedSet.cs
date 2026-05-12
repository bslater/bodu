// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents an insertion-ordered set with index-based access.
/// </summary>
/// <typeparam name="T">The type of elements in the set. Elements must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IndexedSet{T}" /> stores elements in a compact contiguous array for deterministic index order
/// and maintains a custom open-addressing bucket table over those array slots for fast uniqueness checks.
/// The hash table is not a BCL <see cref="System.Collections.Generic.HashSet{T}" /> or
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> wrapper; it is implemented directly with
/// two parallel <see cref="int" /> arrays — one of bucket heads, one of chain links — sized as power-of-two
/// capacities and rehashed when the load factor exceeds three quarters.
/// </para>
/// <para>
/// Adding an item appends it to the end of the logical order. Insertions, removals, moves, and indexed
/// replacement preserve uniqueness and update the hash table.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[Serializable]
public partial class IndexedSet<T> : IList<T>, IReadOnlyList<T>
    where T : notnull
{
    private const int DefaultCapacity = 4;
    private const int MaxLoadFactorNumerator = 3;
    private const int MaxLoadFactorDenominator = 4;

    private readonly IEqualityComparer<T> _comparer;
    private int[] _buckets;
    private int[] _next;
    private T[] _items;
    private int _count;
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class using the default capacity and comparer.
    /// </summary>
    public IndexedSet()
        : this(0, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    public IndexedSet(IEqualityComparer<T>? comparer)
        : this(0, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial element capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is negative.
    /// </exception>
    public IndexedSet(int capacity)
        : this(capacity, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class with the specified initial capacity and comparer.
    /// </summary>
    /// <param name="capacity">The initial element capacity.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is negative.
    /// </exception>
    public IndexedSet(int capacity, IEqualityComparer<T>? comparer)
    {
        ThrowHelper.ThrowIfNegative(capacity);

        _comparer = comparer ?? EqualityComparer<T>.Default;
        _items = capacity == 0 ? Array.Empty<T>() : new T[capacity];
        _next = capacity == 0 ? Array.Empty<int>() : new int[capacity];
        _buckets = Array.Empty<int>();

        if (capacity > 0)
            ResizeBuckets(CalculateBucketCapacity(capacity));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class containing the unique elements from the specified collection.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    public IndexedSet(IEnumerable<T> collection)
        : this(collection, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class containing the unique elements from the specified collection.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    public IndexedSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
        : this(GetCapacityHint(collection), comparer)
    {
        ThrowHelper.ThrowIfNull(collection);

        foreach (T item in collection)
            Add(item);
    }

    /// <summary>
    /// Gets the equality comparer used to compare elements.
    /// </summary>
    /// <returns>The active equality comparer.</returns>
    public IEqualityComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    /// <returns>The number of elements currently stored in the set.</returns>
    public int Count => _count;

    /// <summary>
    /// Gets the allocated element capacity.
    /// </summary>
    /// <returns>The current allocated capacity of the underlying element storage.</returns>
    public int Capacity => _items.Length;

    /// <summary>
    /// Gets a value indicating whether the set is read-only.
    /// </summary>
    /// <returns>Always <see langword="false" />.</returns>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or replaces the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to access.</param>
    /// <returns>The element at <paramref name="index" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// The replacement value is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The replacement value already exists at another index.
    /// </exception>
    public T this[int index]
    {
        get
        {
            ValidateIndex(index);
            return _items[index];
        }

        set
        {
            ThrowHelper.ThrowIfNull(value);
            ValidateIndex(index);

            int existingIndex = IndexOf(value);
            if (existingIndex >= 0 && existingIndex != index)
                throw new ArgumentException("The set already contains the specified value.", nameof(value));

            if (existingIndex == index)
                return;

            _items[index] = value;
            RebuildHashTable();
            _version++;
        }
    }

    /// <summary>
    /// Adds the specified item to the end of the set.
    /// </summary>
    /// <param name="item">The item to add. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the item was added; otherwise, <see langword="false" /> if the set already contained it.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="item" /> is <see langword="null" />.
    /// </exception>
    public bool Add(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        if (IndexOf(item) >= 0)
            return false;

        EnsureCapacity(_count + 1);
        EnsureHashCapacity(_count + 1);

        _items[_count] = item;
        AddToHashTable(item, _count);
        _count++;
        _version++;

        return true;
    }

    /// <summary>
    /// Adds each unique item from the specified collection.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <returns>The number of items added.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    public int AddRange(IEnumerable<T> collection)
    {
        ThrowHelper.ThrowIfNull(collection);

        int added = 0;

        foreach (T item in collection)
        {
            if (Add(item))
                added++;
        }

        return added;
    }

    /// <summary>
    /// Attempts to insert the specified item at the specified index.
    /// </summary>
    /// <param name="index">The insertion index in the range <c>[0, <see cref="Count" />]</c>.</param>
    /// <param name="item">The item to insert. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the item was inserted; otherwise, <see langword="false" /> if the set already contained it.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="item" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than <see cref="Count" />.
    /// </exception>
    public bool TryInsert(int index, T item)
    {
        ValidateInsertionIndex(index);
        ThrowHelper.ThrowIfNull(item);

        if (IndexOf(item) >= 0)
            return false;

        EnsureCapacity(_count + 1);
        EnsureHashCapacity(_count + 1);

        if (index < _count)
            Array.Copy(_items, index, _items, index + 1, _count - index);

        _items[index] = item;
        _count++;
        RebuildHashTable();
        _version++;

        return true;
    }

    /// <summary>
    /// Inserts the specified item at the specified index.
    /// </summary>
    /// <param name="index">The insertion index in the range <c>[0, <see cref="Count" />]</c>.</param>
    /// <param name="item">The item to insert. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="item" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than <see cref="Count" />.
    /// </exception>
    /// <exception cref="ArgumentException">The item already exists in the set.</exception>
    public void Insert(int index, T item)
    {
        if (!TryInsert(index, item))
            throw new ArgumentException("The set already contains the specified value.", nameof(item));
    }

    /// <summary>
    /// Removes the specified item from the set.
    /// </summary>
    /// <param name="item">The item to remove. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the item was removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="item" /> is <see langword="null" />.
    /// </exception>
    public bool Remove(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        int index = IndexOf(item);
        if (index < 0)
            return false;

        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    public void RemoveAt(int index)
    {
        ValidateIndex(index);

        int moveCount = _count - index - 1;

        if (moveCount > 0)
            Array.Copy(_items, index + 1, _items, index, moveCount);

        _count--;
        _items[_count] = default!;

        RebuildHashTable();
        _version++;
    }

    /// <summary>
    /// Moves an existing item from one index to another.
    /// </summary>
    /// <param name="oldIndex">The current index.</param>
    /// <param name="newIndex">The target index.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="oldIndex" /> or <paramref name="newIndex" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    public void Move(int oldIndex, int newIndex)
    {
        ValidateIndex(oldIndex);
        ValidateIndex(newIndex);

        if (oldIndex == newIndex)
            return;

        T item = _items[oldIndex];

        if (oldIndex < newIndex)
            Array.Copy(_items, oldIndex + 1, _items, oldIndex, newIndex - oldIndex);
        else
            Array.Copy(_items, newIndex, _items, newIndex + 1, oldIndex - newIndex);

        _items[newIndex] = item;
        RebuildHashTable();
        _version++;
    }

    /// <summary>
    /// Removes all items from the set.
    /// </summary>
    public void Clear()
    {
        if (_count == 0)
            return;

        Array.Clear(_items, 0, _count);
        Array.Clear(_next, 0, _next.Length);
        Array.Clear(_buckets, 0, _buckets.Length);

        _count = 0;
        _version++;
    }

    /// <summary>
    /// Determines whether the set contains the specified item.
    /// </summary>
    /// <param name="item">The item to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the item exists; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="item" /> is <see langword="null" />.
    /// </exception>
    public bool Contains(T item)
    {
        ThrowHelper.ThrowIfNull(item);
        return IndexOf(item) >= 0;
    }

    /// <summary>
    /// Returns the index of the specified item.
    /// </summary>
    /// <param name="item">The item to locate. Must not be <see langword="null" />.</param>
    /// <returns>The zero-based index, or <c>-1</c> if the item is not present.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="item" /> is <see langword="null" />.
    /// </exception>
    public int IndexOf(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        if (_count == 0 || _buckets.Length == 0)
            return -1;

        int bucket = GetBucket(item, _buckets.Length);

        for (int index = _buckets[bucket] - 1; index >= 0; index = _next[index] - 1)
        {
            if (_comparer.Equals(_items[index], item))
                return index;
        }

        return -1;
    }

    /// <summary>
    /// Copies the set items to the specified array.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The destination start index.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="arrayIndex" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> does not have enough space starting at <paramref name="arrayIndex" /> to hold the set.
    /// </exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(arrayIndex);

        if (array.Length - arrayIndex < _count)
            throw new ArgumentException("The destination array does not have sufficient space.", nameof(array));

        Array.Copy(_items, 0, array, arrayIndex, _count);
    }

    /// <summary>
    /// Ensures that the set can hold at least the specified number of items without reallocating item storage.
    /// </summary>
    /// <param name="capacity">The desired item capacity.</param>
    /// <returns>The current item capacity.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is negative.
    /// </exception>
    public int EnsureCapacity(int capacity)
    {
        ThrowHelper.ThrowIfNegative(capacity);

        if (_items.Length < capacity)
            ResizeItemStorage(GrowCapacity(capacity));

        EnsureHashCapacity(capacity);

        return _items.Length;
    }

    /// <summary>
    /// Shrinks the internal item storage to the current count.
    /// </summary>
    public void TrimExcess()
    {
        if (_count == 0)
        {
            _items = Array.Empty<T>();
            _next = Array.Empty<int>();
            _buckets = Array.Empty<int>();
            _version++;
            return;
        }

        if (_items.Length == _count)
            return;

        ResizeItemStorage(_count);
        ResizeBuckets(CalculateBucketCapacity(_count));
        RebuildHashTable();
        _version++;
    }

    /// <summary>
    /// Copies the items to a new array in indexed order.
    /// </summary>
    /// <returns>A new array containing the set items.</returns>
    public T[] ToArray()
    {
        T[] result = new T[_count];
        Array.Copy(_items, 0, result, 0, _count);
        return result;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the set in indexed order.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the set items.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <summary>
    /// Adds <paramref name="item" /> via the <see cref="ICollection{T}.Add(T)" /> contract.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <remarks>
    /// Discards the boolean result of <see cref="Add(T)" />; callers that need to detect a duplicate-add should
    /// invoke the typed <see cref="Add(T)" /> overload directly.
    /// </remarks>
    void ICollection<T>.Add(T item) =>
        Add(item);

    /// <summary>
    /// Appends the specified item to the open-addressing hash table.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <param name="itemIndex">The index of the item in the contiguous item storage.</param>
    private void AddToHashTable(T item, int itemIndex)
    {
        int bucket = GetBucket(item, _buckets.Length);

        _next[itemIndex] = _buckets[bucket];
        _buckets[bucket] = itemIndex + 1;
    }

    /// <summary>
    /// Rebuilds the hash table from the contiguous item storage. Used after operations that change the
    /// logical order of items (insertion at a non-tail index, removal, move, replace).
    /// </summary>
    private void RebuildHashTable()
    {
        if (_count == 0)
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            Array.Clear(_next, 0, _next.Length);
            return;
        }

        EnsureHashCapacity(_count);

        Array.Clear(_buckets, 0, _buckets.Length);
        Array.Clear(_next, 0, _count);

        for (int i = 0; i < _count; i++)
            AddToHashTable(_items[i], i);
    }

    /// <summary>
    /// Grows the bucket array if the load factor would exceed three quarters at <paramref name="itemCount" />,
    /// and rebuilds the chain links.
    /// </summary>
    /// <param name="itemCount">The intended item count after the pending operation.</param>
    private void EnsureHashCapacity(int itemCount)
    {
        if (itemCount == 0)
            return;

        if (_buckets.Length == 0 ||
            itemCount * MaxLoadFactorDenominator > _buckets.Length * MaxLoadFactorNumerator)
        {
            ResizeBuckets(CalculateBucketCapacity(itemCount));
            RebuildHashTable();
        }
    }

    /// <summary>
    /// Reallocates the bucket array to the specified power-of-two capacity. Existing chain links are
    /// discarded; the caller is expected to follow with <see cref="RebuildHashTable" /> when items are present.
    /// </summary>
    /// <param name="capacity">The new bucket count.</param>
    private void ResizeBuckets(int capacity)
    {
        _buckets = new int[capacity];
    }

    /// <summary>
    /// Resizes the parallel item and chain-link arrays to the specified capacity.
    /// </summary>
    /// <param name="capacity">The new element capacity.</param>
    private void ResizeItemStorage(int capacity)
    {
        Array.Resize(ref _items, capacity);
        Array.Resize(ref _next, capacity);
    }

    /// <summary>
    /// Returns a power-of-two bucket count that keeps the table within the configured load factor for
    /// <paramref name="itemCapacity" /> items.
    /// </summary>
    /// <param name="itemCapacity">The intended item capacity.</param>
    /// <returns>The bucket array length to allocate.</returns>
    private int CalculateBucketCapacity(int itemCapacity)
    {
        int minimum = Math.Max(DefaultCapacity, (itemCapacity * MaxLoadFactorDenominator / MaxLoadFactorNumerator) + 1);
        return RoundUpToPowerOfTwo(minimum);
    }

    /// <summary>
    /// Computes the next item capacity by doubling the current size, with a clamp at
    /// <see cref="Array.MaxLength" /> and a floor at <paramref name="minimum" />.
    /// </summary>
    /// <param name="minimum">The minimum acceptable capacity.</param>
    /// <returns>The chosen capacity.</returns>
    private int GrowCapacity(int minimum)
    {
        int capacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;

        if ((uint)capacity > Array.MaxLength)
            capacity = Array.MaxLength;

        if (capacity < minimum)
            capacity = minimum;

        return capacity;
    }

    /// <summary>
    /// Computes the bucket index for the specified item, masked into the power-of-two bucket array length.
    /// </summary>
    /// <param name="item">The item whose hash is to be bucketed.</param>
    /// <param name="bucketCount">The current bucket array length; must be a power of two.</param>
    /// <returns>The bucket index in the range <c>[0, bucketCount)</c>.</returns>
    private int GetBucket(T item, int bucketCount)
    {
        int hash = _comparer.GetHashCode(item) & 0x7fffffff;
        return hash & (bucketCount - 1);
    }

    /// <summary>
    /// Rounds <paramref name="value" /> up to the next power of two, clamped between <c>1</c> and
    /// <c>2^30</c>.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The smallest power of two greater than or equal to <paramref name="value" />.</returns>
    private static int RoundUpToPowerOfTwo(int value)
    {
        if (value <= 1)
            return 1;

        if (value >= 1 << 30)
            return 1 << 30;

        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value++;

        return value;
    }

    /// <summary>
    /// Returns a capacity hint suitable for sizing storage from an enumerable, preferring the fast paths
    /// exposed by <see cref="ICollection{T}" /> and <see cref="IReadOnlyCollection{T}" />.
    /// </summary>
    /// <param name="collection">The source enumerable, which may be <see langword="null" />.</param>
    /// <returns>The hinted capacity, or <c>0</c> when the count cannot be determined cheaply.</returns>
    private static int GetCapacityHint(IEnumerable<T>? collection)
    {
        if (collection is null)
            return 0;

        return collection is ICollection<T> genericCollection
            ? genericCollection.Count
            : collection is IReadOnlyCollection<T> readOnlyCollection
                ? readOnlyCollection.Count
                : 0;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException" /> if <paramref name="index" /> is not within
    /// <c>[0, <see cref="Count" />)</c>.
    /// </summary>
    /// <param name="index">The index to validate.</param>
    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException" /> if <paramref name="index" /> is not within
    /// <c>[0, <see cref="Count" />]</c>.
    /// </summary>
    /// <param name="index">The insertion index to validate.</param>
    private void ValidateInsertionIndex(int index)
    {
        if ((uint)index > (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
    }
}
