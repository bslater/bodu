// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReader.Helpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

internal sealed partial class ConfigurationReader
{
    /// <summary>
    /// Emits a diagnostic, throwing immediately when the configured mode is
    /// <see cref="ConfigurationDiagnosticMode.Throw" /> and retaining it on the local diagnostic list only when the
    /// mode is <see cref="ConfigurationDiagnosticMode.Collect" />.
    /// </summary>
    /// <param name="severity">The severity classification of the diagnostic.</param>
    /// <param name="code">The stable code identifying the diagnostic category.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="location">The source location the diagnostic refers to.</param>
    /// <exception cref="ConfigurationParseException">
    /// The configured mode is <see cref="ConfigurationDiagnosticMode.Throw" /> and <paramref name="severity" /> is
    /// <see cref="ConfigurationDiagnosticSeverity.Error" />.
    /// </exception>
    private void EmitDiagnostic(
        ConfigurationDiagnosticSeverity severity,
        ConfigurationDiagnosticCode code,
        string message,
        ConfigurationSourceLocation location)
    {
        ConfigurationDiagnostic diagnostic = new(severity, code, message, location);

        switch (_options.DiagnosticMode)
        {
            case ConfigurationDiagnosticMode.Throw:
                if (severity == ConfigurationDiagnosticSeverity.Error)
                    throw new ConfigurationParseException(diagnostic);
                _diagnostics.Add(diagnostic);
                break;

            case ConfigurationDiagnosticMode.Collect:
                _diagnostics.Add(diagnostic);
                break;

            case ConfigurationDiagnosticMode.Ignore:
            default:
                break;
        }
    }

    /// <summary>
    /// Gets the section a property should attach to — the most recently declared section, or the global section when
    /// none have been declared.
    /// </summary>
    /// <param name="document">The document being populated.</param>
    /// <returns>The current target section.</returns>
    private static IniSection GetCurrentSection(ConfigurationDocument document) =>
        document.Sections.Count > 0 ? document.Sections[^1] : document.GlobalSection;

    /// <summary>
    /// Attaches every comment in <paramref name="pending" /> to <paramref name="section" /> and clears the list.
    /// </summary>
    /// <param name="section">The section the comments are attached to.</param>
    /// <param name="pending">The pending leading comments; emptied by this call.</param>
    private static void AttachPendingComments(IniSection section, List<IniComment> pending)
    {
        foreach (IniComment c in pending)
            section.AddLeadingComment(c);
        pending.Clear();
    }

    /// <summary>
    /// Finds the index of the first non-whitespace character in <paramref name="line" />.
    /// </summary>
    /// <param name="line">The line to scan.</param>
    /// <returns>The zero-based index, or <c>-1</c> when the line is blank.</returns>
    private static int FindFirstNonWhitespace(string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (!char.IsWhiteSpace(line[i]))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the index of the last <c>]</c> on <paramref name="line" /> after <paramref name="firstNonWs" />. The scan
    /// does not stop at trailing non-whitespace — characters after the closing <c>]</c> are evaluated separately by the
    /// section-header mode, which decides whether they are an inline comment, harmless trailing text, or a
    /// diagnostic-worthy malformed suffix.
    /// </summary>
    /// <param name="line">The line to scan.</param>
    /// <param name="firstNonWs">The index of the first non-whitespace character on the line.</param>
    /// <returns>The index of the last <c>]</c>, or <c>-1</c> when none is found.</returns>
    private static int FindLastClosingBracket(string line, int firstNonWs)
    {
        for (int i = line.Length - 1; i > firstNonWs; i--)
        {
            if (line[i] == ']')
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the index of the first unescaped occurrence of <paramref name="target" /> at or after
    /// <paramref name="from" />.
    /// </summary>
    /// <param name="line">The line to scan.</param>
    /// <param name="target">The character to search for.</param>
    /// <param name="from">The index to begin scanning from.</param>
    /// <returns>The zero-based index, or <c>-1</c> when no unescaped occurrence is found.</returns>
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

    /// <summary>
    /// Returns the substring of <paramref name="line" /> from <paramref name="firstNonWs" /> with trailing whitespace
    /// removed.
    /// </summary>
    /// <param name="line">The line to trim.</param>
    /// <param name="firstNonWs">The index of the first non-whitespace character on the line.</param>
    /// <returns>The trimmed substring.</returns>
    private static string TrimTrailing(string line, int firstNonWs)
    {
        int end = line.Length - 1;
        while (end >= firstNonWs && char.IsWhiteSpace(line[end]))
            end--;
        return line.Substring(firstNonWs, end - firstNonWs + 1);
    }

    /// <summary>
    /// Attempts to split an inline comment from the end of <paramref name="value" /> under the supplied comment mode.
    /// </summary>
    /// <param name="value">
    /// The property value to scan; trimmed of the comment and trailing whitespace when one is found.
    /// </param>
    /// <param name="mode">The inline comment mode that decides when a comment prefix is recognized.</param>
    /// <param name="lineNumber">The 1-based line number recorded on the extracted comment.</param>
    /// <returns>The extracted inline comment, or <see langword="null" /> when none is present.</returns>
    private static IniComment? TryExtractInlineComment(
        ref string value,
        ConfigurationInlineCommentMode mode,
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

            if (c is not '#' and not ';')
                continue;

            bool whitespaceBefore = i > 0 && char.IsWhiteSpace(value[i - 1]);
            bool isInlineComment = mode == ConfigurationInlineCommentMode.Always
                                   || (mode == ConfigurationInlineCommentMode.WhitespaceIntroduced && whitespaceBefore);

            if (!isInlineComment)
                continue;

            string commentText = value[(i + 1) ..];
            string remaining = value[..i].TrimEnd();
            value = remaining;
            return new IniComment(c, commentText, lineNumber);
        }

        return null;
    }
}
