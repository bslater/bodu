// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionary{T,T}.Views.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class NavigableDictionary<TKey, TValue>
{
    /// <summary>
    /// Returns a live view of the entries in ascending key order.
    /// </summary>
    /// <returns>A lazily evaluated sequence over the current entries, smallest key first.</returns>
    /// <remarks>
    /// <para>
    /// The view is live: each fresh iteration re-resolves against the dictionary's current state, so mutations made
    /// after the view was obtained are reflected the next time it is iterated. Within a single iteration the view is
    /// fail-fast — any structural mutation causes the next advance to throw <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    public IEnumerable<KeyValuePair<TKey, TValue>> Ascending() =>
        EnumerateAscending();

    /// <summary>
    /// Returns a live view of the entries in descending key order.
    /// </summary>
    /// <returns>A lazily evaluated sequence over the current entries, largest key first.</returns>
    /// <remarks>
    /// <para>
    /// The view is live: each fresh iteration re-resolves against the dictionary's current state, so mutations made
    /// after the view was obtained are reflected the next time it is iterated. Within a single iteration the view is
    /// fail-fast — any structural mutation causes the next advance to throw <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    public IEnumerable<KeyValuePair<TKey, TValue>> Descending() =>
        EnumerateDescending();

    /// <summary>
    /// Returns a live view of the entries whose keys fall within the inclusive range [ <paramref name="lowInclusive" />,
    /// <paramref name="highInclusive" />] in ascending key order.
    /// </summary>
    /// <param name="lowInclusive">The inclusive lower key bound. Must not be <see langword="null" />.</param>
    /// <param name="highInclusive">The inclusive upper key bound. Must not be <see langword="null" />.</param>
    /// <returns>A lazily evaluated sequence over the in-range entries, smallest key first.</returns>
    /// <remarks>
    /// <para>
    /// The bounds need not be present in the dictionary. The walk descends directly to the first in-range entry and
    /// stops past the last, so iterating costs O(log n + k) for k yielded entries.
    /// </para>
    /// <para>
    /// The view is live over the bound pair: each fresh iteration re-resolves against the dictionary's current state.
    /// Within a single iteration the view is fail-fast — any structural mutation causes the next advance to throw
    /// <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="lowInclusive" /> or <paramref name="highInclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="lowInclusive" /> orders after <paramref name="highInclusive" /> under the active comparer.
    /// </exception>
    public IEnumerable<KeyValuePair<TKey, TValue>> Range(TKey lowInclusive, TKey highInclusive)
    {
        ThrowHelper.ThrowIfNull(lowInclusive);
        ThrowHelper.ThrowIfNull(highInclusive);
        if (_comparer.Compare(lowInclusive, highInclusive) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(lowInclusive));

        return EnumerateRange(lowInclusive, highInclusive);
    }

    /// <summary>
    /// Iterates the tree in ascending key order with fail-fast version checking.
    /// </summary>
    /// <returns>The ascending entry sequence.</returns>
    private IEnumerable<KeyValuePair<TKey, TValue>> EnumerateAscending()
    {
        int version = _version;
        Node? node = _root == null ? null : MinimumNode(_root);

        while (node != null)
        {
            yield return node.Entry;

            if (version != _version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            node = SuccessorNode(node);
        }
    }

    /// <summary>
    /// Iterates the tree in descending key order with fail-fast version checking.
    /// </summary>
    /// <returns>The descending entry sequence.</returns>
    private IEnumerable<KeyValuePair<TKey, TValue>> EnumerateDescending()
    {
        int version = _version;
        Node? node = _root == null ? null : MaximumNode(_root);

        while (node != null)
        {
            yield return node.Entry;

            if (version != _version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            node = PredecessorNode(node);
        }
    }

    /// <summary>
    /// Iterates the in-range subwalk in ascending key order with fail-fast version checking.
    /// </summary>
    /// <param name="lowInclusive">The inclusive lower key bound.</param>
    /// <param name="highInclusive">The inclusive upper key bound.</param>
    /// <returns>The in-range entry sequence.</returns>
    private IEnumerable<KeyValuePair<TKey, TValue>> EnumerateRange(TKey lowInclusive, TKey highInclusive)
    {
        int version = _version;
        Node? node = FindCeilingNode(lowInclusive);

        while (node != null && _comparer.Compare(node.Key, highInclusive) <= 0)
        {
            yield return node.Entry;

            if (version != _version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            node = SuccessorNode(node);
        }
    }
}
