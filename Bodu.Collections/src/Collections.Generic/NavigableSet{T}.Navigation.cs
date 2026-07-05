// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSet{T}.Navigation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class NavigableSet<T>
{
    /// <summary>
    /// Gets the smallest element in the set.
    /// </summary>
    /// <value>The minimum element under the active comparer.</value>
    /// <exception cref="InvalidOperationException">The set is empty.</exception>
    public T Min =>
        _root == null
            ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionEmpty)
            : MinimumNode(_root).Item;

    /// <summary>
    /// Gets the largest element in the set.
    /// </summary>
    /// <value>The maximum element under the active comparer.</value>
    /// <exception cref="InvalidOperationException">The set is empty.</exception>
    public T Max =>
        _root == null
            ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionEmpty)
            : MaximumNode(_root).Item;

    /// <summary>
    /// Attempts to get the smallest element in the set.
    /// </summary>
    /// <param name="min">When this method returns <see langword="true" />, the minimum element.</param>
    /// <returns><see langword="true" /> if the set is non-empty; otherwise, <see langword="false" />.</returns>
    public bool TryGetMin(out T min)
    {
        if (_root == null)
        {
            min = default!;
            return false;
        }

        min = MinimumNode(_root).Item;
        return true;
    }

    /// <summary>
    /// Attempts to get the largest element in the set.
    /// </summary>
    /// <param name="max">When this method returns <see langword="true" />, the maximum element.</param>
    /// <returns><see langword="true" /> if the set is non-empty; otherwise, <see langword="false" />.</returns>
    public bool TryGetMax(out T max)
    {
        if (_root == null)
        {
            max = default!;
            return false;
        }

        max = MaximumNode(_root).Item;
        return true;
    }

    /// <summary>
    /// Attempts to get the greatest element less than or equal to <paramref name="value" />.
    /// </summary>
    /// <param name="value">The reference value. Must not be <see langword="null" />.</param>
    /// <param name="floor">When this method returns <see langword="true" />, the floor element.</param>
    /// <returns><see langword="true" /> if a floor exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public bool TryGetFloor(T value, out T floor)
    {
        ThrowHelper.ThrowIfNull(value);

        Node? node = FindFloorNode(value);
        if (node == null)
        {
            floor = default!;
            return false;
        }

        floor = node.Item;
        return true;
    }

    /// <summary>
    /// Attempts to get the least element greater than or equal to <paramref name="value" />.
    /// </summary>
    /// <param name="value">The reference value. Must not be <see langword="null" />.</param>
    /// <param name="ceiling">When this method returns <see langword="true" />, the ceiling element.</param>
    /// <returns><see langword="true" /> if a ceiling exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public bool TryGetCeiling(T value, out T ceiling)
    {
        ThrowHelper.ThrowIfNull(value);

        Node? node = FindCeilingNode(value);
        if (node == null)
        {
            ceiling = default!;
            return false;
        }

        ceiling = node.Item;
        return true;
    }

    /// <summary>
    /// Attempts to get the least element strictly greater than <paramref name="value" />.
    /// </summary>
    /// <param name="value">The reference value. Must not be <see langword="null" />.</param>
    /// <param name="higher">When this method returns <see langword="true" />, the next-higher element.</param>
    /// <returns><see langword="true" /> if a higher element exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public bool TryGetHigher(T value, out T higher)
    {
        ThrowHelper.ThrowIfNull(value);

        Node? node = FindHigherNode(value);
        if (node == null)
        {
            higher = default!;
            return false;
        }

        higher = node.Item;
        return true;
    }

    /// <summary>
    /// Attempts to get the greatest element strictly less than <paramref name="value" />.
    /// </summary>
    /// <param name="value">The reference value. Must not be <see langword="null" />.</param>
    /// <param name="lower">When this method returns <see langword="true" />, the next-lower element.</param>
    /// <returns><see langword="true" /> if a lower element exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public bool TryGetLower(T value, out T lower)
    {
        ThrowHelper.ThrowIfNull(value);

        Node? node = FindLowerNode(value);
        if (node == null)
        {
            lower = default!;
            return false;
        }

        lower = node.Item;
        return true;
    }

    /// <summary>
    /// Returns the zero-based rank of <paramref name="value" /> in ascending comparer order.
    /// </summary>
    /// <param name="value">The element to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// The number of elements smaller than <paramref name="value" />, or <c>-1</c> if it is not present.
    /// </returns>
    /// <remarks>
    /// Runs in O(log n) by accumulating subtree sizes along the search path.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public int IndexOf(T value)
    {
        ThrowHelper.ThrowIfNull(value);

        Node? node = _root;
        int rank = 0;

        while (node != null)
        {
            int comparison = _comparer.Compare(value, node.Item);
            if (comparison < 0)
            {
                node = node.Left;
            }
            else if (comparison > 0)
            {
                rank += SizeOf(node.Left) + 1;
                node = node.Right;
            }
            else
            {
                return rank + SizeOf(node.Left);
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns the element at the specified zero-based rank — the k-th smallest element.
    /// </summary>
    /// <param name="rank">The zero-based rank of the element to select.</param>
    /// <returns>The element whose rank is <paramref name="rank" />.</returns>
    /// <remarks>
    /// Runs in O(log n) by descending on subtree sizes.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="rank" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    public T GetAt(int rank)
    {
        if ((uint)rank >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(rank));

        Node node = _root!;
        while (true)
        {
            int leftSize = SizeOf(node.Left);
            if (rank < leftSize)
            {
                node = node.Left!;
            }
            else if (rank == leftSize)
            {
                return node.Item;
            }
            else
            {
                rank -= leftSize + 1;
                node = node.Right!;
            }
        }
    }

    /// <summary>
    /// Returns the number of elements within the inclusive range [<paramref name="lowInclusive" />,
    /// <paramref name="highInclusive" />].
    /// </summary>
    /// <param name="lowInclusive">The inclusive lower bound. Must not be <see langword="null" />.</param>
    /// <param name="highInclusive">The inclusive upper bound. Must not be <see langword="null" />.</param>
    /// <returns>
    /// The count of stored elements no smaller than the lower bound and no larger than the upper bound.
    /// </returns>
    /// <remarks>
    /// Runs in O(log n) as a subtraction of two rank walks; neither bound needs to be present in the set.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="lowInclusive" /> or <paramref name="highInclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="lowInclusive" /> orders after <paramref name="highInclusive" /> under the active comparer.
    /// </exception>
    public int CountInRange(T lowInclusive, T highInclusive)
    {
        ThrowHelper.ThrowIfNull(lowInclusive);
        ThrowHelper.ThrowIfNull(highInclusive);
        if (_comparer.Compare(lowInclusive, highInclusive) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(lowInclusive));

        return CountBefore(highInclusive, inclusive: true) - CountBefore(lowInclusive, inclusive: false);
    }

    /// <summary>
    /// Counts the elements ordered before <paramref name="value" />, optionally including a comparer-equal element.
    /// </summary>
    /// <param name="value">The boundary value.</param>
    /// <param name="inclusive"><see langword="true" /> to count a comparer-equal element as well.</param>
    /// <returns>The number of qualifying elements.</returns>
    private int CountBefore(T value, bool inclusive)
    {
        Node? node = _root;
        int count = 0;

        while (node != null)
        {
            int comparison = _comparer.Compare(value, node.Item);
            if (comparison < 0)
            {
                node = node.Left;
            }
            else if (comparison > 0)
            {
                count += SizeOf(node.Left) + 1;
                node = node.Right;
            }
            else
            {
                count += SizeOf(node.Left) + (inclusive ? 1 : 0);
                break;
            }
        }

        return count;
    }

    /// <summary>
    /// Finds the node holding the greatest element less than or equal to <paramref name="value" />.
    /// </summary>
    /// <param name="value">The reference value.</param>
    /// <returns>The floor node, or <see langword="null" />.</returns>
    private Node? FindFloorNode(T value)
    {
        Node? node = _root;
        Node? best = null;

        while (node != null)
        {
            int comparison = _comparer.Compare(value, node.Item);
            if (comparison == 0)
                return node;

            if (comparison > 0)
            {
                best = node;
                node = node.Right;
            }
            else
            {
                node = node.Left;
            }
        }

        return best;
    }

    /// <summary>
    /// Finds the node holding the least element greater than or equal to <paramref name="value" />.
    /// </summary>
    /// <param name="value">The reference value.</param>
    /// <returns>The ceiling node, or <see langword="null" />.</returns>
    private Node? FindCeilingNode(T value)
    {
        Node? node = _root;
        Node? best = null;

        while (node != null)
        {
            int comparison = _comparer.Compare(value, node.Item);
            if (comparison == 0)
                return node;

            if (comparison < 0)
            {
                best = node;
                node = node.Left;
            }
            else
            {
                node = node.Right;
            }
        }

        return best;
    }

    /// <summary>
    /// Finds the node holding the least element strictly greater than <paramref name="value" />.
    /// </summary>
    /// <param name="value">The reference value.</param>
    /// <returns>The next-higher node, or <see langword="null" />.</returns>
    private Node? FindHigherNode(T value)
    {
        Node? node = _root;
        Node? best = null;

        while (node != null)
        {
            if (_comparer.Compare(value, node.Item) < 0)
            {
                best = node;
                node = node.Left;
            }
            else
            {
                node = node.Right;
            }
        }

        return best;
    }

    /// <summary>
    /// Finds the node holding the greatest element strictly less than <paramref name="value" />.
    /// </summary>
    /// <param name="value">The reference value.</param>
    /// <returns>The next-lower node, or <see langword="null" />.</returns>
    private Node? FindLowerNode(T value)
    {
        Node? node = _root;
        Node? best = null;

        while (node != null)
        {
            if (_comparer.Compare(value, node.Item) > 0)
            {
                best = node;
                node = node.Right;
            }
            else
            {
                node = node.Left;
            }
        }

        return best;
    }
}
