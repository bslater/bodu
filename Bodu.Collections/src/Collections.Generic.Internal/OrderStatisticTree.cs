// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderStatisticTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Provides the order-statistic red-black tree machinery shared by <see cref="NavigableSet{T}" /> and
/// <see cref="NavigableDictionary{TKey, TValue}" />: rotations with subtree-size maintenance, the insertion and
/// deletion fixups, physical node removal, and balanced construction from a sorted node array.
/// </summary>
/// <remarks>
/// <para>
/// All methods are static generic over the concrete <see cref="OrderStatisticNode{TNode}" /> subtype, so link and size
/// maintenance compiles with no interface dispatch or delegate indirection; the only virtual call is the payload
/// copy-down in <see cref="Remove{TNode}" />, which runs at most once per removal.
/// </para>
/// <para>
/// Ordering-dependent operations (locating a node, choosing an insertion point) stay in the consumer, which owns the
/// comparer and the payload; this class handles only the structural protocol after the consumer has linked or chosen a
/// node. The tree root is passed by reference so rotations can republish it.
/// </para>
/// </remarks>
internal static class OrderStatisticTree
{
    /// <summary>
    /// Returns the size of the subtree rooted at <paramref name="node" />, treating <see langword="null" /> as zero.
    /// </summary>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The subtree root, which may be <see langword="null" />.</param>
    /// <returns>The number of nodes in the subtree.</returns>
    internal static int SizeOf<TNode>(TNode? node)
        where TNode : OrderStatisticNode<TNode> =>
        node?.Size ?? 0;

