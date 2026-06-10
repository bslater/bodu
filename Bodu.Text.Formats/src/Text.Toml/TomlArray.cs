// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML array — an ordered, mutable sequence of <see cref="TomlValue" /> elements. TOML permits arrays to
/// hold elements of differing types.
/// </summary>
/// <remarks>
/// Elements are stored and enumerated in document order, and the array implements <see cref="IReadOnlyList{T}" /> so it
/// can be indexed and iterated like any list. Because TOML arrays may be heterogeneous, an element's concrete kind is
/// discovered through <see cref="TomlValue.Kind" /> rather than assumed from its position.
/// </remarks>
/// <example>
///<![CDATA[
/// // Compose an array, then read elements back by index.
/// var ports = new TomlArray
/// {
///     new TomlInteger(8000),
///     new TomlInteger(8001),
/// };
/// ports.Add(new TomlInteger(8002));
///
/// long first = ((TomlInteger)ports[0]).Value;   // 8000
/// int count  = ports.Count;                     // 3
///]]>
/// </example>
public sealed class TomlArray
    : TomlValue
    , IReadOnlyList<TomlValue>
{
    /// <summary>
    /// The backing element list, in document order.
    /// </summary>
    private readonly List<TomlValue> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlArray" /> class that is empty.
    /// </summary>
    public TomlArray()
    {
        _items = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlArray" /> class populated with the supplied elements.
    /// </summary>
    /// <param name="items">The initial elements, in order.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items" /> is <see langword="null" /> or contains a <see langword="null" /> element.
    /// </exception>
    public TomlArray(IEnumerable<TomlValue> items)
    {
        ThrowHelper.ThrowIfNull(items);
        _items = [.. items];
        if (_items.Contains(null!))
            throw new ArgumentNullException(nameof(items));
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.Array;

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is outside the array bounds.
    /// </exception>
    public TomlValue this[int index] => _items[index];

    /// <summary>
    /// Appends <paramref name="value" /> to the end of the array.
    /// </summary>
    /// <param name="value">The value to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public void Add(TomlValue value)
    {
        ThrowHelper.ThrowIfNull(value);
        _items.Add(value);
    }

    /// <inheritdoc />
    public IEnumerator<TomlValue> GetEnumerator() =>
        _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        _items.GetEnumerator();
}
