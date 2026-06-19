// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReadOnlyCollectionOnly{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Test.Collections;

/// <summary>
/// Wraps an array as an <see cref="IReadOnlyCollection{T}" /> only — deliberately not implementing
/// <see cref="ICollection{T}" /> or <see cref="IList{T}" /> — so that capacity-hint and projection helpers exercise the
/// read-only-collection code path rather than the mutable-collection one.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </remarks>
public sealed class ReadOnlyCollectionOnly<T>
    : IReadOnlyCollection<T>
{
    private readonly T[] _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyCollectionOnly{T}" /> class wrapping the supplied items.
    /// </summary>
    /// <param name="items">The items to expose through the read-only collection surface.</param>
    /// <exception cref="ArgumentNullException"><paramref name="items" /> is <see langword="null" />.</exception>
    public ReadOnlyCollectionOnly(T[] items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <inheritdoc />
    public int Count => _items.Length;

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        foreach (T item in _items)
            yield return item;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
