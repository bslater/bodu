// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrieDebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="Trie" />, rendering its keys as a flat array.
/// </summary>
internal sealed class TrieDebugView
{
    private readonly Trie _trie;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrieDebugView" /> class.
    /// </summary>
    /// <param name="trie">The trie to surface in the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="trie" /> is <see langword="null" />.</exception>
    public TrieDebugView(Trie trie)
    {
        _trie = trie ?? throw new ArgumentNullException(nameof(trie));
    }

    /// <summary>
    /// Gets a snapshot of the trie's keys.
    /// </summary>
    /// <returns>An array of the trie's keys captured at inspection time.</returns>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public string[] Items => _trie.ToArrayInternal();
}
