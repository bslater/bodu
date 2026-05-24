// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReader.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Reads a configuration document line by line, producing a populated <see cref="IniDocument" /> together with any
/// diagnostics gathered under the rules of <see cref="ConfigurationParseOptions" />.
/// </summary>
/// <remarks>
/// This reader honors the Configuration-specific features that the underlying <c>Ini.Parser</c> does not: inline
/// comment modes (<see cref="ConfigurationInlineCommentMode" />), diagnostic mode routing (
/// <see cref="ConfigurationDiagnosticMode" />), and source location tracking. The resulting document is a plain
/// <see cref="IniDocument" /> so it composes naturally with everything else in <c>Bodu.Text.Ini.Ini</c>.
/// </remarks>
internal sealed partial class ConfigurationReader
{
    private readonly ConfigurationParseOptions _options;
    private readonly List<ConfigurationDiagnostic> _diagnostics = [];
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
    /// A <see cref="ConfigurationParseResult" /> carrying the populated <see cref="IniDocument" /> and any diagnostics
    /// collected.
    /// </returns>
    internal ConfigurationParseResult Read(TextReader reader, string? path)
    {
        ThrowHelper.ThrowIfNull(reader);

        var caseSensitiveSections = _options.KeyOptions.CaseSensitive;
        IniDocument document = new(caseSensitiveSections);
        IniSection currentSection = document.GlobalSection;

        var lineNumber = 0;
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
        IniDocument document,
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
                string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.Format_Invalid_LineTooLong, _options.MaxLineLength),
                new ConfigurationSourceLocation(lineNumber, 1, line.Length, path));

            return currentSection;
        }

        var firstNonWs = FindFirstNonWhitespace(line);

        // Blank line: any pending leading comments still attach to the next significant line.
        if (firstNonWs < 0)
            return currentSection;

        var first = line[firstNonWs];

        // Full-line comment: capture and defer until we see the next section or property.
        if (first is '#' or ';')
        {
            var commentText = line[(firstNonWs + 1)..];
            _ = path;
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
        IniDocument document,
        string line,
        int firstNonWs,
        int lineNumber,
        string? path)
    {
        // The section name is everything between the first `[` and the final `]` on the line.
        // We allow `]` inside the section name to mirror EditorConfig section conventions.
        var lastClose = FindLastClosingBracket(line, firstNonWs);
        if (lastClose < 0)
        {
            EmitDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.UnterminatedSectionHeader,
                ConfigurationResourceStrings.Format_Invalid_UnterminatedSectionHeader,
                new ConfigurationSourceLocation(lineNumber, firstNonWs + 1, line.Length - firstNonWs, path));

            return GetCurrentSection(document);
        }

        var name = line.Substring(firstNonWs + 1, lastClose - firstNonWs - 1);
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
        IniSection section = ResolveSectionTarget(document, name, headerLoc);

        AttachPendingComments(section, _pendingLeadingComments);

        return section;
    }

    /// <summary>
    /// Resolves the section that a header names, honoring the configured duplicate-section behaviour.
    /// </summary>
    /// <param name="document">The document being populated.</param>
    /// <param name="name">The section name parsed from the header.</param>
    /// <param name="headerLoc">The source location of the header, used when emitting diagnostics.</param>
    /// <returns>The existing or newly created section that subsequent properties are added to.</returns>
    private IniSection ResolveSectionTarget(IniDocument document, string name, ConfigurationSourceLocation headerLoc)
    {
        // Detect duplicates by scanning the existing sections list rather than a separate lookup, so that
        // documents constructed under Preserve / MergeAdjacent (which produce multiple sections with the same
        // name) still allow us to find the last-appended occurrence.
        IniSection? existing = null;
        var existingIndex = -1;
        IEqualityComparer<string> comparer = _options.KeyOptions.CaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;
        for (var i = 0; i < document.Sections.Count; i++)
        {
            if (comparer.Equals(document.Sections[i].Name, name))
            {
                existing = document.Sections[i];
                existingIndex = i;
            }
        }

        if (existing is not null)
        {
            switch (_options.DuplicateSectionMode)
            {
                case IniDuplicateSectionBehavior.Disallowed:
                    EmitDiagnostic(
                        ConfigurationDiagnosticSeverity.Error,
                        ConfigurationDiagnosticCode.DuplicateSection,
                        string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.Format_Invalid_DuplicateSection, name),
                        headerLoc);
                    return existing;

                case IniDuplicateSectionBehavior.MergeAll:
                    return existing;

                case IniDuplicateSectionBehavior.MergeAdjacent when existingIndex == document.Sections.Count - 1:
                    return existing;
            }
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
        var equalsIndex = FindFirstUnescaped(line, '=', firstNonWs);

        if (equalsIndex < 0)
        {
            if (_options.AllowKeyOnlyProperties)
            {
                var keyOnly = TrimTrailing(line, firstNonWs);
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

        var keyText = line[firstNonWs..equalsIndex];
        var valueText = line[(equalsIndex + 1)..];

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
                string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.Format_Invalid_KeyTooLong, _options.MaxKeyLength),
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
        IEqualityComparer<string> comparer = _options.KeyOptions.CaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        IniEntry? existing = null;
        foreach (IniEntry e in section.Entries)
        {
            if (comparer.Equals(e.Key, rawKey))
            {
                existing = e;
                break;
            }
        }

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
                case IniDuplicateKeyBehavior.FirstWins:
                    return;

                case IniDuplicateKeyBehavior.Disallowed:
                    EmitDiagnostic(
                        ConfigurationDiagnosticSeverity.Error,
                        ConfigurationDiagnosticCode.DuplicateKey,
                        string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.Format_Invalid_DuplicateKey, rawKey),
                        loc);
                    return;

                case IniDuplicateKeyBehavior.LastWins:
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
