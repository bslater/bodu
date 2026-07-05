// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionary{T,T}.Navigation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class NavigableDictionary<TKey, TValue>
{
    /// <summary>
    /// Gets the entry with the smallest key in the dictionary.
    /// </summary>
    /// <value>The minimum-key entry under the active comparer.</value>
    /// <exception cref="InvalidOperationException">The dictionary is empty.</exception>
    public KeyValuePair<TKey, TValue> MinEntry =>
        _root == null
            ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionEmpty)
            : MinimumNode(_root).Entry;

    /// <summary>
    /// Gets the entry with the largest key in the dictionary.
    /// </summary>
    /// <value>The maximum-key entry under the active comparer.</value>
    /// <exception cref="InvalidOperationException">The dictionary is empty.</exception>
    public KeyValuePair<TKey, TValue> MaxEntry =>
        _root == null
            ? throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionEmpty)
            : MaximumNode(_root).Entry;

    /// <summary>
    /// Attempts to get the entry with the smallest key in the dictionary.
    /// </summary>
    /// <param name="entry">When this method returns <see langword="true" />, the minimum-key entry.</param>
    /// <returns><see langword="true" /> if the dictionary is non-empty; otherwise, <see langword="false" />.</returns>
    public bool TryGetMinEntry(out KeyValuePair<TKey, TValue> entry)
    {
        if (_root == null)
        {
            entry = default;
            return false;
        }

        entry = MinimumNode(_root).Entry;
        return true;
    }

    /// <summary>
    /// Attempts to get the entry with the largest key in the dictionary.
    /// </summary>
    /// <param name="entry">When this method returns <see langword="true" />, the maximum-key entry.</param>
    /// <returns><see langword="true" /> if the dictionary is non-empty; otherwise, <see langword="false" />.</returns>
    public bool TryGetMaxEntry(out KeyValuePair<TKey, TValue> entry)
    {
        if (_root == null)
        {
            entry = default;
            return false;
        }

        entry = MaximumNode(_root).Entry;
        return true;
    }

    /// <summary>
    /// Attempts to get the entry with the greatest key less than or equal to <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key. Must not be <see langword="null" />.</param>
    /// <param name="entry">When this method returns <see langword="true" />, the floor entry.</param>
    /// <returns><see langword="true" /> if a floor entry exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetFloorEntry(TKey key, out KeyValuePair<TKey, TValue> entry)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindFloorNode(key);
        if (node == null)
        {
            entry = default;
            return false;
        }

        entry = node.Entry;
        return true;
    }

    /// <summary>
    /// Attempts to get the entry with the least key greater than or equal to <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key. Must not be <see langword="null" />.</param>
    /// <param name="entry">When this method returns <see langword="true" />, the ceiling entry.</param>
    /// <returns><see langword="true" /> if a ceiling entry exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetCeilingEntry(TKey key, out KeyValuePair<TKey, TValue> entry)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindCeilingNode(key);
        if (node == null)
        {
            entry = default;
            return false;
        }

        entry = node.Entry;
        return true;
    }

    /// <summary>
    /// Attempts to get the entry with the least key strictly greater than <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key. Must not be <see langword="null" />.</param>
    /// <param name="entry">When this method returns <see langword="true" />, the next-higher entry.</param>
    /// <returns><see langword="true" /> if a higher entry exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetHigherEntry(TKey key, out KeyValuePair<TKey, TValue> entry)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindHigherNode(key);
        if (node == null)
        {
            entry = default;
            return false;
        }

        entry = node.Entry;
        return true;
    }

    /// <summary>
    /// Attempts to get the entry with the greatest key strictly less than <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key. Must not be <see langword="null" />.</param>
    /// <param name="entry">When this method returns <see langword="true" />, the next-lower entry.</param>
    /// <returns><see langword="true" /> if a lower entry exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetLowerEntry(TKey key, out KeyValuePair<TKey, TValue> entry)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindLowerNode(key);
        if (node == null)
        {
            entry = default;
            return false;
        }

        entry = node.Entry;
        return true;
    }

    /// <summary>
    /// Attempts to get the greatest key less than or equal to <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key. Must not be <see langword="null" />.</param>
    /// <param name="floorKey">When this method returns <see langword="true" />, the floor key.</param>
    /// <returns><see langword="true" /> if a floor key exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetFloorKey(TKey key, out TKey floorKey)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindFloorNode(key);
        if (node == null)
        {
            floorKey = default!;
            return false;
        }

        floorKey = node.Key;
        return true;
    }

    /// <summary>
    /// Attempts to get the least key greater than or equal to <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key. Must not be <see langword="null" />.</param>
    /// <param name="ceilingKey">When this method returns <see langword="true" />, the ceiling key.</param>
    /// <returns><see langword="true" /> if a ceiling key exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetCeilingKey(TKey key, out TKey ceilingKey)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindCeilingNode(key);
        if (node == null)
        {
            ceilingKey = default!;
            return false;
        }

        ceilingKey = node.Key;
        return true;
    }

    /// <summary>
    /// Attempts to get the least key strictly greater than <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key. Must not be <see langword="null" />.</param>
    /// <param name="higherKey">When this method returns <see langword="true" />, the next-higher key.</param>
    /// <returns><see langword="true" /> if a higher key exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetHigherKey(TKey key, out TKey higherKey)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindHigherNode(key);
        if (node == null)
        {
            higherKey = default!;
            return false;
        }

        higherKey = node.Key;
        return true;
    }

    /// <summary>
    /// Attempts to get the greatest key strictly less than <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key. Must not be <see langword="null" />.</param>
    /// <param name="lowerKey">When this method returns <see langword="true" />, the next-lower key.</param>
    /// <returns><see langword="true" /> if a lower key exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetLowerKey(TKey key, out TKey lowerKey)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = FindLowerNode(key);
        if (node == null)
        {
            lowerKey = default!;
            return false;
        }

        lowerKey = node.Key;
        return true;
    }

    /// <summary>
    /// Returns the zero-based rank of <paramref name="key" /> in ascending comparer order.
    /// </summary>
    /// <param name="key">The key to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// The number of stored keys smaller than <paramref name="key" />, or <c>-1</c> if it is not present.
    /// </returns>
    /// <remarks>
    /// Runs in O(log n) by accumulating subtree sizes along the search path.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public int IndexOfKey(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        Node? node = _root;
        int rank = 0;

        while (node != null)
        {
            int comparison = _comparer.Compare(key, node.Key);
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
    /// Returns the entry at the specified zero-based rank — the entry with the k-th smallest key.
    /// </summary>
    /// <param name="rank">The zero-based rank of the entry to select.</param>
    /// <returns>The entry whose key rank is <paramref name="rank" />.</returns>
    /// <remarks>
    /// Runs in O(log n) by descending on subtree sizes.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="rank" /> is negative or greater than or equal to <see cref="Count" />.
    /// </exception>
    public KeyValuePair<TKey, TValue> GetAt(int rank)
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
                return node.Entry;
            }
            else
            {
                rank -= leftSize + 1;
                node = node.Right!;
            }
        }
    }

    /// <summary>
    /// Returns the number of entries whose keys fall within the inclusive range [<paramref name="lowInclusive" />,
    /// <paramref name="highInclusive" />].
    /// </summary>
    /// <param name="lowInclusive">The inclusive lower key bound. Must not be <see langword="null" />.</param>
    /// <param name="highInclusive">The inclusive upper key bound. Must not be <see langword="null" />.</param>
    /// <returns>
    /// The count of stored entries whose keys are no smaller than the lower bound and no larger than the upper bound.
    /// </returns>
    /// <remarks>
    /// Runs in O(log n) as a subtraction of two rank walks; neither bound needs to be present in the dictionary.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="lowInclusive" /> or <paramref name="highInclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="lowInclusive" /> orders after <paramref name="highInclusive" /> under the active comparer.
    /// </exception>
    public int CountInRange(TKey lowInclusive, TKey highInclusive)
    {
        ThrowHelper.ThrowIfNull(lowInclusive);
        ThrowHelper.ThrowIfNull(highInclusive);
        if (_comparer.Compare(lowInclusive, highInclusive) > 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_RangeLowerBoundExceedsUpperBound, nameof(lowInclusive));

        return CountBefore(highInclusive, inclusive: true) - CountBefore(lowInclusive, inclusive: false);
    }

    /// <summary>
    /// Counts the entries whose keys order before <paramref name="key" />, optionally including a comparer-equal key.
    /// </summary>
    /// <param name="key">The boundary key.</param>
    /// <param name="inclusive"><see langword="true" /> to count a comparer-equal key as well.</param>
    /// <returns>The number of qualifying entries.</returns>
    private int CountBefore(TKey key, bool inclusive)
    {
        Node? node = _root;
        int count = 0;

        while (node != null)
        {
            int comparison = _comparer.Compare(key, node.Key);
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
    /// Finds the node holding the greatest key less than or equal to <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key.</param>
    /// <returns>The floor node, or <see langword="null" />.</returns>
    private Node? FindFloorNode(TKey key)
    {
        Node? node = _root;
        Node? best = null;

        while (node != null)
        {
            int comparison = _comparer.Compare(key, node.Key);
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
    /// Finds the node holding the least key greater than or equal to <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key.</param>
    /// <returns>The ceiling node, or <see langword="null" />.</returns>
    private Node? FindCeilingNode(TKey key)
    {
        Node? node = _root;
        Node? best = null;

        while (node != null)
        {
            int comparison = _comparer.Compare(key, node.Key);
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
    /// Finds the node holding the least key strictly greater than <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key.</param>
    /// <returns>The next-higher node, or <see langword="null" />.</returns>
    private Node? FindHigherNode(TKey key)
    {
        Node? node = _root;
        Node? best = null;

        while (node != null)
        {
            if (_comparer.Compare(key, node.Key) < 0)
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
    /// Finds the node holding the greatest key strictly less than <paramref name="key" />.
    /// </summary>
    /// <param name="key">The reference key.</param>
    /// <returns>The next-lower node, or <see langword="null" />.</returns>
    private Node? FindLowerNode(TKey key)
    {
        Node? node = _root;
        Node? best = null;

        while (node != null)
        {
            if (_comparer.Compare(key, node.Key) > 0)
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
