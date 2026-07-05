// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieDebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="RadixTrie" />, rendering its keys as a flat array.
/// </summary>
internal sealed class RadixTrieDebugView
{
    /// <summary>The trie whose keys are surfaced in the debugger.</summary>
    private readonly RadixTrie _trie;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadixTrieDebugView" /> class.
    /// </summary>
    /// <param name="trie">The trie to surface in the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="trie" /> is <see langword="null" />.</exception>
    public RadixTrieDebugView(RadixTrie trie)
    {
        _trie = trie ?? throw new ArgumentNullException(nameof(trie));
    }

    /// <summary>
    /// Gets a snapshot of the trie's keys.
    /// </summary>
    /// <value>An array of the trie's keys captured at inspection time.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public string[] Items => _trie.ToArrayInternal();
}
