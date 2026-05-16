// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTokenizer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Bodu.CodeStyle.XmlDocumentation.Tokens;

/// <summary>
/// Decomposes the prose content of an XML documentation comment (with the documentation prefix already stripped)
/// into a flat token stream.
/// </summary>
/// <remarks>
/// <para>
/// The tokenizer is a single-pass state machine. It treats anything inside a tag matching
/// <see cref="XmlDocFormatOptions.InlineTags" /> as one atomic token, so the downstream wrapper can never split it
/// across lines. CDATA and XML comments inside the content are returned verbatim as text runs.
/// </para>
/// </remarks>
internal static class XmlDocTokenizer
{
    /// <summary>
    /// Tokenizes the prose content of a documentation comment.
    /// </summary>
    /// <param name="content">The doc-comment content with the <c>"/// "</c> prefix already stripped from each line.</param>
    /// <param name="inlineTags">The set of element names treated as inline atomic tokens.</param>
    /// <returns>The ordered list of tokens produced from the input.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="content" /> or <paramref name="inlineTags" /> is <see langword="null" />.
    /// </exception>
    public static ImmutableArray<XmlDocToken> Tokenize(string content, ImmutableHashSet<string> inlineTags)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (inlineTags is null) throw new ArgumentNullException(nameof(inlineTags));

        ImmutableArray<XmlDocToken>.Builder builder = ImmutableArray.CreateBuilder<XmlDocToken>();
        int position = 0;
        StringBuilder textRun = new StringBuilder();
        StringBuilder whitespaceRun = new StringBuilder();

        while (position < content.Length)
        {
            char current = content[position];

            if (current == '\n')
            {
                FlushWhitespace(builder, whitespaceRun);
                FlushText(builder, textRun);
                builder.Add(XmlDocToken.LineBreak());
                position++;
                continue;
            }

            if (current == '\r')
            {
                // Skip the CR, will be re-emitted by the line-builder using the context's line ending.
                position++;
                continue;
            }

            if (current == ' ' || current == '\t')
            {
                FlushText(builder, textRun);
                whitespaceRun.Append(current);
                position++;
                continue;
            }

            if (current == '<')
            {
                FlushWhitespace(builder, whitespaceRun);
                FlushText(builder, textRun);

                if (TryReadCData(content, position, out int cdataEnd, out string cdataText))
                {
                    builder.Add(XmlDocToken.Text(cdataText));
                    position = cdataEnd;
                    continue;
                }

                if (TryReadXmlComment(content, position, out int commentEnd, out string commentText))
                {
                    builder.Add(XmlDocToken.Text(commentText));
                    position = commentEnd;
                    continue;
                }

                if (TryReadTag(content, position, inlineTags, out int tagEnd, out XmlDocToken? tagToken))
                {
                    builder.Add(tagToken!);
                    position = tagEnd;
                    continue;
                }

                // Fall through: treat the '<' as literal text when no valid tag follows.
                textRun.Append(current);
                position++;
                continue;
            }

            FlushWhitespace(builder, whitespaceRun);
            textRun.Append(current);
            position++;
        }

        FlushWhitespace(builder, whitespaceRun);
        FlushText(builder, textRun);

