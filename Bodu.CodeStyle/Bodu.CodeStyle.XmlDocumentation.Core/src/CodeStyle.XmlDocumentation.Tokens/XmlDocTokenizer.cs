// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTokenizer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// <param name="preserveTagAttributes">When <see langword="true" />, inter-attribute spacing in a reflowed tag is preserved rather than collapsed.</param>
    /// <param name="preserveCrefText">When <see langword="true" />, whitespace inside attribute values is preserved rather than collapsed.</param>
    /// <returns>The ordered list of tokens produced from the input.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="content" /> or <paramref name="inlineTags" /> is <see langword="null" />.
    /// </exception>
    public static ImmutableArray<XmlDocToken> Tokenize(string content, ImmutableHashSet<string> inlineTags, bool preserveTagAttributes = false, bool preserveCrefText = true)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (inlineTags is null) throw new ArgumentNullException(nameof(inlineTags));

        ImmutableArray<XmlDocToken>.Builder builder = ImmutableArray.CreateBuilder<XmlDocToken>();
        var position = 0;
        var textRun = new StringBuilder();
        var whitespaceRun = new StringBuilder();

        while (position < content.Length)
        {
            var current = content[position];

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

                if (TryReadCData(content, position, out var cdataEnd, out var cdataText))
                {
                    builder.Add(XmlDocToken.CData(cdataText));
                    position = cdataEnd;
                    continue;
                }

                if (TryReadXmlComment(content, position, out var commentEnd, out var commentText))
                {
                    builder.Add(XmlDocToken.Text(commentText));
                    position = commentEnd;
                    continue;
                }

                if (TryReadTag(content, position, inlineTags, preserveTagAttributes, preserveCrefText, out var tagEnd, out XmlDocToken? tagToken))
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

        var closeIndex = content.IndexOf(CloseSequence, start + OpenSequence.Length, StringComparison.Ordinal);
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

        var closeIndex = content.IndexOf(CloseSequence, start + OpenSequence.Length, StringComparison.Ordinal);
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

    private static bool TryReadTag(string content, int start, ImmutableHashSet<string> inlineTags, bool preserveTagAttributes, bool preserveCrefText, out int end, out XmlDocToken? token)
    {
        // Parse: < [/] name [attrs ...] [ /] >
        var position = start;
        if (position >= content.Length || content[position] != '<')
        {
            end = 0;
            token = null;
            return false;
        }

        position++;
        var isClosing = false;
        if (position < content.Length && content[position] == '/')
        {
            isClosing = true;
            position++;
        }

        var nameStart = position;
        while (position < content.Length && IsNameChar(content[position]))
        {
            position++;
        }

        if (position == nameStart || !IsNameStartChar(content[nameStart]))
        {
            // Either no name characters, or the name begins with a character that is not a valid XML
            // NameStartChar (a digit, '-', or '.'). Defer the '<' to the text-run path so the malformed
            // construct is preserved verbatim.
            end = 0;
            token = null;
            return false;
        }

        var tagName = content.Substring(nameStart, position - nameStart);

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
        var selfClosing = false;
        while (position < content.Length)
        {
            var ch = content[position];
            if (ch == '"' || ch == '\'')
            {
                var quoteEnd = content.IndexOf(ch, position + 1);
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

        var raw = NormalizeTagWhitespace(content.Substring(start, position - start), preserveTagAttributes, preserveCrefText);

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
            var closeSequence = "</" + tagName;
            var searchFrom = position;
            while (true)
            {
                var closeIndex = content.IndexOf(closeSequence, searchFrom, StringComparison.Ordinal);
                if (closeIndex < 0)
                {
                    break;
                }

                // Require an element-name boundary after the matched name so "</c" does not match "</code>";
                // the next character must end the close tag (whitespace or '>'), not continue the name.
                var afterName = closeIndex + closeSequence.Length;
                if (afterName < content.Length && IsCloseTagNameBoundary(content[afterName]))
                {
                    var closeTagEnd = content.IndexOf('>', afterName);
                    if (closeTagEnd >= 0)
                    {
                        var totalEnd = closeTagEnd + 1;
                        var atomic = NormalizeTagWhitespace(content.Substring(start, totalEnd - start), preserveTagAttributes, preserveCrefText);
                        end = totalEnd;
                        token = new XmlDocToken(XmlDocTokenKind.InlineXml, atomic, tagName, isSelfClosing: false);
                        return true;
                    }

                    break;
                }

                searchFrom = closeIndex + 1;
            }

            // Inline tag without a matching closer — fall through and treat as a block start.
        }

        end = position;
        token = new XmlDocToken(XmlDocTokenKind.BlockStart, raw, tagName, isSelfClosing: false);
        return true;
    }

    private static string NormalizeTagWhitespace(string raw, bool preserveTagAttributes, bool preserveCrefText)
    {
        if (preserveTagAttributes)
        {
            // Preserve the tag exactly as authored, including any internal line breaks and alignment. The
            // composer emits a multi-line tag verbatim across prefixed output lines, so its layout survives.
            return raw;
        }

        if (raw.IndexOf('\n') < 0 && raw.IndexOf('\r') < 0)
        {
            return raw;
        }

        // A tag spanning multiple physical lines is joined onto one line for the line-based composer: embedded
        // newlines are normalized to a single space. preserveCrefText governs whether redundant whitespace
        // inside attribute values (such as cref) is also collapsed.
        const bool collapseOutsideQuotes = true;
        var collapseInsideQuotes = !preserveCrefText;

        var openTagEnd = ScanToTagEnd(raw, 0);
        if (openTagEnd < 0)
        {
            return NormalizeAttributeSection(raw, 0, raw.Length, collapseOutsideQuotes, collapseInsideQuotes);
        }

        var closeTagStart = FindClosingTagStart(raw, openTagEnd + 1);
        if (closeTagStart < 0)
        {
            return NormalizeAttributeSection(raw, 0, raw.Length, collapseOutsideQuotes, collapseInsideQuotes);
        }

        var result = new StringBuilder(raw.Length);
        result.Append(NormalizeAttributeSection(raw, 0, openTagEnd + 1, collapseOutsideQuotes, collapseInsideQuotes));
        AppendBodyWithoutNewlines(result, raw, openTagEnd + 1, closeTagStart);
        result.Append(NormalizeAttributeSection(raw, closeTagStart, raw.Length, collapseOutsideQuotes, collapseInsideQuotes));
        return result.ToString();
    }

    private static int ScanToTagEnd(string raw, int start)
    {
        var inQuote = false;
        var quoteChar = '\0';
        for (var i = start; i < raw.Length; i++)
        {
            var ch = raw[i];
            if (inQuote)
            {
                if (ch == quoteChar)
                {
                    inQuote = false;
                }

                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                inQuote = true;
                quoteChar = ch;
                continue;
            }

            if (ch == '>')
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindClosingTagStart(string raw, int start)
    {
        var inQuote = false;
        var quoteChar = '\0';
        var lastClosingTagStart = -1;
        for (var i = start; i < raw.Length - 1; i++)
        {
            var ch = raw[i];
            if (inQuote)
            {
                if (ch == quoteChar)
                {
                    inQuote = false;
                }

                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                inQuote = true;
                quoteChar = ch;
                continue;
            }

            if (ch == '<' && raw[i + 1] == '/')
            {
                lastClosingTagStart = i;
            }
        }

        return lastClosingTagStart;
    }

    private static void AppendBodyWithoutNewlines(StringBuilder builder, string raw, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            var ch = raw[i];
            if (ch == '\r' || ch == '\n')
            {
                continue;
            }

            builder.Append(ch);
        }
    }

    private static string NormalizeAttributeSection(string raw, int start, int end, bool collapseOutsideQuotes, bool collapseInsideQuotes)
    {
        var result = new StringBuilder(end - start);
        var inQuote = false;
        var quoteChar = '\0';
        var lastWasSpace = false;
        for (var i = start; i < end; i++)
        {
            var ch = raw[i];
            if (inQuote)
            {
                var isNewline = ch == '\n' || ch == '\r';
                var isHorizontalWs = ch == ' ' || ch == '\t';

                // Newlines inside a value can never survive on a single line, so they always collapse to one
                // space. Other whitespace inside the value collapses only when value/cref preservation is off.
                if (isNewline || (isHorizontalWs && collapseInsideQuotes))
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
                result.Length--;
                result.Append('>');
                lastWasSpace = false;
                continue;
            }

            if (ch == '\n' || ch == '\r' || ch == ' ' || ch == '\t')
            {
                // Newlines outside quotes can never survive on a single line; collapse them. Horizontal
                // whitespace runs collapse only when attribute-spacing preservation is off — otherwise each
                // whitespace character is emitted as a space so the authored spacing is kept.
                if (ch == '\n' || ch == '\r' || collapseOutsideQuotes)
                {
                    if (!lastWasSpace)
                    {
                        result.Append(' ');
                        lastWasSpace = true;
                    }
                }
                else
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

    private static bool IsNameStartChar(char ch) =>
        (ch >= 'A' && ch <= 'Z') ||
        (ch >= 'a' && ch <= 'z') ||
        ch == '_' || ch == ':';

    private static bool IsCloseTagNameBoundary(char ch) =>
        ch == '>' || ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
}
