// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferDebugView.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="CircularBuffer{T}" /> contents.
/// </summary>
/// <typeparam name="T">Specifies the type of elements in the collection.</typeparam>
/// <remarks>
/// This debug view class simplifies inspection by displaying the contents as an array in the debugger.
/// </remarks>
internal sealed class CircularBufferDebugView<T>
{
    /// <summary>
    /// The instance of <see cref="CircularBuffer{T}" /> being debugged.
    /// </summary>
    private readonly CircularBuffer<T> _circularBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBufferDebugView{T}" /> class.
    /// </summary>
    /// <param name="circularBuffer">The circular buffer instance to debug.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="circularBuffer" /> is <see langword="null" />.
    /// </exception>
    public CircularBufferDebugView(CircularBuffer<T> circularBuffer)
    {
        _circularBuffer = circularBuffer ?? throw new ArgumentNullException(nameof(circularBuffer));
    }

    /// <summary>
    /// Gets the items currently contained within the <see cref="CircularBuffer{T}" /> as an array.
    /// </summary>
    /// <remarks>
    /// This property is hidden from the debugger display root to directly expand the items for quick inspection.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items =>
        [.. _circularBuffer];
}
