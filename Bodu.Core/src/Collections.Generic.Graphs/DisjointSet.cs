// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisjointSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Provides a fixed-size, integer-indexed disjoint-set (union-find) structure that tracks a partition of the contiguous
/// range <c>[0, count)</c> into disjoint subsets and supports near-constant-time union and connectivity queries.
/// </summary>
/// <remarks>
/// <para>
/// Each integer in <c>[0, <see cref="Count" />)</c> begins in its own singleton set. <see cref="Union(int, int)" />
/// merges the subsets containing two elements, and <see cref="Find(int)" /> returns the canonical representative of an
/// element's subset. Two elements are in the same subset exactly when they share a representative.
/// </para>
/// <para>
/// The implementation combines <b>path-halving</b> compression with <b>union by size</b>, giving an amortized cost per
/// operation of O(α(n)), where α is the inverse Ackermann function (effectively constant). This integer variant is the
/// allocation-light form used internally by graph algorithms over dense vertex indices; see
/// <see cref="DisjointSet{T}" /> for an element-keyed equivalent.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}, Sets = {SetCount}")]
public sealed class DisjointSet
{
    private readonly int[] _parent;
    private readonly int[] _size;
    private int _setCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisjointSet" /> class with the specified number of elements, each
    /// placed in its own singleton set.
    /// </summary>
    /// <param name="count">The number of elements, indexed <c>0</c> through <c>count - 1</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
    public DisjointSet(int count)
    {
        ThrowHelper.ThrowIfNegative(count);

        _parent = new int[count];
        _size = new int[count];
        for (int i = 0; i < count; i++)
        {
            _parent[i] = i;
            _size[i] = 1;
        }

        _setCount = count;
    }

    /// <summary>
    /// Gets the total number of elements tracked by the structure.
    /// </summary>
    /// <value>The number of elements, equal to the <c>count</c> supplied at construction.</value>
    /// <returns>The total number of elements.</returns>
    public int Count => _parent.Length;

    /// <summary>
    /// Gets the number of disjoint sets currently represented.
    /// </summary>
    /// <value>The count of distinct subsets, starting at <see cref="Count" /> and decreasing by one per merge.</value>
    /// <returns>The number of disjoint sets.</returns>
    public int SetCount => _setCount;

    /// <summary>
    /// Returns the canonical representative of the set containing the specified element.
    /// </summary>
    /// <param name="x">The element whose representative is requested.</param>
    /// <returns>The representative element of the subset containing <paramref name="x" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="x" /> is negative or not less than <see cref="Count" />.
    /// </exception>
    public int Find(int x)
    {
        ValidateElement(x);

        while (_parent[x] != x)
        {
            _parent[x] = _parent[_parent[x]]; // path halving
            x = _parent[x];
        }

        return x;
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Either element is negative or not less than <see cref="Count" />.
    /// </exception>
    public bool Union(int a, int b)
    {
        int rootA = Find(a);
        int rootB = Find(b);
        if (rootA == rootB)
            return false;

        // Union by size: attach the smaller tree under the larger root.
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Either element is negative or not less than <see cref="Count" />.
    /// </exception>
    public bool AreConnected(int a, int b) =>
        Find(a) == Find(b);

    /// <summary>
    /// Returns the number of elements in the subset containing the specified element.
    /// </summary>
    /// <param name="x">The element whose subset size is requested.</param>
    /// <returns>The number of elements in the subset containing <paramref name="x" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="x" /> is negative or not less than <see cref="Count" />.
    /// </exception>
    public int SizeOf(int x) =>
        _size[Find(x)];

    /// <summary>
    /// Restores every element to its own singleton set.
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < _parent.Length; i++)
        {
            _parent[i] = i;
            _size[i] = 1;
        }

        _setCount = _parent.Length;
    }

    /// <summary>
    /// Validates that an element index is within the range <c>[0, Count)</c>.
    /// </summary>
    /// <param name="x">The element index to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="x" /> is negative or not less than <see cref="Count" />.
    /// </exception>
    private void ValidateElement(int x)
    {
        ThrowHelper.ThrowIfNegative(x, nameof(x));
        ThrowHelper.ThrowIfGreaterThanOrEqual(x, _parent.Length, nameof(x));
    }
}
