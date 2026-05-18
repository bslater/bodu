// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocument.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a parsed INI document, providing access to the global section and all named sections.
/// </summary>
/// <remarks>
/// <para>
/// Obtain an instance by calling <see cref="Ini.Parse(ReadOnlySpan{char})" /> or
/// <see cref="Ini.TryParse(ReadOnlySpan{char}, out IniDocument)" />.
/// </para>
/// <para>
/// Section lookup uses the comparer configured via <see cref="IniParseOptions.CaseSensitiveSections" /> at parse time.
/// </para>
/// </remarks>
public sealed class IniDocument
{
    private readonly List<IniSection> _sections;
    private readonly Dictionary<string, IniSection> _lookup;
    private readonly bool _caseSensitiveSections;

    /// <summary>
    /// Initializes a new <see cref="IniDocument" /> with an empty global section and no named sections.
    /// </summary>
    /// <param name="caseSensitiveSections">
    /// <see langword="true" /> to compare section names with ordinal case sensitivity; otherwise,
    /// <see langword="false" /> (the INI default).
    /// </param>
    public IniDocument(bool caseSensitiveSections = false)
    {
        GlobalSection = new IniSection(string.Empty, Array.Empty<IniEntry>());
        _sections = new List<IniSection>();
        _lookup = new Dictionary<string, IniSection>(caseSensitiveSections ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
        _caseSensitiveSections = caseSensitiveSections;
        Sections = _sections.AsReadOnly();
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
    {
        GlobalSection = globalSection;
        _sections = sections;
        _lookup = lookup;
        _caseSensitiveSections = lookup.Comparer == StringComparer.Ordinal;
        Sections = _sections.AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniDocument" /> class from a global section and an ordered sequence
    /// of named sections. Use this constructor to build documents programmatically without first parsing INI text.
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
    {
        ThrowHelper.ThrowIfNull(globalSection);
        ThrowHelper.ThrowIfNull(sections);

        if (globalSection.Name.Length != 0)
            throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_IniGlobalSectionNotEmpty, nameof(globalSection));

        GlobalSection = globalSection;
        _sections = new List<IniSection>();
        _lookup = new Dictionary<string, IniSection>(caseSensitiveSections ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
        _caseSensitiveSections = caseSensitiveSections;

        foreach (IniSection section in sections)
        {
            if (section is null)
                throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_IniSectionsContainsNull, nameof(sections));

            _sections.Add(section);
            if (!_lookup.ContainsKey(section.Name))
                _lookup[section.Name] = section;
        }

        Sections = _sections.AsReadOnly();
    }

    /// <summary>
    /// Gets the global section, which contains any key/value entries that appeared before the first named section
    /// header.
    /// </summary>
    /// <returns>
    /// An <see cref="IniSection" /> whose <see cref="IniSection.Name" /> is the empty string. Never
    /// <see langword="null" />; its <see cref="IniSection.Entries" /> list is empty when the source contained no
    /// pre-section keys.
    /// </returns>
    public IniSection GlobalSection { get; }

    /// <summary>
    /// Gets the named sections in the order they first appeared in the source.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="IniSection" /> instances. Does not include
    /// <see cref="GlobalSection" />.
    /// </returns>
    public IReadOnlyList<IniSection> Sections { get; }

    /// <summary>
    /// Gets the named section with the specified name, or <see langword="null" /> if it is absent.
    /// </summary>
    /// <param name="name">The section name to look up.</param>
    /// <returns>The matching <see cref="IniSection" />, or <see langword="null" /> when not found.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public IniSection? GetSection(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        return _lookup.TryGetValue(name, out IniSection? section) ? section : null;
    }

    /// <summary>
    /// Gets the named section with the specified name.
    /// </summary>
    /// <param name="name">The section name to look up.</param>
    /// <param name="section">
    /// When this method returns <see langword="true" />, contains the matching section; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the section is present; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetSection(string name, [NotNullWhen(true)] out IniSection? section)
    {
        ThrowHelper.ThrowIfNull(name);

        return _lookup.TryGetValue(name, out section);
    }

    /// <summary>
    /// Appends <paramref name="section" /> to the document.
    /// </summary>
    /// <param name="section">The section to append.</param>
    /// <exception cref="ArgumentNullException"><paramref name="section" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="section" /> has an empty <see cref="IniSection.Name" /> (i.e. it is the global section).
    /// </exception>
    public void AddSection(IniSection section)
    {
        ThrowHelper.ThrowIfNull(section);
        if (section.Name.Length == 0)
            throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_IniSectionCannotAddGlobal, nameof(section));

        _sections.Add(section);
        if (!_lookup.ContainsKey(section.Name))
            _lookup[section.Name] = section;
    }

    /// <summary>
    /// Returns the first section with the supplied name, creating and appending one when no match exists.
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <returns>The matching or newly created section.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is empty.</exception>
    public IniSection GetOrAddSection(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        if (name.Length == 0)
            throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_IniSectionNameEmpty, nameof(name));

        if (_lookup.TryGetValue(name, out IniSection? existing))
            return existing;

        IniSection section = new(name, Array.Empty<IniEntry>(), _caseSensitiveSections);
        _sections.Add(section);
        _lookup[name] = section;
        return section;
    }

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
    public bool RemoveSection(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        IEqualityComparer<string> comparer = _lookup.Comparer;
        bool removed = false;
        for (int i = _sections.Count - 1; i >= 0; i--)
        {
            if (comparer.Equals(_sections[i].Name, name))
            {
                _sections.RemoveAt(i);
                removed = true;
            }
        }

        _lookup.Remove(name);
        return removed;
    }
}
