// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryDebugView.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="MultiValueDictionary{TKey, TValue}"/> contents,
/// showing each key alongside its associated value list.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
internal sealed class MultiValueDictionaryDebugView<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// The <see cref="MultiValueDictionary{TKey, TValue}"/> instance being debugged.
    /// </summary>
    private readonly MultiValueDictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionaryDebugView{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary">The dictionary instance to inspect. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public MultiValueDictionaryDebugView(MultiValueDictionary<TKey, TValue> dictionary) =>
        _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

    /// <summary>
    /// Gets the key–value-list pairs currently held in the <see cref="MultiValueDictionary{TKey, TValue}"/>,
    /// displayed in the debugger as root-level entries.
    /// </summary>
    /// <remarks>Shown in the debugger as root-level entries for quick inspection.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, IReadOnlyList<TValue>>[] Entries =>
        _dictionary.ToArray();
}
