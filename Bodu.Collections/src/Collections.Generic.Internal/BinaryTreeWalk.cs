// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryTreeWalk.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Provides the payload-free structural walks shared by every <see cref="BinaryTreeNode{TNode}" />-based tree: subtree
/// minimum/maximum descents and the parent-pointer in-order successor/predecessor steps.
/// </summary>
/// <remarks>
/// All methods are static generic over the concrete node type, so the JIT specializes link traversal per node type with
/// no interface dispatch or delegate indirection.
/// </remarks>
internal static class BinaryTreeWalk
{
    /// <summary>
    /// Returns whether <paramref name="node" /> is red, treating <see langword="null" /> leaves as black.
    /// </summary>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The node to inspect, which may be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the node exists and is red; otherwise, <see langword="false" />.</returns>
    internal static bool IsNodeRed<TNode>(TNode? node)
        where TNode : BinaryTreeNode<TNode> =>
        node?.IsRed ?? false;

    /// <summary>
    /// Returns the leftmost (minimum) node of the subtree rooted at <paramref name="node" />.
    /// </summary>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <returns>The minimum node of the subtree.</returns>
    internal static TNode Minimum<TNode>(TNode node)
        where TNode : BinaryTreeNode<TNode>
    {
        while (node.Left != null)
            node = node.Left;

        return node;
    }

    /// <summary>
    /// Returns the rightmost (maximum) node of the subtree rooted at <paramref name="node" />.
    /// </summary>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <returns>The maximum node of the subtree.</returns>
    internal static TNode Maximum<TNode>(TNode node)
        where TNode : BinaryTreeNode<TNode>
    {
        while (node.Right != null)
            node = node.Right;

        return node;
    }

    /// <summary>
    /// Returns the in-order successor of <paramref name="node" /> via parent pointers, or <see langword="null" /> when
    /// the node is the maximum.
    /// </summary>
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The next node in ascending order, or <see langword="null" />.</returns>
    internal static TNode? Successor<TNode>(TNode node)
        where TNode : BinaryTreeNode<TNode>
    {
        if (node.Right != null)
            return Minimum(node.Right);

        TNode? parent = node.Parent;
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
    /// <typeparam name="TNode">The concrete node type.</typeparam>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The previous node in descending order, or <see langword="null" />.</returns>
    internal static TNode? Predecessor<TNode>(TNode node)
        where TNode : BinaryTreeNode<TNode>
    {
        if (node.Left != null)
            return Maximum(node.Left);

        TNode? parent = node.Parent;
        while (parent != null && node == parent.Left)
        {
            node = parent;
            parent = parent.Parent;
        }

        return parent;
    }
}