        return builder.ToImmutable();
    }

    private static void FlushText(ImmutableArray<XmlDocToken>.Builder builder, StringBuilder textRun)
    {
        if (textRun.Length == 0) return;

        builder.Add(XmlDocToken.Text(textRun.ToString()));
        textRun.Clear();
    }

    private static void FlushWhitespace(ImmutableArray<XmlDocToken>.Builder builder, StringBuilder whitespaceRun)
    {
        if (whitespaceRun.Length == 0) return;

        builder.Add(XmlDocToken.Whitespace(whitespaceRun.ToString()));
        whitespaceRun.Clear();
    }

    private static bool TryReadCData(string content, int start, out int end, out string text)
    {
        const string OpenSequence = "<![CDATA[";
        const string CloseSequence = "]]>";

        if (start + OpenSequence.Length > content.Length ||
            string.CompareOrdinal(content, start, OpenSequence, 0, OpenSequence.Length) != 0)
        {
            end = 0;
            text = string.Empty;
            return false;
        }

        int closeIndex = content.IndexOf(CloseSequence, start + OpenSequence.Length, StringComparison.Ordinal);
        if (closeIndex < 0)
        {
            end = 0;
            text = string.Empty;
            return false;
        }

        end = closeIndex + CloseSequence.Length;
        text = content.Substring(start, end - start);
        return true;
    }

    private static bool TryReadXmlComment(string content, int start, out int end, out string text)
    {
        const string OpenSequence = "<!--";
        const string CloseSequence = "-->";

        if (start + OpenSequence.Length > content.Length ||
            string.CompareOrdinal(content, start, OpenSequence, 0, OpenSequence.Length) != 0)
        {
            end = 0;
            text = string.Empty;
            return false;
        }

        int closeIndex = content.IndexOf(CloseSequence, start + OpenSequence.Length, StringComparison.Ordinal);
        if (closeIndex < 0)
        {
            end = 0;
            text = string.Empty;
            return false;
        }

        end = closeIndex + CloseSequence.Length;
        text = content.Substring(start, end - start);
        return true;
    }

    private static bool TryReadTag(string content, int start, ImmutableHashSet<string> inlineTags, out int end, out XmlDocToken? token)
    {
        // Parse: < [/] name [attrs ...] [ /] >
        int position = start;
        if (position >= content.Length || content[position] != '<')
        {
            end = 0;
            token = null;
            return false;
        }

        position++;
        bool isClosing = false;
        if (position < content.Length && content[position] == '/')
        {
            isClosing = true;
            position++;
        }

        int nameStart = position;
        while (position < content.Length && IsNameChar(content[position]))
        {
            position++;
        }

        if (position == nameStart)
        {
            end = 0;
            token = null;
            return false;
        }

        string tagName = content.Substring(nameStart, position - nameStart);

        // Reject tags whose name is split by an unescaped newline immediately after the captured name characters
        // (e.g. "<sum\nmary>"). Returning false defers the original "<" to the text-run path so the caller
        // emits the malformed input verbatim and the formatter leaves it unchanged.
        if (position < content.Length && content[position] == '\n')
        {
            end = 0;
            token = null;
            return false;
        }

        // Skip attributes until > or />.
        bool selfClosing = false;
        while (position < content.Length)
        {
            char ch = content[position];
            if (ch == '"' || ch == '\'')
            {
                int quoteEnd = content.IndexOf(ch, position + 1);
                if (quoteEnd < 0)
                {
                    end = 0;
                    token = null;
                    return false;
                }

                position = quoteEnd + 1;
                continue;
            }

            if (ch == '/' && position + 1 < content.Length && content[position + 1] == '>')
            {
                selfClosing = true;
                position += 2;
                break;
            }

            if (ch == '>')
            {
                position++;
                break;
            }

            position++;
        }

        if (position == start || (position - 1 < content.Length && content[position - 1] != '>'))
        {
            end = 0;
            token = null;
            return false;
        }

        string raw = NormalizeTagWhitespace(content.Substring(start, position - start));

        if (isClosing)
        {
            end = position;
            token = new XmlDocToken(XmlDocTokenKind.BlockEnd, raw, tagName, isSelfClosing: false);
            return true;
        }

        if (selfClosing)
        {
            end = position;
            token = new XmlDocToken(XmlDocTokenKind.InlineXml, raw, tagName, isSelfClosing: true);
            return true;
        }

        // Opening tag. If the tag is in InlineTags, scan ahead for the matching </tag> and bundle as one atom.
        if (inlineTags.Contains(tagName))
        {
            string closeSequence = "</" + tagName;
            int closeIndex = content.IndexOf(closeSequence, position, StringComparison.Ordinal);
            if (closeIndex >= 0)
            {
                int closeTagEnd = content.IndexOf('>', closeIndex + closeSequence.Length);
                if (closeTagEnd >= 0)
                {
                    int totalEnd = closeTagEnd + 1;
                    string atomic = NormalizeTagWhitespace(content.Substring(start, totalEnd - start));
                    end = totalEnd;
                    token = new XmlDocToken(XmlDocTokenKind.InlineXml, atomic, tagName, isSelfClosing: false);
                    return true;
                }
            }

            // Inline tag without a matching closer — fall through and treat as a block start.
        }

        end = position;
        token = new XmlDocToken(XmlDocTokenKind.BlockStart, raw, tagName, isSelfClosing: false);
        return true;
    }

    private static string NormalizeTagWhitespace(string raw)
    {
        if (raw.IndexOf('\n') < 0 && raw.IndexOf('\r') < 0)
        {
            return raw;
        }

        StringBuilder result = new StringBuilder(raw.Length);
        bool inQuote = false;
        char quoteChar = '\0';
        bool lastWasSpace = false;
        foreach (char ch in raw)
        {
            if (inQuote)
            {
                if (ch == '\n' || ch == '\r')
                {
                    if (!lastWasSpace)
                    {
                        result.Append(' ');
                        lastWasSpace = true;
                    }
                }
                else
                {
                    result.Append(ch);
                    lastWasSpace = false;
                    if (ch == quoteChar)
                    {
                        inQuote = false;
                    }
                }

                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                inQuote = true;
                quoteChar = ch;
                result.Append(ch);
                lastWasSpace = false;
                continue;
            }

            if (ch == '>' && lastWasSpace && result.Length > 0 && result[result.Length - 1] == ' ')
            {
                // Strip the trailing whitespace before a closing > to avoid producing "<tag attr=value >".
                result.Length--;
                result.Append('>');
                lastWasSpace = false;
                continue;
            }

            if (ch == '\n' || ch == '\r' || ch == ' ' || ch == '\t')
            {
                if (!lastWasSpace)
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

        return result.ToString();
    }

    private static bool IsNameChar(char ch) =>
        (ch >= 'A' && ch <= 'Z') ||
        (ch >= 'a' && ch <= 'z') ||
        (ch >= '0' && ch <= '9') ||
        ch == '-' || ch == '_' || ch == '.' || ch == ':';
}
