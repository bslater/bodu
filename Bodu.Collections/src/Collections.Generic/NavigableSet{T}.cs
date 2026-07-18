// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSet{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a sorted set augmented with order statistics — an <see cref="ISet{T}" /> that keeps its elements in
/// comparer order and answers nearest-neighbour (<see cref="TryGetFloor" /> / <see cref="TryGetCeiling" /> /
/// <see cref="TryGetHigher" /> / <see cref="TryGetLower" />), rank/select (<see cref="IndexOf" /> /
/// <see cref="GetAt" />), and range-counting queries in O(log n).
/// </summary>
/// <typeparam name="T">The type of elements in the set. Elements must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// <see cref="NavigableSet{T}" /> is backed by an order-statistic red-black tree: every node carries the size of its
/// subtree, maintained through all rotations, so positional queries read only size fields on a root-to-node path. See
/// <c>Bodu.Collections/docs/navigable-collections-design.md</c> for the backing-structure decision record.
/// </para>
/// <para>
/// Compared with <see cref="SortedSet{T}" />, this type adds rank/select (<see cref="IndexOf" /> is the zero-based rank
/// of an element; <see cref="GetAt" /> is the k-th smallest element), O(log n) <see cref="CountInRange" />, the four
/// nearest-neighbour queries, and cheap <see cref="Ascending" /> / <see cref="Descending" /> / <see cref="Range" />
/// views. Unlike <see cref="SortedSet{T}" />, <see langword="null" /> elements are rejected, consistent with the rest
/// of the Bodu collection family.
/// </para>
/// <para>
/// Elements that the comparer orders equal are treated as duplicates: <see cref="Add" /> returns
/// <see langword="false" /> and preserves the stored element.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var prices = new NavigableSet<decimal>();
/// prices.Add(10.00m);
/// prices.Add(10.25m);
/// prices.Add(10.50m);
///
/// prices.TryGetFloor(10.30m, out decimal floor);    // 10.25 — greatest element <= 10.30
/// prices.TryGetCeiling(10.30m, out decimal ceiling);// 10.50 — least element >= 10.30
///
/// int rank = prices.IndexOf(10.25m);                // 1 — zero-based rank in sorted order
/// decimal median = prices.GetAt(prices.Count / 2);  // 10.25 — k-th smallest
/// int inBand = prices.CountInRange(10.00m, 10.30m); // 2 — O(log n), no iteration
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(NavigableSetDebugView<>))]
[Serializable]
public sealed partial class NavigableSet<T>
    : ISet<T>, IReadOnlyCollection<T>
    where T : notnull
{
    /// <summary>The comparer that defines the element ordering.</summary>
    private readonly IComparer<T> _comparer;

    /// <summary>The root node of the red-black tree, or <see langword="null" /> when the set is empty.</summary>
    private Node? _root;

    /// <summary>The number of elements currently stored in the set.</summary>
    private int _count;

    /// <summary>The structural version, incremented by every mutation to fail-fast in-flight enumerations.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableSet{T}" /> class using the default comparer.
    /// </summary>
    public NavigableSet()
        : this((IComparer<T>?)null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableSet{T}" /> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">The ordering comparer, or <see langword="null" /> to use the default comparer.</param>
    public NavigableSet(IComparer<T>? comparer)
    {
        _comparer = comparer ?? Comparer<T>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableSet{T}" /> class containing the distinct elements from
    /// <paramref name="source" />, bulk-loaded into a balanced tree.
    /// </summary>
    /// <param name="source">The source collection. Must not be <see langword="null" />.</param>
    /// <param name="comparer">The ordering comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <remarks>
    /// The source is sorted and deduplicated (comparer-equal duplicates keep the first occurrence), then built directly
    /// into a balanced tree — O(n log n) overall, O(n) after the sort.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> contains a <see langword="null" /> element.
    /// </exception>
    public NavigableSet(IEnumerable<T> source, IComparer<T>? comparer = null)
        : this(comparer)
    {
        ThrowHelper.ThrowIfNull(source);

        T[] items = source.ToArray();
        foreach (T item in items)
        {
            if (item is null) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_NullCollectionElement, nameof(source));
        }

        if (items.Length == 0)
            return;

        Array.Sort(items, _comparer);

        // Deduplicate in place, keeping the first occurrence of each comparer-equal run.
        int unique = 1;
        for (int i = 1; i < items.Length; i++)
        {
            if (_comparer.Compare(items[i], items[unique - 1]) != 0)
                items[unique++] = items[i];
        }

        _root = BuildFromSortedArray(items, unique);
        _count = unique;
    }

    /// <summary>
    /// Gets the comparer that defines the element ordering.
    /// </summary>
    /// <value>The active ordering comparer.</value>
    public IComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    /// <value>The number of elements currently stored in the set.</value>
    public int Count => _count;

    /// <summary>
    /// Gets a value indicating whether the set is read-only.
    /// </summary>
    /// <value>Always <see langword="false" />.</value>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds the specified item to the set.
    /// </summary>
    /// <param name="item">The item to add. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the item was added; otherwise, <see langword="false" /> when the set already contains
    /// a comparer-equal element.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool Add(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        Node? parent = null;
        Node? current = _root;
        int comparison = 0;

        while (current != null)
        {
            comparison = _comparer.Compare(item, current.Item);
            if (comparison == 0)
                return false;

            parent = current;
            current = comparison < 0 ? current.Left : current.Right;
        }

        var node = new Node(item) { Parent = parent };

        if (parent == null)
            _root = node;
        else if (comparison < 0)
            parent.Left = node;
        else
            parent.Right = node;

        for (Node? ancestor = parent; ancestor != null; ancestor = ancestor.Parent)
            ancestor.Size++;

        FixAfterInsertion(node);

        _count++;
        _version++;
        return true;
    }

    /// <summary>
    /// Removes the specified item from the set.
    /// </summary>
    /// <param name="item">The item to remove. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the item was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool Remove(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        Node? node = FindNode(item);
        if (node == null)
            return false;

        RemoveNode(node);

        _count--;
        _version++;
        return true;
    }

    /// <summary>
    /// Determines whether the set contains the specified item.
    /// </summary>
    /// <param name="item">The item to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if a comparer-equal element exists; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool Contains(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        return FindNode(item) != null;
    }

    /// <summary>
    /// Removes all elements from the set.
    /// </summary>
    public void Clear()
    {
        _root = null;
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Copies the elements to the specified array in ascending order, starting at <paramref name="arrayIndex" />.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The destination start index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> does not have enough space starting at <paramref name="arrayIndex" />.
    /// </exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, _count);

        int index = arrayIndex;
        foreach (T item in this)
            array[index++] = item;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the set in ascending comparer order.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the set elements.</returns>
    public Enumerator GetEnumerator() =>
        new(this);
}
