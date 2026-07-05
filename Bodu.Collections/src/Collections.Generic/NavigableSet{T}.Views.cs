// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSet{T}.Views.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class NavigableSet<T>
{
    /// <summary>
    /// Returns a live view of the set in ascending comparer order.
    /// </summary>
    /// <returns>A lazily evaluated sequence over the current elements, smallest first.</returns>
    /// <remarks>
    /// <para>
    /// The view is live: each fresh iteration re-resolves against the set's current state, so mutations made after the
    /// view was obtained are reflected the next time it is iterated. Within a single iteration the view is fail-fast —
    /// any structural mutation causes the next advance to throw <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    public IEnumerable<T> Ascending() =>
        EnumerateAscending();

    /// <summary>
    /// Returns a live view of the set in descending comparer order.
    /// </summary>
    /// <returns>A lazily evaluated sequence over the current elements, largest first.</returns>
    /// <remarks>
    /// <para>
    /// The view is live: each fresh iteration re-resolves against the set's current state, so mutations made after the
    /// view was obtained are reflected the next time it is iterated. Within a single iteration the view is fail-fast —
    /// any structural mutation causes the next advance to throw <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    public IEnumerable<T> Descending() =>
        EnumerateDescending();

    /// <summary>
    /// Returns a live view of the elements within the inclusive range [<paramref name="lowInclusive" />,
    /// <paramref name="highInclusive" />] in ascending comparer order.
    /// </summary>
    /// <param name="lowInclusive">The inclusive lower bound. Must not be <see langword="null" />.</param>
    /// <param name="highInclusive">The inclusive upper bound. Must not be <see langword="null" />.</param>
    /// <returns>A lazily evaluated sequence over the in-range elements, smallest first.</returns>
    /// <remarks>
    /// <para>
    /// The bounds need not be present in the set. The walk descends directly to the first in-range element and stops
    /// past the last, so iterating costs O(log n + k) for k yielded elements.
    /// </para>
    /// <para>
    /// The view is live over the bound pair: each fresh iteration re-resolves against the set's current state. Within a
    /// single iteration the view is fail-fast — any structural mutation causes the next advance to throw
    /// <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="lowInclusive" /> or <paramref name="highInclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="lowInclusive" /> orders after <paramref name="highInclusive" /> under the active comparer.
    /// </exception>
    public IEnumerable<T> Range(T lowInclusive, T highInclusive)
    {
        ThrowHelper.ThrowIfNull(lowInclusive);
        ThrowHelper.ThrowIfNull(highInclusive);
        if (_comparer.Compare(lowInclusive, highInclusive) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(lowInclusive));

        return EnumerateRange(lowInclusive, highInclusive);
    }

    /// <summary>
    /// Iterates the tree in ascending order with fail-fast version checking.
    /// </summary>
    /// <returns>The ascending element sequence.</returns>
    private IEnumerable<T> EnumerateAscending()
    {
        int version = _version;
        Node? node = _root == null ? null : MinimumNode(_root);

        while (node != null)
        {
            yield return node.Item;

            if (version != _version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            node = SuccessorNode(node);
        }
    }

    /// <summary>
    /// Iterates the tree in descending order with fail-fast version checking.
    /// </summary>
    /// <returns>The descending element sequence.</returns>
    private IEnumerable<T> EnumerateDescending()
    {
        int version = _version;
        Node? node = _root == null ? null : MaximumNode(_root);

        while (node != null)
        {
            yield return node.Item;

            if (version != _version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            node = PredecessorNode(node);
        }
    }

    /// <summary>
    /// Iterates the in-range subwalk in ascending order with fail-fast version checking.
    /// </summary>
    /// <param name="lowInclusive">The inclusive lower bound.</param>
    /// <param name="highInclusive">The inclusive upper bound.</param>
    /// <returns>The in-range element sequence.</returns>
    private IEnumerable<T> EnumerateRange(T lowInclusive, T highInclusive)
    {
        int version = _version;
        Node? node = FindCeilingNode(lowInclusive);

        while (node != null && _comparer.Compare(node.Item, highInclusive) <= 0)
        {
            yield return node.Item;

            if (version != _version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            node = SuccessorNode(node);
        }
    }
}
