// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Provides the max-endpoint augmented red-black tree machinery shared by <see cref="IntervalTree{T}" /> and
/// <see cref="IntervalTree{TKey, TValue}" />: exact-interval search, rotations with subtree-maximum maintenance, the
/// insertion and deletion fixups, physical node removal, the single-descent intersection test, and the pruned
/// parent-pointer walk that backs the overlap enumerations.
/// </summary>
/// <remarks>
/// <para>
/// All methods are static generic over the endpoint type and the concrete <see cref="IntervalNode{TEndpoint, TNode}" />
/// subtype, so link and augmentation maintenance compiles with no added interface dispatch or delegate indirection
/// beyond the comparer the consumers already invoke; the only virtual call is the payload copy-down in
/// <see cref="Remove{TEndpoint, TNode}" />, which runs at most once per removal.
/// </para>
/// <para>
/// Insertion (node construction, duplicate handling, and the pre-fixup Max fold-in) stays in the consumer, which owns
/// the payload representation; this class handles the structural protocol once a node is linked or located. The tree
/// root is passed by reference so rotations can republish it.
/// </para>
/// </remarks>
internal static class IntervalTreeCore
{
    /// <summary>
    /// Compares the (low, high) lexicographic ordering key against the interval stored at <paramref name="node" />.
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="low">The lower endpoint of the probe interval.</param>
    /// <param name="high">The upper endpoint of the probe interval.</param>
    /// <param name="node">The node to compare against.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    /// <returns>A negative, zero, or positive value per the standard comparison contract.</returns>
    internal static int CompareToNode<TEndpoint, TNode>(TEndpoint low, TEndpoint high, TNode node, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
    {
        int comparison = comparer.Compare(low, node.Low);
        return comparison != 0 ? comparison : comparer.Compare(high, node.High);
    }

    /// <summary>
    /// Finds the node storing the exact (low, high) interval, or <see langword="null" /> when absent.
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">The tree root, which may be <see langword="null" />.</param>
    /// <param name="low">The lower endpoint to locate.</param>
    /// <param name="high">The upper endpoint to locate.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    /// <returns>The matching node, or <see langword="null" />.</returns>
    internal static TNode? Find<TEndpoint, TNode>(TNode? root, TEndpoint low, TEndpoint high, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
    {
        TNode? current = root;
        while (current != null)
        {
            int comparison = CompareToNode(low, high, current, comparer);
            if (comparison == 0)
                return current;

            current = comparison < 0 ? current.Left : current.Right;
        }

        return null;
    }

    /// <summary>
    /// Recomputes the <see cref="IntervalNode{TEndpoint, TNode}.Max" /> field of <paramref name="node" /> from its own
    /// high endpoint and its children's (assumed exact) values.
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The node to recompute.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    private static void RecomputeMax<TEndpoint, TNode>(TNode node, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
    {
        TEndpoint max = node.High;
        if (node.Left != null && comparer.Compare(node.Left.Max, max) > 0)
            max = node.Left.Max;
        if (node.Right != null && comparer.Compare(node.Right.Max, max) > 0)
            max = node.Right.Max;

        node.Max = max;
    }

    /// <summary>
    /// Recomputes <see cref="IntervalNode{TEndpoint, TNode}.Max" /> bottom-up from <paramref name="node" /> to the
    /// root, restoring exactness after a deletion whose fixup rotations may have propagated stale values along the
    /// ancestor chain.
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The node to start from, which may be <see langword="null" />.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    private static void RecomputeMaxUpward<TEndpoint, TNode>(TNode? node, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
    {
        for (TNode? current = node; current != null; current = current.Parent)
            RecomputeMax(current, comparer);
    }

    /// <summary>
    /// Rotates the subtree rooted at <paramref name="pivot" /> to the left, recomputing the demoted node's
    /// <see cref="IntervalNode{TEndpoint, TNode}.Max" /> from its new children and then the promoted node's from the
    /// fresh result.
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link, republished when the pivot was the root.</param>
    /// <param name="pivot">The subtree root to rotate. Must have a right child.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    private static void RotateLeft<TEndpoint, TNode>(ref TNode? root, TNode pivot, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
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

        // Recompute the demoted node first — its children are final — then the promoted node, which reads the
        // demoted node's fresh value. With exact children, the rotation preserves Max exactness.
        RecomputeMax(pivot, comparer);
        RecomputeMax(promoted, comparer);
    }

    /// <summary>
    /// Rotates the subtree rooted at <paramref name="pivot" /> to the right, recomputing the demoted node's
    /// <see cref="IntervalNode{TEndpoint, TNode}.Max" /> from its new children and then the promoted node's from the
    /// fresh result.
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link, republished when the pivot was the root.</param>
    /// <param name="pivot">The subtree root to rotate. Must have a left child.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    private static void RotateRight<TEndpoint, TNode>(ref TNode? root, TNode pivot, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
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

        RecomputeMax(pivot, comparer);
        RecomputeMax(promoted, comparer);
    }

    /// <summary>
    /// Restores the red-black invariants after inserting <paramref name="inserted" /> (classic bottom-up fixup).
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link.</param>
    /// <param name="inserted">The newly linked node.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    internal static void FixAfterInsertion<TEndpoint, TNode>(ref TNode? root, TNode inserted, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
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
                        RotateLeft(ref root, node, comparer);
                        parent = node.Parent!;
                        grandparent = parent.Parent!;
                    }

                    parent.IsRed = false;
                    grandparent.IsRed = true;
                    RotateRight(ref root, grandparent, comparer);
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
                        RotateRight(ref root, node, comparer);
                        parent = node.Parent!;
                        grandparent = parent.Parent!;
                    }

