// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTree{T,T}.Node.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class IntervalTree<TKey, TValue>
{
    /// <summary>
    /// Returns whether <paramref name="node" /> is red, treating <see langword="null" /> leaves as black.
    /// </summary>
    /// <param name="node">The node to inspect, which may be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the node exists and is red; otherwise, <see langword="false" />.</returns>
    private static bool IsNodeRed(Node? node) =>
        node?.IsRed ?? false;

    /// <summary>
    /// Compares the (low, high) lexicographic ordering key against the interval stored at <paramref name="node" />.
    /// </summary>
    /// <param name="low">The lower endpoint of the probe interval.</param>
    /// <param name="high">The upper endpoint of the probe interval.</param>
    /// <param name="node">The node to compare against.</param>
    /// <returns>A negative, zero, or positive value per the standard comparison contract.</returns>
    private int CompareToNode(TKey low, TKey high, Node node)
    {
        int comparison = _comparer.Compare(low, node.Low);
        return comparison != 0 ? comparison : _comparer.Compare(high, node.High);
    }

    /// <summary>
    /// Recomputes the <see cref="Node.Max" /> field of <paramref name="node" /> from its own high endpoint and its
    /// children's (assumed exact) <see cref="Node.Max" /> values.
    /// </summary>
    /// <param name="node">The node to recompute.</param>
    private void RecomputeMax(Node node)
    {
        TKey max = node.High;
        if (node.Left != null && _comparer.Compare(node.Left.Max, max) > 0)
            max = node.Left.Max;
        if (node.Right != null && _comparer.Compare(node.Right.Max, max) > 0)
            max = node.Right.Max;

        node.Max = max;
    }

    /// <summary>
    /// Recomputes <see cref="Node.Max" /> bottom-up from <paramref name="node" /> to the root, restoring exactness
    /// after a deletion whose fixup rotations may have propagated stale values along the ancestor chain.
    /// </summary>
    /// <param name="node">The node to start from, which may be <see langword="null" />.</param>
    private void RecomputeMaxUpward(Node? node)
    {
        for (Node? current = node; current != null; current = current.Parent)
            RecomputeMax(current);
    }

    /// <summary>
    /// Finds the node storing the exact (low, high) interval, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="low">The lower endpoint to locate.</param>
    /// <param name="high">The upper endpoint to locate.</param>
    /// <returns>The matching node, or <see langword="null" />.</returns>
    private Node? FindNode(TKey low, TKey high)
    {
        Node? current = _root;
        while (current != null)
        {
            int comparison = CompareToNode(low, high, current);
            if (comparison == 0)
                return current;

            current = comparison < 0 ? current.Left : current.Right;
        }

        return null;
    }

    /// <summary>
    /// Returns the leftmost (minimum) node of the subtree rooted at <paramref name="node" />.
    /// </summary>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <returns>The minimum node of the subtree.</returns>
    private static Node MinimumNode(Node node)
    {
        while (node.Left != null)
            node = node.Left;

        return node;
    }

    /// <summary>
    /// Returns the in-order successor of <paramref name="node" /> via parent pointers, or <see langword="null" /> when
    /// the node is the maximum.
    /// </summary>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The next node in ascending (low, high) order, or <see langword="null" />.</returns>
    private static Node? SuccessorNode(Node node)
    {
        if (node.Right != null)
            return MinimumNode(node.Right);

        Node? parent = node.Parent;
        while (parent != null && node == parent.Right)
        {
            node = parent;
            parent = parent.Parent;
        }

        return parent;
    }

    /// <summary>
    /// Rotates the subtree rooted at <paramref name="pivot" /> to the left, recomputing the demoted node's
    /// <see cref="Node.Max" /> from its new children and then the promoted node's from the fresh result.
    /// </summary>
    /// <param name="pivot">The subtree root to rotate. Must have a right child.</param>
    private void RotateLeft(Node pivot)
    {
        Node promoted = pivot.Right!;

        pivot.Right = promoted.Left;
        if (promoted.Left != null)
            promoted.Left.Parent = pivot;

        promoted.Parent = pivot.Parent;
        if (pivot.Parent == null)
            _root = promoted;
        else if (pivot == pivot.Parent.Left)
            pivot.Parent.Left = promoted;
        else
            pivot.Parent.Right = promoted;

        promoted.Left = pivot;
        pivot.Parent = promoted;

        // Recompute the demoted node first — its children are final — then the promoted node, which reads the
        // demoted node's fresh value. With exact children, the rotation preserves Max exactness.
        RecomputeMax(pivot);
        RecomputeMax(promoted);
    }

    /// <summary>
    /// Rotates the subtree rooted at <paramref name="pivot" /> to the right, recomputing the demoted node's
    /// <see cref="Node.Max" /> from its new children and then the promoted node's from the fresh result.
    /// </summary>
    /// <param name="pivot">The subtree root to rotate. Must have a left child.</param>
    private void RotateRight(Node pivot)
    {
        Node promoted = pivot.Left!;

        pivot.Left = promoted.Right;
        if (promoted.Right != null)
            promoted.Right.Parent = pivot;

        promoted.Parent = pivot.Parent;
        if (pivot.Parent == null)
            _root = promoted;
        else if (pivot == pivot.Parent.Right)
            pivot.Parent.Right = promoted;
        else
            pivot.Parent.Left = promoted;

        promoted.Right = pivot;
        pivot.Parent = promoted;

        RecomputeMax(pivot);
        RecomputeMax(promoted);
    }

    /// <summary>
    /// Restores the red-black invariants after inserting <paramref name="inserted" /> (classic bottom-up fixup).
    /// </summary>
    /// <param name="inserted">The newly linked node.</param>
    private void FixAfterInsertion(Node inserted)
    {
        inserted.IsRed = true;

        Node node = inserted;
        while (node.Parent is { IsRed: true } parent)
        {
            // A red parent is never the root, so the grandparent always exists.
            Node grandparent = parent.Parent!;

            if (parent == grandparent.Left)
            {
                Node? uncle = grandparent.Right;
                if (uncle != null && uncle.IsRed)
                {
                    parent.IsRed = false;
                    uncle.IsRed = false;
                    grandparent.IsRed = true;
                    node = grandparent;
                }
                else
                {
                    if (node == parent.Right)
                    {
                        node = parent;
                        RotateLeft(node);
                        parent = node.Parent!;
                        grandparent = parent.Parent!;
                    }

                    parent.IsRed = false;
                    grandparent.IsRed = true;
                    RotateRight(grandparent);
                }
            }
            else
            {
                Node? uncle = grandparent.Left;
                if (uncle != null && uncle.IsRed)
                {
                    parent.IsRed = false;
                    uncle.IsRed = false;
                    grandparent.IsRed = true;
                    node = grandparent;
                }
                else
                {
                    if (node == parent.Left)
                    {
                        node = parent;
                        RotateRight(node);
                        parent = node.Parent!;
                        grandparent = parent.Parent!;
                    }

                    parent.IsRed = false;
                    grandparent.IsRed = true;
                    RotateLeft(grandparent);
                }
            }
        }

        _root!.IsRed = false;
    }

    /// <summary>
    /// Physically removes <paramref name="node" /> from the tree, restoring the red-black invariants and the
    /// <see cref="Node.Max" /> augmentation.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    /// <remarks>
    /// An interior node with two children is reduced to its in-order successor (the payload is copied down), so the
    /// physically unlinked node always has at most one child. When that node is a childless black leaf it stays linked
    /// as a phantom during the fixup and is unlinked afterwards. Every case finishes with a bottom-up
    /// <see cref="RecomputeMaxUpward" /> from the splice point: any stale <see cref="Node.Max" /> produced by the
    /// payload copy or mid-fixup rotations lives on that ancestor chain, so the single walk restores exactness.
    /// </remarks>
    private void RemoveNode(Node node)
    {
        // Interior node: copy the successor's payload into place and remove the successor instead.
        if (node.Left != null && node.Right != null)
        {
            Node successor = MinimumNode(node.Right);
            node.Low = successor.Low;
            node.High = successor.High;
            node.FirstValue = successor.FirstValue;
            node.OverflowValues = successor.OverflowValues;
            node = successor;
        }

        Node? replacement = node.Left ?? node.Right;

        if (replacement != null)
        {
            // Splice the single child into the removed node's place.
            replacement.Parent = node.Parent;
            if (node.Parent == null)
                _root = replacement;
            else if (node == node.Parent.Left)
                node.Parent.Left = replacement;
            else
                node.Parent.Right = replacement;

            node.Left = node.Right = node.Parent = null;

            if (!node.IsRed)
                FixAfterDeletion(replacement);

            RecomputeMaxUpward(replacement);
        }
        else if (node.Parent == null)
        {
            _root = null;
        }
        else
        {
            // Childless leaf: fix up with the node still linked as a phantom, then unlink it and repair Max.
            if (!node.IsRed)
                FixAfterDeletion(node);

            if (node.Parent != null)
            {
                Node anchor = node.Parent;

                if (node == node.Parent.Left)
                    node.Parent.Left = null;
                else if (node == node.Parent.Right)
                    node.Parent.Right = null;

                node.Parent = null;
                RecomputeMaxUpward(anchor);
            }
        }
    }

    /// <summary>
    /// Restores the red-black invariants after removing a black node, starting the double-black resolution at
    /// <paramref name="node" /> (classic bottom-up fixup).
    /// </summary>
    /// <param name="node">The replacement (or phantom) node carrying the extra black.</param>
    private void FixAfterDeletion(Node node)
    {
        while (node != _root && !node.IsRed)
        {
            // A double-black node below the root always has a parent and a non-null sibling.
            Node parent = node.Parent!;

            if (node == parent.Left)
            {
                Node sibling = parent.Right!;

                if (sibling.IsRed)
                {
                    sibling.IsRed = false;
                    parent.IsRed = true;
                    RotateLeft(parent);
                    sibling = parent.Right!;
                }

                if (!IsNodeRed(sibling.Left) && !IsNodeRed(sibling.Right))
                {
                    sibling.IsRed = true;
                    node = parent;
                }
                else
                {
                    if (!IsNodeRed(sibling.Right))
                    {
                        // The near child is red here, or the previous branch would have run.
                        sibling.Left!.IsRed = false;
                        sibling.IsRed = true;
                        RotateRight(sibling);
                        sibling = parent.Right!;
                    }

                    sibling.IsRed = parent.IsRed;
                    parent.IsRed = false;
                    sibling.Right!.IsRed = false;
                    RotateLeft(parent);
                    node = _root!;
                }
            }
            else
            {
                Node sibling = parent.Left!;

                if (sibling.IsRed)
                {
                    sibling.IsRed = false;
                    parent.IsRed = true;
                    RotateRight(parent);
                    sibling = parent.Left!;
                }

                if (!IsNodeRed(sibling.Right) && !IsNodeRed(sibling.Left))
                {
                    sibling.IsRed = true;
                    node = parent;
                }
                else
                {
                    if (!IsNodeRed(sibling.Left))
                    {
                        sibling.Right!.IsRed = false;
                        sibling.IsRed = true;
                        RotateLeft(sibling);
                        sibling = parent.Left!;
                    }

                    sibling.IsRed = parent.IsRed;
                    parent.IsRed = false;
                    sibling.Left!.IsRed = false;
                    RotateRight(parent);
                    node = _root!;
                }
            }
        }

        node.IsRed = false;
    }

    /// <summary>
    /// Represents a node of the max-endpoint augmented red-black tree backing <see cref="IntervalTree{TKey, TValue}" />.
    /// </summary>
    [Serializable]
    private sealed class Node
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class storing the interval [<paramref name="low" />,
        /// <paramref name="high" />] carrying <paramref name="value" />.
        /// </summary>
        /// <param name="low">The inclusive lower endpoint.</param>
        /// <param name="high">The inclusive upper endpoint.</param>
        /// <param name="value">The first value carried by the interval.</param>
        internal Node(TKey low, TKey high, TValue value)
        {
            Low = low;
            High = high;
            Max = high;
            FirstValue = value;
        }

        /// <summary>
        /// Gets or sets the inclusive lower endpoint of the stored interval.
        /// </summary>
        /// <value>The lower endpoint.</value>
        internal TKey Low { get; set; }

        /// <summary>
        /// Gets or sets the inclusive upper endpoint of the stored interval.
        /// </summary>
        /// <value>The upper endpoint.</value>
        internal TKey High { get; set; }

        /// <summary>
        /// Gets or sets the greatest upper endpoint in the subtree rooted at this node, including itself — the
        /// augmentation that prunes query descents.
        /// </summary>
        /// <value>The subtree's maximum upper endpoint.</value>
        internal TKey Max { get; set; }

        /// <summary>
        /// Gets or sets the oldest value carried by this exact interval, stored inline so the dominant
        /// single-value-per-interval case allocates no list.
        /// </summary>
        /// <value>The first (oldest) stored value.</value>
        internal TValue FirstValue { get; set; }

        /// <summary>
        /// Gets or sets the values carried beyond the first, in insertion order, or <see langword="null" /> until a
        /// second value is added for the same interval.
        /// </summary>
        /// <value>The lazily allocated overflow list.</value>
        internal List<TValue>? OverflowValues { get; set; }

        /// <summary>
        /// Gets the total number of values carried by this interval; always at least one.
        /// </summary>
        /// <value>The per-interval entry count.</value>
        internal int ValueCount => 1 + (OverflowValues?.Count ?? 0);

        /// <summary>
        /// Returns the value at the specified position in insertion order across the inline slot and the overflow list.
        /// </summary>
        /// <param name="index">The zero-based entry index; must be less than <see cref="ValueCount" />.</param>
        /// <returns>The value at <paramref name="index" />.</returns>
        internal TValue GetValueAt(int index) =>
            index == 0 ? FirstValue : OverflowValues![index - 1];

        /// <summary>
        /// Returns the insertion-order index of the first entry equal to <paramref name="value" /> under
        /// <see cref="EqualityComparer{TValue}.Default" />, or <c>-1</c> when no entry matches.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based entry index, or <c>-1</c>.</returns>
        internal int IndexOfValue(TValue value)
        {
            if (EqualityComparer<TValue>.Default.Equals(FirstValue, value))
                return 0;

            if (OverflowValues != null)
            {
                int overflowIndex = OverflowValues.IndexOf(value);
                if (overflowIndex >= 0)
                    return overflowIndex + 1;
            }

            return -1;
        }

        /// <summary>
        /// Gets or sets the left child, or <see langword="null" />.
        /// </summary>
        /// <value>The left child node.</value>
        internal Node? Left { get; set; }

        /// <summary>
        /// Gets or sets the right child, or <see langword="null" />.
        /// </summary>
        /// <value>The right child node.</value>
        internal Node? Right { get; set; }

        /// <summary>
        /// Gets or sets the parent node, or <see langword="null" /> for the root.
        /// </summary>
        /// <value>The parent node.</value>
        internal Node? Parent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the node is red; new nodes are black until the insertion fixup
        /// recolors them.
        /// </summary>
        /// <value><see langword="true" /> when the node is red; otherwise, <see langword="false" />.</value>
        internal bool IsRed { get; set; }
    }
}
