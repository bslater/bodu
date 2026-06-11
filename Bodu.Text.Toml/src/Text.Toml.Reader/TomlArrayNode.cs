// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArrayNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Represents an array in the intermediate value tree — an ordered sequence of child <see cref="TomlReaderNode" />
/// values. A statically declared array and an array-of-tables are both represented by this type; the latter holds
/// <see cref="TomlTableNode" /> elements.
/// </summary>
internal sealed class TomlArrayNode
    : TomlReaderNode
{
    /// <summary>
    /// The backing element list, in document order.
    /// </summary>
    private readonly List<TomlReaderNode> _items = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlArrayNode" /> class.
    /// </summary>
    /// <param name="offset">The zero-based source byte offset at which the array begins.</param>
    internal TomlArrayNode(int offset)
        : base(offset)
    {
    }

    /// <inheritdoc />
    internal override TomlReaderNodeKind Kind => TomlReaderNodeKind.Array;

    /// <summary>
    /// Gets the elements of the array in document order.
    /// </summary>
    /// <returns>The ordered elements.</returns>
    internal IReadOnlyList<TomlReaderNode> Items => _items;

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    /// <returns>The element count.</returns>
    internal int Count => _items.Count;

    /// <summary>
    /// Gets the last element of the array.
    /// </summary>
    /// <returns>The final element.</returns>
    internal TomlReaderNode Last => _items[^1];

    /// <summary>
    /// Appends <paramref name="value" /> to the end of the array.
    /// </summary>
    /// <param name="value">The value to append.</param>
    internal void Add(TomlReaderNode value) =>
        _items.Add(value);
}
