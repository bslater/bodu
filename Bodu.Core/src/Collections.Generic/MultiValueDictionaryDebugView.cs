// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryDebugView.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides the debugger view for <see cref="MultiValueDictionary{TKey, TValue}" />.
/// </summary>
/// <typeparam name="TKey">The type of keys.</typeparam>
/// <typeparam name="TValue">The type of values.</typeparam>
internal sealed class MultiValueDictionaryDebugView<TKey, TValue>
    where TKey : notnull
{
    private readonly MultiValueDictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionaryDebugView{TKey, TValue}" /> class.
    /// </summary>
    /// <param name="dictionary">The dictionary displayed by the debugger.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dictionary" /> is <see langword="null" />.
    /// </exception>
    public MultiValueDictionaryDebugView(MultiValueDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
    }

    /// <summary>
    /// Gets the key-value-list pairs displayed in the debugger.
    /// </summary>
    /// <remarks>
    /// This property is hidden from the debugger display root to directly expand the items for quick inspection.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, IReadOnlyList<TValue>>[] Items =>
        _dictionary.ToArray();
}
