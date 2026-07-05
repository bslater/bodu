// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSet{T}.Node.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class NavigableSet<T>
{
    /// <summary>
    /// Returns the size of the subtree rooted at <paramref name="node" />, treating <see langword="null" /> as zero.
    /// </summary>
    /// <param name="node">The subtree root, which may be <see langword="null" />.</param>
    /// <returns>The number of nodes in the subtree.</returns>
    private static int SizeOf(Node? node) =>
        node?.Size ?? 0;

    /// <summary>
    /// Returns whether <paramref name="node" /> is red, treating <see langword="null" /> leaves as black.
    /// </summary>
    /// <param name="node">The node to inspect, which may be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the node exists and is red; otherwise, <see langword="false" />.</returns>
    private static bool IsNodeRed(Node? node) =>
        node?.IsRed ?? false;

    /// <summary>
    /// Finds the node holding a comparer-equal element, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns>The matching node, or <see langword="null" />.</returns>
    private Node? FindNode(T item)
    {
        Node? current = _root;
        while (current != null)
        {
            int comparison = _comparer.Compare(item, current.Item);
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
    /// Returns the rightmost (maximum) node of the subtree rooted at <paramref name="node" />.
    /// </summary>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <returns>The maximum node of the subtree.</returns>
    private static Node MaximumNode(Node node)
    {
        while (node.Right != null)
            node = node.Right;

        return node;
    }

    /// <summary>
    /// Returns the in-order successor of <paramref name="node" /> via parent pointers, or <see langword="null" /> when
    /// the node is the maximum.
    /// </summary>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The next node in ascending order, or <see langword="null" />.</returns>
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
    /// Returns the in-order predecessor of <paramref name="node" /> via parent pointers, or <see langword="null" />
    /// when the node is the minimum.
    /// </summary>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The previous node in descending order, or <see langword="null" />.</returns>
    private static Node? PredecessorNode(Node node)
    {
        if (node.Left != null)
            return MaximumNode(node.Left);

        Node? parent = node.Parent;
        while (parent != null && node == parent.Left)
        {
            node = parent;
            parent = parent.Parent;
        }

        return parent;
    }

    /// <summary>
    /// Rotates the subtree rooted at <paramref name="pivot" /> to the left, transferring the subtree size to the
    /// promoted right child and recomputing the demoted node's size from its new children.
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

        // The promoted node now roots the same subtree, so it inherits the size; the demoted node is recomputed
        // from its (already exact) children.
        promoted.Size = pivot.Size;
        pivot.Size = SizeOf(pivot.Left) + SizeOf(pivot.Right) + 1;
    }

    /// <summary>
    /// Rotates the subtree rooted at <paramref name="pivot" /> to the right, transferring the subtree size to the
    /// promoted left child and recomputing the demoted node's size from its new children.
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

        promoted.Size = pivot.Size;
        pivot.Size = SizeOf(pivot.Left) + SizeOf(pivot.Right) + 1;
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
    /// Physically removes <paramref name="node" /> from the tree, maintaining subtree sizes and restoring the red-black
    /// invariants.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    /// <remarks>
    /// An interior node with two children is reduced to its in-order successor (the element is copied down), so the
    /// physically unlinked node always has at most one child. When that node is a childless black leaf it stays linked
    /// as a phantom during the fixup — its size of 1 keeps every rotation's size recomputation consistent — and is
    /// unlinked afterwards, at which point the ancestor sizes are decremented.
    /// </remarks>
    private void RemoveNode(Node node)
    {
        // Interior node: copy the successor's element into place and remove the successor instead.
        if (node.Left != null && node.Right != null)
        {
            Node successor = MinimumNode(node.Right);
            node.Item = successor.Item;
            node = successor;
        }

        Node? replacement = node.Left ?? node.Right;

        if (replacement != null)
        {
            // Splice the single child into the removed node's place, then repair the ancestor sizes before any
            // fixup rotation recomputes from them.
            replacement.Parent = node.Parent;
            if (node.Parent == null)
                _root = replacement;
            else if (node == node.Parent.Left)
                node.Parent.Left = replacement;
            else
                node.Parent.Right = replacement;

            for (Node? ancestor = replacement.Parent; ancestor != null; ancestor = ancestor.Parent)
                ancestor.Size--;

            node.Left = node.Right = node.Parent = null;

            if (!node.IsRed)
                FixAfterDeletion(replacement);
        }
        else if (node.Parent == null)
        {
            _root = null;
        }
        else
        {
            // Childless leaf: fix up with the node still linked as a phantom, then unlink it and repair sizes.
            if (!node.IsRed)
                FixAfterDeletion(node);

            if (node.Parent != null)
            {
                for (Node? ancestor = node.Parent; ancestor != null; ancestor = ancestor.Parent)
                    ancestor.Size--;

                if (node == node.Parent.Left)
                    node.Parent.Left = null;
                else if (node == node.Parent.Right)
                    node.Parent.Right = null;

                node.Parent = null;
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
    /// Builds a balanced red-black subtree from the sorted, deduplicated slice of <paramref name="items" /> bounded by
    /// <paramref name="startIndex" /> and <paramref name="endIndex" />, coloring only the spill-over level red (the
    /// <see cref="SortedSet{T}" /> construction scheme).
    /// </summary>
    /// <param name="items">The sorted, deduplicated source elements.</param>
    /// <param name="startIndex">The inclusive start index of the slice.</param>
    /// <param name="endIndex">The inclusive end index of the slice.</param>
    /// <param name="redNode">
    /// A pre-colored red leaf to hang at the leftmost position, or <see langword="null" />.
    /// </param>
    /// <returns>The subtree root, or <see langword="null" /> for an empty slice.</returns>
    private static Node? BuildFromSortedArray(T[] items, int startIndex, int endIndex, Node? redNode)
    {
        int size = endIndex - startIndex + 1;
        if (size <= 0)
            return redNode;

        Node root;
        switch (size)
        {
            case 1:
                root = new Node(items[startIndex]);
                Link(root, redNode, asLeft: true);
                break;

            case 2:
                root = new Node(items[startIndex]);
                Link(root, new Node(items[endIndex]) { IsRed = true }, asLeft: false);
                Link(root, redNode, asLeft: true);
                break;

            case 3:
                root = new Node(items[startIndex + 1]);
                Link(root, new Node(items[startIndex]), asLeft: true);
                Link(root, new Node(items[endIndex]), asLeft: false);
                Link(root.Left!, redNode, asLeft: true);
                root.Left!.Size = SizeOf(root.Left.Left) + 1;
                break;

            default:
                int midpoint = (startIndex + endIndex) / 2;
                root = new Node(items[midpoint]);
                Link(root, BuildFromSortedArray(items, startIndex, midpoint - 1, redNode), asLeft: true);

                Node? rightSubtree = size % 2 == 0
                    ? BuildFromSortedArray(items, midpoint + 2, endIndex, new Node(items[midpoint + 1]) { IsRed = true })
                    : BuildFromSortedArray(items, midpoint + 1, endIndex, null);
                Link(root, rightSubtree, asLeft: false);
                break;
        }

        root.Size = SizeOf(root.Left) + SizeOf(root.Right) + 1;
        return root;
    }

    /// <summary>
    /// Links <paramref name="child" /> under <paramref name="parent" /> on the requested side when the child is not
    /// <see langword="null" />.
    /// </summary>
    /// <param name="parent">The parent node.</param>
    /// <param name="child">The child subtree root, which may be <see langword="null" />.</param>
    /// <param name="asLeft"><see langword="true" /> to link as the left child; otherwise the right child.</param>
    private static void Link(Node parent, Node? child, bool asLeft)
    {
        if (child == null)
            return;

        child.Parent = parent;
        if (asLeft)
            parent.Left = child;
        else
            parent.Right = child;
    }

    /// <summary>
    /// Represents a node of the order-statistic red-black tree backing <see cref="NavigableSet{T}" />.
    /// </summary>
    [Serializable]
    private sealed class Node
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class holding <paramref name="item" />.
        /// </summary>
        /// <param name="item">The element to store.</param>
        internal Node(T item)
        {
            Item = item;
        }

        /// <summary>
        /// Gets or sets the element stored at this node.
        /// </summary>
        /// <value>The stored element.</value>
        internal T Item { get; set; }

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

        /// <summary>
        /// Gets or sets the number of nodes in the subtree rooted at this node, including itself.
        /// </summary>
        /// <value>The subtree size.</value>
        internal int Size { get; set; } = 1;
    }
}
