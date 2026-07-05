// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTree{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a collection of closed intervals [low, high] that may freely overlap, answering stabbing queries (<see cref="QueryPoint" />
/// — all intervals containing a point) and overlap-window queries (<see cref="QueryOverlaps" /> — all intervals
/// intersecting a window) in O(log n + k).
/// </summary>
/// <typeparam name="T">The type of the interval endpoints. Endpoints must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IntervalTree{T}" /> is backed by a max-endpoint augmented red-black tree ordered by the (low, high)
/// lexicographic pair: every node carries the greatest high endpoint in its subtree, maintained through all rotations,
/// so queries prune whole subtrees that cannot reach the probe. See
/// <c>Bodu.Collections/docs/interval-tree-design.md</c> for the design decision record.
/// </para>
/// <para>
/// This type is the only member of the Bodu range family that <em>stores</em> overlapping intervals.
/// <see cref="RangeSet{T}" /> and <see cref="RangeDictionary{TKey, TValue}" /> are sorted non-overlapping maps that
/// merge or reject overlapping inserts, and <c>Bodu.Numerics</c>' <c>IntervalSet&lt;T&gt;</c> normalizes its contents
/// to disjoint ranges — reach for this type when the overlaps themselves are the data.
/// </para>
/// <para>
/// Intervals are closed on both ends: [low, high] contains every point x with low &lt;= x &lt;= high under the active
/// comparer, and low equal to high is a valid degenerate interval. Duplicate intervals are permitted — each
/// <see cref="Add" /> of an existing (low, high) pair increments a per-node multiplicity, and <see cref="Remove" />
/// removes one occurrence at a time. <see cref="Count" /> and enumeration include duplicates.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var bookings = new IntervalTree<int>();
/// bookings.Add(9, 11);    // 09:00-11:00
/// bookings.Add(10, 12);   // 10:00-12:00 — overlaps are stored, not merged
/// bookings.Add(14, 15);
///
/// foreach ((int low, int high) in bookings.QueryPoint(10))
///     Console.WriteLine($"[{low}, {high}]");            // [9, 11] then [10, 12]
///
/// bool clash = bookings.Intersects(11, 13);             // true — [10, 12] reaches into the window
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(IntervalTreeDebugView<>))]
[Serializable]
public sealed partial class IntervalTree<T>
    : IReadOnlyCollection<(T Low, T High)>
    where T : notnull
{
    /// <summary>The comparer that defines the endpoint ordering.</summary>
    private readonly IComparer<T> _comparer;

    /// <summary>The root node of the red-black tree, or <see langword="null" /> when the tree is empty.</summary>
    private Node? _root;

    /// <summary>The total number of stored intervals, duplicates included.</summary>
    private int _count;

    /// <summary>The structural version, incremented by every mutation to fail-fast in-flight enumerations.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalTree{T}" /> class using the default comparer.
    /// </summary>
    public IntervalTree()
        : this(null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalTree{T}" /> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">
    /// The endpoint ordering comparer, or <see langword="null" /> to use the default comparer.
    /// </param>
    public IntervalTree(IComparer<T>? comparer)
    {
        _comparer = comparer ?? Comparer<T>.Default;
    }

    /// <summary>
    /// Gets the comparer that defines the endpoint ordering.
    /// </summary>
    /// <value>The active endpoint comparer.</value>
    public IComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the total number of stored intervals, duplicates included.
    /// </summary>
    /// <value>The number of intervals currently stored in the tree.</value>
    public int Count => _count;

    /// <summary>
    /// Adds the closed interval [<paramref name="low" />, <paramref name="high" />] to the tree.
    /// </summary>
    /// <param name="low">The inclusive lower endpoint. Must not be <see langword="null" />.</param>
    /// <param name="high">The inclusive upper endpoint. Must not be <see langword="null" />.</param>
    /// <remarks>
    /// Overlapping intervals are always accepted, and adding an interval equal to one already stored records a
    /// duplicate occurrence rather than being rejected — the per-node multiplicity is incremented and
    /// <see cref="Count" /> grows by one.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="low" /> or <paramref name="high" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="low" /> orders after <paramref name="high" /> under the active comparer.
    /// </exception>
    public void Add(T low, T high)
    {
        ThrowHelper.ThrowIfNull(low);
        ThrowHelper.ThrowIfNull(high);
        if (_comparer.Compare(low, high) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(low));

        Node? parent = null;
        Node? current = _root;
        int comparison = 0;

        while (current != null)
        {
            comparison = CompareToNode(low, high, current);
            if (comparison == 0)
            {
                // Duplicate interval: record another occurrence on the existing node.
                current.Multiplicity++;
                _count++;
                _version++;
                return;
            }

            parent = current;
            current = comparison < 0 ? current.Left : current.Right;
        }

        var node = new Node(low, high) { Parent = parent };

        if (parent == null)
            _root = node;
        else if (comparison < 0)
            parent.Left = node;
        else
            parent.Right = node;

        // Fold the new high endpoint into the ancestor chain before the fixup so every rotation reads exact Max
        // values. parent.Max >= child.Max is invariant, so the walk can stop at the first covering ancestor.
        for (Node? ancestor = parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (_comparer.Compare(high, ancestor.Max) <= 0)
                break;

            ancestor.Max = high;
        }

        FixAfterInsertion(node);

        _count++;
        _version++;
    }

    /// <summary>
    /// Removes one occurrence of the exact interval [<paramref name="low" />, <paramref name="high" />] from the tree.
    /// </summary>
    /// <param name="low">The inclusive lower endpoint. Must not be <see langword="null" />.</param>
    /// <param name="high">The inclusive upper endpoint. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if an occurrence was removed; otherwise, <see langword="false" /> when the exact
    /// interval is not stored.
    /// </returns>
    /// <remarks>
    /// Only an exact (low, high) match is removed — intervals that merely overlap the arguments are untouched. When the
    /// interval is stored more than once, one occurrence is removed per call; the node itself is deleted only when its
    /// multiplicity reaches zero.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="low" /> or <paramref name="high" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="low" /> orders after <paramref name="high" /> under the active comparer.
    /// </exception>
    public bool Remove(T low, T high)
    {
        ThrowHelper.ThrowIfNull(low);
        ThrowHelper.ThrowIfNull(high);
        if (_comparer.Compare(low, high) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(low));

        Node? node = FindNode(low, high);
        if (node == null)
            return false;

        if (node.Multiplicity > 1)
            node.Multiplicity--;
        else
            RemoveNode(node);

        _count--;
        _version++;
        return true;
    }

    /// <summary>
    /// Determines whether the tree stores the exact interval [<paramref name="low" />, <paramref name="high" />].
    /// </summary>
    /// <param name="low">The inclusive lower endpoint. Must not be <see langword="null" />.</param>
    /// <param name="high">The inclusive upper endpoint. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if at least one occurrence of the exact interval is stored; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is an exact-match test on the (low, high) pair. Use <see cref="Intersects" /> or
    /// <see cref="IntersectsPoint" /> to ask whether any stored interval merely overlaps a window or point.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="low" /> or <paramref name="high" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="low" /> orders after <paramref name="high" /> under the active comparer.
    /// </exception>
    public bool Contains(T low, T high)
    {
        ThrowHelper.ThrowIfNull(low);
        ThrowHelper.ThrowIfNull(high);
        if (_comparer.Compare(low, high) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(low));

        return FindNode(low, high) != null;
    }

    /// <summary>
    /// Removes all intervals from the tree.
    /// </summary>
    public void Clear()
    {
        _root = null;
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the stored intervals in ascending (low, high) order, repeating
    /// duplicate intervals once per occurrence.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the stored intervals.</returns>
    public Enumerator GetEnumerator() =>
        new(this);
}
