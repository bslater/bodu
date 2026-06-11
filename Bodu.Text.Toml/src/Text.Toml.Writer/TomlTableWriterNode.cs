// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTableWriterNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Represents a table buffered in the <see cref="Utf8TomlWriter" /> value tree, holding its key/value pairs in the
/// order they were written.
/// </summary>
/// <remarks>
/// Insertion order is preserved so that canonical output reproduces document order: inline scalars and arrays are
/// emitted first, then sub-tables under <c>[header]</c> sections and arrays of tables under <c>[[header]]</c> sections,
/// each in the order its key first appeared.
/// </remarks>
internal sealed class TomlTableWriterNode
    : TomlWriterNode
{
    /// <summary>
    /// The key/value pairs of the table, in insertion order.
    /// </summary>
    private readonly List<KeyValuePair<string, TomlWriterNode>> _items = [];

    /// <summary>
    /// The set of keys already present, used to reject duplicates.
    /// </summary>
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the key/value pairs of the table, in insertion order.
    /// </summary>
    /// <returns>The ordered key/value pairs.</returns>
    internal IReadOnlyList<KeyValuePair<string, TomlWriterNode>> Items => _items;

    /// <summary>
    /// Indicates whether the table already contains <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to test.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    internal bool ContainsKey(string key) =>
        _keys.Contains(key);

    /// <summary>
    /// Adds a key/value pair to the table.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The buffered value node.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="key" /> has already been written to the table; duplicate keys would make the
    /// emitted document invalid TOML.
    /// </exception>
    internal void Add(string key, TomlWriterNode value)
    {
        if (!_keys.Add(key))
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_WriterDuplicateKey, key));

        _items.Add(new KeyValuePair<string, TomlWriterNode>(key, value));
    }
}
