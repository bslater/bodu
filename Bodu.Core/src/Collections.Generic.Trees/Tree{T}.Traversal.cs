// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Tree{T}.Traversal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class Tree<T>
{
    /// <summary>
    /// Enumerates the subtree rooted at this node in pre-order: each node is visited before its children, which are
    /// visited left to right.
    /// </summary>
    /// <returns>A lazily evaluated pre-order sequence beginning with this node.</returns>
    public IEnumerable<Tree<T>> PreOrder()
    {
        var stack = new Stack<Tree<T>>();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;

            for (var i = node._children.Count - 1; i >= 0; i--)
                stack.Push(node._children[i]);
        }
    }

    /// <summary>
    /// Enumerates the subtree rooted at this node in post-order: each node is visited after its children, which are
    /// visited left to right.
    /// </summary>
    /// <returns>A lazily evaluated post-order sequence ending with this node.</returns>
    public IEnumerable<Tree<T>> PostOrder()
    {
        var stack = new Stack<Tree<T>>();
        var output = new Stack<Tree<T>>();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            output.Push(node);

            foreach (var child in node._children)
                stack.Push(child);
        }

        while (output.Count > 0)
            yield return output.Pop();
    }

    /// <summary>
    /// Enumerates the subtree rooted at this node in level (breadth-first) order.
    /// </summary>
    /// <returns>A lazily evaluated breadth-first sequence beginning with this node.</returns>
    public IEnumerable<Tree<T>> LevelOrder()
    {
        var queue = new Deque<Tree<T>>();
        queue.AddLast(this);
        while (queue.Count > 0)
        {
            var node = queue.RemoveFirst();
            yield return node;

            foreach (var child in node._children)
                queue.AddLast(child);
        }
    }

    /// <summary>
    /// Enumerates every descendant of this node in pre-order, excluding this node itself.
    /// </summary>
    /// <returns>A lazily evaluated sequence of descendant nodes.</returns>
    public IEnumerable<Tree<T>> Descendants()
    {
        var stack = new Stack<Tree<T>>();
        for (var i = _children.Count - 1; i >= 0; i--)
            stack.Push(_children[i]);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;

            for (var i = node._children.Count - 1; i >= 0; i--)
                stack.Push(node._children[i]);
        }
    }

    /// <summary>
    /// Enumerates the ancestors of this node, from its parent up to the root.
    /// </summary>
    /// <returns>A lazily evaluated sequence of ancestor nodes.</returns>
    public IEnumerable<Tree<T>> Ancestors()
    {
        for (var node = _parent; node is not null; node = node.Parent)
            yield return node;
    }

    /// <summary>
    /// Enumerates the leaf nodes of the subtree rooted at this node.
    /// </summary>
    /// <returns>A lazily evaluated sequence of leaf nodes.</returns>
    public IEnumerable<Tree<T>> Leaves()
    {
        foreach (var node in PreOrder())
        {
            if (node.IsLeaf)
                yield return node;
        }
    }

    /// <summary>
    /// Returns the root of the tree containing this node.
    /// </summary>
    /// <returns>The topmost ancestor, or this node when it is already a root.</returns>
    public Tree<T> Root()
    {
        var node = this;
        while (node._parent is not null)
            node = node._parent;

        return node;
    }
}
