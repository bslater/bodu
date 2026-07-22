// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvObject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

using Bodu.Text.DotEnv.Writer;

namespace Bodu.Text.DotEnv.Nodes;

/// <summary>
/// Represents the root of a mutable DotEnv document tree: an insertion-ordered map of key names to
/// <see cref="DotEnvValue" /> string leaves, with an optional per-entry <c>export</c> flag preserved for
/// round-tripping.
/// </summary>
public sealed class DotEnvObject
    : DotEnvNode
{
    /// <summary>The key/value entries, keyed by name for O(1) lookup.</summary>
    private readonly Dictionary<string, DotEnvValue> _entries = new(StringComparer.Ordinal);

    /// <summary>The key names in insertion order.</summary>
    private readonly List<string> _order = [];

    /// <summary>The set of keys declared with an <c>export</c> prefix.</summary>
    private readonly HashSet<string> _exports = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvObject" /> class.
    /// </summary>
    public DotEnvObject()
    {
    }

    /// <inheritdoc />
    public override DotEnvValueKind ValueKind => DotEnvValueKind.Object;

    /// <summary>
    /// Gets the number of entries in this object.
    /// </summary>
    /// <value>The entry count.</value>
    public int Count => _order.Count;

    /// <summary>
    /// Gets the key names in insertion order.
    /// </summary>
    /// <value>The ordered key names.</value>
    public IReadOnlyList<string> Keys => _order;

    /// <summary>
    /// Gets or sets the value associated with the specified key, adding the key at the end when it is new and
    /// preserving its position when it already exists.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <value>The value node associated with <paramref name="key" />.</value>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or the assigned value is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown by the getter when <paramref name="key" /> is not present.
    /// </exception>
    public DotEnvValue this[string key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);

            return _entries[key];
        }

        set
        {
            ThrowHelper.ThrowIfNull(key);
            ThrowHelper.ThrowIfNull(value);

            Set(key, value, IsExport(key));
        }
    }

    /// <summary>
    /// Determines whether this object contains the specified key.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool ContainsKey(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _entries.ContainsKey(key);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the value; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetValue(string key, [NotNullWhen(true)] out DotEnvValue? value)
    {
        ThrowHelper.ThrowIfNull(key);

        return _entries.TryGetValue(key, out value);
    }

    /// <summary>
    /// Removes the entry with the specified key.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <returns><see langword="true" /> when an entry was removed; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool Remove(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_entries.Remove(key))
            return false;

        _order.Remove(key);
        _exports.Remove(key);
        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the entry with the specified key is declared with an <c>export</c> prefix.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <returns>
    /// <see langword="true" /> when the entry carries an <c>export</c> prefix; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool IsExport(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _exports.Contains(key);
    }

    /// <summary>
    /// Sets whether the entry with the specified key is declared with an <c>export</c> prefix.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="export">
    /// <see langword="true" /> to mark the entry for export; otherwise <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="key" /> is not present.</exception>
    public void SetExport(string key, bool export)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_entries.ContainsKey(key))
            throw new KeyNotFoundException();

        if (export)
            _exports.Add(key);
        else
            _exports.Remove(key);
    }

    /// <inheritdoc />
    public override DotEnvNode DeepClone()
    {
        var clone = new DotEnvObject();
        foreach (string key in _order)
            clone.Set(key, (DotEnvValue)_entries[key].DeepClone(), _exports.Contains(key));

        return clone;
    }

    /// <inheritdoc />
    public override void WriteTo(ref Utf8DotEnvWriter writer)
    {
        writer.WriteStartObject();
        foreach (string key in _order)
        {
            writer.WritePropertyName(key, _exports.Contains(key));
            _entries[key].WriteTo(ref writer);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Adds or replaces an entry, preserving insertion position on replacement and recording the export flag.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="value">The value node.</param>
    /// <param name="export">Whether the entry is declared with an <c>export</c> prefix.</param>
    internal void Set(string key, DotEnvValue value, bool export)
    {
        if (!_entries.ContainsKey(key))
            _order.Add(key);

        _entries[key] = value;

        if (export)
            _exports.Add(key);
        else
            _exports.Remove(key);
    }
}
