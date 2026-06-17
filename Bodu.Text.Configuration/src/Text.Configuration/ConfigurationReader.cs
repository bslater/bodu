// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Reads a configuration document line by line, producing a populated <see cref="ConfigurationDocument" /> together
/// with any diagnostics gathered under the rules of <see cref="ConfigurationParseOptions" />.
/// </summary>
/// <remarks>
/// This reader honors the Configuration-specific features that the underlying <c>Ini.Parser</c> does not: inline
/// comment modes (<see cref="ConfigurationInlineCommentMode" />), diagnostic mode routing (
/// <see cref="ConfigurationDiagnosticMode" />), and source location tracking. The resulting document inherits the read
/// surface of <see cref="IniDocumentBase" /> so it composes naturally with everything else in <c>Bodu.Text.Ini</c>.
/// </remarks>
internal sealed partial class ConfigurationReader
{
    /// <summary>The parse options that govern comment, duplicate, and diagnostic handling.</summary>
    private readonly ConfigurationParseOptions _options;

    /// <summary>The diagnostics accumulated while reading the source, in the order they were encountered.</summary>
    private readonly List<ConfigurationDiagnostic> _diagnostics = [];

    /// <summary>The leading comments buffered ahead of the next section or key-value entry they annotate.</summary>
    private readonly List<IniComment> _pendingLeadingComments = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationReader" /> class with the supplied parse options.
    /// </summary>
    /// <param name="options">The parse options that govern comment, duplicate, and diagnostic handling.</param>
    internal ConfigurationReader(ConfigurationParseOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Reads the entire stream represented by <paramref name="reader" /> and produces the parse result.
    /// </summary>
    /// <param name="reader">The source of configuration text.</param>
    /// <param name="path">The optional file path used when emitting source locations.</param>
    /// <returns>
    /// A <see cref="ConfigurationParseResult" /> carrying the populated <see cref="ConfigurationDocument" /> and any
    /// diagnostics collected.
    /// </returns>
    internal ConfigurationParseResult Read(TextReader reader, string? path)
    {
        ThrowHelper.ThrowIfNull(reader);

        bool caseSensitiveSections = _options.KeyOptions.CaseSensitive;
        ConfigurationDocument document = new(caseSensitiveSections);
        IniSection currentSection = document.GlobalSection;

        int lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            currentSection = ProcessLine(document, currentSection, line, lineNumber, path);
        }

        // Any pending leading comments at end-of-file attach to whichever section is current.
        foreach (IniComment c in _pendingLeadingComments)
            currentSection.AddLeadingComment(c);
        _pendingLeadingComments.Clear();

        return new ConfigurationParseResult(document, [.. _diagnostics]);
    }

    /// <summary>
    /// Processes a single input line, dispatching to comment, section-header, or property handling.
    /// </summary>
    /// <param name="document">The document being populated.</param>
    /// <param name="currentSection">The section that subsequent properties are added to.</param>
    /// <param name="line">The raw line text.</param>
    /// <param name="lineNumber">The 1-based number of <paramref name="line" />.</param>
    /// <param name="path">The optional source file path used when emitting source locations.</param>
    /// <returns>The section current after the line has been processed.</returns>
    private IniSection ProcessLine(
        ConfigurationDocument document,
        IniSection currentSection,
        string line,
        int lineNumber,
        string? path)
    {
        if (line.Length > _options.MaxLineLength)
        {
            EmitDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.LineTooLong,
                string.Format(CultureInfo.CurrentCulture, ConfigurationResourceStrings.Format_Invalid_LineTooLong, _options.MaxLineLength),
                new ConfigurationSourceLocation(lineNumber, 1, line.Length, path));

            return currentSection;
        }

        int firstNonWs = FindFirstNonWhitespace(line);

        // Blank line: any pending leading comments still attach to the next significant line.
        if (firstNonWs < 0)
            return currentSection;

        char first = line[firstNonWs];

        // Full-line comment: capture and defer until we see the next section or property.
        if (first is '#' or ';')
        {
            string commentText = line[(firstNonWs + 1)..];
            _pendingLeadingComments.Add(new IniComment(first, commentText, lineNumber));
            return currentSection;
        }

