// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationSection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;

using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents a section in a configuration document. A section is either a glob-pattern-bound group of
/// properties (<c>[*.cs]</c>) or the unique preamble section that holds properties authored before the first
/// section header.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Pattern" /> is <see langword="null" /> for the preamble section; <see cref="IsPreamble" /> derives
/// from this and is the simplest way to discriminate the two cases. The preamble can never have its pattern
/// set after construction; doing so throws <see cref="InvalidOperationException" />.
/// </para>
/// <para>
/// Properties and leading comments are preserved in author order so the writer can round-trip the document
/// faithfully.
/// </para>
/// </remarks>
[DebuggerDisplay("[{Pattern,nq}] ({Properties.Count} properties)")]
public sealed partial class BoduConfigurationSection
{
    private readonly List<BoduConfigurationProperty> _properties = [];
    private readonly List<BoduConfigurationComment> _leadingComments = [];

    /// <summary>
    /// Initializes a new section with the specified glob pattern.
    /// </summary>
    /// <param name="pattern">The section pattern (e.g. <c>*.cs</c>).</param>
    /// <param name="location">The source location at which the section header appears.</param>
    /// <exception cref="ArgumentException"><paramref name="pattern" /> is <see langword="null" />, empty, or
    /// contains only whitespace.</exception>
    public BoduConfigurationSection(string pattern, BoduConfigurationSourceLocation location = default)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(pattern);

        this.Pattern = pattern;
        this.Location = location;
    }

    private BoduConfigurationSection(BoduConfigurationSourceLocation location)
    {
        this.Pattern = null;
        this.Location = location;
    }

    /// <summary>
    /// Gets the section's glob pattern as authored, or <see langword="null" /> when this is the preamble.
    /// </summary>
    /// <returns>The pattern string, or <see langword="null" /> for the preamble.</returns>
    public string? Pattern { get; }

    /// <summary>
    /// Gets a value indicating whether this section is the document's preamble (the section that holds
    /// properties authored before the first <c>[section]</c> header).
    /// </summary>
    /// <returns><see langword="true" /> when this is the preamble; otherwise, <see langword="false" />.</returns>
    public bool IsPreamble => this.Pattern is null;

    /// <summary>
    /// Gets the source location at which the section header was authored. The preamble exposes
    /// <see cref="BoduConfigurationSourceLocation.None" />.
    /// </summary>
    /// <returns>The associated source location.</returns>
    public BoduConfigurationSourceLocation Location { get; }

    /// <summary>
    /// Gets the properties in this section, in author order.
    /// </summary>
    /// <returns>A read-only view backed by the section's mutable property list.</returns>
    public IReadOnlyList<BoduConfigurationProperty> Properties => this._properties;

    /// <summary>
    /// Gets the comments authored on lines preceding this section header.
    /// </summary>
    /// <returns>A read-only view of the leading comments.</returns>
    public IReadOnlyList<BoduConfigurationComment> LeadingComments => this._leadingComments;

    /// <summary>
    /// Creates a preamble section for use as <see cref="BoduConfigurationDocument.Preamble" />.
    /// </summary>
    /// <returns>A new preamble section with no pattern.</returns>
    internal static BoduConfigurationSection CreatePreamble() =>
        new(BoduConfigurationSourceLocation.None);

    /// <summary>
    /// Appends a property to this section.
    /// </summary>
    /// <param name="property">The property to append.</param>
    /// <exception cref="ArgumentNullException"><paramref name="property" /> is <see langword="null" />.</exception>
    public void AddProperty(BoduConfigurationProperty property)
    {
        ThrowHelper.ThrowIfNull(property);
        this._properties.Add(property);
    }

    /// <summary>
    /// Replaces the property at <paramref name="index" /> with <paramref name="property" />, preserving the
    /// position in author order. Used by the reader to honour <see cref="IniDuplicateKeyBehavior.LastWins" />
    /// when a duplicate key is encountered.
    /// </summary>
    /// <param name="index">The zero-based index of the property to replace.</param>
    /// <param name="property">The new property.</param>
    /// <exception cref="ArgumentNullException"><paramref name="property" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is outside the valid range.</exception>
    internal void ReplacePropertyAt(int index, BoduConfigurationProperty property)
    {
        ThrowHelper.ThrowIfNull(property);
        if ((uint)index >= (uint)this._properties.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        this._properties[index] = property;
    }

    /// <summary>
    /// Appends a leading comment to this section header.
    /// </summary>
    /// <param name="comment">The comment to append.</param>
    public void AddLeadingComment(BoduConfigurationComment comment) =>
        this._leadingComments.Add(comment);
}
