// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrieCore.cs" company="Bodu Pty. Ltd.">
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

/// <summary>
/// Provides the shared character-walk, insertion, removal, and enumeration logic for the prefix-tree types, operating
/// over a generic <see cref="TrieNode{TValue}" /> so the set and map tries reuse a single implementation.
/// </summary>
internal static class TrieCore
{
    /// <summary>
    /// Walks the trie from the specified root following the characters of <paramref name="key" />.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at terminal nodes.</typeparam>
    /// <param name="root">The root node to walk from.</param>
    /// <param name="key">The character sequence to follow.</param>
    /// <returns>The node reached by the key, or <see langword="null" /> if the path does not exist.</returns>
    public static TrieNode<TValue>? Find<TValue>(TrieNode<TValue> root, ReadOnlySpan<char> key)
    {
        var node = root;
        foreach (var ch in key)
        {
            if (node.Children is null || !node.Children.TryGetValue(ch, out var next))
                return null;

            node = next;
        }

        return node;
    }

    /// <summary>
    /// Walks the trie from the specified root, creating any missing nodes along the path.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at terminal nodes.</typeparam>
    /// <param name="root">The root node to walk from.</param>
    /// <param name="key">The character sequence to follow.</param>
    /// <param name="comparer">The comparer used to key child transitions.</param>
    /// <returns>The node at which the key terminates.</returns>
    public static TrieNode<TValue> GetOrAddNode<TValue>(TrieNode<TValue> root, ReadOnlySpan<char> key, IEqualityComparer<char> comparer)
    {
        var node = root;
        foreach (var ch in key)
        {
            node.Children ??= new Dictionary<char, TrieNode<TValue>>(comparer);
            if (!node.Children.TryGetValue(ch, out var next))
            {
                next = new TrieNode<TValue>();
                node.Children.Add(ch, next);
            }

            node = next;
        }

        return node;
    }

    /// <summary>
    /// Clears the terminal marker for the specified key and prunes any nodes left dangling by the removal.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at terminal nodes.</typeparam>
    /// <param name="root">The root node to walk from.</param>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true" /> if a terminal key was removed; otherwise, <see langword="false" />.</returns>
    public static bool Remove<TValue>(TrieNode<TValue> root, ReadOnlySpan<char> key)
    {
        var path = key.Length == 0 ? Array.Empty<(TrieNode<TValue> Parent, char Ch)>() : new (TrieNode<TValue> Parent, char Ch)[key.Length];
        var node = root;
        for (var i = 0; i < key.Length; i++)
        {
            var ch = key[i];
            if (node.Children is null || !node.Children.TryGetValue(ch, out var next))
                return false;

            path[i] = (node, ch);
            node = next;
        }

        if (!node.IsTerminal)
            return false;

        node.IsTerminal = false;
        node.Key = null;
        node.Value = default!;

        // Prune leaf-ward: drop any node that is no longer terminal and has no children.
        for (var i = key.Length - 1; i >= 0; i--)
        {
            var (parent, ch) = path[i];
            var child = parent.Children![ch];
            if (child.IsTerminal || child.Children is { Count: > 0 })
                break;

            parent.Children.Remove(ch);
            if (parent.Children.Count == 0)
                parent.Children = null;
        }

        return true;
    }

    /// <summary>
    /// Enumerates the key/value pairs of every terminal node at or below the specified node, yielding each node's
    /// stored key verbatim.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at terminal nodes.</typeparam>
    /// <param name="node">The subtree root to enumerate.</param>
    /// <returns>A lazy sequence of key/value pairs in unspecified order.</returns>
    public static IEnumerable<KeyValuePair<string, TValue>> EnumerateItems<TValue>(TrieNode<TValue> node)
    {
        var stack = new Stack<TrieNode<TValue>>();
        stack.Push(node);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current.IsTerminal)
                yield return new KeyValuePair<string, TValue>(current.Key!, current.Value);

            if (current.Children is not null)
            {
                foreach (var child in current.Children)
                    stack.Push(child.Value);
            }
        }
    }
}
