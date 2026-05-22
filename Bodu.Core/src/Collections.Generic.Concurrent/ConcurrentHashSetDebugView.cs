// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetDebugView.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a debugger-friendly view of the contents of a <see cref="ConcurrentHashSet{T}" />.
/// </summary>
/// <typeparam name="T">Specifies the type of elements stored in the set.</typeparam>
/// <remarks>
/// This proxy renders the set's contents as a flat array in the debugger by capturing a snapshot via
/// <see cref="ConcurrentHashSet{T}.ToArray" /> each time the debugger expands the items. The snapshot is stable for
/// inspection but is not synchronized with concurrent writers.
/// </remarks>
internal sealed class ConcurrentHashSetDebugView<T>
    where T : notnull
{
    /// <summary>
    /// The set instance being inspected.
    /// </summary>
    private readonly ConcurrentHashSet<T> _set;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSetDebugView{T}" /> class.
    /// </summary>
    /// <param name="set">The set instance to expose to the debugger. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="set" /> is <see langword="null" />.</exception>
    public ConcurrentHashSetDebugView(ConcurrentHashSet<T> set)
    {
        _set = set ?? throw new ArgumentNullException(nameof(set));
    }

    /// <summary>
    /// Gets the elements currently observable in the <see cref="ConcurrentHashSet{T}" /> as an array.
    /// </summary>
    /// <returns>A snapshot array of the set's elements.</returns>
    /// <remarks>
    /// The display root is hidden so the debugger expands directly into the element list.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => _set.ToArray();
}
