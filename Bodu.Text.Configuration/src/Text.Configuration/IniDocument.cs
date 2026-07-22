// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents a mutable INI-style document, providing access to the global section and all named sections, together
/// with a mutation surface for authoring documents programmatically.
/// </summary>
/// <remarks>
/// <para>
/// Build an instance from scratch using the constructors and the <see cref="AddSection(IniSection)" />/<see cref="GetOrAddSection(string)" />
/// members; a parsed document is obtained through
/// <see cref="ConfigurationDocument.Parse(string, ConfigurationParseOptions?)" />, which exposes the same base read
/// surface.
/// </para>
/// <para>
/// Section lookup uses ordinal comparison, case-insensitive by default and case-sensitive when the document was created
/// with that option.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var doc = new IniDocument();
/// IniSection db = doc.GetOrAddSection("database");
/// db.SetEntry("host", "localhost");
///
/// // Walk every named section.
/// foreach (IniSection section in doc.Sections)
///     Console.WriteLine($"[{section.Name}] — {section.Entries.Count} entries");
///
/// // Read the global section (keys that appeared before any header).
/// IniSection global = doc.GlobalSection;
///
/// // Lookup by section name.
/// if (doc.TryGetSection("database", out IniSection? found))
///     Console.WriteLine(found!["host"]);
///]]>
/// </code>
/// </example>
public sealed class IniDocument
    : IniDocumentBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IniDocument" /> class with an empty global section and no named
    /// sections.
    /// </summary>
    /// <param name="caseSensitiveSections">
    /// <see langword="true" /> to compare section names with ordinal case sensitivity; otherwise,
    /// <see langword="false" /> (the INI default).
    /// </param>
    public IniDocument(bool caseSensitiveSections = false)
        : base(caseSensitiveSections)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniDocument" /> class.
    /// </summary>
    /// <param name="globalSection">The global section (keys before the first named section header).</param>
    /// <param name="sections">Named sections in source order.</param>
    /// <param name="lookup">Section-name-to-section lookup with the appropriate comparer.</param>
    internal IniDocument(
        IniSection globalSection,
        List<IniSection> sections,
        Dictionary<string, IniSection> lookup)
        : base(globalSection, sections, lookup)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniDocument" /> class from a global section and an ordered sequence
    /// of named sections. Use this constructor to build documents programmatically without first parsing configuration
    /// text.
    /// </summary>
    /// <param name="globalSection">
    /// The global section (entries authored before the first named section header). Its <see cref="IniSection.Name" />
    /// must be the empty string.
    /// </param>
    /// <param name="sections">The ordered, named sections that follow the global section.</param>
    /// <param name="caseSensitiveSections">
    /// <see langword="true" /> to compare section names with ordinal case sensitivity; otherwise,
    /// <see langword="false" /> (the INI default).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="globalSection" /> or <paramref name="sections" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="globalSection" /> has a non-empty <see cref="IniSection.Name" />, or when
    /// <paramref name="sections" /> contains a <see langword="null" /> entry.
    /// </exception>
    public IniDocument(IniSection globalSection, IEnumerable<IniSection> sections, bool caseSensitiveSections = false)
        : base(globalSection, sections, caseSensitiveSections)
    {
    }

    /// <summary>
    /// Appends <paramref name="section" /> to the document.
    /// </summary>
    /// <param name="section">The section to append.</param>
    /// <exception cref="ArgumentNullException"><paramref name="section" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="section" /> has an empty <see cref="IniSection.Name" /> (i.e. it is the global section).
    /// </exception>
    public void AddSection(IniSection section) =>
        AddSectionCore(section);

    /// <summary>
    /// Returns the first section with the supplied name, creating and appending one when no match exists.
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <returns>The matching or newly created section.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is empty.</exception>
    public IniSection GetOrAddSection(string name) =>
        GetOrAddSectionCore(name);

    /// <summary>
    /// Removes the section with the supplied name. When multiple sections share the same name (under
    /// <see cref="IniDuplicateSectionBehavior.Preserve" /> or <see cref="IniDuplicateSectionBehavior.MergeAdjacent" />
    /// ), every occurrence is removed.
    /// </summary>
    /// <param name="name">The section name to remove.</param>
    /// <returns>
    /// <see langword="true" /> when at least one section was removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    public bool RemoveSection(string name) =>
        RemoveSectionCore(name);
}
