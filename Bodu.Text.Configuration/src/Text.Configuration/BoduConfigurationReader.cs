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
/// Reads a configuration document line by line, producing a <see cref="BoduConfigurationDocument" /> from
/// the supplied source under the rules of <see cref="BoduConfigurationParseOptions" />.
/// </summary>
internal sealed partial class BoduConfigurationReader
{
    private readonly BoduConfigurationParseOptions _options;
    private readonly List<BoduConfigurationDiagnostic> _diagnostics = [];
    private readonly List<BoduConfigurationComment> _pendingLeadingComments = [];

    internal BoduConfigurationReader(BoduConfigurationParseOptions options)
    {
        this._options = options;
    }

    /// <summary>
    /// Reads the entire stream represented by <paramref name="reader" /> and produces a populated document.
    /// </summary>
    /// <param name="reader">The source of configuration text.</param>
    /// <param name="path">The optional file path used when emitting source locations.</param>
    /// <returns>A populated <see cref="BoduConfigurationDocument" />.</returns>
    internal BoduConfigurationDocument Read(TextReader reader, string? path)
    {
        ThrowHelper.ThrowIfNull(reader);

        BoduConfigurationDocument document = new(this._options);
        BoduConfigurationSection currentSection = document.Preamble;
        Dictionary<string, BoduConfigurationSection> sectionsByPattern = new(StringComparer.Ordinal);

        int lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            currentSection = this.ProcessLine(document, currentSection, sectionsByPattern, line, lineNumber, path);
        }

        // Any pending leading comments at end-of-file attach to whichever section is current.
        foreach (BoduConfigurationComment c in this._pendingLeadingComments)
            currentSection.AddLeadingComment(c);
        this._pendingLeadingComments.Clear();

