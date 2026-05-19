// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationReader.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

/// <summary>
/// Reads a configuration document line by line, producing a populated <see cref="IniDocument" /> together with any
/// diagnostics gathered under the rules of <see cref="BoduConfigurationParseOptions" />.
/// </summary>
/// <remarks>
/// This reader honors the Configuration-specific features that the underlying <c>Ini.Parser</c> does not: inline
/// comment modes (<see cref="BoduConfigurationInlineCommentMode" />), diagnostic mode routing (
/// <see cref="BoduConfigurationDiagnosticMode" />), and source location tracking. The resulting document is a plain
/// <see cref="IniDocument" /> so it composes naturally with everything else in <c>Bodu.Text.Formats.Ini</c>.
/// </remarks>
internal sealed partial class BoduConfigurationReader
{
    private readonly BoduConfigurationParseOptions _options;
    private readonly List<BoduConfigurationDiagnostic> _diagnostics = [];
    private readonly List<IniComment> _pendingLeadingComments = [];

    internal BoduConfigurationReader(BoduConfigurationParseOptions options)
    {
        this._options = options;
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

        bool caseSensitiveSections = this._options.KeyOptions.CaseSensitive;
        IniDocument document = new(caseSensitiveSections);
        IniSection currentSection = document.GlobalSection;

        int lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            currentSection = this.ProcessLine(document, currentSection, line, lineNumber, path);
        }

        // Any pending leading comments at end-of-file attach to whichever section is current.
        foreach (IniComment c in this._pendingLeadingComments)
            currentSection.AddLeadingComment(c);
        this._pendingLeadingComments.Clear();

        return new BoduConfigurationParseResult(document, this._diagnostics.ToImmutableArray());
    }

    private IniSection ProcessLine(
        IniDocument document,
        IniSection currentSection,
        string line,
        int lineNumber,
        string? path)
    {
        if (line.Length > this._options.MaxLineLength)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.LineTooLong,
                string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.Format_Invalid_LineTooLong, this._options.MaxLineLength),
                new BoduConfigurationSourceLocation(lineNumber, 1, line.Length, path));
            return currentSection;
        }

        int firstNonWs = FindFirstNonWhitespace(line);

        // Blank line: any pending leading comments still attach to the next significant line.
        if (firstNonWs < 0)
            return currentSection;

        char first = line[firstNonWs];

        // Full-line comment: capture and defer until we see the next section or property.
        if (first == '#' || first == ';')
        {
            string commentText = line.Substring(firstNonWs + 1);
            _ = path;
            this._pendingLeadingComments.Add(new IniComment(first, commentText, lineNumber));
            return currentSection;
        }

        if (first == '[')
            return this.ProcessSectionHeader(document, line, firstNonWs, lineNumber, path);

        return this.ProcessPropertyLine(currentSection, line, firstNonWs, lineNumber, path);
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
        int lastClose = FindLastClosingBracket(line, firstNonWs);
        if (lastClose < 0)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.UnterminatedSectionHeader,
                ConfigurationResourceStrings.Format_Invalid_UnterminatedSectionHeader,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, line.Length - firstNonWs, path));
            return GetCurrentSection(document);
        }

        string name = line.Substring(firstNonWs + 1, lastClose - firstNonWs - 1);
        if (name.Length == 0)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.EmptySectionHeader,
                ConfigurationResourceStrings.Format_Invalid_EmptySectionHeader,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, lastClose - firstNonWs + 1, path));
            return GetCurrentSection(document);
        }

        BoduConfigurationSourceLocation headerLoc = new(lineNumber, firstNonWs + 1, lastClose - firstNonWs + 1, path);
        IniSection section = this.ResolveSectionTarget(document, name, headerLoc);

        AttachPendingComments(section, this._pendingLeadingComments);
        return section;
    }

    private IniSection ResolveSectionTarget(IniDocument document, string name, BoduConfigurationSourceLocation headerLoc)
    {
        // Detect duplicates by scanning the existing sections list rather than a separate lookup, so that
        // documents constructed under Preserve / MergeAdjacent (which produce multiple sections with the same
        // name) still allow us to find the last-appended occurrence.
        IniSection? existing = null;
        int existingIndex = -1;
        IEqualityComparer<string> comparer = this._options.KeyOptions.CaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;
        for (int i = 0; i < document.Sections.Count; i++)
        {
            if (comparer.Equals(document.Sections[i].Name, name))
            {
                existing = document.Sections[i];
                existingIndex = i;
            }
        }

        if (existing is not null)
        {
            switch (this._options.DuplicateSectionMode)
            {
                case IniDuplicateSectionBehavior.Disallowed:
                    this.EmitDiagnostic(
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

        IniSection created = new(name, Array.Empty<IniEntry>(), this._options.KeyOptions.CaseSensitive);
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
        int equalsIndex = FindFirstUnescaped(line, '=', firstNonWs);

        if (equalsIndex < 0)
        {
            if (this._options.AllowKeyOnlyProperties)
            {
                string keyOnly = TrimTrailing(line, firstNonWs);
                this.AppendEntry(currentSection, keyOnly, value: string.Empty, lineNumber, firstNonWs, path);
                return currentSection;
            }

            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.MissingEquals,
                ConfigurationResourceStrings.Format_Invalid_MissingEquals,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, line.Length - firstNonWs, path));
            return currentSection;
        }

        string keyText = line.Substring(firstNonWs, equalsIndex - firstNonWs);
        string valueText = line.Substring(equalsIndex + 1);

        if (this._options.TrimKeysAndValues)
        {
            keyText = keyText.Trim();
            valueText = valueText.Trim();
        }

        if (keyText.Length == 0)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.EmptyKey,
                ConfigurationResourceStrings.Format_Invalid_EmptyKey,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, equalsIndex - firstNonWs, path));
            return currentSection;
        }

        if (keyText.Length > this._options.MaxKeyLength)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.KeyTooLong,
                string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.Format_Invalid_KeyTooLong, this._options.MaxKeyLength),
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, keyText.Length, path));
            return currentSection;
        }

        IniComment? inlineComment = null;
        if (this._options.InlineCommentMode != BoduConfigurationInlineCommentMode.Disabled)
            inlineComment = TryExtractInlineComment(ref valueText, this._options.InlineCommentMode, lineNumber);

        this.AppendEntry(currentSection, keyText, valueText, lineNumber, firstNonWs, path, inlineComment);
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
        IEqualityComparer<string> comparer = this._options.KeyOptions.CaseSensitive
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
            _ = new BoduConfigurationKey(rawKey, this._options.KeyOptions);
        }
        catch (ArgumentException ex)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.InvalidKeyCharacter,
                ex.Message,
                loc);
            return;
        }

        if (existing is not null)
        {
            switch (this._options.DuplicateKeyMode)
            {
                case IniDuplicateKeyBehavior.FirstWins:
                    return;

                case IniDuplicateKeyBehavior.Disallowed:
                    this.EmitDiagnostic(
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
                    foreach (IniComment c in this._pendingLeadingComments)
                        replacement.AddLeadingComment(c);
                    this._pendingLeadingComments.Clear();
                    section.AddEntry(replacement);
                    return;
            }
        }

        IniEntry entry = new(rawKey, value, lineNumber);
        if (inlineComment.HasValue)
            entry.InlineComment = inlineComment.Value;
        foreach (IniComment c in this._pendingLeadingComments)
            entry.AddLeadingComment(c);
        this._pendingLeadingComments.Clear();
        section.AddEntry(entry);
    }
}
