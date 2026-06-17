// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocProseText.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Provides helpers for reducing the raw textual content of a documentation element to its canonical single-line
/// rendering, shared by the content-quality analyzer and its code fix so the measured length and the relocated
/// prose match what the formatter would emit.
/// </summary>
public static class XmlDocProseText
{
    /// <summary>
    /// Canonicalizes the raw content of a documentation element to a single line: each physical line's leading
    /// indentation and <c>///</c> doc-comment prefix is stripped, the lines are joined, and runs of whitespace
    /// are collapsed to a single space with the ends trimmed.
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

            var line = StripLeadingDocPrefix(rawContent, start, i);
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

    private static string StripLeadingDocPrefix(string text, int start, int end)
    {
        var i = start;
        while (i < end && (text[i] == ' ' || text[i] == '\t')) i++;

        if (i + 2 < end && text[i] == '/' && text[i + 1] == '/' && text[i + 2] == '/')
        {
            i += 3;
            if (i < end && text[i] == ' ') i++;
        }

        return text.Substring(i, end - i);
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
