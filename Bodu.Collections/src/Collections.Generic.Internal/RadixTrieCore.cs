// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Represents a single node in a path-compressed (radix) prefix tree, holding the string label of the edge leading to
/// it, its child transitions, terminal flag, and associated value.
/// </summary>
/// <typeparam name="TValue">The type of value stored at terminal nodes.</typeparam>
internal sealed class RadixTrieNode<TValue>
{
    /// <summary>
    /// Gets or sets the child transitions keyed by the first character of each child's edge label, or
    /// <see langword="null" /> when the node is a leaf.
    /// </summary>
    public Dictionary<char, RadixTrieNode<TValue>>? Children { get; set; }

    /// <summary>
    /// Gets or sets the label of the edge from the parent to this node; the empty string on the root.
    /// </summary>
    public string Label { get; set; } = string.Empty;

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
/// Provides the shared edge-walk, split-on-insert, merge-on-remove, and enumeration logic for the path-compressed
/// prefix-tree types, operating over a generic <see cref="RadixTrieNode{TValue}" /> so the set and map radix tries
/// reuse a single implementation.
/// </summary>
/// <remarks>
/// Structural invariant: no non-root, non-terminal node has exactly one child — insertion splits edges only at genuine
/// divergence points and removal re-fuses any pass-through node it leaves behind, so node count stays proportional to
/// the number of stored keys.
/// </remarks>
internal static class RadixTrieCore
{
    /// <summary>
    /// Walks the radix trie from the specified root following the characters of <paramref name="key" /> across whole
    /// edge labels.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at terminal nodes.</typeparam>
    /// <param name="root">The root node to walk from.</param>
    /// <param name="key">The character sequence to follow.</param>
    /// <param name="comparer">The comparer used to match characters.</param>
    /// <returns>
    /// The node at which the key ends exactly on a node boundary, or <see langword="null" /> if the path does not exist
    /// or the key ends inside an edge label.
    /// </returns>
    public static RadixTrieNode<TValue>? Find<TValue>(RadixTrieNode<TValue> root, ReadOnlySpan<char> key, IEqualityComparer<char> comparer)
    {
        RadixTrieNode<TValue> node = root;
        int i = 0;
        while (i < key.Length)
        {
            if (node.Children is null || !node.Children.TryGetValue(key[i], out RadixTrieNode<TValue>? child))
                return null;

            string label = child.Label;
            if (key.Length - i < label.Length)
                return null;

            for (int j = 1; j < label.Length; j++)
            {
                if (!comparer.Equals(label[j], key[i + j]))
                    return null;
            }

            i += label.Length;
            node = child;
        }

        return node;
    }

    /// <summary>
    /// Locates the node whose subtree contains exactly the keys beginning with <paramref name="prefix" />, allowing the
    /// prefix to end partway along an edge label.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at terminal nodes.</typeparam>
    /// <param name="root">The root node to walk from.</param>
    /// <param name="prefix">The prefix to follow.</param>
    /// <param name="comparer">The comparer used to match characters.</param>
    /// <returns>The subtree root for the prefix, or <see langword="null" /> if no stored key begins with it.</returns>
    public static RadixTrieNode<TValue>? FindSubtree<TValue>(RadixTrieNode<TValue> root, ReadOnlySpan<char> prefix, IEqualityComparer<char> comparer)
    {
        RadixTrieNode<TValue> node = root;
        int i = 0;
        while (i < prefix.Length)
        {
            if (node.Children is null || !node.Children.TryGetValue(prefix[i], out RadixTrieNode<TValue>? child))
                return null;

            string label = child.Label;
            int max = Math.Min(label.Length, prefix.Length - i);
            for (int j = 1; j < max; j++)
            {
                if (!comparer.Equals(label[j], prefix[i + j]))
                    return null;
            }

            // When the prefix is exhausted inside the edge label, every key below the child still starts with it.
            i += max;
            node = child;
        }

        return node;
    }

