// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Text;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Represents a mutable TOML array as an ordered, index-addressable collection of child nodes. Mirrors the role of
/// <see cref="System.Text.Json.Nodes.JsonArray" /> for TOML.
/// </summary>
/// <remarks>
/// Elements are kept in insertion order, which is also the order in which they are serialized. An element may be
/// <see langword="null" /> in memory, but an array containing a <see langword="null" /> element cannot be written
/// because TOML has no null token. Adding a node that already belongs to another container throws an
/// <see cref="InvalidOperationException" />; removing or replacing an element detaches it, clearing its
/// <see cref="TomlNode.Parent" /> so it can be added to another container.
/// </remarks>
public sealed class TomlArray
    : TomlNode, IList<TomlNode?>
{
    /// <summary>
    /// The backing list of child nodes, in order.
    /// </summary>
    private readonly List<TomlNode?> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlArray" /> class that is empty.
    /// </summary>
    public TomlArray()
    {
        _items = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlArray" /> class containing the supplied items in order.
    /// </summary>
    /// <param name="items">The initial elements.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an element already belongs to another container.
    /// </exception>
    public TomlArray(params TomlNode?[] items)
    {
        ThrowHelper.ThrowIfNull(items);

        _items = new List<TomlNode?>(items.Length);
        foreach (TomlNode? item in items)
            Add(item);
    }

    /// <inheritdoc />
    public int Count =>
        _items.Count;

    /// <inheritdoc />
    public bool IsReadOnly =>
        false;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <value>The element at <paramref name="index" />.</value>
    /// <returns>The element at <paramref name="index" />, which may be <see langword="null" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is outside the bounds of the list.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the assigned node already belongs to another container.
    /// </exception>
    /// <remarks>
    /// Assigning over an existing element detaches the replaced node, clearing its <see cref="TomlNode.Parent" />.
    /// </remarks>
    public new TomlNode? this[int index]
    {
        get => _items[index];
        set
        {
            TomlNode? existing = _items[index];
            value?.AssignParent(this);

            if (existing is not null && !ReferenceEquals(existing, value))
                existing.Parent = null;

            _items[index] = value;
        }
    }

    /// <inheritdoc />
    public override TomlValueKind GetValueKind() =>
        TomlValueKind.Array;

    /// <inheritdoc />
    public void Add(TomlNode? item)
    {
        item?.AssignParent(this);
        _items.Add(item);
    }

    /// <inheritdoc />
    public void Clear()
    {
        foreach (TomlNode? item in _items)
        {
            if (item is not null)
                item.Parent = null;
        }

        _items.Clear();
    }

    /// <inheritdoc />
    public bool Contains(TomlNode? item) =>
        _items.Contains(item);

    /// <inheritdoc />
    public void CopyTo(TomlNode?[] array, int arrayIndex) =>
        _items.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<TomlNode?> GetEnumerator() =>
        _items.GetEnumerator();

    /// <inheritdoc />
    public int IndexOf(TomlNode? item) =>
        _items.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, TomlNode? item)
    {
        item?.AssignParent(this);
        _items.Insert(index, item);
    }

    /// <inheritdoc />
    public bool Remove(TomlNode? item)
    {
        if (!_items.Remove(item))
            return false;

        if (item is not null)
            item.Parent = null;

        return true;
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        TomlNode? removed = _items[index];
        _items.RemoveAt(index);

        if (removed is not null)
            removed.Parent = null;
    }

    /// <inheritdoc />
    public override void WriteTo(Utf8TomlWriter writer)
    {
        writer.WriteStartArray();
        foreach (TomlNode? item in _items)
        {
            if (item is null)
                throw new TomlSerializationException(TomlResourceStrings.Op_NotSupported_NullNode);

            item.WriteTo(writer);
        }

        writer.WriteEndArray();
    }

    /// <inheritdoc />
    public override TomlNode DeepClone()
    {
        var clone = new TomlArray();
        foreach (TomlNode? item in _items)
            clone.Add(item?.DeepClone());

        return clone;
    }

    /// <inheritdoc />
    public override string ToString() =>
        Encoding.UTF8.GetString(ToUtf8Bytes());

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