        return first == '['
            ? ProcessSectionHeader(document, line, firstNonWs, lineNumber, path)
            : ProcessPropertyLine(currentSection, line, firstNonWs, lineNumber, path);
    }

    /// <summary>
    /// Processes a section-header line, registering the section and attaching any pending leading comments.
    /// </summary>
    /// <param name="document">The document being populated.</param>
    /// <param name="line">The raw line text containing the section header.</param>
    /// <param name="firstNonWs">The index of the first non-whitespace character on the line.</param>
    /// <param name="lineNumber">The 1-based number of <paramref name="line" />.</param>
    /// <param name="path">The optional source file path used when emitting source locations.</param>
    /// <returns>The section that subsequent properties are added to.</returns>
    private IniSection ProcessSectionHeader(
        ConfigurationDocument document,
        string line,
        int firstNonWs,
        int lineNumber,
        string? path)
    {
        // The section name is everything between the first `[` and the final `]` on the line.
        // We allow `]` inside the section name to mirror EditorConfig section conventions.
        int lastClose = FindLastClosingBracket(line, firstNonWs);
        if (lastClose < 0)
        {
            EmitDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.UnterminatedSectionHeader,
                ConfigurationResourceStrings.Format_Invalid_UnterminatedSectionHeader,
                new ConfigurationSourceLocation(lineNumber, firstNonWs + 1, line.Length - firstNonWs, path));

            return GetCurrentSection(document);
        }

        string name = line.Substring(firstNonWs + 1, lastClose - firstNonWs - 1);
        if (name.Length == 0)
        {
            EmitDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.EmptySectionHeader,
                ConfigurationResourceStrings.Format_Invalid_EmptySectionHeader,
                new ConfigurationSourceLocation(lineNumber, firstNonWs + 1, lastClose - firstNonWs + 1, path));

            return GetCurrentSection(document);
        }

        ConfigurationSourceLocation headerLoc = new(lineNumber, firstNonWs + 1, lastClose - firstNonWs + 1, path);

        if (!IsTrailingContentAllowed(line, lastClose + 1, _options.SectionHeaderMode))
        {
            EmitDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.TrailingContentAfterSectionHeader,
                ConfigurationResourceStrings.Format_Invalid_TrailingContentAfterSectionHeader,
                new ConfigurationSourceLocation(lineNumber, lastClose + 2, line.Length - lastClose - 1, path));

            return GetCurrentSection(document);
        }

        IniSection section = ResolveSectionTarget(document, name, headerLoc);

        AttachPendingComments(section, _pendingLeadingComments);

        return section;
    }

    /// <summary>
    /// Returns <see langword="true" /> when the trailing run of characters in <paramref name="line" /> starting at
    /// <paramref name="from" /> is acceptable under <paramref name="mode" />. Trailing whitespace is always permitted;
    /// a leading <c>#</c> or <c>;</c> after optional whitespace is permitted only when the mode is
    /// <see cref="ConfigurationSectionHeaderMode.AllowTrailingInlineComment" />; any other non-whitespace content is
    /// permitted only under <see cref="ConfigurationSectionHeaderMode.Lenient" />.
    /// </summary>
    /// <param name="line">The full section-header line being processed.</param>
    /// <param name="from">The index immediately following the closing <c>]</c>.</param>
    /// <param name="mode">The configured section-header mode.</param>
    /// <returns><see langword="true" /> when the trailing content is acceptable.</returns>
    private static bool IsTrailingContentAllowed(string line, int from, ConfigurationSectionHeaderMode mode)
    {
        int firstNonWs = -1;
        for (int i = from; i < line.Length; i++)
        {
            if (!char.IsWhiteSpace(line[i]))
            {
                firstNonWs = i;
                break;
            }
        }

        if (firstNonWs < 0)
            return true;

        return mode switch
        {
            ConfigurationSectionHeaderMode.Lenient => true,
            ConfigurationSectionHeaderMode.AllowTrailingInlineComment => line[firstNonWs] is '#' or ';',
            ConfigurationSectionHeaderMode.Strict => false,
            _ => true,
        };
    }

    /// <summary>
    /// Resolves the section that a header names, honoring the configured duplicate-section behaviour.
    /// </summary>
    /// <param name="document">The document being populated.</param>
    /// <param name="name">The section name parsed from the header.</param>
    /// <param name="headerLoc">The source location of the header, used when emitting diagnostics.</param>
    /// <returns>The existing or newly created section that subsequent properties are added to.</returns>
    /// <remarks>
    /// Uses <see cref="IniDocumentBase.TryGetSection(string, out IniSection?)" /> for O(1) duplicate detection in
    /// <see cref="IniDuplicateSectionBehavior.Disallowed" /> and <see cref="IniDuplicateSectionBehavior.MergeAll" />.
    /// <see cref="IniDuplicateSectionBehavior.MergeAdjacent" /> checks only the last section in the document, which is
    /// also O(1). <see cref="IniDuplicateSectionBehavior.Preserve" /> always creates a new section and pays no lookup
    /// cost.
    /// </remarks>
    private IniSection ResolveSectionTarget(ConfigurationDocument document, string name, ConfigurationSourceLocation headerLoc)
    {
        switch (_options.DuplicateSectionMode)
        {
            case IniDuplicateSectionBehavior.Disallowed:
                if (document.TryGetSection(name, out IniSection? disallowedExisting))
                {
                    EmitDiagnostic(
                        ConfigurationDiagnosticSeverity.Error,
                        ConfigurationDiagnosticCode.DuplicateSection,
                        string.Format(CultureInfo.CurrentCulture, ConfigurationResourceStrings.Format_Invalid_DuplicateSection, name),
                        headerLoc);
                    return disallowedExisting;
                }

                break;

            case IniDuplicateSectionBehavior.MergeAll:
                if (document.TryGetSection(name, out IniSection? mergeAllExisting))
                    return mergeAllExisting;

                break;

            case IniDuplicateSectionBehavior.MergeAdjacent:
                {
                IEqualityComparer<string> comparer = _options.KeyOptions.CaseSensitive
                    ? StringComparer.Ordinal
                    : StringComparer.OrdinalIgnoreCase;
                if (document.Sections.Count > 0 && comparer.Equals(document.Sections[^1].Name, name))
                    return document.Sections[^1];
            }

                break;
        }

        IniSection created = new(name, [], _options.KeyOptions.CaseSensitive);
        document.AddSection(created);
        return created;
    }

    /// <summary>
    /// Processes a property line, splitting it into a key and value and appending the resulting entry.
    /// </summary>
    /// <param name="currentSection">The section the entry is added to.</param>
    /// <param name="line">The raw line text containing the property.</param>
    /// <param name="firstNonWs">The index of the first non-whitespace character on the line.</param>
    /// <param name="lineNumber">The 1-based number of <paramref name="line" />.</param>
    /// <param name="path">The optional source file path used when emitting source locations.</param>
    /// <returns>The unchanged <paramref name="currentSection" />.</returns>
    private IniSection ProcessPropertyLine(
        IniSection currentSection,
        string line,
        int firstNonWs,
        int lineNumber,
        string? path)
    {
        int equalsIndex = FindFirstUnescaped(line, '=', firstNonWs);

        if (equalsIndex < 0)
        {
            if (_options.AllowKeyOnlyProperties)
            {
                string keyOnly = TrimTrailing(line, firstNonWs);
                AppendEntry(currentSection, keyOnly, value: string.Empty, lineNumber, firstNonWs, path);

                return currentSection;
            }

            EmitDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.MissingEquals,
                ConfigurationResourceStrings.Format_Invalid_MissingEquals,
                new ConfigurationSourceLocation(lineNumber, firstNonWs + 1, line.Length - firstNonWs, path));

            return currentSection;
        }

        string keyText = line[firstNonWs..equalsIndex];
        string valueText = line[(equalsIndex + 1)..];

        if (_options.TrimKeysAndValues)
        {
            keyText = keyText.Trim();
            valueText = valueText.Trim();
        }

        if (keyText.Length == 0)
        {
            EmitDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.EmptyKey,
                ConfigurationResourceStrings.Format_Invalid_EmptyKey,
                new ConfigurationSourceLocation(lineNumber, firstNonWs + 1, equalsIndex - firstNonWs, path));

            return currentSection;
        }

        if (keyText.Length > _options.MaxKeyLength)
        {
            EmitDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.KeyTooLong,
                string.Format(CultureInfo.CurrentCulture, ConfigurationResourceStrings.Format_Invalid_KeyTooLong, _options.MaxKeyLength),
                new ConfigurationSourceLocation(lineNumber, firstNonWs + 1, keyText.Length, path));

            return currentSection;
        }

        IniComment? inlineComment = null;
        if (_options.InlineCommentMode != ConfigurationInlineCommentMode.Disabled)
            inlineComment = TryExtractInlineComment(ref valueText, _options.InlineCommentMode, lineNumber);

        AppendEntry(currentSection, keyText, valueText, lineNumber, firstNonWs, path, inlineComment);

        return currentSection;
    }

    /// <summary>
    /// Appends a property entry to <paramref name="section" />, validating the key and honoring the configured
    /// duplicate-key behaviour.
    /// </summary>
    /// <param name="section">The section the entry is added to.</param>
    /// <param name="rawKey">The raw property key.</param>
    /// <param name="value">The property value.</param>
    /// <param name="lineNumber">The 1-based line number the entry was read from.</param>
    /// <param name="linePosition">The zero-based index of the key on the line.</param>
    /// <param name="path">The optional source file path used when emitting source locations.</param>
    /// <param name="inlineComment">The inline comment trailing the value, or <see langword="null" />.</param>
    private void AppendEntry(
        IniSection section,
        string rawKey,
        string value,
        int lineNumber,
        int linePosition,
        string? path,
        IniComment? inlineComment = null)
    {
        // O(1) duplicate detection via IniSection's internal lookup, rather than scanning section.Entries.
        IniEntry? existing = section.TryGetEntry(rawKey, out IniEntry? found) ? found : null;

        ConfigurationSourceLocation loc = new(lineNumber, linePosition + 1, rawKey.Length, path);

        // Validate that the key has no control characters by routing through ConfigurationKey, which
        // applies the same rejection rule as direct callers.
        try
        {
            _ = new ConfigurationKey(rawKey, _options.KeyOptions);
        }
        catch (ArgumentException ex)
        {
            EmitDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.InvalidKeyCharacter,
                ex.Message,
                loc);
            return;
        }

        if (existing is not null)
        {
            switch (_options.DuplicateKeyMode)
            {
                case DuplicateKeyPolicy.FirstWins:
                    return;

                case DuplicateKeyPolicy.Disallowed:
                    EmitDiagnostic(
                        ConfigurationDiagnosticSeverity.Error,
                        ConfigurationDiagnosticCode.DuplicateKey,
                        string.Format(CultureInfo.CurrentCulture, ConfigurationResourceStrings.Format_Invalid_DuplicateKey, rawKey),
                        loc);
                    return;

                case DuplicateKeyPolicy.LastWins:
                default:
                    // Replace via AddEntry which preserves the existing position and refreshes the lookup.
                    IniEntry replacement = new(rawKey, value, lineNumber);
                    if (inlineComment.HasValue)
                        replacement.InlineComment = inlineComment.Value;
                    foreach (IniComment c in _pendingLeadingComments)
                        replacement.AddLeadingComment(c);
                    _pendingLeadingComments.Clear();
                    section.AddEntry(replacement);
                    return;
            }
        }

        IniEntry entry = new(rawKey, value, lineNumber);
        if (inlineComment.HasValue)
            entry.InlineComment = inlineComment.Value;
        foreach (IniComment c in _pendingLeadingComments)
            entry.AddLeadingComment(c);
        _pendingLeadingComments.Clear();
        section.AddEntry(entry);
    }
}
