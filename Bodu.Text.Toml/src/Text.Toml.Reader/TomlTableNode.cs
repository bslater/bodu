// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTableNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Represents a table in the intermediate value tree — an insertion-ordered map of keys to child
/// <see cref="TomlReaderNode" /> values. A header-defined table, an inline table, the intermediate of a dotted key, and
/// an array-of-tables element are all represented by this type.
/// </summary>
internal sealed class TomlTableNode
    : TomlReaderNode
{
    /// <summary>
    /// The key/value pairs in insertion order.
    /// </summary>
    private readonly List<KeyValuePair<string, TomlReaderNode>> _items = [];

    /// <summary>
    /// Maps a key to its index within <see cref="_items" /> for constant-time lookup.
    /// </summary>
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlTableNode" /> class.
    /// </summary>
    /// <param name="offset">The zero-based source character offset at which the table begins.</param>
    /// <param name="depth">The nesting depth of the table within its tree, where the document root is zero.</param>
    internal TomlTableNode(int offset, int depth = 0)
        : base(offset)
    {
        Depth = depth;
    }

    /// <summary>
    /// Gets the nesting depth of the table within its tree, where the document root is zero.
    /// </summary>
    /// <returns>The zero-based nesting depth.</returns>
    /// <remarks>
    /// The parser uses the depth to bound table nesting created by dotted keys and <c>[a.b.c]</c> headers against the
    /// configured maximum, so that a hostile document cannot drive unbounded recursion when the finished tree is
    /// flattened.
    /// </remarks>
    internal int Depth { get; }

    /// <inheritdoc />
    internal override TomlReaderNodeKind Kind => TomlReaderNodeKind.Table;

    /// <summary>
    /// Gets the key/value pairs of the table in insertion order.
    /// </summary>
    /// <returns>The ordered key/value pairs.</returns>
    internal IReadOnlyList<KeyValuePair<string, TomlReaderNode>> Items => _items;

    /// <summary>
    /// Sets the value associated with <paramref name="key" />, adding it when absent.
    /// </summary>
    /// <param name="key">The key to assign.</param>
    /// <param name="value">The value to associate with the key.</param>
    internal void Set(string key, TomlReaderNode value)
    {
        if (_index.TryGetValue(key, out var i))
        {
            _items[i] = new KeyValuePair<string, TomlReaderNode>(key, value);
        }
        else
        {
            _index[key] = _items.Count;
            _items.Add(new KeyValuePair<string, TomlReaderNode>(key, value));
        }
    }

    /// <summary>
    /// Indicates whether the table contains <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to test.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    internal bool ContainsKey(string key) =>
        _index.ContainsKey(key);

    /// <summary>
    /// Attempts to retrieve the value associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the associated value; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    internal bool TryGetValue(string key, [NotNullWhen(true)] out TomlReaderNode? value)
    {
        if (_index.TryGetValue(key, out var i))
        {
            value = _items[i].Value;
            return true;
        }

        value = null;
        return false;
    }
}
