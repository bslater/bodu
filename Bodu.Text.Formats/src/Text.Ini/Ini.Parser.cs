// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ini.Parser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.Ini;

public static partial class Ini
{
    /// <summary>
    /// Throws an <see cref="IniFormatException" /> for a duplicate key.
    /// </summary>
    /// <param name="key">The duplicate key name.</param>
    /// <param name="lineNumber">The 1-based line number where the duplicate key was found.</param>
    [DoesNotReturn]
    private static void ThrowDuplicateKey(string key, int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_IniDuplicateKey, key, lineNumber), lineNumber);

    /// <summary>
    /// Throws an <see cref="IniFormatException" /> for a duplicate section name.
    /// </summary>
    /// <param name="name">The duplicate section name.</param>
    /// <param name="lineNumber">The 1-based line number where the duplicate section header was found.</param>
    [DoesNotReturn]
    private static void ThrowDuplicateSection(string name, int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_IniDuplicateSection, name, lineNumber), lineNumber);

    /// <summary>
    /// Throws an <see cref="IniFormatException" /> when a key appears before the first section header and global
    /// entries are disallowed.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number of the disallowed global key.</param>
    [DoesNotReturn]
    internal static void ThrowGlobalKeyDisallowed(int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_IniGlobalKeyDisallowed, lineNumber), lineNumber);

    /// <summary>
    /// Throws an <see cref="IniFormatException" /> for a malformed section header.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number of the malformed section header.</param>
    [DoesNotReturn]
    internal static void ThrowMalformedSectionHeader(int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_IniMalformedSectionHeader, lineNumber), lineNumber);

    /// <summary>
    /// Throws an <see cref="IniFormatException" /> for a property line with an empty key.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number of the property line with the empty key.</param>
    [DoesNotReturn]
    internal static void ThrowMissingKey(int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_IniMissingKey, lineNumber), lineNumber);

    /// <summary>
    /// Provides line-by-line INI parsing over a <see cref="ReadOnlySpan{T}" /> of characters.
    /// </summary>
    private ref struct Parser
    {
        /// <summary>
        /// The parse options that govern section, comment, and duplicate-key behavior.
        /// </summary>
        private readonly IniParseOptions _options;

        /// <summary>
        /// The unconsumed portion of the source text remaining to be parsed.
        /// </summary>
        private ReadOnlySpan<char> _remaining;

        /// <summary>
        /// The zero-based line number of the current parse position.
        /// </summary>
        private int _lineNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser" /> struct.
        /// </summary>
        /// <param name="source">The full INI source text.</param>
        /// <param name="options">Options that control parsing behaviour.</param>
        public Parser(ReadOnlySpan<char> source, IniParseOptions options)
        {
            _remaining = source;
            _lineNumber = 0;
            _options = options;
        }

        /// <summary>
        /// Parses the source text and returns a fully constructed <see cref="IniDocument" />.
        /// </summary>
        /// <returns>The parsed document.</returns>
        /// <exception cref="IniFormatException">Thrown when the source is malformed.</exception>
        public IniDocument Parse()
        {
            StringComparer keyComparer = _options.CaseSensitiveKeys
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;

            StringComparer sectionComparer = _options.CaseSensitiveSections
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;

            List<IniEntry> globalEntries = new();
            Dictionary<string, IniEntry> globalLookup = new(keyComparer);
            List<IniComment> globalLeadingComments = new();

            List<IniEntry> currentEntries = globalEntries;
            Dictionary<string, IniEntry> currentLookup = globalLookup;
            bool inGlobal = true;

            // Named section builders — kept as mutable lists/dicts until IniSection is constructed at the end
            // so that the Merge section behavior can redirect back to a previously opened section's state.
            List<SectionBuilder> namedData = new();
            Dictionary<string, int> namedIndexByName = new(sectionComparer);
            int lastBuilderIndex = -1;

            // Pending comments collected from full-line `#` / `;` trivia. Attached to the next section or
            // entry encountered, then cleared. When PreserveComments is false the list is never populated.
            List<IniComment> pendingComments = new();

            while (TryReadLine(out ReadOnlySpan<char> rawLine))
            {
                ReadOnlySpan<char> line = rawLine.Trim();

                if (line.IsEmpty)
                    continue;

                if (line[0] is ';' or '#')
                {
                    if (_options.PreserveComments)
                    {
                        char prefix = line[0];
                        string text = line[1..].ToString();
                        pendingComments.Add(new IniComment(prefix, text, _lineNumber));
                    }

                    continue;
                }

                if (line[0] == '[')
                {
                    int closeBracket = line.IndexOf(']');

                    if (closeBracket < 0)
                        Ini.ThrowMalformedSectionHeader(_lineNumber);

                    ReadOnlySpan<char> nameSpan = line[1..closeBracket].Trim();

                    if (nameSpan.IsEmpty)
                        Ini.ThrowMalformedSectionHeader(_lineNumber);

                    string sectionName = nameSpan.ToString();

                    int targetIdx = SelectSectionTarget(sectionName, namedData, namedIndexByName, lastBuilderIndex);

                    if (targetIdx < 0)
                    {
                        // New builder.
                        currentEntries = new List<IniEntry>();
                        currentLookup = new Dictionary<string, IniEntry>(keyComparer);
                        SectionBuilder builder = new(sectionName, currentEntries, currentLookup);

                        if (pendingComments.Count > 0)
                        {
                            builder.LeadingComments = pendingComments.ToArray();
                            pendingComments.Clear();
                        }

                        // Maintain a first-occurrence lookup so Merge can find earlier builders; under Preserve
                        // or MergeAdjacent this is intentionally not overwritten by later occurrences.
                        if (!namedIndexByName.ContainsKey(sectionName))
                            namedIndexByName[sectionName] = namedData.Count;

                        lastBuilderIndex = namedData.Count;
                        namedData.Add(builder);
                    }
                    else
                    {
                        // Redirect to existing builder (Merge or MergeAdjacent into immediate predecessor).
                        SectionBuilder existing = namedData[targetIdx];
                        currentEntries = existing.Entries;
                        currentLookup = existing.Lookup;

                        // Append any pending comments to the existing section's leading comments.
                        if (pendingComments.Count > 0)
                        {
                            var combined = new IniComment[existing.LeadingComments.Length + pendingComments.Count];
                            existing.LeadingComments.CopyTo(combined, 0);
                            for (int i = 0; i < pendingComments.Count; i++)
                                combined[existing.LeadingComments.Length + i] = pendingComments[i];
                            existing.LeadingComments = combined;
                            pendingComments.Clear();
                        }

                        lastBuilderIndex = targetIdx;
                        namedData[targetIdx] = existing;
                    }

                    inGlobal = false;
                }
                else
                {
                    if (inGlobal && !_options.AllowGlobalSection)
                        Ini.ThrowGlobalKeyDisallowed(_lineNumber);

                    // Capture the pending comment list to attach to the resulting entry; clear immediately so a
                    // subsequent entry on the same line never reuses these comments.
                    IniComment[]? entryComments = null;
                    if (pendingComments.Count > 0)
                    {
                        entryComments = pendingComments.ToArray();
                        pendingComments.Clear();
                    }
                    else if (inGlobal && globalLeadingComments.Count > 0)
                    {
                        // No-op: globalLeadingComments are seeded once and not consumed per-entry.
                    }

                    AddEntry(line, currentEntries, currentLookup, entryComments);
                }
            }

            IniSection globalSection = new(string.Empty, globalEntries, globalLookup);

            List<IniSection> sections = new(namedData.Count);
            Dictionary<string, IniSection> sectionsLookup = new(sectionComparer);

            foreach (SectionBuilder builder in namedData)
            {
                IniSection section = new(builder.Name, builder.Entries, builder.Lookup);
                if (builder.LeadingComments.Length > 0)
                    section.SetLeadingComments(builder.LeadingComments);
                sections.Add(section);
                sectionsLookup[builder.Name] = section;
            }

            return new IniDocument(globalSection, sections, sectionsLookup);
        }

        /// <summary>
        /// Resolves which existing builder a section header should redirect to under the configured
        /// <see cref="IniDuplicateSectionBehavior" />.
        /// </summary>
        /// <param name="sectionName">The name of the section being opened.</param>
        /// <param name="namedData">All section builders accumulated so far.</param>
        /// <param name="namedIndexByName">First-occurrence lookup by section name.</param>
        /// <param name="lastBuilderIndex">Index of the builder most recently appended or merged into.</param>
        /// <returns>
        /// The index of an existing builder to redirect into, or <c>-1</c> when a new builder must be created.
        /// </returns>
        /// <exception cref="IniFormatException">
        /// Thrown under <see cref="IniDuplicateSectionBehavior.Disallowed" /> when a duplicate is encountered.
        /// </exception>
        private readonly int SelectSectionTarget(
            string sectionName,
            List<SectionBuilder> namedData,
            Dictionary<string, int> namedIndexByName,
            int lastBuilderIndex)
        {
            bool isDuplicate = namedIndexByName.TryGetValue(sectionName, out int firstIdx);

            switch (_options.DuplicateSectionBehavior)
            {
                case IniDuplicateSectionBehavior.Disallowed:
                    if (isDuplicate)
                        Ini.ThrowDuplicateSection(sectionName, _lineNumber);
                    return -1;

                case IniDuplicateSectionBehavior.MergeAdjacent:
                    if (lastBuilderIndex >= 0
                        && string.Equals(namedData[lastBuilderIndex].Name, sectionName, _options.CaseSensitiveSections ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                    {
                        return lastBuilderIndex;
                    }

                    return -1;

                case IniDuplicateSectionBehavior.Preserve:
                    return -1;

                case IniDuplicateSectionBehavior.Merge:
                default:
                    return isDuplicate ? firstIdx : -1;
            }
        }

        /// <summary>
        /// Mutable builder used to accumulate one section's state until <see cref="IniSection" /> is materialized at
        /// the end of parsing.
        /// </summary>
        private struct SectionBuilder
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SectionBuilder" /> struct.
            /// </summary>
            /// <param name="name">The section name.</param>
            /// <param name="entries">The ordered entry list backing the section.</param>
            /// <param name="lookup">The case-resolved key lookup backing the section.</param>
            internal SectionBuilder(string name, List<IniEntry> entries, Dictionary<string, IniEntry> lookup)
            {
                Name = name;
                Entries = entries;
                Lookup = lookup;
                LeadingComments = [];
            }

            /// <summary>
            /// Gets the name of the section being accumulated.
            /// </summary>
            /// <returns>The section name.</returns>
            internal string Name { get; }

            /// <summary>
            /// Gets the ordered list of entries collected for the section.
            /// </summary>
            /// <returns>The mutable, source-ordered entry list.</returns>
            internal List<IniEntry> Entries { get; }

            /// <summary>
            /// Gets the case-resolved key lookup for the section's entries.
            /// </summary>
            /// <returns>The key-to-entry lookup dictionary.</returns>
            internal Dictionary<string, IniEntry> Lookup { get; }

            /// <summary>
            /// Gets or sets the comments that precede the section header.
            /// </summary>
            /// <returns>The leading comments associated with the section.</returns>
            internal IniComment[] LeadingComments { get; set; }
        }

        /// <summary>
        /// Parses a single key/value line and adds the resulting entry to the active section's state, applying the
        /// configured <see cref="DuplicateKeyPolicy" />.
        /// </summary>
        /// <param name="line">The trimmed source line (not a comment, not a section header, not empty).</param>
        /// <param name="entries">The ordered entry list for the active section.</param>
        /// <param name="lookup">The key-to-entry lookup for the active section.</param>
        /// <param name="leadingComments">
        /// The trivia comments accumulated before this entry, or <see langword="null" /> when none were pending.
        /// </param>
        private readonly void AddEntry(
            ReadOnlySpan<char> line,
            List<IniEntry> entries,
            Dictionary<string, IniEntry> lookup,
            IReadOnlyList<IniComment>? leadingComments)
        {
            // Find the first = or : separator.
            int sepIdx = -1;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] is '=' or ':')
                {
                    sepIdx = i;
                    break;
                }
            }

            string key;
            string value;

            if (sepIdx < 0)
            {
                // Key-only line — treat as a key with an empty value.
                key = line.ToString();
                value = string.Empty;
            }
            else
            {
                key = line[..sepIdx].TrimEnd().ToString();
                value = line[(sepIdx + 1)..].TrimStart().ToString();
            }

            if (key.Length == 0)
                Ini.ThrowMissingKey(_lineNumber);

            if (lookup.TryGetValue(key, out IniEntry? existing))
            {
                switch (_options.DuplicateKeyBehavior)
                {
                    case DuplicateKeyPolicy.Disallowed:
                        Ini.ThrowDuplicateKey(key, _lineNumber);
                        return;

                    case DuplicateKeyPolicy.FirstWins:
                        return;

                    case DuplicateKeyPolicy.LastWins:
                        // Replace the existing entry in-place so its original position is preserved.
                        int idx = entries.IndexOf(existing);
                        IniEntry replacement = new(key, value, _lineNumber, leadingComments);
                        entries[idx] = replacement;
                        lookup[key] = replacement;
                        return;
                }
            }

            IniEntry entry = new(key, value, _lineNumber, leadingComments);
            entries.Add(entry);
            lookup[key] = entry;
        }

        /// <summary>
        /// Reads the next line from the remaining source, advancing the position and incrementing
        /// <see cref="_lineNumber" />.
        /// </summary>
        /// <param name="line">The raw (untrimmed) line text, excluding the line terminator.</param>
        /// <returns>
        /// <see langword="true" /> when a line was read; <see langword="false" /> when the source is exhausted.
        /// </returns>
        private bool TryReadLine(out ReadOnlySpan<char> line)
        {
            if (_remaining.IsEmpty)
            {
                line = default;
                return false;
            }

            _lineNumber++;

            ReadOnlySpan<char> source = _remaining;

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];

                if (c == '\n')
                {
                    line = source[..i];
                    _remaining = source[(i + 1)..];
                    return true;
                }

                if (c == '\r')
                {
                    line = source[..i];
                    int next = i + 1;

                    // Consume LF following CR to handle \r\n as a single line ending.
                    _remaining = next < source.Length && source[next] == '\n'
                        ? source[(next + 1)..]
                        : source[(i + 1)..];

                    return true;
                }
            }

            // No line terminator — the remaining text is the final line.
            line = source;
            _remaining = [];
            return true;
        }
    }
}
