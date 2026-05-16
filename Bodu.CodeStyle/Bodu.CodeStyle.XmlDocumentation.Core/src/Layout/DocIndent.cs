// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocIndent.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Bodu.CodeStyle.XmlDocumentation.Layout;

/// <summary>
/// Provides helpers for stripping and re-applying the <c>"/// "</c> prefix and base indentation around the prose
/// content of an XML documentation comment.
/// </summary>
internal static class DocIndent
{
    /// <summary>
    /// Strips the documentation prefix from every physical line of the given trivia text.
    /// </summary>
    /// <param name="triviaText">The raw documentation comment trivia text.</param>
    /// <param name="baseIndent">The base indent reported by the caller for non-first lines.</param>
    /// <param name="documentationPrefix">The documentation prefix to recognise; typically <c>"/// "</c>.</param>
    /// <returns>The prose content with the prefix and indent removed from every line, joined by <c>'\n'</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any argument is <see langword="null" />.
    /// </exception>
    public static string Strip(string triviaText, string baseIndent, string documentationPrefix)
    {
        if (triviaText is null) throw new ArgumentNullException(nameof(triviaText));
        if (baseIndent is null) throw new ArgumentNullException(nameof(baseIndent));
        if (documentationPrefix is null) throw new ArgumentNullException(nameof(documentationPrefix));

        string prefixNoTrailingSpace = TrimPrefixTrailingSpace(documentationPrefix);

        StringBuilder result = new StringBuilder(triviaText.Length);
        int position = 0;
        bool firstLine = true;
        while (position < triviaText.Length)
        {
            int lineEnd = FindLineEnd(triviaText, position);
            string line = triviaText.Substring(position, lineEnd - position);

            string stripped = StripLine(line, firstLine ? string.Empty : baseIndent, prefixNoTrailingSpace);
            result.Append(stripped);

            position = lineEnd;
            if (position < triviaText.Length)
            {
                if (triviaText[position] == '\r' && position + 1 < triviaText.Length && triviaText[position + 1] == '\n')
                {
                    position += 2;
                }
                else
                {
                    position++;
                }

                result.Append('\n');
            }

            firstLine = false;
        }

        return result.ToString();
    }

    /// <summary>
    /// Re-applies the documentation prefix to every line of formatted content and joins them with the
    /// supplied line ending.
    /// </summary>
    /// <param name="contentLines">The formatted content lines, one entry per physical output line.</param>
    /// <param name="baseIndent">The base indent to apply before every line's prefix.</param>
    /// <param name="documentationPrefix">The documentation prefix; typically <c>"/// "</c>.</param>
    /// <param name="lineEnding">The line ending sequence to emit between lines.</param>
    /// <returns>The reassembled documentation trivia text including the trailing line ending.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any argument is <see langword="null" />.
    /// </exception>
    public static string Reapply(IReadOnlyList<string> contentLines, string baseIndent, string documentationPrefix, string lineEnding)
    {
        if (contentLines is null) throw new ArgumentNullException(nameof(contentLines));
        if (baseIndent is null) throw new ArgumentNullException(nameof(baseIndent));
        if (documentationPrefix is null) throw new ArgumentNullException(nameof(documentationPrefix));
        if (lineEnding is null) throw new ArgumentNullException(nameof(lineEnding));

        string prefixNoTrailingSpace = TrimPrefixTrailingSpace(documentationPrefix);

        StringBuilder result = new StringBuilder();
        for (int i = 0; i < contentLines.Count; i++)
        {
            string content = contentLines[i];
            if (i > 0)
            {
                result.Append(baseIndent);
            }

            if (content.Length == 0)
            {
                result.Append(prefixNoTrailingSpace);
            }
            else
            {
                result.Append(documentationPrefix);
                result.Append(content);
            }

            result.Append(lineEnding);
        }

        return result.ToString();
    }

    private static string TrimPrefixTrailingSpace(string prefix)
    {
        int end = prefix.Length;
        while (end > 0 && (prefix[end - 1] == ' ' || prefix[end - 1] == '\t'))
        {
            end--;
        }

        return prefix.Substring(0, end);
    }

    private static int FindLineEnd(string text, int start)
    {
        int position = start;
        while (position < text.Length && text[position] != '\r' && text[position] != '\n')
        {
            position++;
        }

        return position;
    }

    private static string StripLine(string line, string expectedIndent, string prefixNoTrailingSpace)
    {
        int cursor = 0;

        // Consume the expected leading indent if it matches; otherwise tolerate any leading whitespace.
        if (expectedIndent.Length > 0 &&
            line.Length >= expectedIndent.Length &&
            string.CompareOrdinal(line, 0, expectedIndent, 0, expectedIndent.Length) == 0)
        {
            cursor = expectedIndent.Length;
        }
        else
        {
            while (cursor < line.Length && (line[cursor] == ' ' || line[cursor] == '\t'))
            {
                cursor++;
            }
        }

        if (cursor + prefixNoTrailingSpace.Length > line.Length ||
            string.CompareOrdinal(line, cursor, prefixNoTrailingSpace, 0, prefixNoTrailingSpace.Length) != 0)
        {
            // Line does not begin with the documentation prefix; return as-is.
            return line.Substring(cursor);
        }

        cursor += prefixNoTrailingSpace.Length;

        // Skip a single optional space (the "/// " convention).
        if (cursor < line.Length && line[cursor] == ' ')
        {
            cursor++;
        }

        return line.Substring(cursor);
    }
}
