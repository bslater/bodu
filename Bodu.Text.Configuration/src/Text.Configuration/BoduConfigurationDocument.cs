// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocument.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents an in-memory Bodu Text Configuration document. Holds the preamble section, ordered sections,
/// diagnostics gathered during parsing, and the optional <c>root</c> flag.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Parse(string, BoduConfigurationParseOptions?)" /> or
/// <see cref="Load(string, BoduConfigurationParseOptions?)" /> to obtain a document from text or a file.
/// Use <see cref="Resolve(string?, BoduConfigurationResolveOptions?)" /> to project the document into a
/// resolved <see cref="BoduConfigurationView" /> for a specific target path.
/// </para>
/// <para>
/// The document model is intentionally mutable so that authoring scenarios (tools that load, modify, and save)
/// can be supported. The reader populates the document; subsequent mutation is unconstrained.
/// </para>
/// </remarks>
[DebuggerDisplay("Sections={Sections.Count} Diagnostics={Diagnostics.Length}")]
public sealed partial class BoduConfigurationDocument
{
    private readonly List<BoduConfigurationSection> _sections = [];
    private ImmutableArray<BoduConfigurationDiagnostic> _diagnostics = ImmutableArray<BoduConfigurationDiagnostic>.Empty;

    /// <summary>
    /// Initializes an empty configuration document with a fresh preamble.
    /// </summary>
    /// <param name="parseOptions">The parse options used to build this document, or <see langword="null" />
    /// for the defaults. Retained so that subsequent mutation can honour the same key options.</param>
    public BoduConfigurationDocument(BoduConfigurationParseOptions? parseOptions = null)
    {
        this.Preamble = BoduConfigurationSection.CreatePreamble();
        this.ParseOptions = parseOptions ?? BoduConfigurationParseOptions.Bodu;
    }

    /// <summary>
    /// Gets the preamble section that holds properties authored before the first <c>[section]</c> header.
    /// </summary>
    /// <returns>The preamble section. Always non-null and has <see cref="BoduConfigurationSection.IsPreamble" />
    /// equal to <see langword="true" />.</returns>
    public BoduConfigurationSection Preamble { get; }

    /// <summary>
    /// Gets a convenience alias for <see cref="Preamble" />, matching the
    /// <c>Microsoft.Extensions.Configuration</c> mental model in which top-level keys live in a "global"
    /// namespace.
    /// </summary>
    /// <returns>The preamble section.</returns>
    public BoduConfigurationSection Global => this.Preamble;

    /// <summary>
    /// Gets the ordered list of sections that follow the preamble, in author order.
    /// </summary>
    /// <returns>A read-only view of the document's sections.</returns>
    public IReadOnlyList<BoduConfigurationSection> Sections => this._sections;

    /// <summary>
    /// Gets the parse options used to materialize this document. These options control how subsequent
    /// mutation operations (such as <c>Set</c>) interpret raw keys.
    /// </summary>
    /// <returns>The parse options supplied at construction.</returns>
    public BoduConfigurationParseOptions ParseOptions { get; }

    /// <summary>
    /// Gets the value of the special preamble <c>root</c> property when present and parseable, otherwise
    /// <see langword="null" />.
    /// </summary>
    /// <returns>The parsed <c>root</c> value, or <see langword="null" /> when the property was absent or
    /// could not be parsed.</returns>
    public bool? Root
    {
        get
        {
            if (this.Preamble.TryGetProperty("root", out BoduConfigurationProperty? property, this.ParseOptions.KeyOptions)
                && bool.TryParse(property.Value, out bool value))
            {
                return value;
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the diagnostics gathered during parsing, in encounter order.
    /// </summary>
    /// <returns>An immutable, possibly empty array of diagnostics.</returns>
    public ImmutableArray<BoduConfigurationDiagnostic> Diagnostics => this._diagnostics;

    /// <summary>
    /// Appends a section to the document.
    /// </summary>
    /// <param name="section">The section to append.</param>
    /// <exception cref="ArgumentNullException"><paramref name="section" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="section" /> is the preamble or a preamble created
    /// by another document.</exception>
    public void AddSection(BoduConfigurationSection section)
    {
        ThrowHelper.ThrowIfNull(section);
        if (section.IsPreamble)
            throw new ArgumentException("The preamble cannot be added as a regular section.", nameof(section));

        this._sections.Add(section);
    }

    /// <summary>
    /// Gets the first existing section whose pattern matches <paramref name="pattern" /> exactly, or appends
    /// a new section with that pattern if none exists.
    /// </summary>
    /// <param name="pattern">The section pattern.</param>
    /// <returns>The matching or newly created section.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern" /> is <see langword="null" />, empty, or
    /// contains only whitespace.</exception>
    public BoduConfigurationSection GetOrAddSection(string pattern)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(pattern);

        foreach (BoduConfigurationSection section in this._sections)
        {
            if (string.Equals(section.Pattern, pattern, StringComparison.Ordinal))
                return section;
        }

        BoduConfigurationSection created = new(pattern);
        this._sections.Add(created);
        return created;
    }

    /// <summary>
    /// Replaces the document's diagnostics with the supplied collection.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to retain on the document.</param>
    internal void SetDiagnostics(ImmutableArray<BoduConfigurationDiagnostic> diagnostics)
    {
        this._diagnostics = diagnostics.IsDefault
            ? ImmutableArray<BoduConfigurationDiagnostic>.Empty
            : diagnostics;
    }
}
