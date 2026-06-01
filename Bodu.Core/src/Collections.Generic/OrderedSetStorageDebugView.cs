// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageDebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides the debugger view for ordered-set collections backed by <see cref="OrderedSetStorage{T}" />.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
internal sealed class OrderedSetStorageDebugView<T>
    where T : notnull
{
    private readonly OrderedSetStorage<T> _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSetStorageDebugView{T}" /> class.
    /// </summary>
    /// <param name="set">The ordered set displayed by the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="set" /> is <see langword="null" />.</exception>
    public OrderedSetStorageDebugView(OrderedSet<T> set)
    {
        _storage = set?.DebuggerStorage ?? throw new ArgumentNullException(nameof(set));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSetStorageDebugView{T}" /> class.
    /// </summary>
    /// <param name="set">The indexed set displayed by the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="set" /> is <see langword="null" />.</exception>
    public OrderedSetStorageDebugView(IndexedSet<T> set)
    {
        _storage = set?.DebuggerStorage ?? throw new ArgumentNullException(nameof(set));
    }

    /// <summary>
    /// Gets the elements displayed in the debugger.
    /// </summary>
    /// <remarks>
    /// This property is hidden from the debugger display root to directly expand the ordered items for quick
    /// inspection.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items =>
        _storage.ToArray();
}