        document.SetDiagnostics(this._diagnostics.ToImmutableArray());
        return document;
    }

    private BoduConfigurationSection ProcessLine(
        BoduConfigurationDocument document,
        BoduConfigurationSection currentSection,
        Dictionary<string, BoduConfigurationSection> sectionsByPattern,
        string line,
        int lineNumber,
        string? path)
    {
        if (line.Length > this._options.MaxLineLength)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.LineTooLong,
                string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.ParseException_LineTooLong, this._options.MaxLineLength),
                new BoduConfigurationSourceLocation(lineNumber, 1, line.Length, path));
            return currentSection;
        }

        // Trim leading whitespace to classify the line. Preserve original column when emitting locations.
        int firstNonWs = FindFirstNonWhitespace(line);

        // Blank line: any pending leading comments still attach to the next significant line.
        if (firstNonWs < 0)
            return currentSection;

        char first = line[firstNonWs];

        // Full-line comment: capture and defer until we see the next section or property.
        if (first == '#' || first == ';')
        {
            string commentText = line.Substring(firstNonWs + 1);
            BoduConfigurationSourceLocation loc = new(lineNumber, firstNonWs + 1, line.Length - firstNonWs, path);
            this._pendingLeadingComments.Add(new BoduConfigurationComment(first, commentText, loc));
            return currentSection;
        }

        // Section header.
        if (first == '[')
            return this.ProcessSectionHeader(document, sectionsByPattern, line, firstNonWs, lineNumber, path);

        // Property line.
        return this.ProcessPropertyLine(currentSection, line, firstNonWs, lineNumber, path);
    }

    private BoduConfigurationSection ProcessSectionHeader(
        BoduConfigurationDocument document,
        Dictionary<string, BoduConfigurationSection> sectionsByPattern,
        string line,
        int firstNonWs,
        int lineNumber,
        string? path)
    {
        // The section name is everything between the first `[` and the final `]` on the line.
        // We allow `]` inside the section name because EditorConfig section names may include `[`/`]`.
        int lastClose = FindLastClosingBracket(line, firstNonWs);
        if (lastClose < 0)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.UnterminatedSectionHeader,
                ConfigurationResourceStrings.ParseException_UnterminatedSectionHeader,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, line.Length - firstNonWs, path));
            return GetCurrentSection(document);
        }

        string pattern = line.Substring(firstNonWs + 1, lastClose - firstNonWs - 1);
        if (pattern.Length == 0)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.EmptySectionHeader,
                ConfigurationResourceStrings.ParseException_EmptySectionHeader,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, lastClose - firstNonWs + 1, path));
            return GetCurrentSection(document);
        }

        BoduConfigurationSourceLocation headerLoc = new(lineNumber, firstNonWs + 1, lastClose - firstNonWs + 1, path);
        BoduConfigurationSection section;

        if (sectionsByPattern.TryGetValue(pattern, out BoduConfigurationSection? existing))
        {
            switch (this._options.DuplicateSectionMode)
            {
                case BoduConfigurationDuplicateSectionMode.Reject:
                    this.EmitDiagnostic(
                        BoduConfigurationDiagnosticSeverity.Error,
                        BoduConfigurationDiagnosticCode.UnterminatedSectionHeader,
                        $"Duplicate section pattern '{pattern}'.",
                        headerLoc);
                    section = existing;
                    break;

                case BoduConfigurationDuplicateSectionMode.MergeAll:
                case BoduConfigurationDuplicateSectionMode.MergeAdjacent when ReferenceEquals(GetCurrentSection(document), existing):
                    section = existing;
                    break;

                default:
                    section = new BoduConfigurationSection(pattern, headerLoc);
                    document.AddSection(section);
                    sectionsByPattern[pattern] = section;
                    break;
            }
        }
        else
        {
            section = new BoduConfigurationSection(pattern, headerLoc);
            document.AddSection(section);
            sectionsByPattern[pattern] = section;
        }

        AttachPendingComments(section, this._pendingLeadingComments);
        return section;
    }

    private BoduConfigurationSection ProcessPropertyLine(
        BoduConfigurationSection currentSection,
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
                this.AppendProperty(currentSection, keyOnly, value: string.Empty, lineNumber, firstNonWs, path);
                return currentSection;
            }

            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.MissingEquals,
                ConfigurationResourceStrings.ParseException_MissingEquals,
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
                ConfigurationResourceStrings.ParseException_EmptyKey,
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, equalsIndex - firstNonWs, path));
            return currentSection;
        }

        if (keyText.Length > this._options.MaxKeyLength)
        {
            this.EmitDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.KeyTooLong,
                string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.ParseException_KeyTooLong, this._options.MaxKeyLength),
                new BoduConfigurationSourceLocation(lineNumber, firstNonWs + 1, keyText.Length, path));
            return currentSection;
        }

        BoduConfigurationComment? inlineComment = null;
        if (this._options.InlineCommentMode != BoduConfigurationInlineCommentMode.Disabled)
        {
            inlineComment = TryExtractInlineComment(ref valueText, this._options.InlineCommentMode, lineNumber, equalsIndex + 1, path);
        }

        this.AppendProperty(currentSection, keyText, valueText, lineNumber, firstNonWs, path, inlineComment);
        return currentSection;
    }

    private void AppendProperty(
        BoduConfigurationSection section,
        string rawKey,
        string value,
        int lineNumber,
        int linePosition,
        string? path,
        BoduConfigurationComment? inlineComment = null)
    {
        BoduConfigurationProperty? existing = null;
        int existingIndex = -1;
        StringComparer comparer = this._options.KeyOptions.KeyComparer;
        for (int i = 0; i < section.Properties.Count; i++)
        {
            if (comparer.Equals(section.Properties[i].RawKey, rawKey))
            {
                existing = section.Properties[i];
                existingIndex = i;
                break;
            }
        }

        BoduConfigurationSourceLocation loc = new(lineNumber, linePosition + 1, rawKey.Length, path);

        BoduConfigurationProperty property;
        try
        {
            property = new BoduConfigurationProperty(rawKey, value, this._options.KeyOptions, loc);
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

        if (inlineComment.HasValue)
            property.InlineComment = inlineComment.Value;

        foreach (BoduConfigurationComment c in this._pendingLeadingComments)
            property.AddLeadingComment(c);
        this._pendingLeadingComments.Clear();

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
                        string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.ParseException_DuplicateKey, rawKey),
                        loc);
                    return;

                case IniDuplicateKeyBehavior.LastWins:
                default:
                    section.ReplacePropertyAt(existingIndex, property);
                    return;
            }
        }

        section.AddProperty(property);
    }
}
