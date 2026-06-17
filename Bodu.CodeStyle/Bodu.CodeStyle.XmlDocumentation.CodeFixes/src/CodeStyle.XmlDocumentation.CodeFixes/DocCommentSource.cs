// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocCommentSource.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.Text;

namespace Bodu.CodeStyle.XmlDocumentation.CodeFixes;

/// <summary>
/// Provides the shared, position-oriented <see cref="SourceText" /> primitives used by the documentation
/// code fixes — resolving the document line ending, locating line boundaries, and recovering the indentation
/// and <c>///</c> prefix of a doc-comment line. Centralizing them keeps every fix reading and re-emitting the
/// surrounding doc-comment layout the same way.
/// </summary>
internal static class DocCommentSource
{
    private const string DefaultPrefix = "/// ";

    /// <summary>
    /// Resolves the line ending used by the document, scanning for the first terminator.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <returns>The detected line ending, or <c>"\r\n"</c> when the document contains no terminator.</returns>
    public static string ResolveLineEnding(SourceText text)
    {
        var length = text.Length;
        for (var i = 0; i < length; i++)
        {
            var ch = text[i];
            if (ch == '\r')
            {
                return i + 1 < length && text[i + 1] == '\n' ? "\r\n" : "\r";
            }

            if (ch == '\n')
            {
                return "\n";
            }
        }

        return "\r\n";
    }

    /// <summary>
    /// Locates the start of the line that contains the supplied position.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <param name="position">A character offset within the line.</param>
    /// <returns>The offset of the first character on the line, immediately after any preceding terminator.</returns>
    public static int FindLineStart(SourceText text, int position)
    {
        var lineStart = position;
        while (lineStart > 0 && text[lineStart - 1] != '\n' && text[lineStart - 1] != '\r')
        {
            lineStart--;
        }

        return lineStart;
    }

    /// <summary>
    /// Locates the end of the line that contains the supplied position, including the line's terminator.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <param name="position">A character offset within the line.</param>
    /// <returns>The offset immediately after the line's terminator, or the document length when the line is unterminated.</returns>
    public static int FindLineEndIncludingTerminator(SourceText text, int position)
    {
        var index = position;
        while (index < text.Length && text[index] != '\n' && text[index] != '\r')
        {
            index++;
        }

        if (index < text.Length && text[index] == '\r')
        {
            index++;
            if (index < text.Length && text[index] == '\n')
            {
                index++;
            }
        }
        else if (index < text.Length && text[index] == '\n')
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// Recovers the leading whitespace run of the line that contains the supplied position.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <param name="position">A character offset within the line.</param>
    /// <returns>The line's leading indentation, up to its first non-whitespace character.</returns>
    public static string ExtractIndent(SourceText text, int position)
    {
        var lineStart = FindLineStart(text, position);

        var end = lineStart;
        while (end < text.Length && (text[end] == ' ' || text[end] == '\t'))
        {
            end++;
        }

        return text.ToString(TextSpan.FromBounds(lineStart, end));
    }

    /// <summary>
    /// Recovers the documentation-comment prefix actually used on the line that contains the supplied position —
    /// the <c>///</c> run plus a single following space when present — so synthesized blocks match the source
    /// style instead of assuming a hardcoded prefix.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <param name="position">A character offset within the line.</param>
    /// <returns>The detected prefix, or the canonical <c>"/// "</c> when the line carries no <c>///</c> prefix.</returns>
    public static string DetectPrefix(SourceText text, int position)
    {
        var lineStart = FindLineStart(text, position);
        var i = lineStart;
        while (i < text.Length && (text[i] == ' ' || text[i] == '\t')) i++;

        if (i + 2 < text.Length && text[i] == '/' && text[i + 1] == '/' && text[i + 2] == '/')
        {
            var end = i + 3;
            if (end < text.Length && text[end] == ' ') end++;
            return text.ToString(TextSpan.FromBounds(i, end));
        }

        return DefaultPrefix;
    }
}
