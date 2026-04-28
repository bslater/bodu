// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeDebugView.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="ArrayDeque{T}"/> contents.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the deque.</typeparam>
internal sealed class ArrayDequeDebugView<T>
{
    /// <summary>
    /// The instance of <see cref="ArrayDeque{T}"/> being displayed.
    /// </summary>
    private readonly ArrayDeque<T> _deque;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDequeDebugView{T}"/> class.
    /// </summary>
    /// <param name="deque">The deque instance to display.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deque"/> is <see langword="null"/>.</exception>
    public ArrayDequeDebugView(ArrayDeque<T> deque) =>
        _deque = deque ?? throw new ArgumentNullException(nameof(deque));

    /// <summary>
    /// Gets the deque's elements as an array, ordered from head to tail.
    /// </summary>
    /// <returns>An array of the deque's current elements.</returns>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => _deque.ToArray();
}
