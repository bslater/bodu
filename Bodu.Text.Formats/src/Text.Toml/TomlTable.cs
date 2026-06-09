// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML table — an ordered, mutable collection of key/value pairs. The root of every TOML document is a
/// table, as is every value declared with a <c>[table]</c> header or an inline <c>{ }</c> table.
/// </summary>
/// <remarks>
/// <para>
/// Keys are case-sensitive Unicode strings compared with ordinal semantics, matching the TOML specification. Insertion
/// order is preserved for stable enumeration and deterministic writer output. The writer canonicalizes structure — it
/// emits inline key/values before sub-table sections and standard tables rather than inline tables — so a round trip
/// through the writer preserves the document's values and semantics, not its original source layout or table style.
/// </para>
/// </remarks>
public sealed class TomlTable
    : TomlValue
    , IReadOnlyCollection<KeyValuePair<string, TomlValue>>
{
    /// <summary>
    /// The key/value pairs in insertion order.
    /// </summary>
    private readonly List<KeyValuePair<string, TomlValue>> _items;

    /// <summary>
    /// Maps a key to its index within <see cref="_items" /> for constant-time lookup.
    /// </summary>
    private readonly Dictionary<string, int> _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlTable" /> class that is empty.
    /// </summary>
    public TomlTable()
    {
        _items = [];
        _index = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.Table;

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <summary>
    /// Gets the keys of the table in insertion order.
    /// </summary>
    /// <returns>An ordered, read-only view of the keys.</returns>
    public IReadOnlyList<string> Keys =>
        _items.ConvertAll(static pair => pair.Key);

    /// <summary>
    /// Gets or sets the value associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to look up or assign.</param>
    /// <returns>The value associated with <paramref name="key" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or the assigned value is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown by the getter when <paramref name="key" /> is not present.
    /// </exception>
    public TomlValue this[string key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);
            return _index.TryGetValue(key, out var i)
                ? _items[i].Value
                : throw new KeyNotFoundException();
        }

        set
        {
            ThrowHelper.ThrowIfNull(key);
            ThrowHelper.ThrowIfNull(value);
            if (_index.TryGetValue(key, out var i))
            {
                _items[i] = new KeyValuePair<string, TomlValue>(key, value);
            }
            else
            {
                _index[key] = _items.Count;
                _items.Add(new KeyValuePair<string, TomlValue>(key, value));
            }
        }
    }

    /// <summary>
    /// Adds a key/value pair to the table.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key" /> already exists in the table.</exception>
    public void Add(string key, TomlValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(value);
        if (_index.ContainsKey(key))
            throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, FormatsResourceStrings.Arg_Invalid_TomlDuplicateKey, key), nameof(key));

        _index[key] = _items.Count;
        _items.Add(new KeyValuePair<string, TomlValue>(key, value));
    }

    /// <summary>
    /// Indicates whether the table contains <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to test.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool ContainsKey(string key)
    {
        ThrowHelper.ThrowIfNull(key);
        return _index.ContainsKey(key);
    }

    /// <summary>
    /// Attempts to retrieve the value associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the associated value; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetValue(string key, [NotNullWhen(true)] out TomlValue? value)
    {
        ThrowHelper.ThrowIfNull(key);
        if (_index.TryGetValue(key, out var i))
        {
            value = _items[i].Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, TomlValue>> GetEnumerator() =>
        _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        _items.GetEnumerator();
}
