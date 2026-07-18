// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorage{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides the shared ordered-uniqueness storage engine used by <see cref="OrderedSet{T}" /> and
/// <see cref="IndexedSet{T}" />.
/// </summary>
/// <typeparam name="T">The element type. Elements must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// Elements live in a contiguous <see cref="Array" /> for deterministic insertion order. A hand-rolled open-addressing
/// hash table over two parallel <see cref="int" /> arrays — one of bucket heads, one of chain links — provides O(1)
/// average-case <see cref="Contains" /> and <see cref="IndexOf" />. The bucket count is always a power of two and the
/// table is rehashed when the load factor exceeds three quarters.
/// </para>
/// <para>
/// No BCL collection types back this storage; it is sized and laid out specifically for the ordered-set surface exposed
/// by its consumers.
/// </para>
/// <para>
/// This type is internal and is not thread-safe. Consumers are responsible for boundary semantics, choosing which
/// operations to expose, and synchronizing external mutators.
/// </para>
/// </remarks>
[Serializable]
internal sealed class OrderedSetStorage<T>
    where T : notnull
{
    /// <summary>The capacity used when the storage is constructed without an explicit capacity.</summary>
    private const int DefaultCapacity = 4;

    /// <summary>The numerator of the maximum load factor (3/4) that triggers a resize.</summary>
    private const int MaxLoadFactorNumerator = 3;

    /// <summary>The denominator of the maximum load factor (3/4) that triggers a resize.</summary>
    private const int MaxLoadFactorDenominator = 4;

    /// <summary>The equality comparer used for element identity and hash-table lookup.</summary>
    internal readonly IEqualityComparer<T> _comparer;

    /// <summary>The one-based bucket heads used by the open-addressing hash table.</summary>
    internal int[] _buckets;

    /// <summary>The one-based chain links for entries stored in each bucket.</summary>
    internal int[] _next;

    /// <summary>The contiguous element storage that preserves insertion order.</summary>
    internal T[] _items;

    /// <summary>The number of active elements stored in <see cref="_items" />.</summary>
    internal int _count;

    /// <summary>The mutation version used to detect changes during enumeration.</summary>
    internal int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSetStorage{T}" /> class with the specified capacity and
    /// comparer.
    /// </summary>
    /// <param name="capacity">The initial element capacity.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    internal OrderedSetStorage(int capacity, IEqualityComparer<T>? comparer)
    {
        ThrowHelper.ThrowIfNegative(capacity);

        _comparer = comparer ?? EqualityComparer<T>.Default;
        _items = capacity == 0 ? [] : new T[capacity];
        _next = capacity == 0 ? [] : new int[capacity];
        _buckets = [];

        if (capacity > 0)
            ResizeBuckets(OrderedSetStorage<T>.CalculateBucketCapacity(capacity));
    }

    /// <summary>
    /// Gets the equality comparer used to compare elements.
    /// </summary>
    /// <value>The active equality comparer.</value>
    internal IEqualityComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the number of elements currently stored.
    /// </summary>
    /// <value>The number of elements currently stored.</value>
    internal int Count => _count;

    /// <summary>
    /// Gets the allocated element capacity.
    /// </summary>
    /// <value>The current allocated capacity of the underlying element storage.</value>
    internal int Capacity => _items.Length;

    /// <summary>
    /// Returns the element at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The element at <paramref name="index" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    internal T GetAt(int index) => (uint)index >= (uint)_count ? throw new ArgumentOutOfRangeException(nameof(index)) : _items[index];

    /// <summary>
    /// Appends the specified item to the end of the order if it is not already present.
    /// </summary>
    /// <param name="item">The item to add. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the item was added; otherwise, <see langword="false" /> when the item was already
    /// present.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    internal bool Add(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        if (FindIndex(item) >= 0)
            return false;

        EnsureItemCapacity(_count + 1);
        EnsureHashCapacity(_count + 1);

        _items[_count] = item;
        AddToHashTable(item, _count);
        _count++;
        _version++;

        return true;
    }

    /// <summary>
    /// Appends each unique item from <paramref name="collection" />.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <returns>The number of items added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    internal int AddRange(IEnumerable<T> collection)
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
    /// Attempts to insert the specified item at the specified position.
    /// </summary>
    /// <param name="index">The insertion index in the range <c>[0, <see cref="Count" />]</c>.</param>
    /// <param name="item">The item to insert. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the item was inserted; otherwise, <see langword="false" /> when the item was already
    /// present.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than <see cref="Count" />.
    /// </exception>
    internal bool TryInsert(int index, T item)
    {
        ThrowHelper.ThrowIfNegative(index);
        ThrowHelper.ThrowIfGreaterThan(index, _count);
        ThrowHelper.ThrowIfNull(item);

        if (FindIndex(item) >= 0)
            return false;

        EnsureItemCapacity(_count + 1);
        EnsureHashCapacity(_count + 1);

        if (index < _count)
        {
            Array.Copy(_items, index, _items, index + 1, _count - index);
            Array.Copy(_next, index, _next, index + 1, _count - index);
        }

        // The copy leaves a stale link in the vacated slot; clear it so the reference sweep skips it and the
        // new item links cleanly afterwards.
        _next[index] = 0;
        AdjustHashReferences(index + 1, _count, 1, _count + 1);

        _items[index] = item;
        AddToHashTable(item, index);
        _count++;
        _version++;

        return true;
    }

    /// <summary>
    /// Removes the specified item if present.
    /// </summary>
    /// <param name="item">The item to remove. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the item was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    internal bool Remove(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        int index = FindIndex(item);
        if (index < 0)
            return false;

        RemoveCore(index);
        return true;
    }

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    internal void RemoveAt(int index)
    {
        ThrowHelper.ThrowIfNegative(index);
        ThrowHelper.ThrowIfGreaterThanOrEqual(index, _count);

        RemoveCore(index);
    }

    /// <summary>
    /// Moves an existing element from one index to another.
    /// </summary>
    /// <param name="oldIndex">The current zero-based index.</param>
    /// <param name="newIndex">The target zero-based index.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="oldIndex" /> or <paramref name="newIndex" /> is negative or greater than or equal to
    /// <see cref="Count" />.
    /// </exception>
    internal void Move(int oldIndex, int newIndex)
    {
        ThrowHelper.ThrowIfNegative(oldIndex);
        ThrowHelper.ThrowIfNegative(newIndex);
        ThrowHelper.ThrowIfGreaterThanOrEqual(oldIndex, _count);
        ThrowHelper.ThrowIfGreaterThanOrEqual(newIndex, _count);

        if (oldIndex == newIndex)
            return;

        T item = _items[oldIndex];
        UnlinkFromHashTable(oldIndex);

        if (oldIndex < newIndex)
        {
            Array.Copy(_items, oldIndex + 1, _items, oldIndex, newIndex - oldIndex);
            Array.Copy(_next, oldIndex + 1, _next, oldIndex, newIndex - oldIndex);
            _next[newIndex] = 0;
            AdjustHashReferences(oldIndex + 2, newIndex + 1, -1, _count);
        }
        else
        {
            Array.Copy(_items, newIndex, _items, newIndex + 1, oldIndex - newIndex);
            Array.Copy(_next, newIndex, _next, newIndex + 1, oldIndex - newIndex);
            _next[newIndex] = 0;
            AdjustHashReferences(newIndex + 1, oldIndex, 1, _count);
        }

        _items[newIndex] = item;
        AddToHashTable(item, newIndex);
        _version++;
    }

    /// <summary>
    /// Replaces the element at the specified index with the specified value.
    /// </summary>
    /// <param name="index">The zero-based index to replace.</param>
    /// <param name="value">The replacement value. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="value" /> already exists at another index.</exception>
    internal void ReplaceAt(int index, T value)
    {
        ThrowHelper.ThrowIfNegative(index);
        ThrowHelper.ThrowIfGreaterThanOrEqual(index, _count);
        ThrowHelper.ThrowIfNull(value);

        int existingIndex = FindIndex(value);
        if (existingIndex >= 0 && existingIndex != index) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateSetValue, nameof(value));

        if (existingIndex == index)
            return;

        // Two-node bucket fix-up: detach the old key's chain node, then link the new key in its place.
        UnlinkFromHashTable(index);
        _items[index] = value;
        AddToHashTable(value, index);
        _version++;
    }

    /// <summary>
    /// Removes every element for which <paramref name="match" /> returns <see langword="true" />.
    /// </summary>
    /// <param name="match">The predicate that selects elements to remove.</param>
    /// <returns>The number of elements removed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="match" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Compacts surviving elements in a single pass and then rebuilds the hash table once, which is faster than calling
    /// <see cref="Remove" /> repeatedly when many removals are anticipated.
    /// </remarks>
    internal int RemoveWhere(Predicate<T> match)
    {
        ThrowHelper.ThrowIfNull(match);

        int kept = 0;
        for (int i = 0; i < _count; i++)
        {
            T item = _items[i];
            if (!match(item))
            {
                if (kept != i)
                    _items[kept] = item;
                kept++;
            }
        }

        int removed = _count - kept;
        if (removed > 0)
        {
            Array.Clear(_items, kept, removed);
            _count = kept;
            RebuildHashTable();
            _version++;
        }

        return removed;
    }

    /// <summary>
    /// Removes all elements.
    /// </summary>
    internal void Clear()
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
    /// Determines whether the storage contains <paramref name="item" />.
    /// </summary>
    /// <param name="item">The item to locate. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the item exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    internal bool Contains(T item)
    {
        ThrowHelper.ThrowIfNull(item);
        return FindIndex(item) >= 0;
    }

    /// <summary>
    /// Returns the zero-based index of <paramref name="item" />, or <c>-1</c> if not present.
    /// </summary>
    /// <param name="item">The item to locate. Must not be <see langword="null" />.</param>
    /// <returns>The zero-based index of the item, or <c>-1</c> if it is not present.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    internal int IndexOf(T item)
    {
        ThrowHelper.ThrowIfNull(item);
        return FindIndex(item);
    }

    /// <summary>
    /// Copies the elements to <paramref name="array" /> starting at <paramref name="arrayIndex" />.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The destination start index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> does not have enough space starting at <paramref name="arrayIndex" />.
    /// </exception>
    internal void CopyTo(T[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(arrayIndex);

        if (array.Length - arrayIndex < _count) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_ArrayLengthExceedsCapacity, nameof(array));

        Array.Copy(_items, 0, array, arrayIndex, _count);
    }

    /// <summary>
    /// Ensures that the storage can hold at least <paramref name="capacity" /> elements without reallocating.
    /// </summary>
    /// <param name="capacity">The desired capacity.</param>
    /// <returns>The resulting element capacity.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    internal int EnsureCapacity(int capacity)
    {
        ThrowHelper.ThrowIfNegative(capacity);

        EnsureItemCapacity(capacity);
        EnsureHashCapacity(capacity);

        return _items.Length;
    }

    /// <summary>
    /// Shrinks the underlying storage to the current element count.
    /// </summary>
    internal void TrimExcess()
    {
        if (_count == 0)
        {
            _items = [];
            _next = [];
            _buckets = [];
            _version++;
            return;
        }

        if (_items.Length == _count)
            return;

        Array.Resize(ref _items, _count);
        Array.Resize(ref _next, _count);
        ResizeBuckets(OrderedSetStorage<T>.CalculateBucketCapacity(_count));
        RebuildHashTable();
        _version++;
    }

    /// <summary>
    /// Copies the elements to a new array in insertion order.
    /// </summary>
    /// <returns>A new array containing the elements.</returns>
    internal T[] ToArray()
    {
        var result = new T[_count];
        Array.Copy(_items, 0, result, 0, _count);
        return result;
    }

    /// <summary>
    /// Looks up the index of <paramref name="item" /> through the hash table without validating arguments.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>The zero-based index of the item, or <c>-1</c> if it is not present.</returns>
    private int FindIndex(T item)
    {
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
    /// Removes the element at the specified index without validating the argument.
    /// </summary>
    /// <param name="index">The zero-based index to remove.</param>
    /// <remarks>
    /// The hash table is maintained incrementally: the removed entry's chain node is unlinked and the stored
    /// references of the shifted entries are adjusted in place, so no user
    /// <see cref="IEqualityComparer{T}.GetHashCode(T)" /> calls beyond the single unlink lookup and no allocations
    /// occur.
    /// </remarks>
    private void RemoveCore(int index)
    {
        UnlinkFromHashTable(index);

        int moveCount = _count - index - 1;

        if (moveCount > 0)
        {
            Array.Copy(_items, index + 1, _items, index, moveCount);
            Array.Copy(_next, index + 1, _next, index, moveCount);
        }

        _count--;
        _items[_count] = default!;
        _next[_count] = 0;

        AdjustHashReferences(index + 2, _count + 1, -1, _count);
        _version++;
    }

    /// <summary>
    /// Detaches the chain node of the element at <paramref name="index" /> from its hash bucket.
    /// </summary>
    /// <param name="index">The zero-based index of the element to unlink.</param>
    /// <remarks>
    /// Costs a single <see cref="IEqualityComparer{T}.GetHashCode(T)" /> call plus a walk of the element's bucket
    /// chain; no other entry is touched.
    /// </remarks>
    private void UnlinkFromHashTable(int index)
    {
        int bucket = GetBucket(_items[index], _buckets.Length);
        int current = _buckets[bucket] - 1;

        if (current == index)
        {
            _buckets[bucket] = _next[index];
            return;
        }

        while (current >= 0)
        {
            int following = _next[current] - 1;
            if (following == index)
            {
                _next[current] = _next[index];
                return;
            }

            current = following;
        }
    }

    /// <summary>
    /// Shifts every stored one-based hash-table reference within the specified inclusive range by
    /// <paramref name="delta" />, after an element block has been moved within the contiguous storage.
    /// </summary>
    /// <param name="lowInclusive">The smallest one-based reference to adjust.</param>
    /// <param name="highInclusive">The largest one-based reference to adjust.</param>
    /// <param name="delta">The signed adjustment to apply.</param>
    /// <param name="slotCount">The number of live <see cref="_next" /> slots to sweep.</param>
    /// <remarks>
    /// This is a pure integer sweep over the bucket and chain arrays — O(buckets + slots) with no user
    /// <see cref="IEqualityComparer{T}.GetHashCode(T)" /> calls and no allocation, replacing the full rehash the
    /// shift-based mutators previously performed.
    /// </remarks>
    private void AdjustHashReferences(int lowInclusive, int highInclusive, int delta, int slotCount)
    {
        if (lowInclusive > highInclusive)
            return;

        int[] buckets = _buckets;
        for (int i = 0; i < buckets.Length; i++)
        {
            int reference = buckets[i];
            if (reference >= lowInclusive && reference <= highInclusive)
                buckets[i] = reference + delta;
        }

        int[] next = _next;
        for (int i = 0; i < slotCount; i++)
        {
            int reference = next[i];
            if (reference >= lowInclusive && reference <= highInclusive)
                next[i] = reference + delta;
        }
    }

    /// <summary>
    /// Grows the contiguous element storage if it is smaller than <paramref name="capacity" />.
    /// </summary>
    /// <param name="capacity">The required capacity.</param>
    private void EnsureItemCapacity(int capacity)
    {
        if (_items.Length >= capacity)
            return;

        int newCapacity = GrowCapacity(capacity);
        Array.Resize(ref _items, newCapacity);
        Array.Resize(ref _next, newCapacity);
    }

    /// <summary>
    /// Adds an item index to the open-addressing hash table.
    /// </summary>
    /// <param name="item">The item whose bucket is being populated.</param>
    /// <param name="itemIndex">The index of the item in the contiguous storage.</param>
    private void AddToHashTable(T item, int itemIndex)
    {
        int bucket = GetBucket(item, _buckets.Length);

        _next[itemIndex] = _buckets[bucket];
        _buckets[bucket] = itemIndex + 1;
    }

    /// <summary>
    /// Rebuilds the hash table from the contiguous element storage.
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
    /// Grows the bucket array if the load factor would exceed three quarters at <paramref name="itemCount" />.
    /// </summary>
    /// <param name="itemCount">The intended item count.</param>
    private void EnsureHashCapacity(int itemCount)
    {
        if (itemCount == 0)
            return;

        if (_buckets.Length == 0 ||
            itemCount * MaxLoadFactorDenominator > _buckets.Length * MaxLoadFactorNumerator)
        {
            ResizeBuckets(OrderedSetStorage<T>.CalculateBucketCapacity(itemCount));
            RebuildHashTable();
        }
    }

    /// <summary>
    /// Reallocates the bucket array to <paramref name="capacity" /> entries.
    /// </summary>
    /// <param name="capacity">The new bucket count; must be a power of two.</param>
    private void ResizeBuckets(int capacity) =>
        _buckets = new int[capacity];

    /// <summary>
    /// Returns a power-of-two bucket count that keeps the table within the configured load factor for
    /// <paramref name="itemCapacity" /> items.
    /// </summary>
    /// <param name="itemCapacity">The intended item capacity.</param>
    /// <returns>The bucket array length to allocate.</returns>
    private static int CalculateBucketCapacity(int itemCapacity)
    {
        int minimum = Math.Max(DefaultCapacity, (itemCapacity * MaxLoadFactorDenominator / MaxLoadFactorNumerator) + 1);
        return RoundUpToPowerOfTwo(minimum);
    }

    /// <summary>
    /// Computes the next element capacity by doubling, clamped to <see cref="Array.MaxLength" /> and floored at
    /// <paramref name="minimum" />.
    /// </summary>
    /// <param name="minimum">The minimum required capacity.</param>
    /// <returns>The chosen capacity.</returns>
    private int GrowCapacity(int minimum) =>
        CollectionCapacity.Grow(_items.Length, DefaultCapacity, minimum);

    /// <summary>
    /// Computes the bucket index for <paramref name="item" />.
    /// </summary>
    /// <param name="item">The item to hash.</param>
    /// <param name="bucketCount">The bucket count; must be a power of two.</param>
    /// <returns>The bucket index in <c>[0, bucketCount)</c>.</returns>
    private int GetBucket(T item, int bucketCount)
    {
        int hash = _comparer.GetHashCode(item) & 0x7fffffff;
        return hash & (bucketCount - 1);
    }

    /// <summary>
    /// Rounds <paramref name="value" /> up to the next power of two. The single caller in
    /// <see cref="CalculateBucketCapacity(int)" /> always supplies a value in the range
    /// <c>[<see cref="DefaultCapacity" />, Array.MaxLength]</c>, so the <c>value &lt;= 1</c> and
    /// <c>value &gt;= 2^30</c> clamps required by a fully-general implementation are not needed here.
    /// </summary>
    /// <param name="value">The value to round. Must be greater than one and less than <c>2^30</c>.</param>
    /// <returns>The smallest power of two greater than or equal to <paramref name="value" />.</returns>
    private static int RoundUpToPowerOfTwo(int value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value++;

        return value;
    }
}
