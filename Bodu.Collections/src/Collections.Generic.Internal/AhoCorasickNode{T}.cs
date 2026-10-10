// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickNode{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Represents a single state in an Aho-Corasick automaton, holding its goto transitions, failure link, output link, and
/// - when the state terminates a pattern - the pattern and its associated value.
/// </summary>
/// <typeparam name="TValue">The type of value stored at pattern-terminating states.</typeparam>
internal sealed class AhoCorasickNode<TValue>
{
    /// <summary>
    /// Gets or sets the goto transitions keyed by character, or <see langword="null" /> when the state is a leaf.
    /// </summary>
    public Dictionary<char, AhoCorasickNode<TValue>>? Children { get; set; }

    /// <summary>
    /// Gets or sets the failure link - the state for the longest proper suffix of this state's path that is itself a
    /// path in the goto trie. <see langword="null" /> only on the root.
    /// </summary>
    public AhoCorasickNode<TValue>? Fail { get; set; }

    /// <summary>
    /// Gets or sets the output link - the nearest state on the failure chain that terminates a pattern, or
    /// <see langword="null" /> when no proper suffix of this state's path is a pattern.
    /// </summary>
    public AhoCorasickNode<TValue>? Output { get; set; }

    /// <summary>
    /// Gets or sets the pattern terminating at this state, or <see langword="null" /> when the state is not terminal.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the value associated with the pattern terminating at this state.
    /// </summary>
    public TValue Value { get; set; } = default!;
}
