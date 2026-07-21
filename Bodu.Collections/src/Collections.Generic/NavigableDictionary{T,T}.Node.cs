// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionary{T,T}.Node.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic;

public sealed partial class NavigableDictionary<TKey, TValue>
{
    /// <summary>
    /// Returns the size of the subtree rooted at <paramref name="node" />, treating <see langword="null" /> as zero.
    /// </summary>
    /// <param name="node">The subtree root, which may be <see langword="null" />.</param>
    /// <returns>The number of nodes in the subtree.</returns>
    private static int SizeOf(Node? node) =>
        OrderStatisticTree.SizeOf(node);

    /// <summary>
    /// Finds the node holding a comparer-equal key, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>The matching node, or <see langword="null" />.</returns>
    private Node? FindNode(TKey key)
    {
        Node? current = _root;
        while (current != null)
        {
            int comparison = _comparer.Compare(key, current.Key);
            if (comparison == 0)
                return current;

            current = comparison < 0 ? current.Left : current.Right;
        }

        return null;
    }

    /// <summary>
    /// Returns the leftmost (minimum-key) node of the subtree rooted at <paramref name="node" />.
    /// </summary>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <returns>The minimum node of the subtree.</returns>
    private static Node MinimumNode(Node node) =>
        BinaryTreeWalk.Minimum(node);

    /// <summary>
    /// Returns the rightmost (maximum-key) node of the subtree rooted at <paramref name="node" />.
    /// </summary>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <returns>The maximum node of the subtree.</returns>
    private static Node MaximumNode(Node node) =>
        BinaryTreeWalk.Maximum(node);

    /// <summary>
    /// Returns the in-order successor of <paramref name="node" /> via parent pointers, or <see langword="null" /> when
    /// the node holds the maximum key.
    /// </summary>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The next node in ascending key order, or <see langword="null" />.</returns>
    private static Node? SuccessorNode(Node node) =>
        BinaryTreeWalk.Successor(node);

    /// <summary>
    /// Returns the in-order predecessor of <paramref name="node" /> via parent pointers, or <see langword="null" />
    /// when the node holds the minimum key.
    /// </summary>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The previous node in descending key order, or <see langword="null" />.</returns>
    private static Node? PredecessorNode(Node node) =>
        BinaryTreeWalk.Predecessor(node);

    /// <summary>
    /// Restores the red-black invariants after inserting <paramref name="inserted" /> via the shared
    /// <see cref="OrderStatisticTree" /> fixup.
    /// </summary>
    /// <param name="inserted">The newly linked node.</param>
    private void FixAfterInsertion(Node inserted) =>
        OrderStatisticTree.FixAfterInsertion(ref _root, inserted);

    /// <summary>
    /// Physically removes <paramref name="node" /> from the tree via the shared <see cref="OrderStatisticTree" />
    /// removal, maintaining subtree sizes and restoring the red-black invariants.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    private void RemoveNode(Node node) =>
        OrderStatisticTree.Remove(ref _root, node);

    /// <summary>
    /// Builds a balanced red-black tree from the first <paramref name="count" /> key-sorted, duplicate-free entries of
    /// <paramref name="entries" /> via the shared <see cref="OrderStatisticTree" /> construction (the
    /// <see cref="SortedSet{T}" /> scheme).
    /// </summary>
    /// <param name="entries">The key-sorted, duplicate-free source entries.</param>
    /// <param name="count">The number of leading entries to build from. Must be greater than zero.</param>
    /// <returns>The tree root.</returns>
    private static Node? BuildFromSortedArray(KeyValuePair<TKey, TValue>[] entries, int count)
    {
        var nodes = new Node[count];
        for (int i = 0; i < count; i++)
            nodes[i] = new Node(entries[i].Key, entries[i].Value);

        return OrderStatisticTree.BuildFromSorted(nodes, 0, count - 1, null);
    }

    /// <summary>
    /// Represents a node of the order-statistic red-black tree backing <see cref="NavigableDictionary{TKey, TValue}" />;
    /// the structural links, color bit, and subtree size live on the shared <see cref="OrderStatisticNode{TNode}" />
    /// base.
    /// </summary>
    /// <remarks>
    /// This is the key-value adaptation of the <see cref="NavigableSet{T}" /> node: the single element field becomes a
    /// key/value pair of fields, with both trees sharing the <see cref="OrderStatisticTree" /> rotation/fixup core.
    /// </remarks>
    [Serializable]
    private sealed class Node
        : OrderStatisticNode<Node>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class holding the specified entry.
        /// </summary>
        /// <param name="key">The key to store.</param>
        /// <param name="value">The value to store.</param>
        internal Node(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the key stored at this node; mutated only by the successor copy-down during removal.
        /// </summary>
        /// <value>The stored key.</value>
        internal TKey Key { get; set; }

        /// <summary>
        /// Gets or sets the value stored at this node.
        /// </summary>
        /// <value>The stored value.</value>
        internal TValue Value { get; set; }

        /// <summary>
        /// Gets the node's key and value as a <see cref="KeyValuePair{TKey, TValue}" />.
        /// </summary>
        /// <value>The stored entry.</value>
        internal KeyValuePair<TKey, TValue> Entry => new(Key, Value);

        /// <summary>
        /// Copies the stored key and value of <paramref name="source" /> into this node (the successor copy-down during
        /// removal).
        /// </summary>
        /// <param name="source">The node whose entry is copied down. Must not be <see langword="null" />.</param>
        internal override void CopyPayloadFrom(Node source)
        {
            Key = source.Key;
            Value = source.Value;
        }
    }
}
