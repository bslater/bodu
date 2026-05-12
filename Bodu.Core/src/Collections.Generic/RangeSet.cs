// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a sorted set of non-overlapping half-open ranges.
/// </summary>
/// <typeparam name="T">The comparable endpoint type.</typeparam>
/// <remarks>
/// <para>
/// Ranges are stored in two compact parallel arrays: one for starts and one for ends. The arrays are kept
/// sorted by start value and adjacent or overlapping ranges are merged on insertion.
/// </para>
/// <para>
/// Ranges use half-open semantics: <c>[startInclusive, endExclusive)</c>.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[Serializable]
public sealed class RangeSet<T> 
    : IReadOnlyCollection<RangeDictionary<T>>
    where T : IComparable<T>
{
    private const int DefaultCapacity = 4;

    private readonly IComparer<T> _comparer;
    private T[] _starts;
    private T[] _ends;
    private int _count;
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeSet{T}" /> class.
    /// </summary>
    public RangeSet()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeSet{T}" /> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">The endpoint comparer, or <see langword="null" /> to use the default comparer.</param>
    public RangeSet(IComparer<T>? comparer)
    {
        _comparer = comparer ?? Comparer<T>.Default;
        _starts = Array.Empty<T>();
        _ends = Array.Empty<T>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeSet{T}" /> class containing the specified ranges.
    /// </summary>
    /// <param name="ranges">The ranges to add.</param>
    /// <param name="comparer">The endpoint comparer, or <see langword="null" /> to use the default comparer.</param>
    public RangeSet(IEnumerable<RangeDictionary<T>> ranges, IComparer<T>? comparer = null)
        : this(comparer)
    {
        if (ranges is null)
            throw new ArgumentNullException(nameof(ranges));

        foreach (RangeDictionary<T> range in ranges)
            Add(range.StartInclusive, range.EndExclusive);
    }

    /// <summary>
    /// Gets the comparer used to order range endpoints.
    /// </summary>
    public IComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the number of stored ranges.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets the allocated range capacity.
    /// </summary>
    public int Capacity => _starts.Length;

    /// <summary>
    /// Gets the range at the specified sorted index.
    /// </summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The range at <paramref name="index" />.</returns>
    public RangeDictionary<T> this[int index]
    {
        get
        {
            ValidateIndex(index);
            return new RangeDictionary<T>(_starts[index], _ends[index]);
        }
    }

    /// <summary>
    /// Adds a half-open range to the set, merging any overlapping or adjacent ranges.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    public void Add(T startInclusive, T endExclusive)
    {
        RangeDictionary<T>.ValidateRange(startInclusive, endExclusive, _comparer);

        if (_count == 0)
        {
            InsertAt(0, startInclusive, endExclusive);
            return;
        }

        int index = LowerBound(startInclusive);
        int mergeFrom = index;
        T start = startInclusive;
        T end = endExclusive;

        if (index > 0 && _comparer.Compare(_ends[index - 1], start) >= 0)
        {
            mergeFrom = index - 1;
            start = Min(_starts[mergeFrom], start);
            end = Max(_ends[mergeFrom], end);
        }

        int mergeTo = index;
        while (mergeTo < _count && _comparer.Compare(_starts[mergeTo], end) <= 0)
        {
            end = Max(end, _ends[mergeTo]);
            mergeTo++;
        }

        if (mergeFrom == mergeTo)
        {
            InsertAt(index, start, end);
            return;
        }

        _starts[mergeFrom] = start;
        _ends[mergeFrom] = end;

        int removeCount = mergeTo - mergeFrom - 1;
        if (removeCount > 0)
            RemoveRange(mergeFrom + 1, removeCount);

        _version++;
    }

    /// <summary>
    /// Adds the specified range.
    /// </summary>
    /// <param name="range">The range to add.</param>
    public void Add(RangeDictionary<T> range) =>
        Add(range.StartInclusive, range.EndExclusive);

    /// <summary>
    /// Removes the specified half-open range from the set.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <returns>
    /// <see langword="true" /> if the set was changed; otherwise, <see langword="false" />.
    /// </returns>
    public bool Remove(T startInclusive, T endExclusive)
    {
        RangeDictionary<T>.ValidateRange(startInclusive, endExclusive, _comparer);

        if (_count == 0)
            return false;

        bool changed = false;
        int index = Math.Max(0, UpperBound(startInclusive) - 1);

        while (index < _count)
        {
            if (_comparer.Compare(_starts[index], endExclusive) >= 0)
                break;

            if (_comparer.Compare(_ends[index], startInclusive) <= 0)
            {
                index++;
                continue;
            }

            T rangeStart = _starts[index];
            T rangeEnd = _ends[index];

            bool removesLeft = _comparer.Compare(startInclusive, rangeStart) <= 0;
            bool removesRight = _comparer.Compare(endExclusive, rangeEnd) >= 0;

            if (removesLeft && removesRight)
            {
                RemoveAt(index);
                changed = true;
                continue;
            }

            if (removesLeft)
            {
                _starts[index] = endExclusive;
                _version++;
                changed = true;
                break;
            }

            if (removesRight)
            {
                _ends[index] = startInclusive;
                _version++;
                changed = true;
                index++;
                continue;
            }

            // Split one range into two ranges.
            _ends[index] = startInclusive;
            InsertAt(index + 1, endExclusive, rangeEnd);
            changed = true;
            break;
        }

        return changed;
    }

    /// <summary>
    /// Removes the specified range from the set.
    /// </summary>
    /// <param name="range">The range to remove.</param>
    /// <returns>
    /// <see langword="true" /> if the set was changed; otherwise, <see langword="false" />.
    /// </returns>
    public bool Remove(RangeDictionary<T> range) =>
        Remove(range.StartInclusive, range.EndExclusive);

    /// <summary>
    /// Removes all ranges from the set.
    /// </summary>
    public void Clear()
    {
        if (_count == 0)
            return;

        Array.Clear(_starts, 0, _count);
        Array.Clear(_ends, 0, _count);
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Determines whether the specified value falls inside any stored range.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> if the value is contained; otherwise, <see langword="false" />.
    /// </returns>
    public bool Contains(T value) =>
        FindContainingIndex(value) >= 0;

    /// <summary>
    /// Determines whether the specified range is fully contained in this set.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <returns>
    /// <see langword="true" /> if the range is fully contained; otherwise, <see langword="false" />.
    /// </returns>
    public bool Contains(T startInclusive, T endExclusive)
    {
        RangeDictionary<T>.ValidateRange(startInclusive, endExclusive, _comparer);

        int index = FindContainingIndex(startInclusive);
        return index >= 0 && _comparer.Compare(endExclusive, _ends[index]) <= 0;
    }

    /// <summary>
    /// Determines whether the specified range overlaps any stored range.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <returns>
    /// <see langword="true" /> if the range overlaps a stored range; otherwise, <see langword="false" />.
    /// </returns>
    public bool Overlaps(T startInclusive, T endExclusive)
    {
        RangeDictionary<T>.ValidateRange(startInclusive, endExclusive, _comparer);

        int index = Math.Max(0, UpperBound(startInclusive) - 1);

        while (index < _count && _comparer.Compare(_starts[index], endExclusive) < 0)
        {
            if (_comparer.Compare(_ends[index], startInclusive) > 0)
                return true;

            index++;
        }

        return false;
    }

    /// <summary>
    /// Returns a new set containing the union of this set and another set.
    /// </summary>
    /// <param name="other">The other set.</param>
    /// <returns>The union set.</returns>
    public RangeSet<T> Union(RangeSet<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        var result = new RangeSet<T>(_comparer);
        result.EnsureCapacity(_count + other._count);

        for (int i = 0; i < _count; i++)
            result.Add(_starts[i], _ends[i]);

        for (int i = 0; i < other._count; i++)
            result.Add(other._starts[i], other._ends[i]);

        return result;
    }

    /// <summary>
    /// Returns a new set containing the intersection of this set and another set.
    /// </summary>
    /// <param name="other">The other set.</param>
    /// <returns>The intersection set.</returns>
    public RangeSet<T> Intersect(RangeSet<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        var result = new RangeSet<T>(_comparer);

        int left = 0;
        int right = 0;

        while (left < _count && right < other._count)
        {
            T start = Max(_starts[left], other._starts[right]);
            T end = Min(_ends[left], other._ends[right]);

            if (_comparer.Compare(start, end) < 0)
                result.Add(start, end);

            if (_comparer.Compare(_ends[left], other._ends[right]) < 0)
                left++;
            else
                right++;
        }

        return result;
    }

    /// <summary>
    /// Returns a new set containing the ranges in this set except those covered by another set.
    /// </summary>
    /// <param name="other">The other set.</param>
    /// <returns>The difference set.</returns>
    public RangeSet<T> Except(RangeSet<T> other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        var result = new RangeSet<T>(_comparer);
        result.EnsureCapacity(_count);

        for (int i = 0; i < _count; i++)
            result.Add(_starts[i], _ends[i]);

        for (int i = 0; i < other._count; i++)
            result.Remove(other._starts[i], other._ends[i]);

        return result;
    }

    /// <summary>
    /// Ensures that the set can hold at least the specified number of ranges without reallocating.
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
    /// Copies the stored ranges to a new array.
    /// </summary>
    /// <returns>A new array containing the stored ranges.</returns>
    public RangeDictionary<T>[] ToArray()
    {
        var result = new RangeDictionary<T>[_count];

        for (int i = 0; i < _count; i++)
            result[i] = new RangeDictionary<T>(_starts[i], _ends[i]);

        return result;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the stored ranges in ascending order.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <inheritdoc />
    IEnumerator<RangeDictionary<T>> IEnumerable<RangeDictionary<T>>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    private int FindContainingIndex(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        int index = UpperBound(value) - 1;

        if (index < 0)
            return -1;

        return _comparer.Compare(value, _ends[index]) < 0 ? index : -1;
    }

    private void InsertAt(int index, T startInclusive, T endExclusive)
    {
        EnsureCapacity(_count + 1);

        if (index < _count)
        {
            Array.Copy(_starts, index, _starts, index + 1, _count - index);
            Array.Copy(_ends, index, _ends, index + 1, _count - index);
        }

        _starts[index] = startInclusive;
        _ends[index] = endExclusive;
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
        }

        _count--;
        _starts[_count] = default!;
        _ends[_count] = default!;
        _version++;
    }

    private void RemoveRange(int index, int count)
    {
        if (count <= 0)
            return;

        int moveCount = _count - index - count;

        if (moveCount > 0)
        {
            Array.Copy(_starts, index + count, _starts, index, moveCount);
            Array.Copy(_ends, index + count, _ends, index, moveCount);
        }

        Array.Clear(_starts, _count - count, count);
        Array.Clear(_ends, _count - count, count);

        _count -= count;
        _version++;
    }

    private int LowerBound(T value)
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

    private int UpperBound(T value)
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

    private T Min(T left, T right) =>
        _comparer.Compare(left, right) <= 0 ? left : right;

    private T Max(T left, T right) =>
        _comparer.Compare(left, right) >= 0 ? left : right;

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    private void ResizeStorage(int capacity)
    {
        Array.Resize(ref _starts, capacity);
        Array.Resize(ref _ends, capacity);
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
    /// Enumerates a <see cref="RangeSet{T}" /> without allocating.
    /// </summary>
    public struct Enumerator : IEnumerator<RangeDictionary<T>>
    {
        private readonly RangeSet<T> _owner;
        private readonly int _version;
        private int _index;
        private RangeDictionary<T> _current;

        internal Enumerator(RangeSet<T> owner)
        {
            _owner = owner;
            _version = owner._version;
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public RangeDictionary<T> Current => _current;

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException("The collection was modified during enumeration.");

            if (_index >= _owner._count)
                return false;

            _current = new RangeDictionary<T>(_owner._starts[_index], _owner._ends[_index]);
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

