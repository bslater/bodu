// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferDebugView.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="ConcurrentCircularBuffer{T}"/> contents.
/// </summary>
/// <typeparam name="T">Specifies the reference type stored in the buffer.</typeparam>
/// <remarks>
/// This proxy renders the buffer's contents as a flat array in the debugger by capturing a snapshot via
/// <see cref="ConcurrentCircularBuffer{T}.ToArray"/> each time the debugger expands the items. The snapshot is
/// stable for inspection but is not synchronized with concurrent producers or consumers.
/// </remarks>
internal sealed class ConcurrentCircularBufferDebugView<T>
    where T : class?
{
    /// <summary>
    /// The buffer instance being inspected.
    /// </summary>
    private readonly ConcurrentCircularBuffer<T> _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCircularBufferDebugView{T}"/> class.
    /// </summary>
    /// <param name="buffer">The buffer instance to expose to the debugger. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
    public ConcurrentCircularBufferDebugView(ConcurrentCircularBuffer<T> buffer) =>
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

    /// <summary>
    /// Gets the items currently observable in the <see cref="ConcurrentCircularBuffer{T}"/> as an array.
    /// </summary>
    /// <returns>A snapshot array of buffer contents in head-to-tail logical order.</returns>
    /// <remarks>The display root is hidden so the debugger expands directly into the item list.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => _buffer.ToArray();
}
