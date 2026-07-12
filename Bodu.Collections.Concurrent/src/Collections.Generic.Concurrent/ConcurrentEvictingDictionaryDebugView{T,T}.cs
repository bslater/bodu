// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryDebugView{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a debugger-friendly view of the contents of a <see cref="ConcurrentEvictingDictionary{TKey, TValue}" />.
/// </summary>
/// <typeparam name="TKey">Specifies the type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">Specifies the type of values in the dictionary.</typeparam>
/// <remarks>
/// This proxy renders the dictionary's live entries as a flat array in the debugger by capturing a snapshot via
/// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.ToArray" /> each time the debugger expands the items. The
/// snapshot is stable for inspection but is not synchronized with concurrent writers.
/// </remarks>
internal sealed class ConcurrentEvictingDictionaryDebugView<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The dictionary instance being inspected.</summary>
    private readonly ConcurrentEvictingDictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionaryDebugView{TKey, TValue}" /> class.
    /// </summary>
    /// <param name="dictionary">
    /// The dictionary instance to expose to the debugger. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary" /> is <see langword="null" />.</exception>
    public ConcurrentEvictingDictionaryDebugView(ConcurrentEvictingDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
    }

    /// <summary>
    /// Gets the entries currently observable in the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> as an
    /// array.
    /// </summary>
    /// <value>A snapshot array of the dictionary's live entries.</value>
    /// <remarks>
    /// The display root is hidden so the debugger expands directly into the entry list.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, TValue>[] Items => _dictionary.ToArray();
}
