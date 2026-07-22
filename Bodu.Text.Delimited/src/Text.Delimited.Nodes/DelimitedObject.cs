// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedObject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

using Bodu.Text.Delimited.Writer;

namespace Bodu.Text.Delimited.Nodes;

/// <summary>
/// Represents a header-keyed record object in a mutable delimited document tree: an insertion-ordered map of column
/// names to <see cref="DelimitedNode" /> field values.
/// </summary>
public sealed class DelimitedObject
    : DelimitedNode
{
    /// <summary>The field values, keyed by column name.</summary>
    private readonly Dictionary<string, DelimitedNode> _fields = new(StringComparer.Ordinal);

    /// <summary>The column names in insertion order.</summary>
    private readonly List<string> _order = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedObject" /> class.
    /// </summary>
    public DelimitedObject()
    {
    }

    /// <inheritdoc />
    public override DelimitedValueKind ValueKind => DelimitedValueKind.Object;

    /// <summary>
    /// Gets the number of fields in this record.
    /// </summary>
    /// <value>The field count.</value>
    public int Count => _order.Count;

    /// <summary>
    /// Gets the column names in insertion order.
    /// </summary>
    /// <value>The ordered column names.</value>
    public IReadOnlyList<string> Keys => _order;

    /// <summary>
    /// Gets or sets the field value for the specified column, adding the column at the end when it is new.
    /// </summary>
    /// <param name="key">The column name.</param>
    /// <value>The field value node.</value>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or the assigned value is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown by the getter when <paramref name="key" /> is not present.
    /// </exception>
    public DelimitedNode this[string key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);

            return _fields[key];
        }

        set
        {
            ThrowHelper.ThrowIfNull(key);
            ThrowHelper.ThrowIfNull(value);

            if (!_fields.ContainsKey(key))
                _order.Add(key);

            _fields[key] = value;
        }
    }

    /// <summary>
    /// Determines whether this record contains the specified column.
    /// </summary>
    /// <param name="key">The column name.</param>
    /// <returns><see langword="true" /> when the column is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool ContainsKey(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _fields.ContainsKey(key);
    }

    /// <summary>
    /// Attempts to get the field value for the specified column.
    /// </summary>
    /// <param name="key">The column name.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the field value; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when the column is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetValue(string key, [NotNullWhen(true)] out DelimitedNode? value)
    {
        ThrowHelper.ThrowIfNull(key);

        return _fields.TryGetValue(key, out value);
    }

    /// <summary>
    /// Removes the field with the specified column name.
    /// </summary>
    /// <param name="key">The column name.</param>
    /// <returns><see langword="true" /> when a field was removed; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool Remove(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_fields.Remove(key))
            return false;

        _order.Remove(key);
        return true;
    }

    /// <inheritdoc />
    public override DelimitedNode DeepClone()
    {
        var clone = new DelimitedObject();
        foreach (string key in _order)
            clone[key] = _fields[key].DeepClone();

        return clone;
    }

    /// <inheritdoc />
    public override void WriteTo(ref Utf8DelimitedWriter writer)
    {
        writer.WriteStartObject();
        foreach (string key in _order)
        {
            writer.WritePropertyName(key);
            _fields[key].WriteTo(ref writer);
        }

        writer.WriteEndObject();
    }
}