                    parent.IsRed = false;
                    grandparent.IsRed = true;
                    RotateLeft(ref root, grandparent, comparer);
                }
            }
        }

        root!.IsRed = false;
    }

    /// <summary>
    /// Physically removes <paramref name="node" /> from the tree, restoring the red-black invariants and the
    /// <see cref="IntervalNode{TEndpoint, TNode}.Max" /> augmentation.
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link.</param>
    /// <param name="node">The node to remove.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    /// <remarks>
    /// An interior node with two children is reduced to its in-order successor (the endpoints are copied down here, the
    /// payload via <see cref="IntervalNode{TEndpoint, TNode}.CopyPayloadFrom" />), so the physically unlinked node
    /// always has at most one child. When that node is a childless black leaf it stays linked as a phantom during the
    /// fixup and is unlinked afterwards. Every case finishes with a bottom-up Max recomputation from the splice point:
    /// any stale Max produced by the payload copy or mid-fixup rotations lives on that ancestor chain, so the single
    /// walk restores exactness.
    /// </remarks>
    internal static void Remove<TEndpoint, TNode>(ref TNode? root, TNode node, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
    {
        // Interior node: copy the successor's payload into place and remove the successor instead.
        if (node.Left != null && node.Right != null)
        {
            TNode successor = BinaryTreeWalk.Minimum(node.Right);
            node.Low = successor.Low;
            node.High = successor.High;
            node.CopyPayloadFrom(successor);
            node = successor;
        }

        TNode? replacement = node.Left ?? node.Right;

        if (replacement != null)
        {
            // Splice the single child into the removed node's place.
            replacement.Parent = node.Parent;
            if (node.Parent == null)
                root = replacement;
            else if (node == node.Parent.Left)
                node.Parent.Left = replacement;
            else
                node.Parent.Right = replacement;

            node.Left = node.Right = node.Parent = null;

            if (!node.IsRed)
                FixAfterDeletion(ref root, replacement, comparer);

            RecomputeMaxUpward(replacement, comparer);
        }
        else if (node.Parent == null)
        {
            root = null;
        }
        else
        {
            // Childless leaf: fix up with the node still linked as a phantom, then unlink it and repair Max.
            if (!node.IsRed)
                FixAfterDeletion(ref root, node, comparer);

            if (node.Parent != null)
            {
                TNode anchor = node.Parent;

                if (node == node.Parent.Left)
                    node.Parent.Left = null;
                else if (node == node.Parent.Right)
                    node.Parent.Right = null;

                node.Parent = null;
                RecomputeMaxUpward(anchor, comparer);
            }
        }
    }

    /// <summary>
    /// Restores the red-black invariants after removing a black node, starting the double-black resolution at
    /// <paramref name="node" /> (classic bottom-up fixup).
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">A reference to the owning tree's root link.</param>
    /// <param name="node">The replacement (or phantom) node carrying the extra black.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    private static void FixAfterDeletion<TEndpoint, TNode>(ref TNode? root, TNode node, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
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
                    RotateLeft(ref root, parent, comparer);
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
                        RotateRight(ref root, sibling, comparer);
                        sibling = parent.Right!;
                    }

                    sibling.IsRed = parent.IsRed;
                    parent.IsRed = false;
                    sibling.Right!.IsRed = false;
                    RotateLeft(ref root, parent, comparer);
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
                    RotateRight(ref root, parent, comparer);
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
                        RotateLeft(ref root, sibling, comparer);
                        sibling = parent.Left!;
                    }

                    sibling.IsRed = parent.IsRed;
                    parent.IsRed = false;
                    sibling.Left!.IsRed = false;
                    RotateRight(ref root, parent, comparer);
                    node = root!;
                }
            }
        }

        node.IsRed = false;
    }

    /// <summary>
    /// Performs the classic single-descent interval search for any interval overlapping [<paramref name="low" />,
    /// <paramref name="high" />].
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="root">The tree root, which may be <see langword="null" />.</param>
    /// <param name="low">The inclusive lower edge of the window.</param>
    /// <param name="high">The inclusive upper edge of the window.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    /// <returns>
    /// <see langword="true" /> when an overlapping interval exists; otherwise, <see langword="false" />.
    /// </returns>
    internal static bool Intersects<TEndpoint, TNode>(TNode? root, TEndpoint low, TEndpoint high, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
    {
        TNode? current = root;
        while (current != null)
        {
            if (comparer.Compare(current.Low, high) <= 0 && comparer.Compare(current.High, low) >= 0)
                return true;

            // If the left subtree's Max reaches the window it is guaranteed to hold any existing overlap on this
            // path; otherwise no left descendant can match and the search continues right.
            current = current.Left != null && comparer.Compare(current.Left.Max, low) >= 0
                ? current.Left
                : current.Right;
        }

        return false;
    }

    /// <summary>
    /// Descends to the leftmost node of the subtree rooted at <paramref name="node" /> whose left branches can still
    /// reach the window's low edge, skipping left subtrees whose <see cref="IntervalNode{TEndpoint, TNode}.Max" />
    /// falls short.
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <param name="low">The inclusive lower edge of the query window.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    /// <returns>The first node of the pruned in-order walk of the subtree.</returns>
    internal static TNode PrunedMinimum<TEndpoint, TNode>(TNode node, TEndpoint low, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
    {
        while (node.Left != null && comparer.Compare(node.Left.Max, low) >= 0)
            node = node.Left;

        return node;
    }

    /// <summary>
    /// Advances the pruned in-order walk from <paramref name="node" /> via parent pointers, entering a right subtree
    /// only when its <see cref="IntervalNode{TEndpoint, TNode}.Max" /> can still reach the window's low edge.
    /// </summary>
    /// <typeparam name="TEndpoint">The interval endpoint type.</typeparam>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <param name="low">The inclusive lower edge of the query window.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    /// <returns>The next node of the pruned walk, or <see langword="null" /> when the walk is exhausted.</returns>
    internal static TNode? PrunedSuccessor<TEndpoint, TNode>(TNode node, TEndpoint low, IComparer<TEndpoint> comparer)
        where TNode : IntervalNode<TEndpoint, TNode>
    {
        TNode? right = node.Right;
        if (right != null && comparer.Compare(right.Max, low) >= 0)
            return PrunedMinimum(right, low, comparer);

        TNode? parent = node.Parent;
        while (parent != null && node == parent.Right)
        {
            node = parent;
            parent = parent.Parent;
        }

        return parent;
    }
}
