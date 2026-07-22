// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniNormalizedDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Represents an INI document normalized into its logical object-of-objects shape: hoisted global entries plus an
/// insertion-ordered list of sections, with the duplicate-section and duplicate-key policies already applied.
/// </summary>
/// <remarks>
/// The normalizer exists because duplicate-section merge declares structure out of source order — a later
/// <c>[section]</c> appends to an earlier one — so the logical shape cannot be produced by a single forward token pass.
/// It backs the read-only <see cref="Bodu.Text.Ini.Document.IniDocument" /> DOM and the serializer's normalized reader.
/// </remarks>
internal sealed class IniNormalizedDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IniNormalizedDocument" /> class with no entries.
    /// </summary>
    private IniNormalizedDocument()
    {
        GlobalEntries = [];
        Sections = [];
    }

    /// <summary>
    /// Gets the global (pre-section) key/value entries, hoisted to the document root.
    /// </summary>
    /// <value>The global entries in resolved order.</value>
    public List<KeyValuePair<string, string>> GlobalEntries { get; }

    /// <summary>
    /// Gets the document's sections in first-appearance order.
    /// </summary>
    /// <value>The normalized sections.</value>
    public List<IniNormalizedSection> Sections { get; }

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a normalized document, applying the configured duplicate-section and
    /// duplicate-key policies and rejecting a global key that collides with a section name.
    /// </summary>
    /// <param name="utf8Ini">The INI source bytes.</param>
    /// <param name="readerOptions">The source-order reader options.</param>
    /// <param name="documentOptions">The document-model duplicate policies.</param>
    /// <returns>The normalized document.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when the bytes are not valid INI, or when a duplicate section, duplicate key, or global-key/section-name
    /// collision violates the configured policies.
    /// </exception>
    public static IniNormalizedDocument Parse(ReadOnlySpan<byte> utf8Ini, IniReaderOptions readerOptions, IniDocumentOptions documentOptions)
    {
        var document = new IniNormalizedDocument();
        var sectionIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var globalIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var entryIndexes = new List<Dictionary<string, int>>();

        var reader = new Utf8IniReader(utf8Ini, readerOptions);
        int currentSection = -1;
        string? pendingKey = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case IniTokenType.SectionHeader:
                    string name = reader.GetString();
                    if (sectionIndex.TryGetValue(name, out int existing))
                    {
                        if (documentOptions.DuplicateSectionBehavior == IniDuplicateSectionBehavior.Disallowed)
                        {
                            throw new IniFormatException(
                                string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniDuplicateSection, name),
                                reader.LineNumber,
                                reader.BytesConsumed);
                        }

                        currentSection = existing;
                    }
                    else
                    {
                        sectionIndex.Add(name, document.Sections.Count);
                        currentSection = document.Sections.Count;
                        document.Sections.Add(new IniNormalizedSection(name));
                        entryIndexes.Add(new Dictionary<string, int>(StringComparer.Ordinal));
                    }

                    break;

                case IniTokenType.PropertyName:
                    pendingKey = reader.GetString();
                    break;

                case IniTokenType.String:
                    List<KeyValuePair<string, string>> entries = currentSection < 0 ? document.GlobalEntries : document.Sections[currentSection].Entries;
                    Dictionary<string, int> index = currentSection < 0 ? globalIndex : entryIndexes[currentSection];
                    string sectionName = currentSection < 0 ? string.Empty : document.Sections[currentSection].Name;
                    AddEntry(entries, index, pendingKey!, reader.GetString(), sectionName, documentOptions.DuplicateKeyBehavior, reader.LineNumber, reader.BytesConsumed);
                    pendingKey = null;
                    break;

                default:
                    break;
            }
        }

        foreach (KeyValuePair<string, string> global in document.GlobalEntries)
        {
            if (sectionIndex.ContainsKey(global.Key))
            {
                throw new IniFormatException(
                    string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniGlobalKeyCollision, global.Key));
            }
        }

        return document;
    }

    /// <summary>
    /// Adds an entry to the supplied list, resolving a duplicate key by the configured policy.
    /// </summary>
    /// <param name="entries">The target entry list.</param>
    /// <param name="index">The key-to-position index for <paramref name="entries" />.</param>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The entry value.</param>
    /// <param name="sectionName">The owning section name, or an empty string for the global section.</param>
    /// <param name="behavior">The duplicate-key policy.</param>
    /// <param name="line">The 1-based line number for a policy violation.</param>
    /// <param name="offset">The byte offset for a policy violation.</param>
    /// <exception cref="IniFormatException">
    /// Thrown when the key is a duplicate and the policy is <see cref="IniDuplicateKeyBehavior.Disallowed" />.
    /// </exception>
    private static void AddEntry(
        List<KeyValuePair<string, string>> entries,
        Dictionary<string, int> index,
        string key,
        string value,
        string sectionName,
        IniDuplicateKeyBehavior behavior,
        int line,
        int offset)
    {
        if (index.TryGetValue(key, out int existing))
        {
            switch (behavior)
            {
                case IniDuplicateKeyBehavior.LastWins:
                    entries[existing] = new KeyValuePair<string, string>(key, value);
                    break;

                case IniDuplicateKeyBehavior.FirstWins:
                    break;

                default:
                    throw new IniFormatException(
                        string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniDuplicateKey, sectionName, key),
                        line,
                        offset);
            }

            return;
        }

        index.Add(key, entries.Count);
        entries.Add(new KeyValuePair<string, string>(key, value));
    }
}
