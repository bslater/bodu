// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="SegmentedBuffer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents an append-only segmented buffer that grows efficiently by allocating fixed-size chunks (segments) of memory.
/// </summary>
/// <typeparam name="T">The type of elements stored in the buffer.</typeparam>
/// <remarks>
/// <para>
/// <see cref="SegmentedBuffer{T}" /> is designed for high-performance buffering scenarios where the number of elements is not known in
/// advance and large contiguous memory allocations (e.g., via <see cref="List{T}" />) may lead to excessive memory copying or large object
/// heap allocations.
/// </para>
/// <para>
/// This class avoids resizing overhead by allocating small fixed-size segments (e.g., 512 items) as needed, ensuring consistent append
/// performance even as the buffer grows. All elements are stored in order and are accessible via zero-based index.
/// </para>
/// <para>
/// Common usage scenarios include caching enumerator results, buffering streamed data, or logging events without knowing the upper bound in advance.
/// </para>
/// </remarks>
public sealed class SegmentedBuffer<T> :
    System.Collections.Generic.IEnumerable<T>
{
    private const int DefaultSegmentSize = 512;

    private readonly List<T[]> _segments;
    private readonly int _segmentSize;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentedBuffer{T}" /> class using the default segment size.
    /// </summary>
    /// <remarks>The default segment size is 512 items. New segments are allocated only as needed.</remarks>
    public SegmentedBuffer()
        : this(DefaultSegmentSize)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentedBuffer{T}" /> class using the specified segment size.
    /// </summary>
    /// <param name="segmentSize">The number of elements per segment.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="segmentSize" /> is less than 1.</exception>
    /// <remarks>
    /// A smaller segment size reduces memory per segment but may incur higher overhead for larger data sets. Larger segment sizes reduce
    /// segment management cost but increase memory fragmentation.
    /// </remarks>
    public SegmentedBuffer(int segmentSize)
    {
        ThrowHelper.ThrowIfLessThan(segmentSize, 1);

        this._segmentSize = segmentSize;
        this._segments = new List<T[]>();
    }

    /// <summary>
    /// Gets the number of elements contained in the buffer.
    /// </summary>
    /// <remarks>The value returned reflects the total number of elements added via <see cref="Add" />.</remarks>
    public int Count => this._count;

    /// <summary>
    /// Gets or sets the element at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve or assign.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="index" /> is less than 0 or greater than or equal to <see cref="Count" />.
    /// </exception>
    /// <remarks>This property provides O(1) access to buffered items, backed by segmented storage.</remarks>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)this._count)
                throw new ArgumentOutOfRangeException(nameof(index));

            int segmentIndex = index / this._segmentSize;
            int offset = index % this._segmentSize;
            return this._segments[segmentIndex][offset];
        }

        set
        {
            if ((uint)index >= (uint)this._count)
                throw new ArgumentOutOfRangeException(nameof(index));

            int segmentIndex = index / this._segmentSize;
            int offset = index % this._segmentSize;
            this._segments[segmentIndex][offset] = value;
        }
    }

    /// <summary>
    /// Adds an element to the end of the buffer.
    /// </summary>
    /// <param name="item">The item to add to the buffer.</param>
    /// <remarks>This operation executes in amortized constant time and does not require reallocation or copying of existing elements.</remarks>
    public void Add(T item)
    {
        if (this._count % this._segmentSize == 0)
            this._segments.Add(new T[this._segmentSize]);

        int segmentIndex = this._count / this._segmentSize;
        int offset = this._count % this._segmentSize;
        this._segments[segmentIndex][offset] = item;

        this._count++;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the buffer.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the buffer contents in insertion order.</returns>
    /// <remarks>
    /// Enumeration yields elements in the order they were added. The enumerator reflects the buffer state at the time of enumeration and
    /// does not account for elements added concurrently in multi-threaded scenarios.
    /// </remarks>
    public IEnumerator<T> GetEnumerator()
    {
        int count = this._count;
        int fullSegments = count / this._segmentSize;
        int lastSegmentCount = count % this._segmentSize;

        for (int i = 0; i < fullSegments; i++)
        {
            T[] segment = this._segments[i];
            for (int j = 0; j < this._segmentSize; j++)
                yield return segment[j];
        }

        if (lastSegmentCount > 0)
        {
            T[] lastSegment = this._segments[fullSegments];
            for (int j = 0; j < lastSegmentCount; j++)
                yield return lastSegment[j];
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
