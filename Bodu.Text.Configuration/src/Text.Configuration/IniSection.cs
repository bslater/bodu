// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents a single <c>[section]</c> block in an INI-style configuration document, exposing its ordered entries and
/// providing O(1) key lookup with optional typed value conversion.
/// </summary>
/// <remarks>
/// <para>
/// The global section (keys that appear before any named section header) is also represented as an
/// <see cref="IniSection" /> whose <see cref="Name" /> is the empty string.
/// </para>
/// <para>
/// Key lookup uses the comparer implied by <see cref="ConfigurationKeyOptions.CaseSensitive" /> at parse time.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// IniSection db = doc.GetSection("database")!;
///
/// // TryGetValue + typed accessors for optional keys.
/// int port = db.TryGetValue("port", out int parsed) ? parsed : 5432;
/// if (db.TryGetValue("password", out string? password))
///     Console.WriteLine($"Password length: {password.Length}");
///
/// // Walk the entries in source order.
/// foreach (IniEntry entry in db.Entries)
///     Console.WriteLine($"{entry.Key} = {entry.Value}");
///]]>
/// </code>
/// </example>
public sealed class IniSection
{
    /// <summary>The entries belonging to this section in source order.</summary>
    private readonly List<IniEntry> _entries;

    /// <summary>The key-to-entry lookup used for fast value resolution within this section.</summary>
    private readonly Dictionary<string, IniEntry> _lookup;

    /// <summary>The comments that immediately precede this section's header in source order.</summary>
    private readonly List<IniComment> _leadingComments = new();

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
    /// <value>
    /// A read-only view onto the section's leading-comment list. The list is empty when no comments precede the
    /// section.
    /// </value>
    public IReadOnlyList<IniComment> LeadingComments => _leadingComments;

    /// <summary>
    /// Appends a leading comment to this section.
    /// </summary>
    /// <param name="comment">The comment to append.</param>
    public void AddLeadingComment(IniComment comment) => _leadingComments.Add(comment);

    /// <summary>
    /// Replaces the leading comments of this section with the supplied sequence.
    /// </summary>
    /// <param name="comments">The new sequence of leading comments.</param>
    /// <exception cref="ArgumentNullException"><paramref name="comments" /> is <see langword="null" />.</exception>
    public void SetLeadingComments(IEnumerable<IniComment> comments)
    {
        ThrowHelper.ThrowIfNull(comments);
        _leadingComments.Clear();
        _leadingComments.AddRange(comments);
    }

    /// <summary>
    /// Removes every leading comment from this section.
    /// </summary>
    public void ClearLeadingComments() => _leadingComments.Clear();

    /// <summary>
    /// Initializes a new instance of the <see cref="IniSection" /> class with the supplied name and entries. Use this
    /// constructor to build sections programmatically without first parsing configuration text.
    /// </summary>
    /// <param name="name">The section name, or an empty string for the global section.</param>
    /// <param name="entries">
    /// The ordered entries for this section. Duplicate keys are resolved by last-wins to mirror
    /// <see cref="DuplicateKeyPolicy.LastWins" />; supply a deduplicated sequence to preserve order exactly.
    /// </param>
    /// <param name="caseSensitiveKeys">
    /// <see langword="true" /> to compare keys with ordinal case sensitivity; otherwise, <see langword="false" /> (the
    /// INI default) for ordinal case-insensitive comparison.
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
                throw new ArgumentException(ConfigurationResourceStrings.Arg_Invalid_IniEntriesContainsNull, nameof(entries));

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
    /// <value>The section name as it appeared in the source, or an empty string for the global section.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the ordered, deduplicated entries in this section.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="IniEntry" /> instances in source order, with duplicates resolved according to the
    /// <see cref="DuplicateKeyPolicy" /> that was active during parsing.
    /// </value>
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
            ? throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, ConfigurationResourceStrings.Op_Invalid_IniSectionKeyNotFound, key))
            : entry.GetValue<T>();
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key as type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the parsed result; otherwise, the default value of
    /// <typeparamref name="T" />.
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
    /// When this method returns <see langword="true" />, contains the string value; otherwise, <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when the key is present; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Attempts to locate the entry with the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="entry">When this method returns <see langword="true" />, the matching entry.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise, <see langword="false" />.</returns>
    public bool TryGetEntry(string key, [NotNullWhen(true)] out IniEntry? entry)
    {
        if (key is null)
        {
            entry = null;
            return false;
        }

        return _lookup.TryGetValue(key, out entry);
    }

    /// <summary>
    /// Adds the supplied entry to this section, replacing an existing entry with the same key.
    /// </summary>
    /// <param name="entry">The entry to add or replace.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entry" /> is <see langword="null" />.</exception>
    public void AddEntry(IniEntry entry)
    {
        ThrowHelper.ThrowIfNull(entry);

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

    /// <summary>
    /// Sets the value for <paramref name="key" />, creating a new entry when no entry with that key exists.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to assign.</param>
    /// <returns>The entry that now holds <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public IniEntry SetEntry(string key, string value)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(value);

        if (_lookup.TryGetValue(key, out IniEntry? existing))
        {
            // Value is immutable; replace the entry in place, preserving its source line and comment trivia.
            IniEntry replacement = new(key, value, existing.LineNumber, existing.LeadingComments)
            {
                InlineComment = existing.InlineComment,
            };
            int idx = _entries.IndexOf(existing);
            _entries[idx] = replacement;
            _lookup[key] = replacement;
            return replacement;
        }

        IniEntry entry = new(key, value);
        _entries.Add(entry);
        _lookup[key] = entry;
        return entry;
    }

    /// <summary>
    /// Removes the entry with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true" /> when an entry was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool RemoveEntry(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_lookup.TryGetValue(key, out IniEntry? entry))
            return false;

        _entries.Remove(entry);
        _lookup.Remove(key);
        return true;
    }

    /// <summary>
    /// Removes every entry from this section.
    /// </summary>
    public void ClearEntries()
    {
        _entries.Clear();
        _lookup.Clear();
    }
}
