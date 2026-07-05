// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="Deque{T}" /> contents.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the deque.</typeparam>
internal sealed class DequeDebugView<T>
{
    /// <summary>The instance of <see cref="Deque{T}" /> being displayed.</summary>
    private readonly Deque<T> _deque;

    /// <summary>
    /// Initializes a new instance of the <see cref="DequeDebugView{T}" /> class.
    /// </summary>
    /// <param name="deque">The deque instance to display.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deque" /> is <see langword="null" />.</exception>
    public DequeDebugView(Deque<T> deque)
    {
        _deque = deque ?? throw new ArgumentNullException(nameof(deque));
    }

    /// <summary>
    /// Gets the deque's elements as an array, ordered from head to tail.
    /// </summary>
    /// <value>An array of the deque's current elements.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => [.. _deque];
}
