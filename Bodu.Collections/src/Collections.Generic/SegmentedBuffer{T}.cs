// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBuffer{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

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
public sealed class SegmentedBuffer<T> :
    System.Collections.Generic.IEnumerable<T>
{
    /// <summary>The segment size used when the buffer is constructed without an explicit size.</summary>
    private const int DefaultSegmentSize = 512;

    /// <summary>The list of fixed-size array segments that together hold the buffered elements.</summary>
    private readonly List<T[]> _segments;

    /// <summary>The fixed number of elements each segment can hold.</summary>
    private readonly int _segmentSize;

    /// <summary>Incremented on every mutation; used by enumerators to detect concurrent modification.</summary>
    private int _version;

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
        if (Count % _segmentSize == 0)
            _segments.Add(new T[_segmentSize]);

        int segmentIndex = Count / _segmentSize;
        int offset = Count % _segmentSize;
        _segments[segmentIndex][offset] = item;

        Count++;
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
}
