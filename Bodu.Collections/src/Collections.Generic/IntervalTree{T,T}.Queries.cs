// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTree{T,T}.Queries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    private bool IntersectsCore(TKey low, TKey high)
    {
        Node? current = _root;
        while (current != null)
        {
            if (_comparer.Compare(current.Low, high) <= 0 && _comparer.Compare(current.High, low) >= 0)
                return true;

            // If the left subtree's Max reaches the window it is guaranteed to hold any existing overlap on this
            // path; otherwise no left descendant can match and the search continues right.
            current = current.Left != null && _comparer.Compare(current.Left.Max, low) >= 0
                ? current.Left
                : current.Right;
        }

        return false;
    }

    /// <summary>
    /// Iterates the entries overlapping [<paramref name="low" />, <paramref name="high" />] via a pruned in-order walk
    /// with fail-fast version checking.
    /// </summary>
    /// <param name="low">The inclusive lower edge of the window.</param>
    /// <param name="high">The inclusive upper edge of the window.</param>
    /// <returns>
    /// The overlapping entries in ascending (low, high) order, values in insertion order per interval.
    /// </returns>
    private IEnumerable<(TKey Low, TKey High, TValue Value)> EnumerateOverlaps(TKey low, TKey high)
    {
        int version = _version;
        var stack = new Stack<Node>();
        Node? current = _root;

        while (current != null || stack.Count > 0)
        {
            if (current != null)
            {
                stack.Push(current);

                // Descend left only while the left subtree's Max can still reach the window's low edge.
                current = current.Left != null && _comparer.Compare(current.Left.Max, low) >= 0
                    ? current.Left
                    : null;
                continue;
            }

            Node node = stack.Pop();

            // In-order lows are non-decreasing, so once a node starts past the window nothing later can overlap.
            if (_comparer.Compare(node.Low, high) > 0)
                yield break;

            if (_comparer.Compare(node.High, low) >= 0)
            {
                for (int index = 0; index < node.Values.Count; index++)
                {
                    yield return (node.Low, node.High, node.Values[index]);

                    if (version != _version)
                        throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);
                }
            }

            current = node.Right;
            if (current != null && _comparer.Compare(current.Max, low) < 0)
                current = null;
        }
    }
}
