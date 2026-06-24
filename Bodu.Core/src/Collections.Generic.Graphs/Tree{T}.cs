// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Tree{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Represents a node in a mutable n-ary tree. Each instance is simultaneously a node carrying a value and the root of
/// the subtree formed by its descendants.
/// </summary>
/// <typeparam name="T">The type of the value stored at each node.</typeparam>
/// <remarks>
/// <para>
/// A <see cref="Tree{T}" /> exposes its <see cref="Parent" /> and ordered <see cref="Children" /> and supports
/// structural mutation through <see cref="AddChild(T)" />, <see cref="AddChild(Tree{T})" />,
/// <see cref="RemoveChild(Tree{T})" />, and <see cref="Remove" />. Traversals (<see cref="PreOrder" />,
/// <see cref="PostOrder" />, <see cref="LevelOrder" />) are evaluated iteratively, so arbitrarily deep trees can be
/// walked without risking a stack overflow.
/// </para>
/// <para>
/// The tree is not thread-safe for concurrent mutation. Mutating the tree while a traversal is being enumerated
/// produces undefined results.
/// </para>
/// </remarks>
[DebuggerDisplay("Value = {Value}, Children = {ChildCount}")]
[DebuggerTypeProxy(typeof(TreeDebugView<>))]
public sealed partial class Tree<T>
{
    private readonly List<Tree<T>> _children;
    private readonly ReadOnlyCollection<Tree<T>> _childrenView;
    private Tree<T>? _parent;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree{T}" /> class as a root node with the specified value.
    /// </summary>
    /// <param name="value">The value stored at the node.</param>
    public Tree(T value)
    {
        Value = value;
        _children = new List<Tree<T>>();
        _childrenView = new ReadOnlyCollection<Tree<T>>(_children);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree{T}" /> class as a root node with the specified value and a
    /// child created for each supplied child value.
    /// </summary>
    /// <param name="value">The value stored at the node.</param>
    /// <param name="childValues">The values from which to create child nodes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="childValues" /> is <see langword="null" />.</exception>
    public Tree(T value, IEnumerable<T> childValues)
        : this(value)
    {
        ThrowHelper.ThrowIfNull(childValues);

        foreach (var childValue in childValues)
            AddChild(childValue);
    }

    /// <summary>
    /// Gets or sets the value stored at this node.
    /// </summary>
    /// <value>The node's value.</value>
    /// <returns>The node's value.</returns>
    public T Value { get; set; }

    /// <summary>
    /// Gets the parent node, or <see langword="null" /> when this node is a root.
    /// </summary>
    /// <value>The parent node, or <see langword="null" />.</value>
    /// <returns>The parent node, or <see langword="null" /> when this node is a root.</returns>
    public Tree<T>? Parent => _parent;

    /// <summary>
    /// Gets the ordered list of child nodes.
    /// </summary>
    /// <value>A read-only view of the node's children.</value>
    /// <returns>The node's children.</returns>
    /// <remarks>
    /// The returned collection is a live, read-only view over the node's children: it reflects subsequent
    /// <see cref="AddChild(T)" /> and <see cref="RemoveChild" /> operations but cannot be mutated directly, preserving
    /// the parent and acyclicity invariants the mutating members enforce.
    /// </remarks>
    public IReadOnlyList<Tree<T>> Children => _childrenView;

    /// <summary>
    /// Gets the number of immediate children.
    /// </summary>
    /// <value>The child count.</value>
    /// <returns>The number of immediate children.</returns>
    public int ChildCount => _children.Count;

    /// <summary>
    /// Gets a value indicating whether this node has no parent.
    /// </summary>
    /// <value><see langword="true" /> if the node is a root; otherwise, <see langword="false" />.</value>
    /// <returns><see langword="true" /> if the node is a root; otherwise, <see langword="false" />.</returns>
    public bool IsRoot => _parent is null;

    /// <summary>
    /// Gets a value indicating whether this node has no children.
    /// </summary>
    /// <value><see langword="true" /> if the node is a leaf; otherwise, <see langword="false" />.</value>
    /// <returns><see langword="true" /> if the node is a leaf; otherwise, <see langword="false" />.</returns>
    public bool IsLeaf => _children.Count == 0;

    /// <summary>
    /// Gets the depth of this node, measured as the number of edges from the root.
    /// </summary>
    /// <value>Zero for a root node, increasing by one per level.</value>
    /// <returns>The number of edges between this node and the root.</returns>
    public int Depth
    {
        get
        {
            var depth = 0;
            for (var node = _parent; node is not null; node = node._parent)
                depth++;

            return depth;
        }
    }

    /// <summary>
    /// Gets the height of the subtree rooted at this node, measured as the number of edges on the longest path to a
    /// descendant leaf.
    /// </summary>
    /// <value>Zero for a leaf node.</value>
    /// <returns>The length of the longest downward path to a leaf.</returns>
    public int Height
    {
        get
        {
            var height = 0;
            var stack = new Stack<(Tree<T> Node, int Depth)>();
            stack.Push((this, 0));
            while (stack.Count > 0)
            {
                var (node, depth) = stack.Pop();
                if (depth > height)
                    height = depth;

                foreach (var child in node._children)
                    stack.Push((child, depth + 1));
            }

            return height;
        }
    }

    /// <summary>
    /// Creates a new child node with the specified value and appends it to this node's children.
    /// </summary>
    /// <param name="value">The value of the new child node.</param>
    /// <returns>The newly created child node.</returns>
    public Tree<T> AddChild(T value)
    {
        var child = new Tree<T>(value) { _parent = this };
        _children.Add(child);
        return child;
    }

    /// <summary>
    /// Attaches an existing detached subtree as a child of this node.
    /// </summary>
    /// <param name="child">The subtree to attach.</param>
    /// <exception cref="ArgumentNullException"><paramref name="child" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="child" /> already has a parent, or attaching it would create a cycle.</exception>
    public void AddChild(Tree<T> child)
    {
        ThrowHelper.ThrowIfNull(child);

        if (child._parent is not null)
            throw new InvalidOperationException(ResourceStrings.Op_Invalid_NodeAlreadyHasParent);

        for (var node = this; node is not null; node = node._parent)
        {
            if (ReferenceEquals(node, child))
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ResourceStrings.Op_Invalid_CircularReference, child.Value));
        }

        child._parent = this;
        _children.Add(child);
    }

    /// <summary>
    /// Detaches the specified child from this node.
    /// </summary>
    /// <param name="child">The child to detach.</param>
    /// <returns><see langword="true" /> if the child was found and detached; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="child" /> is <see langword="null" />.</exception>
    public bool RemoveChild(Tree<T> child)
    {
        ThrowHelper.ThrowIfNull(child);

        if (!_children.Remove(child))
            return false;

        child._parent = null;
        return true;
    }

    /// <summary>
    /// Detaches this node from its parent.
    /// </summary>
    /// <returns><see langword="true" /> if the node was detached; <see langword="false" /> if it was already a root.</returns>
    public bool Remove()
    {
        if (_parent is null)
            return false;

        _parent._children.Remove(this);
        _parent = null;
        return true;
    }

    /// <summary>
    /// Detaches all children from this node.
    /// </summary>
    public void Clear()
    {
        foreach (var child in _children)
            child._parent = null;

        _children.Clear();
    }
}
