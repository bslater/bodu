// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocProseText.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Provides the shared, Roslyn-free text primitives for working with the logical content of a documentation
/// comment — stripping the physical <c>///</c> prefixes, recovering logical body lines, and canonicalizing prose
/// to a single line. These are the doc-comment text operations shared by the formatter and by the content-quality
/// code fixes so they all reduce and re-emit doc-comment text the same way.
/// </summary>
public static class XmlDocProseText
{
    /// <summary>
    /// Strips a single physical documentation line down to its logical content: leading indentation, the
    /// <c>///</c> doc-comment prefix, and one following space are removed.
    /// </summary>
    /// <param name="physicalLine">The physical line, without its terminating newline.</param>
    /// <returns>The logical content of the line.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="physicalLine" /> is <see langword="null" />.</exception>
    public static string StripPrefix(string physicalLine)
    {
        if (physicalLine is null) throw new ArgumentNullException(nameof(physicalLine));

        var i = 0;
        while (i < physicalLine.Length && (physicalLine[i] == ' ' || physicalLine[i] == '\t')) i++;

        // Strip a single `///` prefix when present, plus one following space (the standard `/// ` form).
        if (i + 2 < physicalLine.Length && physicalLine[i] == '/' && physicalLine[i + 1] == '/' && physicalLine[i + 2] == '/')
        {
            i += 3;
            if (i < physicalLine.Length && physicalLine[i] == ' ') i++;
        }

        return physicalLine.Substring(i);
    }

    /// <summary>
    /// Splits the raw text between an element's start and end tags into its logical body lines: each physical
    /// line is stripped of its indentation and <c>///</c> prefix (and trailing whitespace), and leading and
    /// trailing blank lines are dropped so the caller alone controls the surrounding layout.
    /// </summary>
    /// <param name="rawBody">The raw inter-tag body text.</param>
    /// <returns>The logical body lines.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rawBody" /> is <see langword="null" />.</exception>
    public static IReadOnlyList<string> ToLogicalBodyLines(string rawBody)
    {
        if (rawBody is null) throw new ArgumentNullException(nameof(rawBody));

        var stripped = new List<string>();
        var start = 0;
        for (var i = 0; i <= rawBody.Length; i++)
        {
            if (i < rawBody.Length && rawBody[i] != '\n' && rawBody[i] != '\r') continue;

            stripped.Add(StripPrefix(rawBody.Substring(start, i - start)).TrimEnd());
            if (i < rawBody.Length && rawBody[i] == '\r' && i + 1 < rawBody.Length && rawBody[i + 1] == '\n') i++;
            start = i + 1;
        }

        var first = 0;
        while (first < stripped.Count && stripped[first].Length == 0) first++;
        var last = stripped.Count - 1;
        while (last >= first && stripped[last].Length == 0) last--;

        var result = new List<string>();
        for (var k = first; k <= last; k++)
        {
            result.Add(stripped[k]);
        }

        return result;
    }

    /// <summary>
    /// Canonicalizes the raw content of a documentation element to a single line: each physical line's
    /// indentation and <c>///</c> prefix is stripped, the lines are joined, and runs of whitespace are collapsed
    /// to a single space with the ends trimmed.
    /// </summary>
    /// <param name="rawContent">The raw element content, possibly spanning multiple physical lines.</param>
    /// <returns>The canonical single-line form of the content.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rawContent" /> is <see langword="null" />.</exception>
    public static string Canonicalize(string rawContent)
    {
        if (rawContent is null) throw new ArgumentNullException(nameof(rawContent));

        var joined = new StringBuilder(rawContent.Length);
        var start = 0;
        for (var i = 0; i <= rawContent.Length; i++)
        {
            if (i < rawContent.Length && rawContent[i] != '\n' && rawContent[i] != '\r') continue;

            var line = StripPrefix(rawContent.Substring(start, i - start));
            if (line.Length > 0)
            {
                if (joined.Length > 0) joined.Append(' ');
                joined.Append(line);
            }

            if (i < rawContent.Length && rawContent[i] == '\r' && i + 1 < rawContent.Length && rawContent[i + 1] == '\n') i++;
            start = i + 1;
        }

        return CollapseWhitespace(joined.ToString());
    }

    private static string CollapseWhitespace(string text)
    {
        var result = new StringBuilder(text.Length);
        var lastWasSpace = false;
        foreach (var ch in text)
        {
            if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n')
            {
                if (result.Length > 0 && !lastWasSpace)
                {
                    result.Append(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                result.Append(ch);
                lastWasSpace = false;
            }
        }

        if (result.Length > 0 && result[result.Length - 1] == ' ')
        {
            result.Length--;
        }

        return result.ToString();
    }
}
