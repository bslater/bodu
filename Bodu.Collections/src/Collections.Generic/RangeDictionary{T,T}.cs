// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionary{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a sorted dictionary that maps non-overlapping half-open ranges to values.
/// </summary>
/// <typeparam name="TKey">The comparable endpoint type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>
/// <para>
/// Entries are stored in three parallel arrays — one for the inclusive start of each range, one for the exclusive end,
/// and one for the associated value. Lookups use binary search across the start endpoints, followed by a single
/// end-boundary check. Insertions and removals shift the affected suffix of each array.
/// </para>
/// <para>
/// Ranges use half-open semantics: <c>[startInclusive, endExclusive)</c>. Adjacent ranges are allowed; overlapping
/// ranges are rejected with <see cref="ArgumentException" />.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Tax-bracket lookup: ranges are half-open and must not overlap.
/// var brackets = new RangeDictionary<decimal, decimal>
/// {
///     {     0m,  18_200m, 0.00m },
///     { 18_200m,  45_000m, 0.19m },
///     { 45_000m, 135_000m, 0.30m },
/// };
///
/// if (brackets.TryGetValue(50_000m, out decimal rate))
///     Console.WriteLine($"Rate: {rate:P0}"); // Rate: 30%
///
/// // Adjacent ranges are allowed; an overlapping insertion throws ArgumentException.
/// brackets.Add(135_000m, 190_000m, 0.37m);
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[Serializable]
public sealed partial class RangeDictionary<TKey, TValue>
    : IReadOnlyCollection<ValueRange<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>The capacity used when the dictionary is constructed without an explicit capacity.</summary>
    private const int DefaultCapacity = 4;

    /// <summary>The comparer used to order and compare range endpoints.</summary>
    private readonly IComparer<TKey> _comparer;

    /// <summary>The inclusive lower endpoints of the stored ranges, kept sorted and parallel to <see cref="_ends" />.</summary>
    private TKey[] _starts;

    /// <summary>The upper endpoints of the stored ranges, parallel to <see cref="_starts" />.</summary>
    private TKey[] _ends;

    /// <summary>The values associated with each range, parallel to <see cref="_starts" /> and <see cref="_ends" />.</summary>
    private TValue[] _values;

    /// <summary>The number of ranges currently stored.</summary>
    private int _count;

    /// <summary>The modification counter used to detect concurrent mutation during enumeration.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeDictionary{TKey, TValue}" /> class using the default endpoint
    /// comparer.
    /// </summary>
    public RangeDictionary()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeDictionary{TKey, TValue}" /> class using the specified
    /// comparer.
    /// </summary>
    /// <param name="comparer">
    /// The endpoint comparer, or <see langword="null" /> to use <see cref="Comparer{TKey}.Default" />.
    /// </param>
    public RangeDictionary(IComparer<TKey>? comparer)
    {
        _comparer = comparer ?? Comparer<TKey>.Default;
        _starts = [];
        _ends = [];
        _values = [];
    }

    /// <summary>
    /// Gets the comparer used to order range endpoints.
    /// </summary>
    /// <value>The active endpoint comparer.</value>
    public IComparer<TKey> Comparer => _comparer;

    /// <summary>
    /// Gets the number of stored ranges.
    /// </summary>
    /// <value>The number of ranges currently stored in the dictionary.</value>
    public int Count => _count;

    /// <summary>
    /// Gets the allocated range capacity.
    /// </summary>
    /// <value>The current allocated capacity of the underlying storage.</value>
    public int Capacity => _starts.Length;

    /// <summary>
    /// Gets the value associated with the range containing the specified key.
    /// </summary>
    /// <param name="key">The key to locate. Must not be <see langword="null" />.</param>
    /// <returns>The value associated with the containing range.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">No range contains <paramref name="key" />.</exception>
    public TValue this[TKey key] =>
        TryGetValue(key, out TValue? value)
            ? value
            : throw new KeyNotFoundException(CollectionsResourceStrings.KeyNotFound_Range);

    /// <summary>
    /// Gets the range entry at the specified sorted index.
    /// </summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The range entry at <paramref name="index" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    public ValueRange<TKey, TValue> GetEntryAt(int index)
    {
        ValidateIndex(index);
        return new ValueRange<TKey, TValue>(_starts[index], _ends[index], _values[index], skipValidation: true);
    }

    /// <summary>
    /// Adds a non-overlapping half-open range and its value.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <param name="value">The value to associate with the range.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />, or the range
    /// overlaps an existing range.
    /// </exception>
    public void Add(TKey startInclusive, TKey endExclusive, TValue value)
    {
        Range<TKey>.ValidateRange(startInclusive, endExclusive, _comparer);

        int index = LowerBound(startInclusive);

        if (index > 0 && _comparer.Compare(_ends[index - 1], startInclusive) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeOverlap, nameof(startInclusive));

        if (index < _count && _comparer.Compare(_starts[index], endExclusive) < 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeOverlap, nameof(endExclusive));

        InsertAt(index, startInclusive, endExclusive, value);
    }

    /// <summary>
    /// Adds the specified range entry.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <exception cref="ArgumentException">The entry overlaps an existing range.</exception>
    public void Add(ValueRange<TKey, TValue> entry) =>
        Add(entry.StartInclusive, entry.EndExclusive, entry.Value);

    /// <summary>
    /// Removes an exact range from the dictionary.
    /// </summary>
    /// <param name="startInclusive">The inclusive start of the range.</param>
    /// <param name="endExclusive">The exclusive end of the range.</param>
    /// <returns><see langword="true" /> if the exact range was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />.
    /// </exception>
    public bool Remove(TKey startInclusive, TKey endExclusive)
    {
        Range<TKey>.ValidateRange(startInclusive, endExclusive, _comparer);

        int index = LowerBound(startInclusive);

        if (index >= _count)
            return false;

        if (_comparer.Compare(_starts[index], startInclusive) != 0 || _comparer.Compare(_ends[index], endExclusive) != 0)
            return false;

        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes all ranges from the dictionary.
    /// </summary>
    public void Clear()
    {
        if (_count == 0)
            return;

        Array.Clear(_starts, 0, _count);
        Array.Clear(_ends, 0, _count);
        Array.Clear(_values, 0, _count);
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Determines whether any stored range contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if a range contains the key; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool ContainsKey(TKey key) =>
        FindContainingIndex(key) >= 0;

    /// <summary>
    /// Attempts to get the value associated with the range containing the specified key.
    /// </summary>
    /// <param name="key">The key to locate. Must not be <see langword="null" />.</param>
    /// <param name="value">The value associated with the containing range, if found.</param>
    /// <returns><see langword="true" /> if a range contains the key; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetValue(TKey key, out TValue value)
    {
        int index = FindContainingIndex(key);

        if (index >= 0)
        {
            value = _values[index];
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Attempts to get the range entry containing the specified key.
    /// </summary>
    /// <param name="key">The key to locate. Must not be <see langword="null" />.</param>
    /// <param name="entry">The containing range entry, if found.</param>
    /// <returns><see langword="true" /> if a range contains the key; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetEntry(TKey key, out ValueRange<TKey, TValue> entry)
    {
        int index = FindContainingIndex(key);

        if (index >= 0)
        {
            entry = new ValueRange<TKey, TValue>(_starts[index], _ends[index], _values[index], skipValidation: true);
            return true;
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// Determines whether the specified range overlaps any existing range.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <returns>
    /// <see langword="true" /> if the range overlaps an existing range; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />.
    /// </exception>
    public bool Overlaps(TKey startInclusive, TKey endExclusive)
    {
        Range<TKey>.ValidateRange(startInclusive, endExclusive, _comparer);

        int index = LowerBound(startInclusive);

        return (index > 0 && _comparer.Compare(_ends[index - 1], startInclusive) > 0) || (index < _count && _comparer.Compare(_starts[index], endExclusive) < 0);
    }

    /// <summary>
    /// Ensures that the dictionary can hold at least the specified number of ranges without reallocating.
    /// </summary>
    /// <param name="capacity">The desired capacity.</param>
    /// <returns>The current capacity.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    public int EnsureCapacity(int capacity)
    {
        ThrowHelper.ThrowIfNegative(capacity);

        if (_starts.Length < capacity)
            ResizeStorage(GrowCapacity(capacity));

        return _starts.Length;
    }

    /// <summary>
    /// Copies the stored range entries to a new array in ascending sorted order.
    /// </summary>
    /// <returns>A new array containing the stored entries.</returns>
    public ValueRange<TKey, TValue>[] ToArray()
    {
        var result = new ValueRange<TKey, TValue>[_count];

        for (int i = 0; i < _count; i++)
            result[i] = new ValueRange<TKey, TValue>(_starts[i], _ends[i], _values[i], skipValidation: true);

        return result;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the stored range entries in ascending order.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the dictionary entries.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <summary>
    /// Locates the index of the range that contains the specified key, if any.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>The index of the containing range, or <c>-1</c> if no range contains the key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    private int FindContainingIndex(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        int index = UpperBound(key) - 1;

        return index < 0 ? -1 : _comparer.Compare(key, _ends[index]) < 0 ? index : -1;
    }

    /// <summary>
    /// Inserts an entry at the specified position, growing storage as needed and shifting trailing entries.
    /// </summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <param name="value">The value associated with the entry.</param>
    private void InsertAt(int index, TKey startInclusive, TKey endExclusive, TValue value)
    {
        EnsureCapacity(_count + 1);

        if (index < _count)
        {
            Array.Copy(_starts, index, _starts, index + 1, _count - index);
            Array.Copy(_ends, index, _ends, index + 1, _count - index);
            Array.Copy(_values, index, _values, index + 1, _count - index);
        }

        _starts[index] = startInclusive;
        _ends[index] = endExclusive;
        _values[index] = value;
        _count++;
        _version++;
    }

    /// <summary>
    /// Removes the entry at the specified index and shifts trailing entries left to keep storage contiguous.
    /// </summary>
    /// <param name="index">The index of the entry to remove.</param>
    private void RemoveAt(int index)
    {
        int moveCount = _count - index - 1;

        if (moveCount > 0)
        {
            Array.Copy(_starts, index + 1, _starts, index, moveCount);
            Array.Copy(_ends, index + 1, _ends, index, moveCount);
            Array.Copy(_values, index + 1, _values, index, moveCount);
        }

        _count--;
        _starts[_count] = default!;
        _ends[_count] = default!;
        _values[_count] = default!;
        _version++;
    }

    /// <summary>
    /// Returns the lowest index whose start endpoint is not less than <paramref name="value" />.
    /// </summary>
    /// <param name="value">The endpoint to locate.</param>
    /// <returns>The lower-bound index for <paramref name="value" />.</returns>
    private int LowerBound(TKey value)
    {
        int low = 0;
        int high = _count;

        while (low < high)
        {
            int middle = low + ((high - low) >> 1);

            if (_comparer.Compare(_starts[middle], value) < 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    /// <summary>
    /// Returns the lowest index whose start endpoint is greater than <paramref name="value" />.
    /// </summary>
    /// <param name="value">The endpoint to locate.</param>
    /// <returns>The upper-bound index for <paramref name="value" />.</returns>
    private int UpperBound(TKey value)
    {
        int low = 0;
        int high = _count;

        while (low < high)
        {
            int middle = low + ((high - low) >> 1);

            if (_comparer.Compare(_starts[middle], value) <= 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
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
    /// Reallocates the parallel storage arrays to the specified capacity.
    /// </summary>
    /// <param name="capacity">The new capacity.</param>
    private void ResizeStorage(int capacity)
    {
        Array.Resize(ref _starts, capacity);
        Array.Resize(ref _ends, capacity);
        Array.Resize(ref _values, capacity);
    }

    /// <summary>
    /// Computes the next capacity by doubling the current size, with a clamp at <see cref="Array.MaxLength" /> and a
    /// floor at <paramref name="minimum" />.
    /// </summary>
    /// <param name="minimum">The minimum acceptable capacity.</param>
    /// <returns>The chosen capacity.</returns>
    private int GrowCapacity(int minimum)
    {
        int capacity = _starts.Length == 0 ? DefaultCapacity : _starts.Length * 2;

        if ((uint)capacity > Array.MaxLength)
            capacity = Array.MaxLength;

        if (capacity < minimum)
            capacity = minimum;

        return capacity;
    }
}
