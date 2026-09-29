// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrieNode{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Represents a single node in a prefix tree, holding its child transitions, terminal flag, and associated value.
/// </summary>
/// <typeparam name="TValue">The type of value stored at terminal nodes.</typeparam>
internal sealed class TrieNode<TValue>
{
    /// <summary>
    /// Gets or sets the child transitions keyed by character, or <see langword="null" /> when the node is a leaf.
    /// </summary>
    public Dictionary<char, TrieNode<TValue>>? Children { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a key terminates at this node.
    /// </summary>
    public bool IsTerminal { get; set; }

    /// <summary>
    /// Gets or sets the full key string terminating at this node, stored verbatim so enumeration reproduces the
    /// original key regardless of the character comparer in use.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the value associated with the key terminating at this node.
    /// </summary>
    public TValue Value { get; set; } = default!;
}