    /// <summary>
    /// Rotates the subtree rooted at <paramref name="pivot" /> to the left, transferring the subtree size to the
    /// promoted right child and recomputing the demoted node's size from its new children.
    /// </summary>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link, republished when the pivot was the root.</param>
    /// <param name="pivot">The subtree root to rotate. Must have a right child.</param>
    private static void RotateLeft<TNode>(ref TNode? root, TNode pivot)
        where TNode : OrderStatisticNode<TNode>
    {
        TNode promoted = pivot.Right!;

        pivot.Right = promoted.Left;
        if (promoted.Left != null)
            promoted.Left.Parent = pivot;

        promoted.Parent = pivot.Parent;
        if (pivot.Parent == null)
            root = promoted;
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
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link, republished when the pivot was the root.</param>
    /// <param name="pivot">The subtree root to rotate. Must have a left child.</param>
    private static void RotateRight<TNode>(ref TNode? root, TNode pivot)
        where TNode : OrderStatisticNode<TNode>
    {
        TNode promoted = pivot.Left!;

        pivot.Left = promoted.Right;
        if (promoted.Right != null)
            promoted.Right.Parent = pivot;

        promoted.Parent = pivot.Parent;
        if (pivot.Parent == null)
            root = promoted;
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
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link.</param>
    /// <param name="inserted">The newly linked node.</param>
    internal static void FixAfterInsertion<TNode>(ref TNode? root, TNode inserted)
        where TNode : OrderStatisticNode<TNode>
    {
        inserted.IsRed = true;

        TNode node = inserted;
        while (node.Parent is { IsRed: true } parent)
        {
            // A red parent is never the root, so the grandparent always exists.
            TNode grandparent = parent.Parent!;

            if (parent == grandparent.Left)
            {
                TNode? uncle = grandparent.Right;
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
                        RotateLeft(ref root, node);
                        parent = node.Parent!;
                        grandparent = parent.Parent!;
                    }

                    parent.IsRed = false;
                    grandparent.IsRed = true;
                    RotateRight(ref root, grandparent);
                }
            }
            else
            {
                TNode? uncle = grandparent.Left;
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
                        RotateRight(ref root, node);
                        parent = node.Parent!;
                        grandparent = parent.Parent!;
                    }

                    parent.IsRed = false;
                    grandparent.IsRed = true;
                    RotateLeft(ref root, grandparent);
                }
            }
        }

        root!.IsRed = false;
    }

    /// <summary>
    /// Physically removes <paramref name="node" /> from the tree, maintaining subtree sizes and restoring the red-black
    /// invariants.
    /// </summary>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link.</param>
    /// <param name="node">The node to remove.</param>
    /// <remarks>
    /// An interior node with two children is reduced to its in-order successor (the payload is copied down via
    /// <see cref="OrderStatisticNode{TNode}.CopyPayloadFrom" />), so the physically unlinked node always has at most
    /// one child. When that node is a childless black leaf it stays linked as a phantom during the fixup — its size of
    /// 1 keeps every rotation's size recomputation consistent — and is unlinked afterwards, at which point the ancestor
    /// sizes are decremented.
    /// </remarks>
    internal static void Remove<TNode>(ref TNode? root, TNode node)
        where TNode : OrderStatisticNode<TNode>
    {
        // Interior node: copy the successor's payload into place and remove the successor instead.
        if (node.Left != null && node.Right != null)
        {
            TNode successor = BinaryTreeWalk.Minimum(node.Right);
            node.CopyPayloadFrom(successor);
            node = successor;
        }

        TNode? replacement = node.Left ?? node.Right;

        if (replacement != null)
        {
            // Splice the single child into the removed node's place, then repair the ancestor sizes before any
            // fixup rotation recomputes from them.
            replacement.Parent = node.Parent;
            if (node.Parent == null)
                root = replacement;
            else if (node == node.Parent.Left)
                node.Parent.Left = replacement;
            else
                node.Parent.Right = replacement;

            for (TNode? ancestor = replacement.Parent; ancestor != null; ancestor = ancestor.Parent)
                ancestor.Size--;

            node.Left = node.Right = node.Parent = null;

            if (!node.IsRed)
                FixAfterDeletion(ref root, replacement);
        }
        else if (node.Parent == null)
        {
            root = null;
        }
        else
        {
            // Childless leaf: fix up with the node still linked as a phantom, then unlink it and repair sizes.
            if (!node.IsRed)
                FixAfterDeletion(ref root, node);

            if (node.Parent != null)
            {
                for (TNode? ancestor = node.Parent; ancestor != null; ancestor = ancestor.Parent)
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
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link.</param>
    /// <param name="node">The replacement (or phantom) node carrying the extra black.</param>
    private static void FixAfterDeletion<TNode>(ref TNode? root, TNode node)
        where TNode : OrderStatisticNode<TNode>
    {
        while (node != root && !node.IsRed)
        {
            // A double-black node below the root always has a parent and a non-null sibling.
            TNode parent = node.Parent!;

            if (node == parent.Left)
            {
                TNode sibling = parent.Right!;

                if (sibling.IsRed)
                {
                    sibling.IsRed = false;
                    parent.IsRed = true;
                    RotateLeft(ref root, parent);
                    sibling = parent.Right!;
                }

                if (!BinaryTreeWalk.IsNodeRed(sibling.Left) && !BinaryTreeWalk.IsNodeRed(sibling.Right))
                {
                    sibling.IsRed = true;
                    node = parent;
                }
                else
                {
                    if (!BinaryTreeWalk.IsNodeRed(sibling.Right))
                    {
                        // The near child is red here, or the previous branch would have run.
                        sibling.Left!.IsRed = false;
                        sibling.IsRed = true;
                        RotateRight(ref root, sibling);
                        sibling = parent.Right!;
                    }

                    sibling.IsRed = parent.IsRed;
                    parent.IsRed = false;
                    sibling.Right!.IsRed = false;
                    RotateLeft(ref root, parent);
                    node = root!;
                }
            }
            else
            {
                TNode sibling = parent.Left!;

                if (sibling.IsRed)
                {
                    sibling.IsRed = false;
                    parent.IsRed = true;
                    RotateRight(ref root, parent);
                    sibling = parent.Left!;
                }

                if (!BinaryTreeWalk.IsNodeRed(sibling.Right) && !BinaryTreeWalk.IsNodeRed(sibling.Left))
                {
                    sibling.IsRed = true;
                    node = parent;
                }
                else
                {
                    if (!BinaryTreeWalk.IsNodeRed(sibling.Left))
                    {
                        sibling.Right!.IsRed = false;
                        sibling.IsRed = true;
                        RotateLeft(ref root, sibling);
                        sibling = parent.Left!;
                    }

                    sibling.IsRed = parent.IsRed;
                    parent.IsRed = false;
                    sibling.Left!.IsRed = false;
                    RotateRight(ref root, parent);
                    node = root!;
                }
            }
        }

        node.IsRed = false;
    }

    /// <summary>
    /// Builds a balanced red-black subtree from the slice of <paramref name="nodes" /> bounded by
    /// <paramref name="startIndex" /> and <paramref name="endIndex" />, coloring only the spill-over level red (the
    /// <see cref="SortedSet{T}" /> construction scheme).
    /// </summary>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="nodes">
    /// Freshly created, unlinked nodes in ascending payload order, each with the default size of 1 and colored black.
    /// </param>
    /// <param name="startIndex">The inclusive start index of the slice.</param>
    /// <param name="endIndex">The inclusive end index of the slice.</param>
    /// <param name="redNode">
    /// A pre-colored red leaf to hang at the leftmost position, or <see langword="null" />.
    /// </param>
    /// <returns>The subtree root, or <see langword="null" /> for an empty slice.</returns>
    internal static TNode? BuildFromSorted<TNode>(TNode[] nodes, int startIndex, int endIndex, TNode? redNode)
        where TNode : OrderStatisticNode<TNode>
    {
        int size = endIndex - startIndex + 1;
        if (size <= 0)
            return redNode;

        TNode root;
        switch (size)
        {
            case 1:
                root = nodes[startIndex];
                Link(root, redNode, asLeft: true);
                break;

            case 2:
                root = nodes[startIndex];
                nodes[endIndex].IsRed = true;
                Link(root, nodes[endIndex], asLeft: false);
                Link(root, redNode, asLeft: true);
                break;

            case 3:
                root = nodes[startIndex + 1];
                Link(root, nodes[startIndex], asLeft: true);
                Link(root, nodes[endIndex], asLeft: false);
                Link(root.Left!, redNode, asLeft: true);
                root.Left!.Size = SizeOf(root.Left.Left) + 1;
                break;

            default:
                int midpoint = (startIndex + endIndex) / 2;
                root = nodes[midpoint];
                Link(root, BuildFromSorted(nodes, startIndex, midpoint - 1, redNode), asLeft: true);

                TNode? rightSubtree;
                if (size % 2 == 0)
                {
                    nodes[midpoint + 1].IsRed = true;
                    rightSubtree = BuildFromSorted(nodes, midpoint + 2, endIndex, nodes[midpoint + 1]);
                }
                else
                {
                    rightSubtree = BuildFromSorted(nodes, midpoint + 1, endIndex, null);
                }

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
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="parent">The parent node.</param>
    /// <param name="child">The child subtree root, which may be <see langword="null" />.</param>
    /// <param name="asLeft"><see langword="true" /> to link as the left child; otherwise the right child.</param>
    private static void Link<TNode>(TNode parent, TNode? child, bool asLeft)
        where TNode : OrderStatisticNode<TNode>
    {
        if (child == null)
            return;

        child.Parent = parent;
        if (asLeft)
            parent.Left = child;
        else
            parent.Right = child;
    }
}
