// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryDebugView.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    public MultiValueDictionaryDebugView(MultiValueDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary;
    }

    /// <summary>
    /// Gets the key-value-list pairs displayed in the debugger.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, IReadOnlyList<TValue>>[] Items =>
        _dictionary.ToArray();
}