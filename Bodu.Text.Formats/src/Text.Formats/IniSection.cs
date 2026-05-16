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

    private readonly List<IniEntry> _entries;
    private readonly Dictionary<string, IniEntry> _lookup;

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
