// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents an insertion-ordered set.
/// </summary>
/// <typeparam name="T">The type of elements in the set. Elements must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// <see cref="OrderedSet{T}" /> is a sealed semantic alias over <see cref="IndexedSet{T}" />. It preserves
/// insertion order, rejects duplicate values, and inherits index-based access.
/// </para>
/// <para>
/// Use <see cref="IndexedSet{T}" /> when the index-addressable behaviour is the primary concept. Use
/// <see cref="OrderedSet{T}" /> when the order-preserving set behaviour is the primary concept.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[Serializable]
public sealed class OrderedSet<T> : IndexedSet<T>
    where T : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class using the default capacity and comparer.
    /// </summary>
    public OrderedSet()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    public OrderedSet(IEqualityComparer<T>? comparer)
        : base(comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial item capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is negative.
    /// </exception>
    public OrderedSet(int capacity)
        : base(capacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class with the specified initial capacity and comparer.
    /// </summary>
    /// <param name="capacity">The initial item capacity.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is negative.
    /// </exception>
    public OrderedSet(int capacity, IEqualityComparer<T>? comparer)
        : base(capacity, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class containing the unique elements from the specified collection.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    public OrderedSet(IEnumerable<T> collection)
        : base(collection)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}" /> class containing the unique elements from the specified collection.
    /// </summary>
    /// <param name="collection">The source collection. Must not be <see langword="null" />.</param>
    /// <param name="comparer">The equality comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    public OrderedSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
        : base(collection, comparer)
    {
    }
}
