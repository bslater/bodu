// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlObject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Represents a mutable YAML mapping node whose entries preserve insertion order.
/// </summary>
public sealed class YamlObject : YamlNode, IEnumerable<KeyValuePair<string, YamlNode?>>
{
    private readonly List<string> _order = [];
    private readonly Dictionary<string, YamlNode?> _entries = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the number of entries in the mapping.
    /// </summary>
    /// <value>The entry count.</value>
    public int Count => _order.Count;

    /// <summary>
    /// Gets the keys of the mapping in insertion order.
    /// </summary>
    /// <value>The ordered keys.</value>
    public IReadOnlyList<string> Keys => _order;

    /// <summary>
    /// Gets or sets the node mapped to the specified key, adding it when absent.
    /// </summary>
    /// <param name="key">The mapping key.</param>
    /// <value>The mapped node, or <see langword="null" />.</value>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public override YamlNode? this[string key]
    {
        get
        {
            Bodu.ThrowHelper.ThrowIfNull(key);
            return _entries.TryGetValue(key, out var node) ? node : null;
        }

        set
        {
            Bodu.ThrowHelper.ThrowIfNull(key);
            if (!_entries.ContainsKey(key))
                _order.Add(key);

            _entries[key] = value;
        }
    }

    /// <summary>
    /// Adds an entry to the mapping.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The entry value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An entry with the same key already exists.</exception>
    public void Add(string key, YamlNode? value)
    {
        Bodu.ThrowHelper.ThrowIfNull(key);
        if (_entries.ContainsKey(key))
            throw new ArgumentException("An entry with the same key already exists.", nameof(key));

        _order.Add(key);
        _entries[key] = value;
    }

    /// <summary>
    /// Determines whether the mapping contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true" /> when the key is present.</returns>
    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    /// <summary>
    /// Removes the entry with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true" /> when an entry was removed.</returns>
    public bool Remove(string key)
    {
        if (!_entries.Remove(key))
            return false;

        _order.Remove(key);
        return true;
    }

    /// <summary>
    /// Attempts to get the node mapped to the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When the method returns <see langword="true" />, the mapped node.</param>
    /// <returns><see langword="true" /> when the key is present.</returns>
    public bool TryGetValue(string key, out YamlNode? value) => _entries.TryGetValue(key, out value);

    /// <summary>
    /// Returns an enumerator over the entries in insertion order.
    /// </summary>
    /// <returns>The entry enumerator.</returns>
    public IEnumerator<KeyValuePair<string, YamlNode?>> GetEnumerator()
    {
        foreach (var key in _order)
            yield return new KeyValuePair<string, YamlNode?>(key, _entries[key]);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public override void WriteTo(ref Utf8YamlWriter writer)
    {
        writer.WriteStartMapping();
        foreach (var key in _order)
        {
            writer.WritePropertyName(key);
            WriteChild(ref writer, _entries[key]);
        }

        writer.WriteEndMapping();
    }
}
