// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTree{T,T}.Queries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic;

public sealed partial class IntervalTree<TKey, TValue>
{
    /// <summary>
    /// Returns all stored entries whose interval contains <paramref name="point" /> (the stabbing query), in ascending
    /// (low, high) order with each interval's values in insertion order.
    /// </summary>
    /// <param name="point">The point to stab. Must not be <see langword="null" />.</param>
    /// <returns>A lazily evaluated sequence of the entries whose closed interval contains the point.</returns>
    /// <remarks>
    /// <para>
    /// Both endpoints are inclusive: an interval [low, high] matches when low &lt;= point &lt;= high under the active
    /// comparer. Iterating costs O(log n + k) for k reported entries in the common case.
    /// </para>
    /// <para>
    /// The sequence is live: each fresh iteration re-resolves against the tree's current state. Within a single
    /// iteration it is fail-fast — any structural mutation causes the next advance to throw
    /// <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="point" /> is <see langword="null" />.</exception>
    public IEnumerable<(TKey Low, TKey High, TValue Value)> QueryPoint(TKey point)
    {
        ThrowHelper.ThrowIfNull(point);

        return EnumerateOverlaps(point, point);
    }

    /// <summary>
    /// Returns all stored entries whose interval intersects the closed window [<paramref name="low" />,
    /// <paramref name="high" />], in ascending (low, high) order with each interval's values in insertion order.
    /// </summary>
    /// <param name="low">The inclusive lower edge of the window. Must not be <see langword="null" />.</param>
    /// <param name="high">The inclusive upper edge of the window. Must not be <see langword="null" />.</param>
    /// <returns>A lazily evaluated sequence of the entries overlapping the window.</returns>
    /// <remarks>
    /// <para>
    /// An interval matches when it shares at least one point with the window — touching at a single common endpoint
    /// counts, because both the stored intervals and the window are closed. Iterating costs O(log n + k) for k reported
    /// entries in the common case.
    /// </para>
    /// <para>
    /// The sequence is live: each fresh iteration re-resolves against the tree's current state. Within a single
    /// iteration it is fail-fast — any structural mutation causes the next advance to throw
    /// <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="low" /> or <paramref name="high" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="low" /> orders after <paramref name="high" /> under the active comparer.
    /// </exception>
    public IEnumerable<(TKey Low, TKey High, TValue Value)> QueryOverlaps(TKey low, TKey high)
    {
        ThrowHelper.ThrowIfNull(low);
        ThrowHelper.ThrowIfNull(high);
        if (_comparer.Compare(low, high) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(low));

        return EnumerateOverlaps(low, high);
    }

    /// <summary>
    /// Determines whether any stored interval contains <paramref name="point" />, without enumerating the matches.
    /// </summary>
    /// <param name="point">The point to test. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if at least one stored interval contains the point; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is the early-exit form of <see cref="QueryPoint" /> — a single O(log n) descent regardless of how many
    /// entries match.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="point" /> is <see langword="null" />.</exception>
    public bool IntersectsPoint(TKey point)
    {
        ThrowHelper.ThrowIfNull(point);

        return IntersectsCore(point, point);
    }

    /// <summary>
    /// Determines whether any stored interval intersects the closed window [<paramref name="low" />,
    /// <paramref name="high" />], without enumerating the matches.
    /// </summary>
    /// <param name="low">The inclusive lower edge of the window. Must not be <see langword="null" />.</param>
    /// <param name="high">The inclusive upper edge of the window. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if at least one stored interval overlaps the window; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is the early-exit form of <see cref="QueryOverlaps" /> — a single O(log n) descent regardless of how many
    /// entries match.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="low" /> or <paramref name="high" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="low" /> orders after <paramref name="high" /> under the active comparer.
    /// </exception>
    public bool Intersects(TKey low, TKey high)
    {
        ThrowHelper.ThrowIfNull(low);
        ThrowHelper.ThrowIfNull(high);
        if (_comparer.Compare(low, high) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(low));

        return IntersectsCore(low, high);
    }

    /// <summary>
    /// Performs the classic single-descent interval search for any interval overlapping [<paramref name="low" />,
    /// <paramref name="high" />].
    /// </summary>
    /// <param name="low">The inclusive lower edge of the window.</param>
    /// <param name="high">The inclusive upper edge of the window.</param>
    /// <returns>
    /// <see langword="true" /> when an overlapping interval exists; otherwise, <see langword="false" />.
    /// </returns>
    private bool IntersectsCore(TKey low, TKey high) =>
        IntervalTreeCore.Intersects(_root, low, high, _comparer);

    /// <summary>
    /// Iterates the entries overlapping [<paramref name="low" />, <paramref name="high" />] via a pruned parent-pointer
    /// in-order walk with fail-fast version checking.
    /// </summary>
    /// <param name="low">The inclusive lower edge of the window.</param>
    /// <param name="high">The inclusive upper edge of the window.</param>
    /// <returns>
    /// The overlapping entries in ascending (low, high) order, values in insertion order per interval.
    /// </returns>
    private IEnumerable<(TKey Low, TKey High, TValue Value)> EnumerateOverlaps(TKey low, TKey high)
    {
        int version = _version;

        for (Node? node = _root == null ? null : PrunedMinimum(_root, low); node != null; node = PrunedSuccessor(node, low))
        {
            // In-order lows are non-decreasing, so once a node starts past the window nothing later can overlap.
            if (_comparer.Compare(node.Low, high) > 0)
                yield break;

            if (_comparer.Compare(node.High, low) >= 0)
            {
                for (int index = 0; index < node.ValueCount; index++)
                {
                    yield return (node.Low, node.High, node.GetValueAt(index));

                    if (version != _version)
                        throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);
                }
            }
        }
    }

    /// <summary>
    /// Descends to the leftmost node of the subtree rooted at <paramref name="node" /> whose left branches can still
    /// reach the window's low edge, skipping left subtrees whose <see cref="IntervalNode{TEndpoint, TNode}.Max" />
    /// falls short.
    /// </summary>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <param name="low">The inclusive lower edge of the query window.</param>
    /// <returns>The first node of the pruned in-order walk of the subtree.</returns>
    private Node PrunedMinimum(Node node, TKey low) =>
        IntervalTreeCore.PrunedMinimum(node, low, _comparer);

    /// <summary>
    /// Advances the pruned in-order walk from <paramref name="node" /> via parent pointers, entering a right subtree
    /// only when its <see cref="IntervalNode{TEndpoint, TNode}.Max" /> can still reach the window's low edge.
    /// </summary>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <param name="low">The inclusive lower edge of the query window.</param>
    /// <returns>The next node of the pruned walk, or <see langword="null" /> when the walk is exhausted.</returns>
    private Node? PrunedSuccessor(Node node, TKey low) =>
        IntervalTreeCore.PrunedSuccessor(node, low, _comparer);
}
