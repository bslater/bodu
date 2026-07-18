// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBufferDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="SegmentedBuffer{T}" /> contents.
/// </summary>
/// <typeparam name="T">The type of elements stored in the buffer.</typeparam>
/// <remarks>
/// This debug view class simplifies inspection by displaying the buffered elements as an array in the debugger.
/// </remarks>
internal sealed class SegmentedBufferDebugView<T>
{
    /// <summary>The instance of <see cref="SegmentedBuffer{T}" /> being debugged.</summary>
    private readonly SegmentedBuffer<T> _segmentedBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentedBufferDebugView{T}" /> class.
    /// </summary>
    /// <param name="segmentedBuffer">The segmented buffer instance to debug.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="segmentedBuffer" /> is <see langword="null" />.
    /// </exception>
    public SegmentedBufferDebugView(SegmentedBuffer<T> segmentedBuffer)
    {
        _segmentedBuffer = segmentedBuffer ?? throw new ArgumentNullException(nameof(segmentedBuffer));
    }

    /// <summary>
    /// Gets the items currently contained within the <see cref="SegmentedBuffer{T}" /> as an array.
    /// </summary>
    /// <remarks>
    /// This property is hidden from the debugger display root to directly expand the items for quick inspection.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items =>
        _segmentedBuffer.ToArray();
}
