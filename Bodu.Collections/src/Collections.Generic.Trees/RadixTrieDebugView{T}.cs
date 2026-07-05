// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="RadixTrie{TValue}" />, rendering its key/value pairs as a flat
/// array.
/// </summary>
/// <typeparam name="TValue">The type of the values stored in the trie.</typeparam>
internal sealed class RadixTrieDebugView<TValue>
{
    /// <summary>The trie whose key/value pairs are surfaced in the debugger.</summary>
    private readonly RadixTrie<TValue> _trie;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadixTrieDebugView{TValue}" /> class.
    /// </summary>
    /// <param name="trie">The trie to surface in the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="trie" /> is <see langword="null" />.</exception>
    public RadixTrieDebugView(RadixTrie<TValue> trie)
    {
        _trie = trie ?? throw new ArgumentNullException(nameof(trie));
    }

    /// <summary>
    /// Gets a snapshot of the trie's key/value pairs.
    /// </summary>
    /// <value>An array of the trie's entries captured at inspection time.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<string, TValue>[] Items => _trie.ToArrayInternal();
}
