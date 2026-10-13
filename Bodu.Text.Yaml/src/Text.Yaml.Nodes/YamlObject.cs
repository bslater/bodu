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
/// <remarks>
/// A node may appear at most once in a tree. Adding or assigning a value that already belongs to a container, this
/// mapping included, throws an <see cref="InvalidOperationException" />; removing an entry or replacing its value
/// detaches the value so that it can be added elsewhere.
/// </remarks>
public sealed class YamlObject
    : YamlNode,
    IEnumerable<KeyValuePair<string, YamlNode?>>
{
    /// <summary>The mapping keys in insertion order.</summary>
    private readonly List<string> _order = [];

    /// <summary>The key-to-node map, compared ordinally.</summary>
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
    /// <exception cref="InvalidOperationException">
    /// The assigned node already belongs to a container, and is not the value mapped to <paramref name="key" />.
    /// </exception>
    /// <remarks>
    /// Assigning over an entry detaches the replaced node, so that it can be added elsewhere. Assigning an entry's
    /// value over itself leaves the mapping unchanged.
    /// </remarks>
    public override YamlNode? this[string key]
    {
        get
        {
            Bodu.ThrowHelper.ThrowIfNull(key);
            return _entries.TryGetValue(key, out YamlNode? node) ? node : null;
        }

        set
        {
            Bodu.ThrowHelper.ThrowIfNull(key);

            bool exists = _entries.TryGetValue(key, out YamlNode? existing);
            if (exists && ReferenceEquals(existing, value))
                return;

            value?.ThrowIfAttached();

            if (!exists)
                _order.Add(key);

            _entries[key] = value;
            value?.Parent = this;
            existing?.Parent = null;
        }
    }

    /// <summary>
    /// Adds an entry to the mapping.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The entry value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An entry with the same key already exists.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="value" /> already belongs to a container.
    /// </exception>
    public void Add(string key, YamlNode? value)
    {
        Bodu.ThrowHelper.ThrowIfNull(key);
        if (_entries.ContainsKey(key))
            throw new ArgumentException(YamlResourceStrings.Arg_Invalid_YamlDuplicateKey, nameof(key));

        value?.ThrowIfAttached();

        _order.Add(key);
        _entries[key] = value;
        value?.Parent = this;
    }

    /// <summary>
    /// Determines whether the mapping contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true" /> when the key is present.</returns>
    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    /// <summary>
    /// Removes the entry with the specified key, detaching its value.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true" /> when an entry was removed.</returns>
    public bool Remove(string key)
    {
        if (!_entries.Remove(key, out YamlNode? removed))
            return false;

        _order.Remove(key);
        removed?.Parent = null;
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
        foreach (string key in _order)
            yield return new KeyValuePair<string, YamlNode?>(key, _entries[key]);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public override void WriteTo(Utf8YamlWriter writer)
    {
        writer.WriteStartMapping();
        foreach (string key in _order)
        {
            writer.WritePropertyName(key);
            WriteChild(writer, _entries[key]);
        }

        writer.WriteEndMapping();
    }
}
