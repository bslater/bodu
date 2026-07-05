// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTree{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a collection of closed intervals [low, high] that may freely overlap, each carrying an associated value,
/// answering stabbing queries (<see cref="QueryPoint" />) and overlap-window queries (<see cref="QueryOverlaps" />) in
/// O(log n + k).
/// </summary>
/// <typeparam name="TKey">The type of the interval endpoints.</typeparam>
/// <typeparam name="TValue">The type of the value carried by each stored interval.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IntervalTree{TKey, TValue}" /> is the value-carrying counterpart of <see cref="IntervalTree{T}" />,
/// backed by the same max-endpoint augmented red-black tree ordered by the (low, high) lexicographic pair. See
/// <c>Bodu.Collections/docs/interval-tree-design.md</c> for the design decision record and the boundary against the
/// non-overlapping <see cref="RangeSet{T}" /> / <see cref="RangeDictionary{TKey, TValue}" /> range maps.
/// </para>
/// <para>
/// Intervals are closed on both ends and duplicates of the same (low, high) pair are permitted — including with equal
/// values. Where the unkeyed tree keeps a per-node multiplicity count, this type keeps a per-node value list in
/// insertion order: <see cref="Add" /> appends, <see cref="Remove(TKey, TKey)" /> removes the first stored entry, and
/// <see cref="Remove(TKey, TKey, TValue)" /> removes the first entry whose value matches under
/// <see cref="EqualityComparer{TValue}.Default" />. <see cref="Count" /> and enumeration include every stored entry.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// <para>
/// Endpoints must not be <see langword="null" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var meetings = new IntervalTree<int, string>();
/// meetings.Add(9, 11, "stand-up");
/// meetings.Add(10, 12, "design review");   // overlaps are stored, not merged
/// meetings.Add(10, 12, "1:1");             // same slot, different value — also stored
///
/// foreach ((int low, int high, string name) in meetings.QueryPoint(10))
///     Console.WriteLine($"[{low}, {high}] {name}");
///
/// meetings.Remove(10, 12, "1:1");          // removes exactly that entry
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(IntervalTreeDebugView<,>))]
[Serializable]
public sealed partial class IntervalTree<TKey, TValue>
    : IReadOnlyCollection<(TKey Low, TKey High, TValue Value)>
    where TKey : notnull
{
    /// <summary>The comparer that defines the endpoint ordering.</summary>
    private readonly IComparer<TKey> _comparer;

    /// <summary>The root node of the red-black tree, or <see langword="null" /> when the tree is empty.</summary>
    private Node? _root;

    /// <summary>The total number of stored entries, duplicates included.</summary>
    private int _count;

    /// <summary>The structural version, incremented by every mutation to fail-fast in-flight enumerations.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalTree{TKey, TValue}" /> class using the default comparer.
    /// </summary>
    public IntervalTree()
        : this(null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalTree{TKey, TValue}" /> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">
    /// The endpoint ordering comparer, or <see langword="null" /> to use the default comparer.
    /// </param>
    public IntervalTree(IComparer<TKey>? comparer)
    {
        _comparer = comparer ?? Comparer<TKey>.Default;
    }

    /// <summary>
    /// Gets the comparer that defines the endpoint ordering.
    /// </summary>
    /// <value>The active endpoint comparer.</value>
    public IComparer<TKey> Comparer => _comparer;

    /// <summary>
    /// Gets the total number of stored entries, duplicates included.
    /// </summary>
    /// <value>The number of interval/value entries currently stored in the tree.</value>
    public int Count => _count;

    /// <summary>
    /// Adds the closed interval [<paramref name="low" />, <paramref name="high" />] carrying <paramref name="value" />
    /// to the tree.
    /// </summary>
    /// <param name="low">The inclusive lower endpoint. Must not be <see langword="null" />.</param>
    /// <param name="high">The inclusive upper endpoint. Must not be <see langword="null" />.</param>
    /// <param name="value">The value to associate with the interval.</param>
    /// <remarks>
    /// Overlapping intervals are always accepted, and adding to an interval already stored appends the value to that
    /// node's entry list — the same (low, high) pair may carry many values, including comparer-equal duplicates. Values
    /// are retained in insertion order per interval.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="low" /> or <paramref name="high" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="low" /> orders after <paramref name="high" /> under the active comparer.
    /// </exception>
    public void Add(TKey low, TKey high, TValue value)
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
                // Existing interval: append the value to the node's entry list.
                current.Values.Add(value);
                _count++;
                _version++;
                return;
            }

            parent = current;
            current = comparison < 0 ? current.Left : current.Right;
        }

        var node = new Node(low, high, value) { Parent = parent };

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
    /// Removes the first stored entry of the exact interval [<paramref name="low" />, <paramref name="high" />] from
    /// the tree.
    /// </summary>
    /// <param name="low">The inclusive lower endpoint. Must not be <see langword="null" />.</param>
    /// <param name="high">The inclusive upper endpoint. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if an entry was removed; otherwise, <see langword="false" /> when the exact interval is
    /// not stored.
    /// </returns>
    /// <remarks>
    /// Only an exact (low, high) match is affected. When the interval carries multiple values, the earliest inserted
    /// entry is removed; use <see cref="Remove(TKey, TKey, TValue)" /> to remove a specific value. The node is deleted
    /// only when its entry list empties.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="low" /> or <paramref name="high" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="low" /> orders after <paramref name="high" /> under the active comparer.
    /// </exception>
    public bool Remove(TKey low, TKey high)
    {
        ThrowHelper.ThrowIfNull(low);
        ThrowHelper.ThrowIfNull(high);
        if (_comparer.Compare(low, high) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(low));

        Node? node = FindNode(low, high);
        if (node == null)
            return false;

        RemoveEntryAt(node, 0);
        return true;
    }

    /// <summary>
    /// Removes the first stored entry of the exact interval [<paramref name="low" />, <paramref name="high" />] whose
    /// value equals <paramref name="value" /> under <see cref="EqualityComparer{TValue}.Default" />.
    /// </summary>
    /// <param name="low">The inclusive lower endpoint. Must not be <see langword="null" />.</param>
    /// <param name="high">The inclusive upper endpoint. Must not be <see langword="null" />.</param>
    /// <param name="value">The value identifying the entry to remove.</param>
    /// <returns>
    /// <see langword="true" /> if a matching entry was removed; otherwise, <see langword="false" /> when the exact
    /// interval is not stored or carries no matching value.
    /// </returns>
    /// <remarks>
    /// When the interval carries several equal values, one entry is removed per call. The node is deleted only when its
    /// entry list empties.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="low" /> or <paramref name="high" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="low" /> orders after <paramref name="high" /> under the active comparer.
    /// </exception>
    public bool Remove(TKey low, TKey high, TValue value)
    {
        ThrowHelper.ThrowIfNull(low);
        ThrowHelper.ThrowIfNull(high);
        if (_comparer.Compare(low, high) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(low));

        Node? node = FindNode(low, high);
        if (node == null)
            return false;

        int index = node.Values.IndexOf(value);
        if (index < 0)
            return false;

        RemoveEntryAt(node, index);
        return true;
    }

    /// <summary>
    /// Determines whether the tree stores the exact interval [<paramref name="low" />, <paramref name="high" />].
    /// </summary>
    /// <param name="low">The inclusive lower endpoint. Must not be <see langword="null" />.</param>
    /// <param name="high">The inclusive upper endpoint. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if at least one entry with the exact interval is stored; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is an exact-match test on the (low, high) pair, regardless of the values carried. Use
    /// <see cref="Intersects" /> or <see cref="IntersectsPoint" /> to ask whether any stored interval merely overlaps a
    /// window or point.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="low" /> or <paramref name="high" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="low" /> orders after <paramref name="high" /> under the active comparer.
    /// </exception>
    public bool Contains(TKey low, TKey high)
    {
        ThrowHelper.ThrowIfNull(low);
        ThrowHelper.ThrowIfNull(high);
        if (_comparer.Compare(low, high) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(low));

        return FindNode(low, high) != null;
    }

    /// <summary>
    /// Removes all entries from the tree.
    /// </summary>
    public void Clear()
    {
        _root = null;
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the stored entries in ascending (low, high) order, yielding an
    /// interval's values in insertion order.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the stored entries.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <summary>
    /// Removes the entry at <paramref name="index" /> from <paramref name="node" />, deleting the node when its entry
    /// list empties, and updates the count and version.
    /// </summary>
    /// <param name="node">The node holding the entry.</param>
    /// <param name="index">The index of the entry within the node's value list.</param>
    private void RemoveEntryAt(Node node, int index)
    {
        if (node.Values.Count > 1)
            node.Values.RemoveAt(index);
        else
            RemoveNode(node);

        _count--;
        _version++;
    }
}
