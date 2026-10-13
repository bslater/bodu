// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Represents a mutable YAML sequence node whose elements are <see cref="YamlNode" /> values.
/// </summary>
/// <remarks>
/// A node may appear at most once in a tree. Adding, inserting, or assigning a node that already belongs to a
/// container, this sequence included, throws an <see cref="InvalidOperationException" />; removing or replacing an
/// element, or clearing the sequence, detaches it so that it can be added elsewhere. A <see langword="null" /> element
/// is not a node and may appear any number of times.
/// </remarks>
public sealed class YamlArray
    : YamlNode,
    IList<YamlNode?>
{
    /// <summary>The element nodes in sequence order.</summary>
    private readonly List<YamlNode?> _items = [];

    /// <summary>
    /// Gets the number of elements in the sequence.
    /// </summary>
    /// <value>The element count.</value>
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the sequence is read-only.
    /// </summary>
    /// <value>Always <see langword="false" />.</value>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <value>The element node, or <see langword="null" />.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than zero, or is greater than or equal to <see cref="Count" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The assigned node already belongs to a container, and is not the element at <paramref name="index" />.
    /// </exception>
    /// <remarks>
    /// Assigning over an element detaches the replaced node, so that it can be added elsewhere. Assigning an element
    /// over itself leaves the sequence unchanged.
    /// </remarks>
    public override YamlNode? this[int index]
    {
        get => _items[index];
        set
        {
            YamlNode? existing = _items[index];
            if (ReferenceEquals(existing, value))
                return;

            value?.ThrowIfAttached();

            _items[index] = value;
            value?.Parent = this;
            existing?.Parent = null;
        }
    }

    /// <summary>
    /// Adds an element to the end of the sequence.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <exception cref="InvalidOperationException"><paramref name="item" /> already belongs to a container.</exception>
    public void Add(YamlNode? item)
    {
        item?.ThrowIfAttached();

        _items.Add(item);
        item?.Parent = this;
    }

    /// <summary>
    /// Removes all elements from the sequence, detaching each of them.
    /// </summary>
    public void Clear()
    {
        foreach (YamlNode? item in _items)
            item?.Parent = null;

        _items.Clear();
    }

    /// <summary>
    /// Determines whether the sequence contains the specified element.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns><see langword="true" /> when the element is present.</returns>
    public bool Contains(YamlNode? item) => _items.Contains(item);

    /// <summary>
    /// Copies the sequence elements to an array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The starting index in the destination.</param>
    public void CopyTo(YamlNode?[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns the index of the specified element.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns>The zero-based index, or <c>-1</c> when not found.</returns>
    public int IndexOf(YamlNode? item) => _items.IndexOf(item);

    /// <summary>
    /// Inserts an element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <param name="item">The element to insert.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than zero, or is greater than <see cref="Count" />.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="item" /> already belongs to a container.</exception>
    public void Insert(int index, YamlNode? item)
    {
        item?.ThrowIfAttached();

        _items.Insert(index, item);
        item?.Parent = this;
    }

    /// <summary>
    /// Removes the first occurrence of the specified element, detaching it.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns><see langword="true" /> when an element was removed.</returns>
    public bool Remove(YamlNode? item)
    {
        if (!_items.Remove(item))
            return false;

        item?.Parent = null;
        return true;
    }

    /// <summary>
    /// Removes the element at the specified index, detaching it.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than zero, or is greater than or equal to <see cref="Count" />.
    /// </exception>
    public void RemoveAt(int index)
    {
        YamlNode? removed = _items[index];
        _items.RemoveAt(index);

        removed?.Parent = null;
    }

    /// <summary>
    /// Returns an enumerator over the elements.
    /// </summary>
    /// <returns>The element enumerator.</returns>
    public IEnumerator<YamlNode?> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc />
    public override void WriteTo(Utf8YamlWriter writer)
    {
        writer.WriteStartSequence();
        foreach (YamlNode? item in _items)
            WriteChild(writer, item);

        writer.WriteEndSequence();
    }
}
