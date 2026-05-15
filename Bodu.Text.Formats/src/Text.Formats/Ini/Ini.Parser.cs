// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ini.Parser.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.Ini;

public static partial class Ini
{

    /// <summary>
    /// Provides line-by-line INI parsing over a <see cref="ReadOnlySpan{T}" /> of characters.
    /// </summary>
    private ref struct Parser
    {

        private readonly IniParseOptions _options;
        private ReadOnlySpan<char> _remaining;
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

            List<IniEntry> currentEntries = globalEntries;
            Dictionary<string, IniEntry> currentLookup = globalLookup;
            bool inGlobal = true;

            // Named section builders — kept as mutable lists/dicts until IniSection is constructed at the end
            // so that the Merge section behavior can redirect back to a previously opened section's state.
            List<(string Name, List<IniEntry> Entries, Dictionary<string, IniEntry> Lookup)> namedData = new();
            Dictionary<string, int> namedIndexByName = new(sectionComparer);

            while (TryReadLine(out ReadOnlySpan<char> rawLine))
            {
                ReadOnlySpan<char> line = rawLine.Trim();

                if (line.IsEmpty || line[0] == ';' || line[0] == '#')
                    continue;

                if (line[0] == '[')
                {
                    int closeBracket = line.IndexOf(']');

                    if (closeBracket < 0)
                        ThrowHelper.ThrowIniFormatException_MalformedSectionHeader(_lineNumber);

                    ReadOnlySpan<char> nameSpan = line[1..closeBracket].Trim();

                    if (nameSpan.IsEmpty)
                        ThrowHelper.ThrowIniFormatException_MalformedSectionHeader(_lineNumber);

                    string sectionName = nameSpan.ToString();

                    if (namedIndexByName.TryGetValue(sectionName, out int existingIdx))
                    {
                        if (_options.DuplicateSectionBehavior == IniDuplicateSectionBehavior.Disallowed)
                            ThrowHelper.ThrowIniFormatException_DuplicateSection(sectionName, _lineNumber);

                        // Merge: redirect current state to the existing section's builders.
                        (_, currentEntries, currentLookup) = namedData[existingIdx];
                    }
                    else
                    {
                        currentEntries = new List<IniEntry>();
                        currentLookup = new Dictionary<string, IniEntry>(keyComparer);
                        namedIndexByName[sectionName] = namedData.Count;
                        namedData.Add((sectionName, currentEntries, currentLookup));
                    }

                    inGlobal = false;
                }
                else
                {
                    if (inGlobal && !_options.AllowGlobalSection)
                        ThrowHelper.ThrowIniFormatException_GlobalKeyDisallowed(_lineNumber);

                    AddEntry(line, currentEntries, currentLookup);
                }
            }

            IniSection globalSection = new(string.Empty, globalEntries, globalLookup);

            List<IniSection> sections = new(namedData.Count);
            Dictionary<string, IniSection> sectionsLookup = new(sectionComparer);

            foreach ((string name, List<IniEntry> entries, Dictionary<string, IniEntry> lookup) in namedData)
            {
                IniSection section = new(name, entries, lookup);
                sections.Add(section);
                sectionsLookup[name] = section;
            }

            return new IniDocument(globalSection, sections, sectionsLookup);
        }

        /// <summary>
        /// Parses a single key/value line and adds the resulting entry to the active section's state, applying
        /// the configured <see cref="IniDuplicateKeyBehavior" />.
        /// </summary>
        /// <param name="line">The trimmed source line (not a comment, not a section header, not empty).</param>
        /// <param name="entries">The ordered entry list for the active section.</param>
        /// <param name="lookup">The key-to-entry lookup for the active section.</param>
        private void AddEntry(
            ReadOnlySpan<char> line,
            List<IniEntry> entries,
            Dictionary<string, IniEntry> lookup)
        {
            // Find the first = or : separator.
            int sepIdx = -1;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '=' || line[i] == ':')
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
                value = line[(sepIdx + 1) ..].TrimStart().ToString();
            }

            if (key.Length == 0)
                ThrowHelper.ThrowIniFormatException_MissingKey(_lineNumber);

            if (lookup.TryGetValue(key, out IniEntry? existing))
            {
                switch (_options.DuplicateKeyBehavior)
                {
                    case IniDuplicateKeyBehavior.Disallowed:
                        ThrowHelper.ThrowIniFormatException_DuplicateKey(key, _lineNumber);
                        return;

                    case IniDuplicateKeyBehavior.FirstWins:
                        return;

                    case IniDuplicateKeyBehavior.LastWins:
                        // Replace the existing entry in-place so its original position is preserved.
                        int idx = entries.IndexOf(existing);
                        IniEntry replacement = new(key, value);
                        entries[idx] = replacement;
                        lookup[key] = replacement;
                        return;
                }
            }

            IniEntry entry = new(key, value);
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
                    _remaining = source[(i + 1) ..];
                    return true;
                }

                if (c == '\r')
                {
                    line = source[..i];
                    int next = i + 1;

                    // Consume LF following CR to handle \r\n as a single line ending.
                    _remaining = next < source.Length && source[next] == '\n'
                        ? source[(next + 1) ..]
                        : source[(i + 1) ..];

                    return true;
                }
            }

            // No line terminator — the remaining text is the final line.
            line = source;
            _remaining = ReadOnlySpan<char>.Empty;
            return true;
        }

    }

}
