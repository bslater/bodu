// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBuffer{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents an append-only segmented buffer that grows efficiently by allocating fixed-size chunks (segments) of
/// memory.
/// </summary>
/// <typeparam name="T">The type of elements stored in the buffer.</typeparam>
/// <remarks>
/// <para>
/// <see cref="SegmentedBuffer{T}" /> is designed for high-performance buffering scenarios where the number of elements
/// is not known in advance and large contiguous memory allocations (e.g., via <see cref="List{T}" />) may lead to
/// excessive memory copying or large object heap allocations.
/// </para>
/// <para>
/// This class avoids resizing overhead by allocating small fixed-size segments (e.g., 512 items) as needed, ensuring
/// consistent append performance even as the buffer grows. All elements are stored in order and are accessible via
/// zero-based index.
/// </para>
/// <para>
/// Common usage scenarios include caching enumerator results, buffering streamed data, or logging events without
/// knowing the upper bound in advance.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Buffer a streamed sequence whose length is unknown, then random-access it by index.
/// var buffer = new SegmentedBuffer<int>(segmentSize: 512);
/// foreach (int value in ReadStream())
///     buffer.Add(value);
///
/// Console.WriteLine($"Buffered {buffer.Count} values");
/// int first = buffer[0];
/// int last  = buffer[buffer.Count - 1];
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(SegmentedBufferDebugView<>))]
public sealed class SegmentedBuffer<T> :
    System.Collections.Generic.IEnumerable<T>,
    System.Collections.Generic.IReadOnlyCollection<T>
{
    /// <summary>The segment size used when the buffer is constructed without an explicit size.</summary>
    private const int DefaultSegmentSize = 512;

    /// <summary>The list of fixed-size array segments that together hold the buffered elements.</summary>
    private readonly List<T[]> _segments;

    /// <summary>The fixed number of elements each segment can hold.</summary>
    private readonly int _segmentSize;

    /// <summary>Incremented on every mutation; used by enumerators to detect concurrent modification.</summary>
    private int _version;

    /// <summary>The segment currently receiving appended elements, or <see langword="null" /> before the first add.</summary>
    private T[]? _tailSegment;

    /// <summary>
    /// The next write position within <see cref="_tailSegment" />. Initialized to <see cref="_segmentSize" /> so the
    /// first add allocates a segment; keeping the cursor removes the per-add division and modulo on the append path.
    /// </summary>
    private int _tailOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentedBuffer{T}" /> class using the default segment size.
    /// </summary>
    /// <remarks>
    /// The default segment size is 512 items. New segments are allocated only as needed.
    /// </remarks>
    public SegmentedBuffer()
        : this(DefaultSegmentSize)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentedBuffer{T}" /> class using the specified segment size.
    /// </summary>
    /// <param name="segmentSize">The number of elements per segment.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="segmentSize" /> is less than 1.
    /// </exception>
    /// <remarks>
    /// A smaller segment size reduces memory per segment but may incur higher overhead for larger data sets. Larger
    /// segment sizes reduce segment management cost but increase memory fragmentation.
    /// </remarks>
    public SegmentedBuffer(int segmentSize)
    {
        ThrowHelper.ThrowIfLessThan(segmentSize, 1);

        _segmentSize = segmentSize;
        _segments = new List<T[]>();
        _tailOffset = segmentSize;
    }

    /// <summary>
    /// Gets the number of elements contained in the buffer.
    /// </summary>
    /// <remarks>
    /// The value returned reflects the total number of elements added via <see cref="Add" />.
    /// </remarks>
    public int Count { get; private set; }

    /// <summary>
    /// Gets or sets the element at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve or assign.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="index" /> is less than 0 or greater than or equal to <see cref="Count" />.
    /// </exception>
    /// <remarks>
    /// This property provides O(1) access to buffered items, backed by segmented storage.
    /// </remarks>
    public T this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfLessThan(index, 0);
            ThrowHelper.ThrowIfGreaterThanOrEqual(index, Count);

            int segmentIndex = index / _segmentSize;
            int offset = index % _segmentSize;
            return _segments[segmentIndex][offset];
        }

        set
        {
            ThrowHelper.ThrowIfLessThan(index, 0);
            ThrowHelper.ThrowIfGreaterThanOrEqual(index, Count);

            int segmentIndex = index / _segmentSize;
            int offset = index % _segmentSize;
            _segments[segmentIndex][offset] = value;

            _version++;
        }
    }

    /// <summary>
    /// Adds an element to the end of the buffer.
    /// </summary>
    /// <param name="item">The item to add to the buffer.</param>
    /// <remarks>
    /// This operation executes in amortized constant time and does not require reallocation or copying of existing
    /// elements.
    /// </remarks>
    public void Add(T item)
    {
        // The write cursor makes the append path arithmetic-free: no division or modulo per add, just a
        // bounds compare against the current tail segment.
        if (_tailOffset == _segmentSize)
        {
            _tailSegment = new T[_segmentSize];
            _segments.Add(_tailSegment);
            _tailOffset = 0;
        }

        _tailSegment![_tailOffset++] = item;

        Count++;
        _version++;
    }

    /// <summary>
    /// Removes all elements from the buffer, releasing the allocated segments.
    /// </summary>
    /// <remarks>
    /// The structural version is bumped when the buffer was non-empty, so any in-flight enumerator fails fast on its
    /// next step. Clearing an already-empty buffer is a no-op and does not invalidate active enumerators.
    /// </remarks>
    public void Clear()
    {
        if (Count == 0)
            return;

        _segments.Clear();
        _tailSegment = null;
        _tailOffset = _segmentSize;
        Count = 0;
        _version++;
    }

    /// <summary>
    /// Copies the buffer's elements to <paramref name="array" /> in insertion order, starting at
    /// <paramref name="arrayIndex" />.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The zero-based starting index in <paramref name="array" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// The destination is too small to hold the contents starting at <paramref name="arrayIndex" />.
    /// </exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(arrayIndex, nameof(arrayIndex));
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, Count);

        CopyToInternal(array, arrayIndex);
    }

    /// <summary>
    /// Returns a new array containing the buffer's elements in insertion order.
    /// </summary>
    /// <returns>A freshly allocated array of length <see cref="Count" />.</returns>
    public T[] ToArray()
    {
        var result = new T[Count];
        if (Count > 0)
            CopyToInternal(result, 0);

        return result;
    }

    /// <summary>
    /// Reduces the internal segment-list capacity to the number of allocated segments, releasing the spare segment
    /// references without touching the buffered elements or their segment layout.
    /// </summary>
    /// <remarks>
    /// The append-only segment layout means no data segment is ever over-allocated; this method trims only the spare
    /// capacity of the list that tracks the segments. The structural version is bumped so any in-flight enumerator
    /// fails fast on its next step.
    /// </remarks>
    public void TrimExcess()
    {
        _segments.TrimExcess();
        _version++;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the buffer.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the buffer contents in insertion order.</returns>
    /// <remarks>
    /// Enumeration yields elements in the order they were added. The enumerator is fail-fast: if the buffer is
    /// modified — by <see cref="Add" /> or by assignment through the indexer — after enumeration begins, the next
    /// iteration step throws <see cref="InvalidOperationException" />.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The buffer was modified after enumeration began.</exception>
    public IEnumerator<T> GetEnumerator()
    {
        int version = _version;
        int count = Count;
        int fullSegments = count / _segmentSize;
        int lastSegmentCount = count % _segmentSize;

        for (int i = 0; i < fullSegments; i++)
        {
            T[] segment = _segments[i];
            for (int j = 0; j < _segmentSize; j++)
            {
                yield return version != _version ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified) : segment[j];
            }
        }

        if (lastSegmentCount > 0)
        {
            T[] lastSegment = _segments[fullSegments];
            for (int j = 0; j < lastSegmentCount; j++)
            {
                yield return version != _version ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified) : lastSegment[j];
            }
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Copies the buffered elements into <paramref name="destination" /> starting at
    /// <paramref name="destinationIndex" />, in insertion order. The caller must ensure the destination has sufficient
    /// space.
    /// </summary>
    /// <param name="destination">The destination array.</param>
    /// <param name="destinationIndex">The zero-based starting index in <paramref name="destination" />.</param>
    private void CopyToInternal(T[] destination, int destinationIndex)
    {
        int fullSegments = Count / _segmentSize;
        int lastSegmentCount = Count % _segmentSize;
        int offset = destinationIndex;

        for (int i = 0; i < fullSegments; i++)
        {
            Array.Copy(_segments[i], 0, destination, offset, _segmentSize);
            offset += _segmentSize;
        }

        if (lastSegmentCount > 0)
            Array.Copy(_segments[fullSegments], 0, destination, offset, lastSegmentCount);
    }
}
