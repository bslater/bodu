// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryDebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger view for <see cref="EvictingDictionary{TKey, TValue}" /> that displays the dictionary's
/// key/value pairs directly in the debugger.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <remarks>
/// This type is intended to be used as a debugger proxy (e.g., via <c>[DebuggerTypeProxy]</c>) so that the dictionary's
/// contents appear as a flat list in the watch/locals window.
/// </remarks>
internal sealed class EvictingDictionaryDebugView<TKey, TValue>
    where TKey : notnull
{
    private readonly EvictingDictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionaryDebugView{TKey, TValue}" /> class for the
    /// specified <paramref name="dictionary" />.
    /// </summary>
    /// <param name="dictionary">The dictionary to expose in the debugger view.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary" /> is <see langword="null" />.</exception>
    public EvictingDictionaryDebugView(EvictingDictionary<TKey, TValue> dictionary)
    {
        ThrowHelper.ThrowIfNull(dictionary);
        _dictionary = dictionary;
    }

    /// <summary>
    /// Gets the dictionary entries as an array for display in the debugger.
    /// </summary>
    /// <remarks>
    /// Marked with <see cref="DebuggerBrowsableAttribute" /> using <see cref="DebuggerBrowsableState.RootHidden" />, so
    /// the elements appear directly under the parent node in the debugger rather than as a nested <c>Items</c>
    /// property.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, TValue>[] Items => [.. _dictionary];
}
