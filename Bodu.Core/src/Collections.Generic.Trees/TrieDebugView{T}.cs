// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrieDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="Trie{TValue}" />, rendering its key/value pairs as a flat array.
/// </summary>
/// <typeparam name="TValue">The type of the values stored in the trie.</typeparam>
internal sealed class TrieDebugView<TValue>
{
    /// <summary>The trie whose key/value pairs are surfaced in the debugger.</summary>
    private readonly Trie<TValue> _trie;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrieDebugView{TValue}" /> class.
    /// </summary>
    /// <param name="trie">The trie to surface in the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="trie" /> is <see langword="null" />.</exception>
    public TrieDebugView(Trie<TValue> trie)
    {
        _trie = trie ?? throw new ArgumentNullException(nameof(trie));
    }

    /// <summary>
    /// Gets a snapshot of the trie's key/value pairs.
    /// </summary>
    /// <returns>An array of the trie's entries captured at inspection time.</returns>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<string, TValue>[] Items => _trie.ToArrayInternal();
}
