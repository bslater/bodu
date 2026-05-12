// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionary.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a half-open range, where <see cref="StartInclusive" /> is included and
/// <see cref="EndExclusive" /> is excluded.
/// </summary>
/// <typeparam name="T">The comparable endpoint type.</typeparam>
[DebuggerDisplay("[{StartInclusive}, {EndExclusive})")]
public readonly struct RangeDictionary<T>
    where T : IComparable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RangeDictionary{T}" /> struct.
    /// </summary>
    /// <param name="startInclusive">The inclusive start of the range.</param>
    /// <param name="endExclusive">The exclusive end of the range.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />.
    /// </exception>
    public RangeDictionary(T startInclusive, T endExclusive)
    {
        ValidateRange(startInclusive, endExclusive, Comparer<T>.Default);

        StartInclusive = startInclusive;
        EndExclusive = endExclusive;
    }

    /// <summary>
    /// Gets the inclusive start of the range.
    /// </summary>
    public T StartInclusive { get; }

    /// <summary>
    /// Gets the exclusive end of the range.
    /// </summary>
    public T EndExclusive { get; }

    /// <summary>
    /// Determines whether the specified value falls inside this range.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="value" /> is inside the range; otherwise, <see langword="false" />.
    /// </returns>
    public bool Contains(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var comparer = Comparer<T>.Default;
        return comparer.Compare(StartInclusive, value) <= 0 &&
               comparer.Compare(value, EndExclusive) < 0;
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"[{StartInclusive}, {EndExclusive})";

    internal static void ValidateRange(T startInclusive, T endExclusive, IComparer<T> comparer)
    {
        if (startInclusive is null)
            throw new ArgumentNullException(nameof(startInclusive));

        if (endExclusive is null)
            throw new ArgumentNullException(nameof(endExclusive));

        if (comparer.Compare(startInclusive, endExclusive) >= 0)
            throw new ArgumentException("The range start must be less than the range end.");
    }
}

/// <summary>
/// Represents a half-open range mapped to a value.
/// </summary>
/// <typeparam name="TKey">The comparable endpoint type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
[DebuggerDisplay("[{StartInclusive}, {EndExclusive}) = {Value}")]
public readonly struct ValueRange<TKey, TValue>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueRange{TKey, TValue}" /> struct.
    /// </summary>
    /// <param name="startInclusive">The inclusive start of the range.</param>
    /// <param name="endExclusive">The exclusive end of the range.</param>
    /// <param name="value">The value mapped to the range.</param>
    public ValueRange(TKey startInclusive, TKey endExclusive, TValue value)
    {
        RangeDictionary<TKey>.ValidateRange(startInclusive, endExclusive, Comparer<TKey>.Default);

        StartInclusive = startInclusive;
        EndExclusive = endExclusive;
        Value = value;
    }

    internal ValueRange(TKey startInclusive, TKey endExclusive, TValue value, bool skipValidation)
    {
        StartInclusive = startInclusive;
        EndExclusive = endExclusive;
        Value = value;
    }

    /// <summary>
    /// Gets the inclusive start of the range.
    /// </summary>
    public TKey StartInclusive { get; }

    /// <summary>
    /// Gets the exclusive end of the range.
    /// </summary>
    public TKey EndExclusive { get; }

    /// <summary>
    /// Gets the value associated with the range.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Determines whether the specified key falls inside this range.
    /// </summary>
    /// <param name="key">The key to test.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="key" /> is inside the range; otherwise, <see langword="false" />.
    /// </returns>
    public bool Contains(TKey key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        var comparer = Comparer<TKey>.Default;
        return comparer.Compare(StartInclusive, key) <= 0 &&
               comparer.Compare(key, EndExclusive) < 0;
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"[{StartInclusive}, {EndExclusive}) = {Value}";
}

