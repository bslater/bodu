// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

using Bodu.Text.Delimited.Writer;

namespace Bodu.Text.Delimited.Nodes;

/// <summary>
/// Represents an array node in a mutable delimited document tree: the document of records, or a positional (headerless)
/// record of string fields.
/// </summary>
public sealed class DelimitedArray
    : DelimitedNode, IReadOnlyList<DelimitedNode>
{
    /// <summary>The ordered child nodes.</summary>
    private readonly List<DelimitedNode> _items = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedArray" /> class.
    /// </summary>
    public DelimitedArray()
    {
    }

    /// <inheritdoc />
    public override DelimitedValueKind ValueKind => DelimitedValueKind.Array;

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <summary>
    /// Gets or sets the child node at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <value>The child node.</value>
    /// <exception cref="ArgumentNullException">Thrown when the assigned value is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is out of range.</exception>
    public DelimitedNode this[int index]
    {
        get => _items[index];
        set
        {
            ThrowHelper.ThrowIfNull(value);
            _items[index] = value;
        }
    }

    /// <summary>
    /// Appends a child node.
    /// </summary>
    /// <param name="node">The node to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    public void Add(DelimitedNode node)
    {
        ThrowHelper.ThrowIfNull(node);

        _items.Add(node);
    }

    /// <summary>
    /// Appends a string field.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public void Add(string value) =>
        Add(new DelimitedValue(value));

    /// <summary>
    /// Removes the child node at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is out of range.</exception>
    public void RemoveAt(int index) =>
        _items.RemoveAt(index);

    /// <summary>
    /// Removes all child nodes.
    /// </summary>
    public void Clear() =>
        _items.Clear();

    /// <inheritdoc />
    public override DelimitedNode DeepClone()
    {
        var clone = new DelimitedArray();
        foreach (DelimitedNode item in _items)
            clone.Add(item.DeepClone());

        return clone;
    }

    /// <inheritdoc />
    public override void WriteTo(ref Utf8DelimitedWriter writer)
    {
        writer.WriteStartArray();
        foreach (DelimitedNode item in _items)
            item.WriteTo(ref writer);

        writer.WriteEndArray();
    }

    /// <inheritdoc />
    public IEnumerator<DelimitedNode> GetEnumerator() =>
        _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        _items.GetEnumerator();
}
