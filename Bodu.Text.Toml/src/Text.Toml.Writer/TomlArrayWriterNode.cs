// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArrayWriterNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Represents an array buffered in the <see cref="Utf8TomlWriter" /> value tree, holding its elements in the order they
/// were written.
/// </summary>
/// <remarks>
/// An array whose every element is a table is rendered as a sequence of <c>[[header]]</c> blocks when it is a table
/// member; any other array — including a mixed or scalar array, or a table-valued array nested inside an array — is
/// rendered inline as <c>[ … ]</c>.
/// </remarks>
internal sealed class TomlArrayWriterNode
    : TomlWriterNode
{
    /// <summary>The elements of the array, in insertion order.</summary>
    private readonly List<TomlWriterNode> _items = [];

    /// <summary>
    /// Gets the elements of the array, in insertion order.
    /// </summary>
    /// <value>The ordered elements.</value>
    internal IReadOnlyList<TomlWriterNode> Items => _items;

    /// <summary>
    /// Adds an element to the array.
    /// </summary>
    /// <param name="value">The buffered value node.</param>
    internal void Add(TomlWriterNode value) => _items.Add(value);
}