    /// <summary>
    /// Walks the radix trie from the specified root, creating missing nodes and splitting edges at divergence points so
    /// that <paramref name="key" /> ends exactly on a node boundary.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at terminal nodes.</typeparam>
    /// <param name="root">The root node to walk from.</param>
    /// <param name="key">The character sequence to follow.</param>
    /// <param name="comparer">The comparer used to key child transitions and match label characters.</param>
    /// <returns>The node at which the key terminates.</returns>
    public static RadixTrieNode<TValue> GetOrAddNode<TValue>(RadixTrieNode<TValue> root, ReadOnlySpan<char> key, IEqualityComparer<char> comparer)
    {
        RadixTrieNode<TValue> node = root;
        int i = 0;
        while (i < key.Length)
        {
            node.Children ??= new Dictionary<char, RadixTrieNode<TValue>>(comparer);
            if (!node.Children.TryGetValue(key[i], out RadixTrieNode<TValue>? child))
            {
                var leaf = new RadixTrieNode<TValue> { Label = new string(key[i..]) };
                node.Children.Add(key[i], leaf);
                return leaf;
            }

            string label = child.Label;
            int max = Math.Min(label.Length, key.Length - i);

            // The first character matched through the dictionary; find how far the label and key agree.
            int shared = 1;
            while (shared < max && comparer.Equals(label[shared], key[i + shared]))
                shared++;

            if (shared == label.Length)
            {
                i += shared;
                node = child;
                continue;
            }

            // Divergence inside the edge: split it — an intermediate node takes the shared label prefix and the
            // existing child keeps the remainder.
            var intermediate = new RadixTrieNode<TValue> { Label = label[..shared] };
            child.Label = label[shared..];
            intermediate.Children = new Dictionary<char, RadixTrieNode<TValue>>(comparer)
            {
                [child.Label[0]] = child,
            };
            node.Children[key[i]] = intermediate;

            i += shared;
            if (i == key.Length)
                return intermediate;

            var tail = new RadixTrieNode<TValue> { Label = new string(key[i..]) };
            intermediate.Children.Add(key[i], tail);
            return tail;
        }

        return node;
    }

    /// <summary>
    /// Clears the terminal marker for the specified key, prunes any node left dangling, and re-fuses any pass-through
    /// node the removal creates.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at terminal nodes.</typeparam>
    /// <param name="root">The root node to walk from.</param>
    /// <param name="key">The key to remove.</param>
    /// <param name="comparer">The comparer used to match characters.</param>
    /// <returns><see langword="true" /> if a terminal key was removed; otherwise, <see langword="false" />.</returns>
    public static bool Remove<TValue>(RadixTrieNode<TValue> root, ReadOnlySpan<char> key, IEqualityComparer<char> comparer)
    {
        RadixTrieNode<TValue>? parent = null;
        char edgeKey = default;
        RadixTrieNode<TValue>? grandparent = null;

        RadixTrieNode<TValue> node = root;
        int i = 0;
        while (i < key.Length)
        {
            if (node.Children is null || !node.Children.TryGetValue(key[i], out RadixTrieNode<TValue>? child))
                return false;

            string label = child.Label;
            if (key.Length - i < label.Length)
                return false;

            for (int j = 1; j < label.Length; j++)
            {
                if (!comparer.Equals(label[j], key[i + j]))
                    return false;
            }

            grandparent = parent;
            parent = node;
            edgeKey = key[i];
            i += label.Length;
            node = child;
        }

        if (!node.IsTerminal)
            return false;

        node.IsTerminal = false;
        node.Key = null;
        node.Value = default!;

        if (parent is null)
            return true; // The empty key terminates at the root; no structure to prune.

        if (node.Children is null || node.Children.Count == 0)
        {
            // The node became a dangling leaf: detach it, then re-fuse the parent if the detach left it a
            // non-root, non-terminal pass-through.
            parent.Children!.Remove(edgeKey);
            if (parent.Children.Count == 0)
                parent.Children = null;
            else if (grandparent is not null)
                MergeIfPassThrough(parent);
        }
        else
        {
            // The node keeps its subtree; it may itself have become a pass-through.
            MergeIfPassThrough(node);
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
    public static IEnumerable<KeyValuePair<string, TValue>> EnumerateItems<TValue>(RadixTrieNode<TValue> node)
    {
        var stack = new Stack<RadixTrieNode<TValue>>();
        stack.Push(node);
        while (stack.Count > 0)
        {
            RadixTrieNode<TValue> current = stack.Pop();
            if (current.IsTerminal)
                yield return new KeyValuePair<string, TValue>(current.Key!, current.Value);

            if (current.Children is not null)
            {
                foreach (KeyValuePair<char, RadixTrieNode<TValue>> child in current.Children)
                    stack.Push(child.Value);
            }
        }
    }

    /// <summary>
    /// Fuses the specified node with its single child when the node is a non-terminal pass-through, restoring the
    /// compression invariant.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at terminal nodes.</typeparam>
    /// <param name="node">The candidate node; must not be the root.</param>
    private static void MergeIfPassThrough<TValue>(RadixTrieNode<TValue> node)
    {
        if (node.IsTerminal || node.Children is null || node.Children.Count != 1)
            return;

        RadixTrieNode<TValue>? only = null;
        foreach (RadixTrieNode<TValue> child in node.Children.Values)
            only = child;

        node.Label += only!.Label;
        node.IsTerminal = only.IsTerminal;
        node.Key = only.Key;
        node.Value = only.Value;
        node.Children = only.Children;
    }
}
