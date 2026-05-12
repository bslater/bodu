// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents an insertion-ordered set with index-based access.
/// </summary>
/// <typeparam name="T">The type of elements in the set. Elements must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IndexedSet{T}" /> stores elements in a compact contiguous array for deterministic index order,
/// and maintains a custom open bucket table over those array slots for fast uniqueness checks.
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
public class IndexedSet<T> : IList<T>, IReadOnlyList<T>
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
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class.
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
    public IndexedSet(int capacity)
        : this(capacity, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class with the specified initial capacity and comparer.
    /// </summary>
    /// <param name="capacity">The initial element capacity.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    public IndexedSet(int capacity, IEqualityComparer<T>? comparer)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _comparer = comparer ?? EqualityComparer<T>.Default;
        _items = capacity == 0 ? Array.Empty<T>() : new T[capacity];
        _next = capacity == 0 ? Array.Empty<int>() : new int[capacity];
        _buckets = Array.Empty<int>();

        if (capacity > 0)
            ResizeBuckets(CalculateBucketCapacity(capacity));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class containing unique elements from the specified collection.
    /// </summary>
    /// <param name="collection">The source collection.</param>
    public IndexedSet(IEnumerable<T> collection)
        : this(collection, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedSet{T}" /> class containing unique elements from the specified collection.
    /// </summary>
    /// <param name="collection">The source collection.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    public IndexedSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
        : this(GetCapacityHint(collection), comparer)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        foreach (T item in collection)
            Add(item);
    }

    /// <summary>
    /// Gets the equality comparer used to compare elements.
    /// </summary>
    public IEqualityComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets the allocated element capacity.
    /// </summary>
    public int Capacity => _items.Length;

    /// <summary>
    /// Gets a value indicating whether the set is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or replaces the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The item at <paramref name="index" />.</returns>
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
            ValidateItem(value, nameof(value));
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
    /// <param name="item">The item to add.</param>
    /// <returns>
    /// <see langword="true" /> if the item was added; otherwise, <see langword="false" /> if the set already contained it.
    /// </returns>
    public bool Add(T item)
    {
        ValidateItem(item, nameof(item));

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
    /// <param name="collection">The source collection.</param>
    /// <returns>The number of items added.</returns>
    public int AddRange(IEnumerable<T> collection)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

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
    /// <param name="index">The insertion index.</param>
    /// <param name="item">The item to insert.</param>
    /// <returns>
    /// <see langword="true" /> if the item was inserted; otherwise, <see langword="false" /> if the set already contained it.
    /// </returns>
    public bool TryInsert(int index, T item)
    {
        ValidateInsertionIndex(index);
        ValidateItem(item, nameof(item));

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
    /// <param name="index">The insertion index.</param>
    /// <param name="item">The item to insert.</param>
    /// <exception cref="ArgumentException">The item already exists in the set.</exception>
    public void Insert(int index, T item)
    {
        if (!TryInsert(index, item))
            throw new ArgumentException("The set already contains the specified value.", nameof(item));
    }

    /// <summary>
    /// Removes the specified item from the set.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>
    /// <see langword="true" /> if the item was removed; otherwise, <see langword="false" />.
    /// </returns>
    public bool Remove(T item)
    {
        ValidateItem(item, nameof(item));

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
    /// <param name="item">The item to locate.</param>
    /// <returns>
    /// <see langword="true" /> if the item exists; otherwise, <see langword="false" />.
    /// </returns>
    public bool Contains(T item)
    {
        ValidateItem(item, nameof(item));
        return IndexOf(item) >= 0;
    }

    /// <summary>
    /// Returns the index of the specified item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>The zero-based index, or <c>-1</c> if the item is not present.</returns>
    public int IndexOf(T item)
    {
        ValidateItem(item, nameof(item));

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
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The destination start index.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        if (array.Length - arrayIndex < _count)
            throw new ArgumentException("The destination array does not have sufficient space.", nameof(array));

        Array.Copy(_items, 0, array, arrayIndex, _count);
    }

    /// <summary>
    /// Ensures that the set can hold at least the specified number of items without reallocating item storage.
    /// </summary>
    /// <param name="capacity">The desired item capacity.</param>
    /// <returns>The current item capacity.</returns>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

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
        var result = new T[_count];
        Array.Copy(_items, 0, result, 0, _count);
        return result;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the set in indexed order.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    void ICollection<T>.Add(T item) =>
        Add(item);

    private void AddToHashTable(T item, int itemIndex)
    {
        int bucket = GetBucket(item, _buckets.Length);

        _next[itemIndex] = _buckets[bucket];
        _buckets[bucket] = itemIndex + 1;
    }

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

    private void ResizeBuckets(int capacity)
    {
        _buckets = new int[capacity];
    }

    private void ResizeItemStorage(int capacity)
    {
        Array.Resize(ref _items, capacity);
        Array.Resize(ref _next, capacity);
    }

    private int CalculateBucketCapacity(int itemCapacity)
    {
        int minimum = Math.Max(DefaultCapacity, (itemCapacity * MaxLoadFactorDenominator / MaxLoadFactorNumerator) + 1);
        return RoundUpToPowerOfTwo(minimum);
    }

    private int GrowCapacity(int minimum)
    {
        int capacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;

        if ((uint)capacity > Array.MaxLength)
            capacity = Array.MaxLength;

        if (capacity < minimum)
            capacity = minimum;

        return capacity;
    }

    private int GetBucket(T item, int bucketCount)
    {
        int hash = _comparer.GetHashCode(item) & 0x7fffffff;
        return hash & (bucketCount - 1);
    }

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

    private static void ValidateItem(T item, string paramName)
    {
        if (item is null)
            throw new ArgumentNullException(paramName);
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    private void ValidateInsertionIndex(int index)
    {
        if ((uint)index > (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    /// Enumerates an <see cref="IndexedSet{T}" /> without allocating.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly IndexedSet<T> _owner;
        private readonly int _version;
        private int _index;
        private T _current;

        internal Enumerator(IndexedSet<T> owner)
        {
            _owner = owner;
            _version = owner._version;
            _index = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public T Current => _current;

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException("The collection was modified during enumeration.");

            if (_index >= _owner._count)
                return false;

            _current = _owner._items[_index];
            _index++;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException("The collection was modified during enumeration.");

            _index = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}