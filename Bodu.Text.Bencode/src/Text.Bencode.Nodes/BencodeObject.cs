// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeObject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Text;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Represents a mutable Bencode (BEP 3) dictionary as a string-keyed collection of child nodes.
/// </summary>
/// <remarks>
/// Keys are CLR strings encoded as UTF-8 byte strings on the wire. In-memory ordering is not contractual: the writer
/// emits entries in canonical ascending bytewise key order regardless of insertion order. A value may be
/// <see langword="null" /> in memory, but an object containing a <see langword="null" /> value cannot be written
/// because Bencode has no null token. Adding a node that already belongs to another container throws an
/// <see cref="InvalidOperationException" />; removing or replacing a value detaches it, clearing its
/// <see cref="BencodeNode.Parent" /> so it can be added to another container.
/// </remarks>
public sealed class BencodeObject
    : BencodeNode, IDictionary<string, BencodeNode?>
{
    /// <summary>
    /// The backing map of property names to child nodes.
    /// </summary>
    private readonly Dictionary<string, BencodeNode?> _properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeObject" /> class that is empty.
    /// </summary>
    public BencodeObject()
    {
        _properties = new Dictionary<string, BencodeNode?>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeObject" /> class that is empty, using the supplied options
    /// to select the property-name comparison.
    /// </summary>
    /// <param name="options">The node options controlling property-name case sensitivity.</param>
    /// <remarks>
    /// When <see cref="BencodeNodeOptions.PropertyNameCaseInsensitive" /> is <see langword="true" />, in-memory
    /// property-name lookups use <see cref="StringComparer.OrdinalIgnoreCase" />; otherwise they use
    /// <see cref="StringComparer.Ordinal" />. The option does not affect serialization.
    /// </remarks>
    public BencodeObject(BencodeNodeOptions options)
    {
        _properties = new Dictionary<string, BencodeNode?>(
            options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeObject" /> class containing the supplied entries.
    /// </summary>
    /// <param name="items">The initial entries.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a value already belongs to another container.
    /// </exception>
    public BencodeObject(IEnumerable<KeyValuePair<string, BencodeNode?>> items)
    {
        ThrowHelper.ThrowIfNull(items);

        _properties = new Dictionary<string, BencodeNode?>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, BencodeNode?> item in items)
            Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    public ICollection<string> Keys =>
        _properties.Keys;

    /// <inheritdoc />
    public ICollection<BencodeNode?> Values =>
        _properties.Values;

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
    /// Assigning over an existing entry detaches the replaced node, clearing its <see cref="BencodeNode.Parent" />.
    /// </remarks>
    public new BencodeNode? this[string key]
    {
        get => _properties[key];
        set
        {
            ThrowHelper.ThrowIfNull(key);
            value?.AssignParent(this);

            if (_properties.TryGetValue(key, out BencodeNode? existing) && existing is not null && !ReferenceEquals(existing, value))
                existing.Parent = null;

            _properties[key] = value;
        }
    }

    /// <inheritdoc />
    public override BencodeValueKind GetValueKind() =>
        BencodeValueKind.Object;

    /// <inheritdoc />
    public void Add(string key, BencodeNode? value)
    {
        ThrowHelper.ThrowIfNull(key);
        value?.AssignParent(this);
        _properties.Add(key, value);
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<string, BencodeNode?> item) =>
        Add(item.Key, item.Value);

    /// <inheritdoc />
    public void Clear()
    {
        foreach (BencodeNode? child in _properties.Values)
        {
            if (child is not null)
                child.Parent = null;
        }

        _properties.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, BencodeNode?> item) =>
        ((ICollection<KeyValuePair<string, BencodeNode?>>)_properties).Contains(item);

    /// <inheritdoc />
    public bool ContainsKey(string key) =>
        _properties.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, BencodeNode?>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, BencodeNode?>>)_properties).CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, BencodeNode?>> GetEnumerator() =>
        _properties.GetEnumerator();

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (!_properties.Remove(key, out BencodeNode? removed))
            return false;

        if (removed is not null)
            removed.Parent = null;

        return true;
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, BencodeNode?> item)
    {
        if (!((ICollection<KeyValuePair<string, BencodeNode?>>)_properties).Remove(item))
            return false;

        if (item.Value is not null)
            item.Value.Parent = null;

        return true;
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out BencodeNode? value) =>
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
    public bool TryGetPropertyValue(string propertyName, out BencodeNode? value) =>
        _properties.TryGetValue(propertyName, out value);

    /// <inheritdoc />
    public override void WriteTo(Utf8BencodeWriter writer)
    {
        writer.WriteStartDictionary();
        foreach (KeyValuePair<string, BencodeNode?> entry in _properties)
        {
            if (entry.Value is null)
                throw new BencodeSerializationException(BencodeResourceStrings.Op_NotSupported_NullNode);

            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndDictionary();
    }

    /// <inheritdoc />
    public override BencodeNode DeepClone()
    {
        var clone = new BencodeObject();
        foreach (KeyValuePair<string, BencodeNode?> entry in _properties)
            clone[entry.Key] = entry.Value?.DeepClone();

        return clone;
    }

    /// <inheritdoc />
    public override string ToString() =>
        Encoding.Latin1.GetString(ToByteArray());

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