/// <summary>
/// Represents a sorted dictionary that maps non-overlapping half-open ranges to values.
/// </summary>
/// <typeparam name="TKey">The comparable endpoint type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>
/// <para>
/// Ranges are stored in sorted parallel arrays. Lookups use binary search over range starts and then a single
/// end-boundary check.
/// </para>
/// <para>
/// Ranges use half-open semantics: <c>[startInclusive, endExclusive)</c>. Adjacent ranges are allowed; overlapping
/// ranges are rejected.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[Serializable]
public sealed class RangeDictionary<TKey, TValue> 
    : IReadOnlyCollection<ValueRange<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    private const int DefaultCapacity = 4;

    private readonly IComparer<TKey> _comparer;
    private TKey[] _starts;
    private TKey[] _ends;
    private TValue[] _values;
    private int _count;
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeDictionary{TKey, TValue}" /> class.
    /// </summary>
    public RangeDictionary()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeDictionary{TKey, TValue}" /> class.
    /// </summary>
    /// <param name="comparer">The endpoint comparer, or <see langword="null" /> to use the default comparer.</param>
    public RangeDictionary(IComparer<TKey>? comparer)
    {
        _comparer = comparer ?? Comparer<TKey>.Default;
        _starts = Array.Empty<TKey>();
        _ends = Array.Empty<TKey>();
        _values = Array.Empty<TValue>();
    }

    /// <summary>
    /// Gets the comparer used to order range endpoints.
    /// </summary>
    public IComparer<TKey> Comparer => _comparer;

    /// <summary>
    /// Gets the number of stored ranges.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets the allocated range capacity.
    /// </summary>
    public int Capacity => _starts.Length;

    /// <summary>
    /// Gets the value associated with the range containing the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>The value associated with the containing range.</returns>
    /// <exception cref="KeyNotFoundException">No range contains <paramref name="key" />.</exception>
    public TValue this[TKey key]
    {
        get
        {
            if (TryGetValue(key, out TValue? value))
                return value;

            throw new KeyNotFoundException("The specified key was not contained in any range.");
        }
    }

    /// <summary>
    /// Gets the range entry at the specified sorted index.
    /// </summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The range entry at <paramref name="index" />.</returns>
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
    /// <exception cref="ArgumentException">The range overlaps an existing range.</exception>
    public void Add(TKey startInclusive, TKey endExclusive, TValue value)
    {
        RangeDictionary<TKey>.ValidateRange(startInclusive, endExclusive, _comparer);

        int index = LowerBound(startInclusive);

        if (index > 0 && _comparer.Compare(_ends[index - 1], startInclusive) > 0)
            throw new ArgumentException("The specified range overlaps an existing range.");

        if (index < _count && _comparer.Compare(_starts[index], endExclusive) < 0)
            throw new ArgumentException("The specified range overlaps an existing range.");

        InsertAt(index, startInclusive, endExclusive, value);
    }

    /// <summary>
    /// Adds the specified range entry.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void Add(ValueRange<TKey, TValue> entry) =>
        Add(entry.StartInclusive, entry.EndExclusive, entry.Value);

    /// <summary>
    /// Removes an exact range from the dictionary.
    /// </summary>
    /// <param name="startInclusive">The inclusive start of the range.</param>
    /// <param name="endExclusive">The exclusive end of the range.</param>
    /// <returns>
    /// <see langword="true" /> if the exact range was removed; otherwise, <see langword="false" />.
    /// </returns>
    public bool Remove(TKey startInclusive, TKey endExclusive)
    {
        RangeDictionary<TKey>.ValidateRange(startInclusive, endExclusive, _comparer);

        int index = LowerBound(startInclusive);

        if (index >= _count)
            return false;

        if (_comparer.Compare(_starts[index], startInclusive) != 0 ||
            _comparer.Compare(_ends[index], endExclusive) != 0)
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
    /// <param name="key">The key to locate.</param>
    /// <returns>
    /// <see langword="true" /> if a range contains the key; otherwise, <see langword="false" />.
    /// </returns>
    public bool ContainsKey(TKey key) =>
        FindContainingIndex(key) >= 0;

    /// <summary>
    /// Attempts to get the value associated with the range containing the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">The value associated with the containing range, if found.</param>
    /// <returns>
    /// <see langword="true" /> if a range contains the key; otherwise, <see langword="false" />.
    /// </returns>
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
    /// <param name="key">The key to locate.</param>
    /// <param name="entry">The containing range entry, if found.</param>
    /// <returns>
    /// <see langword="true" /> if a range contains the key; otherwise, <see langword="false" />.
    /// </returns>
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
    public bool Overlaps(TKey startInclusive, TKey endExclusive)
    {
        RangeDictionary<TKey>.ValidateRange(startInclusive, endExclusive, _comparer);

        int index = LowerBound(startInclusive);

        if (index > 0 && _comparer.Compare(_ends[index - 1], startInclusive) > 0)
            return true;

        return index < _count && _comparer.Compare(_starts[index], endExclusive) < 0;
    }

    /// <summary>
    /// Ensures that the dictionary can hold at least the specified number of ranges without reallocating.
    /// </summary>
    /// <param name="capacity">The desired capacity.</param>
    /// <returns>The current capacity.</returns>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        if (_starts.Length < capacity)
            ResizeStorage(GrowCapacity(capacity));

        return _starts.Length;
    }

    /// <summary>
    /// Copies the stored range entries to a new array.
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
    /// <returns>The enumerator.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <inheritdoc />
    IEnumerator<ValueRange<TKey, TValue>> IEnumerable<ValueRange<TKey, TValue>>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    private int FindContainingIndex(TKey key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        int index = UpperBound(key) - 1;

        if (index < 0)
            return -1;

        return _comparer.Compare(key, _ends[index]) < 0 ? index : -1;
    }

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

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    private void ResizeStorage(int capacity)
    {
        Array.Resize(ref _starts, capacity);
        Array.Resize(ref _ends, capacity);
        Array.Resize(ref _values, capacity);
    }

    private int GrowCapacity(int minimum)
    {
        int capacity = _starts.Length == 0 ? DefaultCapacity : _starts.Length * 2;

        if ((uint)capacity > Array.MaxLength)
            capacity = Array.MaxLength;

        if (capacity < minimum)
            capacity = minimum;

        return capacity;
    }

    /// <summary>
    /// Enumerates a <see cref="RangeDictionary{TKey, TValue}" /> without allocating.
    /// </summary>
    public struct Enumerator : IEnumerator<ValueRange<TKey, TValue>>
    {
        private readonly RangeDictionary<TKey, TValue> _owner;
        private readonly int _version;
        private int _index;
        private ValueRange<TKey, TValue> _current;

        internal Enumerator(RangeDictionary<TKey, TValue> owner)
        {
            _owner = owner;
            _version = owner._version;
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public ValueRange<TKey, TValue> Current => _current;

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException("The collection was modified during enumeration.");

            if (_index >= _owner._count)
                return false;

            _current = new ValueRange<TKey, TValue>(
                _owner._starts[_index],
                _owner._ends[_index],
                _owner._values[_index],
                skipValidation: true);

            _index++;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException("The collection was modified during enumeration.");

            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}