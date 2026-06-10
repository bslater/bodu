// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTableSyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Toml.Syntax;

/// <summary>
/// Represents a TOML table — an ordered, mutable collection of key/value pairs. The root of every document is a table,
/// as is every <c>[table]</c> section, inline <c>{ }</c> table, and array-of-tables element.
/// </summary>
/// <remarks>
/// Keys are case-sensitive Unicode strings compared with ordinal semantics, and insertion order is preserved for
/// deterministic enumeration and writer output.
/// </remarks>
public sealed class TomlTableSyntax
    : TomlSyntaxNode
{
    /// <summary>
    /// The key/value pairs in insertion order.
    /// </summary>
    private readonly List<KeyValuePair<string, TomlSyntaxNode>> _items = [];

    /// <summary>
    /// Maps a key to its index within <see cref="_items" /> for constant-time lookup.
    /// </summary>
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlTableSyntax" /> class.
    /// </summary>
    /// <param name="span">The span the table covers in the source.</param>
    public TomlTableSyntax(SourceSpan span = default)
        : base(TomlSyntaxKind.Table, span)
    {
    }

    /// <summary>
    /// Gets the number of key/value pairs in the table.
    /// </summary>
    /// <returns>The pair count.</returns>
    public int Count => _items.Count;

    /// <summary>
    /// Gets the key/value pairs in insertion order.
    /// </summary>
    /// <returns>The ordered pairs.</returns>
    public IReadOnlyList<KeyValuePair<string, TomlSyntaxNode>> Items => _items;

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to look up or assign.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or the assigned value is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown by the getter when the key is not present.</exception>
    public TomlSyntaxNode this[string key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);
            return _index.TryGetValue(key, out var i) ? _items[i].Value : throw new KeyNotFoundException();
        }

        set
        {
            ThrowHelper.ThrowIfNull(key);
            ThrowHelper.ThrowIfNull(value);
            value.Parent = this;
            if (_index.TryGetValue(key, out var i))
            {
                _items[i] = new KeyValuePair<string, TomlSyntaxNode>(key, value);
            }
            else
            {
                _index[key] = _items.Count;
                _items.Add(new KeyValuePair<string, TomlSyntaxNode>(key, value));
            }
        }
    }

    /// <summary>
    /// Indicates whether the table contains the specified key.
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
    /// Attempts to retrieve the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns <see langword="true" />, the associated value.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetValue(string key, [NotNullWhen(true)] out TomlSyntaxNode? value)
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
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens()
    {
        foreach (KeyValuePair<string, TomlSyntaxNode> pair in _items)
            yield return pair.Value;
    }
}
