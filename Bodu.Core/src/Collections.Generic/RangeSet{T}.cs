// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSet{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a sorted set of non-overlapping half-open ranges.
/// </summary>
/// <typeparam name="T">The comparable endpoint type.</typeparam>
/// <remarks>
/// <para>
/// Ranges are stored in two compact parallel arrays — one for the inclusive start of each range and one for the
/// exclusive end. The arrays are kept sorted by start endpoint, and adjacent or overlapping ranges are merged on
/// insertion.
/// </para>
/// <para>
/// Ranges use half-open semantics: <c>[startInclusive, endExclusive)</c>.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Track disjoint blocks of allocated row IDs. Adjacent and overlapping inserts merge automatically.
/// var allocated = new RangeSet<int>();
/// allocated.Add(  0,  10);   // [0, 10)
/// allocated.Add( 20,  30);   // [20, 30)
/// allocated.Add(  5,  25);   // merges all three into the single range [0, 30)
///
/// Console.WriteLine(allocated.Count);          // 1
/// Console.WriteLine(allocated.Contains(15));   // true
/// allocated.Remove(10, 20);                    // splits into [0, 10) and [20, 30)
///]]>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[Serializable]
public sealed partial class RangeSet<T>
    : IReadOnlyCollection<Range<T>>
    where T : IComparable<T>
{
    /// <summary>The capacity used when the set is constructed without an explicit capacity.</summary>
    private const int DefaultCapacity = 4;

    /// <summary>The comparer used to order and compare range endpoints.</summary>
    private readonly IComparer<T> _comparer;

    /// <summary>The inclusive lower endpoints of the stored ranges, kept sorted and parallel to <see cref="_ends" />.</summary>
    private T[] _starts;

    /// <summary>The upper endpoints of the stored ranges, parallel to <see cref="_starts" />.</summary>
    private T[] _ends;

    /// <summary>The number of ranges currently stored.</summary>
    private int _count;

    /// <summary>The modification counter used to detect concurrent mutation during enumeration.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeSet{T}" /> class using the default comparer.
    /// </summary>
    public RangeSet()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeSet{T}" /> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">
    /// The endpoint comparer, or <see langword="null" /> to use <see cref="Comparer{T}.Default" />.
    /// </param>
    public RangeSet(IComparer<T>? comparer)
    {
        _comparer = comparer ?? Comparer<T>.Default;
        _starts = [];
        _ends = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeSet{T}" /> class containing the specified ranges.
    /// </summary>
    /// <param name="ranges">The ranges to add. Must not be <see langword="null" />.</param>
    /// <param name="comparer">
    /// The endpoint comparer, or <see langword="null" /> to use <see cref="Comparer{T}.Default" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="ranges" /> is <see langword="null" />.</exception>
    public RangeSet(IEnumerable<Range<T>> ranges, IComparer<T>? comparer = null)
        : this(comparer)
    {
        ThrowHelper.ThrowIfNull(ranges);

        foreach (Range<T> range in ranges)
            Add(range.StartInclusive, range.EndExclusive);
    }

    /// <summary>
    /// Gets the comparer used to order range endpoints.
    /// </summary>
    /// <value>The active endpoint comparer.</value>
    public IComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the number of stored ranges.
    /// </summary>
    /// <value>The number of ranges currently stored in the set.</value>
    public int Count => _count;

    /// <summary>
    /// Gets the allocated range capacity.
    /// </summary>
    /// <value>The current allocated capacity of the underlying storage.</value>
    public int Capacity => _starts.Length;

    /// <summary>
    /// Gets the range at the specified sorted index.
    /// </summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The range at <paramref name="index" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    public Range<T> this[int index]
    {
        get
        {
            ValidateIndex(index);
            return new Range<T>(_starts[index], _ends[index]);
        }
    }

    /// <summary>
    /// Adds a half-open range to the set, merging any overlapping or adjacent ranges.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />.
    /// </exception>
    public void Add(T startInclusive, T endExclusive)
    {
        Range<T>.ValidateRange(startInclusive, endExclusive, _comparer);

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
    /// Adds the specified range to the set, merging any overlapping or adjacent ranges.
    /// </summary>
    /// <param name="range">The range to add.</param>
    public void Add(Range<T> range) =>
        Add(range.StartInclusive, range.EndExclusive);

    /// <summary>
    /// Removes the specified half-open range from the set, trimming or splitting overlapping ranges as needed.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <returns><see langword="true" /> if the set was changed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />.
    /// </exception>
    public bool Remove(T startInclusive, T endExclusive)
    {
        Range<T>.ValidateRange(startInclusive, endExclusive, _comparer);

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

            // Split one range into two ranges: the prefix [rangeStart, startInclusive) is retained in
            // place, and the suffix [endExclusive, rangeEnd) is inserted immediately after.
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
    /// <returns><see langword="true" /> if the set was changed; otherwise, <see langword="false" />.</returns>
    public bool Remove(Range<T> range) =>
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
    /// <param name="value">The value to test. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the value is contained; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public bool Contains(T value) =>
        FindContainingIndex(value) >= 0;

    /// <summary>
    /// Determines whether the specified range is fully contained in this set.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <returns><see langword="true" /> if the range is fully contained; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />.
    /// </exception>
    public bool Contains(T startInclusive, T endExclusive)
    {
        Range<T>.ValidateRange(startInclusive, endExclusive, _comparer);

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
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />.
    /// </exception>
    public bool Overlaps(T startInclusive, T endExclusive)
    {
        Range<T>.ValidateRange(startInclusive, endExclusive, _comparer);

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
    /// <param name="other">The other set. Must not be <see langword="null" />.</param>
    /// <returns>A new <see cref="RangeSet{T}" /> containing the union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public RangeSet<T> Union(RangeSet<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        RangeSet<T> result = new(_comparer);
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
    /// <param name="other">The other set. Must not be <see langword="null" />.</param>
    /// <returns>A new <see cref="RangeSet{T}" /> containing the intersection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public RangeSet<T> Intersect(RangeSet<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        RangeSet<T> result = new(_comparer);

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
    /// <param name="other">The other set. Must not be <see langword="null" />.</param>
    /// <returns>A new <see cref="RangeSet{T}" /> containing the difference.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public RangeSet<T> Except(RangeSet<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        RangeSet<T> result = new(_comparer);
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
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    public int EnsureCapacity(int capacity)
    {
        ThrowHelper.ThrowIfNegative(capacity);

        if (_starts.Length < capacity)
            ResizeStorage(GrowCapacity(capacity));

        return _starts.Length;
    }

    /// <summary>
    /// Copies the stored ranges to a new array in ascending sorted order.
    /// </summary>
    /// <returns>A new array containing the stored ranges.</returns>
    public Range<T>[] ToArray()
    {
        var result = new Range<T>[_count];

        for (int i = 0; i < _count; i++)
            result[i] = new Range<T>(_starts[i], _ends[i]);

        return result;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the stored ranges in ascending order.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the stored ranges.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <summary>
    /// Locates the index of the range that contains <paramref name="value" />, if any.
    /// </summary>
    /// <param name="value">The value to locate. Must not be <see langword="null" />.</param>
    /// <returns>The index of the containing range, or <c>-1</c> if no range contains the value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    private int FindContainingIndex(T value)
    {
        ThrowHelper.ThrowIfNull(value);

        int index = UpperBound(value) - 1;

        return index < 0 ? -1 : _comparer.Compare(value, _ends[index]) < 0 ? index : -1;
    }

    /// <summary>
    /// Inserts a range at the specified position, growing storage as needed and shifting trailing entries.
    /// </summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
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

    /// <summary>
    /// Removes the range at the specified index and shifts trailing ranges left to keep storage contiguous.
    /// </summary>
    /// <param name="index">The index of the range to remove.</param>
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

    /// <summary>
    /// Removes <paramref name="count" /> consecutive ranges starting at <paramref name="index" /> and shifts any
    /// trailing ranges left to keep storage contiguous. The single caller in <see cref="Add(T, T)" /> only invokes this
    /// when <paramref name="count" /> is positive, so no defensive guard is performed.
    /// </summary>
    /// <param name="index">The index of the first range to remove.</param>
    /// <param name="count">The number of ranges to remove. Must be greater than zero.</param>
    private void RemoveRange(int index, int count)
    {
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

    /// <summary>
    /// Returns the lowest index whose start endpoint is not less than <paramref name="value" />.
    /// </summary>
    /// <param name="value">The endpoint to locate.</param>
    /// <returns>The lower-bound index for <paramref name="value" />.</returns>
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

    /// <summary>
    /// Returns the lowest index whose start endpoint is greater than <paramref name="value" />.
    /// </summary>
    /// <param name="value">The endpoint to locate.</param>
    /// <returns>The upper-bound index for <paramref name="value" />.</returns>
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

    /// <summary>
    /// Returns the lesser of two values per the configured comparer.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The lesser of the two operands.</returns>
    private T Min(T left, T right) =>
        _comparer.Compare(left, right) <= 0 ? left : right;

    /// <summary>
    /// Returns the greater of two values per the configured comparer.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The greater of the two operands.</returns>
    private T Max(T left, T right) =>
        _comparer.Compare(left, right) >= 0 ? left : right;

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
