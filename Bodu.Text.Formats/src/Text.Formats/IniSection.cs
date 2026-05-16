// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a single <c>[section]</c> block in an INI document, exposing its ordered entries and providing
/// O(1) key lookup with optional typed value conversion.
/// </summary>
/// <remarks>
/// <para>
/// The global section (keys that appear before any named section header) is also represented as an
/// <see cref="IniSection" /> whose <see cref="Name" /> is the empty string.
/// </para>
/// <para>
/// Key lookup uses the comparer configured via <see cref="IniParseOptions.CaseSensitiveKeys" /> at parse time.
/// </para>
/// </remarks>
public sealed class IniSection
{
    /// <summary>Cached format for the <c>KeyNotFound</c> message.</summary>
    private static readonly CompositeFormat s_keyNotFound =
        CompositeFormat.Parse(FormatsResourceStrings.IniSection_KeyNotFound);

    private static readonly IReadOnlyList<IniComment> s_emptyComments = Array.Empty<IniComment>();

    private readonly List<IniEntry> _entries;
    private readonly Dictionary<string, IniEntry> _lookup;
    private IReadOnlyList<IniComment> _leadingComments = s_emptyComments;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniSection" /> class.
    /// </summary>
    /// <param name="name">The section name, or an empty string for the global section.</param>
    /// <param name="entries">The ordered list of deduplicated entries for this section.</param>
    /// <param name="lookup">The key-to-entry lookup dictionary built with the appropriate comparer.</param>
    internal IniSection(string name, List<IniEntry> entries, Dictionary<string, IniEntry> lookup)
    {
        Name = name;
        _entries = entries;
        _lookup = lookup;
        Entries = _entries.AsReadOnly();
    }

    /// <summary>
    /// Gets the comments authored on lines preceding this section header, in source order.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="IniComment" />. The list is empty when no comments precede the section
    /// or when <see cref="IniParseOptions.PreserveComments" /> was <see langword="false" />.
    /// </returns>
    public IReadOnlyList<IniComment> LeadingComments => _leadingComments;

    /// <summary>
    /// Replaces the leading comments associated with this section. Used by the parser when constructing the
    /// section before its entries have been attached.
    /// </summary>
    /// <param name="comments">The comments to associate with this section.</param>
    internal void SetLeadingComments(IReadOnlyList<IniComment> comments) =>
        _leadingComments = comments ?? s_emptyComments;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniSection" /> class with the supplied name and entries.
    /// Use this constructor to build sections programmatically without first parsing INI text.
    /// </summary>
    /// <param name="name">The section name, or an empty string for the global section.</param>
    /// <param name="entries">
    /// The ordered entries for this section. Duplicate keys are resolved by last-wins to mirror
    /// <see cref="IniDuplicateKeyBehavior.LastWins" />; supply a deduplicated sequence to preserve order exactly.
    /// </param>
    /// <param name="caseSensitiveKeys">
    /// <see langword="true" /> to compare keys with ordinal case sensitivity; otherwise,
    /// <see langword="false" /> (the INI default) for ordinal case-insensitive comparison.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> or <paramref name="entries" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="entries" /> contains a <see langword="null" /> entry.
    /// </exception>
    public IniSection(string name, IEnumerable<IniEntry> entries, bool caseSensitiveKeys = false)
    {
        ThrowHelper.ThrowIfNull(name);
        ThrowHelper.ThrowIfNull(entries);

        Name = name;
        _entries = new List<IniEntry>();
        _lookup = new Dictionary<string, IniEntry>(caseSensitiveKeys ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        foreach (IniEntry entry in entries)
        {
            if (entry is null)
                throw new ArgumentException("Entries sequence contains a null entry.", nameof(entries));

            if (_lookup.TryGetValue(entry.Key, out IniEntry? existing))
            {
                int idx = _entries.IndexOf(existing);
                _entries[idx] = entry;
                _lookup[entry.Key] = entry;
            }
            else
            {
                _entries.Add(entry);
                _lookup[entry.Key] = entry;
            }
        }

        Entries = _entries.AsReadOnly();
    }

    /// <summary>
    /// Gets the name of this section.
    /// </summary>
    /// <returns>
    /// The section name as it appeared in the source, or an empty string for the global section.
    /// </returns>
    public string Name { get; }

    /// <summary>
    /// Gets the ordered, deduplicated entries in this section.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="IniEntry" /> instances in source order, with duplicates resolved
    /// according to the <see cref="IniDuplicateKeyBehavior" /> that was active during parsing.
    /// </returns>
    public IReadOnlyList<IniEntry> Entries { get; }

    /// <summary>
    /// Gets the value associated with the specified key, or <see langword="null" /> if the key is absent.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The string value, or <see langword="null" /> when the key is not present.</returns>
    public string? this[string key] =>
        key is not null && _lookup.TryGetValue(key, out IniEntry? entry) ? entry.Value : null;

    /// <summary>
    /// Gets the value associated with the specified key as type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when <paramref name="key" /> is not present in this section.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the value string cannot be parsed as <typeparamref name="T" />.
    /// </exception>
    public T GetValue<T>(string key)
        where T : ISpanParsable<T>
    {
        ThrowHelper.ThrowIfNull(key);

        return !_lookup.TryGetValue(key, out IniEntry? entry)
            ? throw new KeyNotFoundException(string.Format(CultureInfo.InvariantCulture, s_keyNotFound, key))
            : entry.GetValue<T>();
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key as type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the parsed result; otherwise, the default value
    /// of <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the key is present and its value was successfully parsed as
    /// <typeparamref name="T" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value)
        where T : ISpanParsable<T>
    {
        if (key is null || !_lookup.TryGetValue(key, out IniEntry? entry))
        {
            value = default;
            return false;
        }

        return entry.TryGetValue<T>(out value);
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the string value; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the key is present; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        if (key is not null && _lookup.TryGetValue(key, out IniEntry? entry))
        {
            value = entry.Value;
            return true;
        }

        value = null;
        return false;
    }
}
