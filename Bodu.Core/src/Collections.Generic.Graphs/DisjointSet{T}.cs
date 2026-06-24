// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisjointSet{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Provides an element-keyed disjoint-set (union-find) structure that tracks a partition of added elements into
/// disjoint subsets and supports near-constant-time union and connectivity queries.
/// </summary>
/// <typeparam name="T">The type of the elements. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// Elements are introduced with <see cref="MakeSet(T)" /> (or its alias <see cref="Add(T)" />), each starting in its
/// own singleton set. <see cref="Union(T, T)" /> merges the subsets containing two elements, and <see cref="Find(T)" />
/// returns the canonical representative of an element's subset. Two elements are connected exactly when they share a
/// representative.
/// </para>
/// <para>
/// The implementation combines <b>path-halving</b> compression with <b>union by size</b>, giving an amortized cost per
/// operation of O(α(n)), where α is the inverse Ackermann function (effectively constant). Element identity is
/// determined by the <see cref="IEqualityComparer{T}" /> supplied at construction, or
/// <see cref="EqualityComparer{T}.Default" /> when none is supplied.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}, Sets = {SetCount}")]
[DebuggerTypeProxy(typeof(DisjointSetDebugView<>))]
public sealed class DisjointSet<T>
    where T : notnull
{
    /// <summary>Maps each added element to its dense slot index in the backing lists.</summary>
    private readonly Dictionary<T, int> _indices;

    /// <summary>The elements in slot order, indexed by the dense slot assigned at insertion.</summary>
    private readonly List<T> _elements;

    /// <summary>The parent slot of each element, forming the union-find forest; a root references itself.</summary>
    private readonly List<int> _parent;

    /// <summary>The size of the subtree rooted at each slot; meaningful only for root slots.</summary>
    private readonly List<int> _size;

    /// <summary>The number of distinct disjoint sets currently represented.</summary>
    private int _setCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisjointSet{T}" /> class that is empty and uses the default
    /// equality comparer.
    /// </summary>
    public DisjointSet()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisjointSet{T}" /> class that is empty and uses the specified
    /// equality comparer.
    /// </summary>
    /// <param name="comparer">
    /// The comparer used to determine element identity, or <see langword="null" /> to use the default comparer.
    /// </param>
    public DisjointSet(IEqualityComparer<T>? comparer)
    {
        Comparer = comparer ?? EqualityComparer<T>.Default;
        _indices = new Dictionary<T, int>(Comparer);
        _elements = new List<T>();
        _parent = new List<int>();
        _size = new List<int>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisjointSet{T}" /> class containing the specified elements, each in
    /// its own singleton set.
    /// </summary>
    /// <param name="items">The elements to add.</param>
    /// <param name="comparer">
    /// The comparer used to determine element identity, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="items" /> is <see langword="null" />.</exception>
    public DisjointSet(IEnumerable<T> items, IEqualityComparer<T>? comparer = null)
        : this(comparer)
    {
        ThrowHelper.ThrowIfNull(items);

        foreach (T item in items)
            MakeSet(item);
    }

    /// <summary>
    /// Gets the total number of elements tracked by the structure.
    /// </summary>
    /// <value>The number of distinct elements added.</value>
    /// <returns>The total number of elements.</returns>
    public int Count => _indices.Count;

    /// <summary>
    /// Gets the number of disjoint sets currently represented.
    /// </summary>
    /// <value>The count of distinct subsets.</value>
    /// <returns>The number of disjoint sets.</returns>
    public int SetCount => _setCount;

    /// <summary>
    /// Gets the equality comparer used to determine element identity.
    /// </summary>
    /// <value>The comparer supplied at construction, or <see cref="EqualityComparer{T}.Default" />.</value>
    /// <returns>The equality comparer in use.</returns>
    public IEqualityComparer<T> Comparer { get; }

    /// <summary>
    /// Adds the specified element as a new singleton set.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <returns>
    /// <see langword="true" /> if the element was added; <see langword="false" /> if it was already present.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool MakeSet(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        if (_indices.ContainsKey(item))
            return false;

        int slot = _elements.Count;
        _indices.Add(item, slot);
        _elements.Add(item);
        _parent.Add(slot);
        _size.Add(1);
        _setCount++;
        return true;
    }

    /// <summary>
    /// Adds the specified element as a new singleton set. This is an alias for <see cref="MakeSet(T)" />.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <returns>
    /// <see langword="true" /> if the element was added; <see langword="false" /> if it was already present.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool Add(T item) =>
        MakeSet(item);

    /// <summary>
    /// Determines whether the specified element has been added.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns><see langword="true" /> if the element is present; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool Contains(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        return _indices.ContainsKey(item);
    }

    /// <summary>
    /// Returns the canonical representative of the set containing the specified element.
    /// </summary>
    /// <param name="item">The element whose representative is requested.</param>
    /// <returns>The representative element of the subset containing <paramref name="item" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="item" /> has not been added to the structure.</exception>
    public T Find(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        if (!_indices.TryGetValue(item, out int slot))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ElementNotInSet, nameof(item));

        return _elements[FindRoot(slot)];
    }

    /// <summary>
    /// Attempts to return the canonical representative of the set containing the specified element.
    /// </summary>
    /// <param name="item">The element whose representative is requested.</param>
    /// <param name="representative">
    /// When this method returns, contains the representative element, if found; otherwise, the default value.
    /// </param>
    /// <returns><see langword="true" /> if the element was found; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool TryFind(T item, out T representative)
    {
        ThrowHelper.ThrowIfNull(item);

        if (_indices.TryGetValue(item, out int slot))
        {
            representative = _elements[FindRoot(slot)];
            return true;
        }

        representative = default!;
        return false;
    }

    /// <summary>
    /// Merges the subsets containing the two specified elements.
    /// </summary>
    /// <param name="a">The first element.</param>
    /// <param name="b">The second element.</param>
    /// <returns>
    /// <see langword="true" /> if the elements were in different subsets and a merge occurred; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Either element is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Either element has not been added to the structure.</exception>
    public bool Union(T a, T b)
    {
        ThrowHelper.ThrowIfNull(a);
        ThrowHelper.ThrowIfNull(b);

        int rootA = FindRoot(GetSlot(a, nameof(a)));
        int rootB = FindRoot(GetSlot(b, nameof(b)));
        if (rootA == rootB)
            return false;

        if (_size[rootA] < _size[rootB])
            (rootA, rootB) = (rootB, rootA);

        _parent[rootB] = rootA;
        _size[rootA] += _size[rootB];
        _setCount--;
        return true;
    }

    /// <summary>
    /// Determines whether the two specified elements belong to the same subset.
    /// </summary>
    /// <param name="a">The first element.</param>
    /// <param name="b">The second element.</param>
    /// <returns>
    /// <see langword="true" /> if both elements share a representative; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Either element is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Either element has not been added to the structure.</exception>
    public bool AreConnected(T a, T b)
    {
        ThrowHelper.ThrowIfNull(a);
        ThrowHelper.ThrowIfNull(b);

        return FindRoot(GetSlot(a, nameof(a))) == FindRoot(GetSlot(b, nameof(b)));
    }

    /// <summary>
    /// Returns the number of elements in the subset containing the specified element.
    /// </summary>
    /// <param name="item">The element whose subset size is requested.</param>
    /// <returns>The number of elements in the subset containing <paramref name="item" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="item" /> has not been added to the structure.</exception>
    public int SizeOf(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        return _size[FindRoot(GetSlot(item, nameof(item)))];
    }

    /// <summary>
    /// Removes all elements, returning the structure to an empty state.
    /// </summary>
    public void Clear()
    {
        _indices.Clear();
        _elements.Clear();
        _parent.Clear();
        _size.Clear();
        _setCount = 0;
    }

    /// <summary>
    /// Produces a snapshot of the current partition, grouping elements by their representative. Intended for the
    /// debugger proxy.
    /// </summary>
    /// <returns>An array of subsets, each an array of the elements it contains.</returns>
    internal T[][] ToGroups()
    {
        var groups = new Dictionary<int, List<T>>();
        for (int slot = 0; slot < _elements.Count; slot++)
        {
            int root = FindRoot(slot);
            if (!groups.TryGetValue(root, out List<T>? members))
            {
                members = new List<T>();
                groups.Add(root, members);
            }

            members.Add(_elements[slot]);
        }

        var result = new T[groups.Count][];
        int index = 0;
        foreach (List<T> members in groups.Values)
            result[index++] = members.ToArray();

        return result;
    }

    /// <summary>
    /// Resolves the dense slot for an element, throwing when the element is unknown.
    /// </summary>
    /// <param name="item">The element to resolve.</param>
    /// <param name="paramName">The name of the parameter that supplied the element.</param>
    /// <returns>The dense slot index of the element.</returns>
    /// <exception cref="ArgumentException"><paramref name="item" /> has not been added to the structure.</exception>
    private int GetSlot(T item, string paramName)
    {
        if (!_indices.TryGetValue(item, out int slot))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ElementNotInSet, paramName);

        return slot;
    }

    /// <summary>
    /// Finds the root slot of a dense slot, applying path halving as it climbs.
    /// </summary>
    /// <param name="slot">The slot whose root is requested.</param>
    /// <returns>The root slot of the subset containing <paramref name="slot" />.</returns>
    private int FindRoot(int slot)
    {
        while (_parent[slot] != slot)
        {
            _parent[slot] = _parent[_parent[slot]]; // path halving
            slot = _parent[slot];
        }

        return slot;
    }
}
