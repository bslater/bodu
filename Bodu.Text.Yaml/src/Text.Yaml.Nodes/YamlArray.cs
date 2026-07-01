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
public sealed class YamlArray : YamlNode, IList<YamlNode?>
{
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
    public override YamlNode? this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    /// <summary>
    /// Adds an element to the end of the sequence.
    /// </summary>
    /// <param name="item">The element to add.</param>
    public void Add(YamlNode? item) => _items.Add(item);

    /// <summary>
    /// Removes all elements from the sequence.
    /// </summary>
    public void Clear() => _items.Clear();

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
    public void Insert(int index, YamlNode? item) => _items.Insert(index, item);

    /// <summary>
    /// Removes the first occurrence of the specified element.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns><see langword="true" /> when an element was removed.</returns>
    public bool Remove(YamlNode? item) => _items.Remove(item);

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    public void RemoveAt(int index) => _items.RemoveAt(index);

    /// <summary>
    /// Returns an enumerator over the elements.
    /// </summary>
    /// <returns>The element enumerator.</returns>
    public IEnumerator<YamlNode?> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc />
    public override void WriteTo(ref Utf8YamlWriter writer)
    {
        writer.WriteStartSequence();
        foreach (YamlNode? item in _items)
            WriteChild(ref writer, item);

        writer.WriteEndSequence();
    }
}
