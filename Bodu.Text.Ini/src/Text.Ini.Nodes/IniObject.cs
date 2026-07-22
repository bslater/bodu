// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniObject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Bodu.Text.Ini.Writer;

namespace Bodu.Text.Ini.Nodes;

/// <summary>
/// Represents an object in a mutable INI document tree: an insertion-ordered map of names to nodes. The document root
/// maps global keys to <see cref="IniValue" /> leaves and section names to section <see cref="IniObject" /> instances;
/// a section maps key names to <see cref="IniValue" /> leaves only.
/// </summary>
/// <remarks>
/// <para>
/// INI is physically two-level, so depth is enforced when the tree is written: an <see cref="IniObject" /> nested
/// inside a section raises <see cref="InvalidOperationException" /> from <see cref="WriteTo(ref Utf8IniWriter)" />.
/// </para>
/// <para>
/// When the root is written, its <see cref="IniValue" /> entries are emitted before its section objects regardless of
/// insertion interleave — a key emitted after a section header would be re-read as belonging to that section, so the
/// global entries must precede the first header.
/// </para>
/// </remarks>
public sealed class IniObject
    : IniNode
{
    /// <summary>The entries, keyed by name for O(1) lookup.</summary>
    private readonly Dictionary<string, IniNode> _entries = new(StringComparer.Ordinal);

    /// <summary>The entry names in insertion order.</summary>
    private readonly List<string> _order = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="IniObject" /> class.
    /// </summary>
    public IniObject()
    {
        TrailingComments = [];
    }

    /// <inheritdoc />
    public override IniValueKind ValueKind => IniValueKind.Object;

    /// <summary>
    /// Gets the comment lines at the end of this object's scope, without their leading comment prefix character.
    /// </summary>
    /// <value>The mutable trailing-comment list; empty when the scope ends without a comment block.</value>
    /// <remarks>
    /// When parsing, comment lines that are not followed by another section or entry — the block at the end of the
    /// document — attach here on the innermost object being read. When writing, the block is emitted after the object's
    /// entries.
    /// </remarks>
    public IList<string> TrailingComments { get; }

    /// <summary>
    /// Gets the number of entries in this object.
    /// </summary>
    /// <value>The entry count.</value>
    public int Count => _order.Count;

    /// <summary>
    /// Gets the entry names in insertion order.
    /// </summary>
    /// <value>The ordered entry names.</value>
    public IReadOnlyList<string> Keys => _order;

    /// <summary>
    /// Gets or sets the node associated with the specified name, adding the name at the end when it is new and
    /// preserving its position when it already exists.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <value>The node associated with <paramref name="name" />.</value>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> or the assigned value is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown by the getter when <paramref name="name" /> is not present.
    /// </exception>
    public IniNode this[string name]
    {
        get
        {
            ThrowHelper.ThrowIfNull(name);

            return _entries[name];
        }

        set
        {
            ThrowHelper.ThrowIfNull(name);
            ThrowHelper.ThrowIfNull(value);

            SetNode(name, value);
        }
    }

    /// <summary>
    /// Determines whether this object contains the specified name.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <returns><see langword="true" /> when the name is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public bool ContainsKey(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        return _entries.ContainsKey(name);
    }

    /// <summary>
    /// Attempts to get the node associated with the specified name.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="node">
    /// When this method returns <see langword="true" />, the node; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when the name is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetValue(string name, [NotNullWhen(true)] out IniNode? node)
    {
        ThrowHelper.ThrowIfNull(name);

        return _entries.TryGetValue(name, out node);
    }

    /// <summary>
    /// Removes the entry with the specified name.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <returns><see langword="true" /> when an entry was removed; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public bool Remove(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        if (!_entries.Remove(name))
            return false;

        _order.Remove(name);
        return true;
    }

    /// <inheritdoc />
    public override IniNode DeepClone()
    {
        var clone = new IniObject();
        foreach (string comment in LeadingComments)
            clone.LeadingComments.Add(comment);

        foreach (string comment in TrailingComments)
            clone.TrailingComments.Add(comment);

        foreach (string name in _order)
            clone.SetNode(name, _entries[name].DeepClone());

        return clone;
    }

    /// <inheritdoc />
    public override void WriteTo(ref Utf8IniWriter writer)
    {
        foreach (string name in _order)
        {
            if (_entries[name] is IniValue value)
                WriteEntry(ref writer, name, value);
        }

        foreach (string name in _order)
        {
            if (_entries[name] is IniObject section)
            {
                WriteComments(ref writer, section.LeadingComments);
                writer.WriteSectionHeader(name);
                section.WriteSectionEntries(ref writer);
            }
        }

        WriteComments(ref writer, TrailingComments);
    }

    /// <summary>
    /// Adds or replaces an entry, preserving insertion position on replacement.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="node">The node to associate with <paramref name="name" />.</param>
    internal void SetNode(string name, IniNode node)
    {
        if (!_entries.ContainsKey(name))
            _order.Add(name);

        _entries[name] = node;
    }

    /// <summary>
    /// Writes this object's entries as the body of a section, enforcing the two-level depth limit.
    /// </summary>
    /// <param name="writer">The writer that receives the INI bytes.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an entry is itself an <see cref="IniObject" />, which INI cannot represent inside a section.
    /// </exception>
    private void WriteSectionEntries(ref Utf8IniWriter writer)
    {
        foreach (string name in _order)
        {
            if (_entries[name] is not IniValue value)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniDepthExceeded, name));
            }

            WriteEntry(ref writer, name, value);
        }

        WriteComments(ref writer, TrailingComments);
    }

    /// <summary>
    /// Writes one <c>key=value</c> entry preceded by its leading comments.
    /// </summary>
    /// <param name="writer">The writer that receives the INI bytes.</param>
    /// <param name="name">The entry name.</param>
    /// <param name="value">The entry value node.</param>
    private static void WriteEntry(ref Utf8IniWriter writer, string name, IniValue value)
    {
        WriteComments(ref writer, value.LeadingComments);
        writer.WritePropertyName(name);
        value.WriteTo(ref writer);
    }

    /// <summary>
    /// Writes each supplied comment as one comment line.
    /// </summary>
    /// <param name="writer">The writer that receives the INI bytes.</param>
    /// <param name="comments">The comment lines to write.</param>
    private static void WriteComments(ref Utf8IniWriter writer, IList<string> comments)
    {
        foreach (string comment in comments)
            writer.WriteComment(comment);
    }
}
