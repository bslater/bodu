// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="NavigableSet{T}" /> contents.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the set.</typeparam>
internal sealed class NavigableSetDebugView<T>
    where T : notnull
{
    /// <summary>The instance of <see cref="NavigableSet{T}" /> being displayed.</summary>
    private readonly NavigableSet<T> _set;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableSetDebugView{T}" /> class.
    /// </summary>
    /// <param name="set">The set instance to display.</param>
    /// <exception cref="ArgumentNullException"><paramref name="set" /> is <see langword="null" />.</exception>
    public NavigableSetDebugView(NavigableSet<T> set)
    {
        _set = set ?? throw new ArgumentNullException(nameof(set));
    }

    /// <summary>
    /// Gets the set's elements as an array in ascending comparer order.
    /// </summary>
    /// <value>An array of the set's current elements.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => [.. _set];
}
