// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationReader.Helpers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

internal sealed partial class BoduConfigurationReader
{
    /// <summary>
    /// Emits a diagnostic, throwing immediately when the configured mode is
    /// <see cref="BoduConfigurationDiagnosticMode.Throw" /> and retaining it on the local diagnostic list only
    /// when the mode is <see cref="BoduConfigurationDiagnosticMode.Collect" />.
    /// </summary>
    private void EmitDiagnostic(
        BoduConfigurationDiagnosticSeverity severity,
        BoduConfigurationDiagnosticCode code,
        string message,
        BoduConfigurationSourceLocation location)
    {
        BoduConfigurationDiagnostic diagnostic = new(severity, code, message, location);

        switch (this._options.DiagnosticMode)
        {
            case BoduConfigurationDiagnosticMode.Throw:
                if (severity == BoduConfigurationDiagnosticSeverity.Error)
                    throw new BoduConfigurationParseException(diagnostic);
                this._diagnostics.Add(diagnostic);
                break;

            case BoduConfigurationDiagnosticMode.Collect:
                this._diagnostics.Add(diagnostic);
                break;

            case BoduConfigurationDiagnosticMode.Ignore:
            default:
                break;
        }
    }

    private static IniSection GetCurrentSection(IniDocument document) =>
        document.Sections.Count > 0 ? document.Sections[^1] : document.GlobalSection;

    private static void AttachPendingComments(IniSection section, List<IniComment> pending)
    {
        foreach (IniComment c in pending)
            section.AddLeadingComment(c);
        pending.Clear();
    }

    private static int FindFirstNonWhitespace(string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (!char.IsWhiteSpace(line[i]))
                return i;
        }

        return -1;
    }

    private static int FindLastClosingBracket(string line, int firstNonWs)
    {
        for (int i = line.Length - 1; i > firstNonWs; i--)
        {
            if (line[i] == ']')
                return i;

            if (!char.IsWhiteSpace(line[i]))
                break;
        }

        return -1;
    }

    private static int FindFirstUnescaped(string line, char target, int from)
    {
        for (int i = from; i < line.Length; i++)
        {
            if (line[i] == '\\' && i + 1 < line.Length)
            {
                i++;
                continue;
            }

            if (line[i] == target)
                return i;
        }

        return -1;
    }

    private static string TrimTrailing(string line, int firstNonWs)
    {
        int end = line.Length - 1;
        while (end >= firstNonWs && char.IsWhiteSpace(line[end]))
            end--;
        return line.Substring(firstNonWs, end - firstNonWs + 1);
    }

    private static IniComment? TryExtractInlineComment(
        ref string value,
        BoduConfigurationInlineCommentMode mode,
        int lineNumber)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\\' && i + 1 < value.Length)
            {
                i++;
                continue;
            }

            if (c != '#' && c != ';')
                continue;

            bool whitespaceBefore = i > 0 && char.IsWhiteSpace(value[i - 1]);
            bool isInlineComment = mode == BoduConfigurationInlineCommentMode.Always
                                   || (mode == BoduConfigurationInlineCommentMode.WhitespaceIntroduced && whitespaceBefore);

            if (!isInlineComment)
                continue;

            string commentText = value.Substring(i + 1);
            string remaining = value.Substring(0, i).TrimEnd();
            value = remaining;
            return new IniComment(c, commentText, lineNumber);
        }

        return null;
    }
}
