// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlObject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Text;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Represents a mutable TOML table as a string-keyed collection of child nodes.
/// </summary>
/// <remarks>
/// Entries are kept in insertion order — including across removals — which is also the order in which they are
/// serialized: the TOML writer emits a table's members in document order rather than re-sorting them. A value may be <see langword="null" /> in memory, but
/// a table containing a <see langword="null" /> value cannot be written because TOML has no null token. Adding a node
/// that already belongs to another container throws an <see cref="InvalidOperationException" />; removing or replacing
/// a value detaches it, clearing its <see cref="TomlNode.Parent" /> so it can be added to another container.
/// </remarks>
public sealed class TomlObject
    : TomlNode, IDictionary<string, TomlNode?>
{
    /// <summary>
    /// The backing map of property names to child nodes.
    /// </summary>
    private readonly Dictionary<string, TomlNode?> _properties;

    /// <summary>
    /// The property names in insertion order, kept consistent across removals so that enumeration and serialization
    /// order is deterministic.
    /// </summary>
    private readonly List<string> _order;

    /// <summary>
    /// The comparer used for property-name equality, shared by the map and the order list.
    /// </summary>
    private readonly StringComparer _comparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlObject" /> class that is empty.
    /// </summary>
    public TomlObject()
    {
        _comparer = StringComparer.Ordinal;
        _properties = new Dictionary<string, TomlNode?>(_comparer);
        _order = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlObject" /> class that is empty, using the supplied options to
    /// select the property-name comparison.
    /// </summary>
    /// <param name="options">The node options controlling property-name case sensitivity.</param>
    /// <remarks>
    /// When <see cref="TomlNodeOptions.PropertyNameCaseInsensitive" /> is <see langword="true" />, in-memory
    /// property-name lookups use <see cref="StringComparer.OrdinalIgnoreCase" />; otherwise they use
    /// <see cref="StringComparer.Ordinal" />. The option does not affect serialization.
    /// </remarks>
    public TomlObject(TomlNodeOptions options)
    {
        _comparer = options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        _properties = new Dictionary<string, TomlNode?>(_comparer);
        _order = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlObject" /> class containing the supplied entries.
    /// </summary>
    /// <param name="items">The initial entries.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a value already belongs to another container.
    /// </exception>
    public TomlObject(IEnumerable<KeyValuePair<string, TomlNode?>> items)
    {
        ThrowHelper.ThrowIfNull(items);

        _comparer = StringComparer.Ordinal;
        _properties = new Dictionary<string, TomlNode?>(_comparer);
        _order = [];
        foreach (KeyValuePair<string, TomlNode?> item in items)
            Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    /// <remarks>The collection is a read-only snapshot in insertion order.</remarks>
    public ICollection<string> Keys =>
        _order.AsReadOnly();

    /// <inheritdoc />
    /// <remarks>The collection is a read-only snapshot in insertion order.</remarks>
    public ICollection<TomlNode?> Values =>
        _order.Select(key => _properties[key]).ToList().AsReadOnly();

    /// <inheritdoc />
    public int Count =>
        _properties.Count;

    /// <inheritdoc />
    public bool IsReadOnly =>
        false;

    /// <summary>
    /// Gets or sets the value associated with the specified property name.
    /// </summary>
    /// <param name="key">The property name.</param>
    /// <value>The value associated with <paramref name="key" />.</value>
    /// <returns>The value associated with <paramref name="key" />, which may be <see langword="null" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown by the getter when <paramref name="key" /> is not present.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the assigned node already belongs to another container.
    /// </exception>
    /// <remarks>
    /// Assigning over an existing entry detaches the replaced node, clearing its <see cref="TomlNode.Parent" />.
    /// </remarks>
    public new TomlNode? this[string key]
    {
        get => _properties[key];
        set
        {
            ThrowHelper.ThrowIfNull(key);
            value?.AssignParent(this);

            if (_properties.TryGetValue(key, out TomlNode? existing))
            {
                if (existing is not null && !ReferenceEquals(existing, value))
                    existing.Parent = null;
            }
            else
            {
                _order.Add(key);
            }

            _properties[key] = value;
        }
    }

    /// <inheritdoc />
    public override TomlValueKind GetValueKind() =>
        TomlValueKind.Table;

    /// <inheritdoc />
    public void Add(string key, TomlNode? value)
    {
        ThrowHelper.ThrowIfNull(key);
        value?.AssignParent(this);
        _properties.Add(key, value);
        _order.Add(key);
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<string, TomlNode?> item) =>
        Add(item.Key, item.Value);

    /// <inheritdoc />
    public void Clear()
    {
        foreach (TomlNode? child in _properties.Values)
        {
            if (child is not null)
                child.Parent = null;
        }

        _properties.Clear();
        _order.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, TomlNode?> item) =>
        ((ICollection<KeyValuePair<string, TomlNode?>>)_properties).Contains(item);

    /// <inheritdoc />
    public bool ContainsKey(string key) =>
        _properties.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, TomlNode?>[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, Count);

        foreach (KeyValuePair<string, TomlNode?> entry in this)
            array[arrayIndex++] = entry;
    }

    /// <inheritdoc />
    /// <remarks>Entries are enumerated in insertion order.</remarks>
    public IEnumerator<KeyValuePair<string, TomlNode?>> GetEnumerator()
    {
        foreach (string key in _order)
            yield return new KeyValuePair<string, TomlNode?>(key, _properties[key]);
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (!_properties.Remove(key, out TomlNode? removed))
            return false;

        RemoveFromOrder(key);
        if (removed is not null)
            removed.Parent = null;

        return true;
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, TomlNode?> item)
    {
        if (!((ICollection<KeyValuePair<string, TomlNode?>>)_properties).Remove(item))
            return false;

        RemoveFromOrder(item.Key);
        if (item.Value is not null)
            item.Value.Parent = null;

        return true;
    }

    /// <summary>
    /// Removes <paramref name="key" /> from the insertion-order list using the object's property-name comparer.
    /// </summary>
    /// <param name="key">The property name to remove.</param>
    private void RemoveFromOrder(string key)
    {
        int index = _order.FindIndex(entry => _comparer.Equals(entry, key));
        if (index >= 0)
            _order.RemoveAt(index);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out TomlNode? value) =>
        _properties.TryGetValue(key, out value);

    /// <summary>
    /// Attempts to get the value associated with the specified property name.
    /// </summary>
    /// <param name="propertyName">The property name to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the value associated with <paramref name="propertyName" />;
    /// otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a value is associated with <paramref name="propertyName" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool TryGetPropertyValue(string propertyName, out TomlNode? value) =>
        _properties.TryGetValue(propertyName, out value);

    /// <inheritdoc />
    public override void WriteTo(Utf8TomlWriter writer)
    {
        writer.WriteStartTable();
        foreach (KeyValuePair<string, TomlNode?> entry in this)
        {
            if (entry.Value is null)
                throw new TomlSerializationException(TomlResourceStrings.Op_NotSupported_NullNode);

            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndTable();
    }

    /// <inheritdoc />
    public override TomlNode DeepClone()
    {
        var clone = new TomlObject();
        foreach (KeyValuePair<string, TomlNode?> entry in this)
            clone[entry.Key] = entry.Value?.DeepClone();

        return clone;
    }

    /// <inheritdoc />
    public override string ToString() =>
        Encoding.UTF8.GetString(ToUtf8Bytes());

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
