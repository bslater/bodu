// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationReader.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Globalization;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Reads a configuration document line by line, producing a populated <see cref="IniDocument" /> together with any
/// diagnostics gathered under the rules of <see cref="BoduConfigurationParseOptions" />.
/// </summary>
/// <remarks>
/// This reader honors the Configuration-specific features that the underlying <c>Ini.Parser</c> does not: inline
/// comment modes (<see cref="BoduConfigurationInlineCommentMode" />), diagnostic mode routing (
/// <see cref="BoduConfigurationDiagnosticMode" />), and source location tracking. The resulting document is a plain
/// <see cref="IniDocument" /> so it composes naturally with everything else in <c>Bodu.Text.Ini.Ini</c>.
/// </remarks>
internal sealed partial class BoduConfigurationReader
{
    private readonly BoduConfigurationParseOptions _options;
    private readonly List<BoduConfigurationDiagnostic> _diagnostics = [];
    private readonly List<IniComment> _pendingLeadingComments = [];

    internal BoduConfigurationReader(BoduConfigurationParseOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Reads the entire stream represented by <paramref name="reader" /> and produces the parse result.
    /// </summary>
    /// <param name="reader">The source of configuration text.</param>
    /// <param name="path">The optional file path used when emitting source locations.</param>
    /// <returns>
    /// A <see cref="BoduConfigurationParseResult" /> carrying the populated <see cref="IniDocument" /> and any
    /// diagnostics collected.
    /// </returns>
    internal BoduConfigurationParseResult Read(TextReader reader, string? path)
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

        return new BoduConfigurationParseResult(document, [.. _diagnostics]);
    }

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
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.LineTooLong,
                string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.Format_Invalid_LineTooLong, _options.MaxLineLength),
                new BoduConfigurationSourceLocation(lineNumber, 1, line.Length, path));

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
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.UnterminatedSectionHeader,
                ConfigurationResourceStrings.Format_Invalid_UnterminatedSectionHeader,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, line.Length - firstNonWs, path));

            return GetCurrentSection(document);
        }

        var name = line.Substring(firstNonWs + 1, lastClose - firstNonWs - 1);
        if (name.Length == 0)
        {
            EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.EmptySectionHeader,
                ConfigurationResourceStrings.Format_Invalid_EmptySectionHeader,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, lastClose - firstNonWs + 1, path));

            return GetCurrentSection(document);
        }

        BoduConfigurationSourceLocation headerLoc = new(lineNumber, firstNonWs + 1, lastClose - firstNonWs + 1, path);
        IniSection section = ResolveSectionTarget(document, name, headerLoc);

        AttachPendingComments(section, _pendingLeadingComments);

        return section;
    }

    private IniSection ResolveSectionTarget(IniDocument document, string name, BoduConfigurationSourceLocation headerLoc)
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
                        BoduConfigurationDiagnosticSeverity.Error,
                        BoduConfigurationDiagnosticCode.UnterminatedSectionHeader,
                        $"Duplicate section pattern '{name}'.",
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
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.MissingEquals,
                ConfigurationResourceStrings.Format_Invalid_MissingEquals,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, line.Length - firstNonWs, path));

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
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.EmptyKey,
                ConfigurationResourceStrings.Format_Invalid_EmptyKey,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, equalsIndex - firstNonWs, path));

            return currentSection;
        }

        if (keyText.Length > _options.MaxKeyLength)
        {
            EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.KeyTooLong,
                string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.Format_Invalid_KeyTooLong, _options.MaxKeyLength),
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, keyText.Length, path));

            return currentSection;
        }

        IniComment? inlineComment = null;
        if (_options.InlineCommentMode != BoduConfigurationInlineCommentMode.Disabled)
            inlineComment = TryExtractInlineComment(ref valueText, _options.InlineCommentMode, lineNumber);

        AppendEntry(currentSection, keyText, valueText, lineNumber, firstNonWs, path, inlineComment);

        return currentSection;
    }

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

        BoduConfigurationSourceLocation loc = new(lineNumber, linePosition + 1, rawKey.Length, path);

        // Validate that the key has no control characters by routing through BoduConfigurationKey, which
        // applies the same rejection rule as direct callers.
        try
        {
            _ = new BoduConfigurationKey(rawKey, _options.KeyOptions);
        }
        catch (ArgumentException ex)
        {
            EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.InvalidKeyCharacter,
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
                        BoduConfigurationDiagnosticSeverity.Error,
                        BoduConfigurationDiagnosticCode.DuplicateKey,
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
