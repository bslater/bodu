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

        var prefixNoTrailingSpace = TrimPrefixTrailingSpace(documentationPrefix);

        var result = new StringBuilder(triviaText.Length);
        var position = 0;
        var firstLine = true;
        while (position < triviaText.Length)
        {
            var lineEnd = FindLineEnd(triviaText, position);
            var line = triviaText.Substring(position, lineEnd - position);

            var stripped = StripLine(line, firstLine ? string.Empty : baseIndent, prefixNoTrailingSpace);
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

        var prefixNoTrailingSpace = TrimPrefixTrailingSpace(documentationPrefix);

        var result = new StringBuilder();
        for (var i = 0; i < contentLines.Count; i++)
        {
            var content = contentLines[i];
            if (i > 0)
            {
                result.Append(baseIndent);
            }

            if (content.Length == 0)
            {
                result.Append(prefixNoTrailingSpace);
            }
            else if (IsCDataDelimiterLine(content) || StartsWithCDataOpener(content))
            {
                // CDATA open / close delimiter on its own line, or a line that begins with `<![CDATA[`
                // (with body on the same line): omit the space between the documentation prefix and the
                // delimiter so the round-trip preserves the Bodu convention of `///<![CDATA[` and `///]]>`
                // (no space, tightly attached). The no-space form is required by BODU1405 — any whitespace
                // between `///` and `<![CDATA[` trips that rule.
                result.Append(prefixNoTrailingSpace);
                result.Append(content);
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

    private static bool IsCDataDelimiterLine(string content)
    {
        // Only treat a line as a CDATA delimiter when it's the open or close marker alone — anything else
        // (single-line `<![CDATA[var x = 5;]]>` embedded in prose, or an unrelated string that happens to
        // start with `<![`) gets the normal `/// ` prefix.
        return string.Equals(content, "<![CDATA[", StringComparison.Ordinal)
            || string.Equals(content, "]]>", StringComparison.Ordinal);
    }

    private static bool StartsWithCDataOpener(string content)
    {
        // Treat a line as starting with the CDATA opener when `<![CDATA[` is at position 0 and the next
        // character (if any) is not a tag-name character. This catches the case where prior reflow has
        // collapsed a multi-line CDATA body onto a single content line, leaving the opener fused to its
        // payload (for example `<![CDATA[ int volume = 11; ... ]]>` on one line). Inline CDATA further
        // into prose (`Use <![CDATA[var x = …]]> for X.`) is unaffected because the line does not begin
        // with the opener.
        const string Opener = "<![CDATA[";
        return content.StartsWith(Opener, StringComparison.Ordinal);
    }

    private static string TrimPrefixTrailingSpace(string prefix)
    {
        var end = prefix.Length;
        while (end > 0 && (prefix[end - 1] == ' ' || prefix[end - 1] == '\t'))
        {
            end--;
        }

        return prefix.Substring(0, end);
    }

    private static int FindLineEnd(string text, int start)
    {
        var position = start;
        while (position < text.Length && text[position] != '\r' && text[position] != '\n')
        {
            position++;
        }

        return position;
    }

    private static string StripLine(string line, string expectedIndent, string prefixNoTrailingSpace)
    {
        var cursor = 0;

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
