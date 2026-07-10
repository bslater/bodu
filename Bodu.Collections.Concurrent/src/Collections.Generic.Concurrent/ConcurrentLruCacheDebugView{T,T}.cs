// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheDebugView{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a debugger-friendly view of the contents of a <see cref="ConcurrentLruCache{TKey, TValue}" />.
/// </summary>
/// <typeparam name="TKey">Specifies the type of keys in the cache.</typeparam>
/// <typeparam name="TValue">Specifies the type of values in the cache.</typeparam>
/// <remarks>
/// This proxy renders the cache's entries as a flat array in the debugger by capturing a snapshot via
/// <see cref="ConcurrentLruCache{TKey, TValue}.ToArray" /> each time the debugger expands the items. The snapshot is
/// stable for inspection but is not synchronized with concurrent writers.
/// </remarks>
internal sealed class ConcurrentLruCacheDebugView<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The cache instance being inspected.</summary>
    private readonly ConcurrentLruCache<TKey, TValue> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentLruCacheDebugView{TKey, TValue}" /> class.
    /// </summary>
    /// <param name="cache">The cache instance to expose to the debugger. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cache" /> is <see langword="null" />.</exception>
    public ConcurrentLruCacheDebugView(ConcurrentLruCache<TKey, TValue> cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Gets the entries currently observable in the <see cref="ConcurrentLruCache{TKey, TValue}" /> as an array.
    /// </summary>
    /// <value>A snapshot array of the cache's entries.</value>
    /// <remarks>
    /// The display root is hidden so the debugger expands directly into the entry list.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, TValue>[] Items => _cache.ToArray();
}
