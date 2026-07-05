// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryDebugView{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="NavigableDictionary{TKey, TValue}" /> contents.
/// </summary>
/// <typeparam name="TKey">Specifies the type of keys stored in the dictionary.</typeparam>
/// <typeparam name="TValue">Specifies the type of values stored in the dictionary.</typeparam>
internal sealed class NavigableDictionaryDebugView<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The instance of <see cref="NavigableDictionary{TKey, TValue}" /> being displayed.</summary>
    private readonly NavigableDictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableDictionaryDebugView{TKey, TValue}" /> class.
    /// </summary>
    /// <param name="dictionary">The dictionary instance to display.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary" /> is <see langword="null" />.</exception>
    public NavigableDictionaryDebugView(NavigableDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
    }

    /// <summary>
    /// Gets the dictionary's entries as an array in ascending key order.
    /// </summary>
    /// <value>An array of the dictionary's current entries.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, TValue>[] Items => [.. _dictionary];
}
