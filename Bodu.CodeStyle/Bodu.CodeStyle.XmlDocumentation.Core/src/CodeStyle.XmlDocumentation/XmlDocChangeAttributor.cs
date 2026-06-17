// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocChangeAttributor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Bodu.CodeStyle.XmlDocumentation.Layout;
using Bodu.CodeStyle.XmlDocumentation.Tokens;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Compares the raw input and the formatter's output trivia text and produces a list of
/// <see cref="XmlDocFormattingChange" /> records attributing each difference to the responsible XML doc tag
/// (or to cross-cutting prose / prefix / indent when no tag is implicated).
/// </summary>
/// <remarks>
/// <para>
/// Attribution model: at the top level of the doc comment, each block-tag scope whose rendered text differs
/// from input to output emits one per-tag change record. Inside any such scope the walker only looks for
/// inline-XML token differences (<c>&lt;see /&gt;</c>, <c>&lt;c&gt;…&lt;/c&gt;</c>, <c>&lt;paramref /&gt;</c>,
/// <c>&lt;typeparamref /&gt;</c>) and attributes those to the inline tag. Nested block tags inside a top-level
/// scope are deliberately not separately attributed in this version — suppressing the enclosing block tag
/// silences the whole subtree, which matches the user-suppression model expressed in <c>.editorconfig</c>.
/// </para>
/// <para>
/// Prose differences outside any top-level scope (line prefix, indent, text between tags) collapse into a
/// single cross-cutting change (<c>tagName: null</c>) so consumers can silence the cross-cutting bucket
/// independently of any tag.
/// </para>
/// </remarks>
internal static class XmlDocChangeAttributor
{
    /// <summary>
    /// Produces the ordered, deduplicated set of <see cref="XmlDocFormattingChange" /> records describing how
    /// <paramref name="formattedText" /> differs from <paramref name="inputTriviaText" />.
    /// </summary>
    /// <param name="inputTriviaText">The raw documentation comment trivia text passed to the formatter.</param>
    /// <param name="formattedText">The canonical text emitted by the formatter.</param>
    /// <param name="context">The per-comment context used by the formatter.</param>
    /// <param name="options">The formatting policy used by the formatter.</param>
    /// <returns>The immutable list of attributed changes; empty when the two texts are identical.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null" />.</exception>
    public static ImmutableArray<XmlDocFormattingChange> Attribute(string inputTriviaText, string formattedText, XmlDocFormatContext context, XmlDocFormatOptions options)
    {
        if (inputTriviaText is null) throw new ArgumentNullException(nameof(inputTriviaText));
        if (formattedText is null) throw new ArgumentNullException(nameof(formattedText));
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (options is null) throw new ArgumentNullException(nameof(options));

        if (string.Equals(inputTriviaText, formattedText, StringComparison.Ordinal))
        {
            return ImmutableArray<XmlDocFormattingChange>.Empty;
        }

        var inputContent = DocIndent.Strip(inputTriviaText, context.BaseIndent, options.DocumentationPrefix);
        var outputContent = DocIndent.Strip(formattedText, context.BaseIndent, options.DocumentationPrefix);

        ImmutableArray<XmlDocToken> inputTokens = XmlDocTokenizer.Tokenize(inputContent, options.InlineTags, options.PreserveXmlTagAttributes, options.PreserveCrefText);
        ImmutableArray<XmlDocToken> outputTokens = XmlDocTokenizer.Tokenize(outputContent, options.InlineTags, options.PreserveXmlTagAttributes, options.PreserveCrefText);

        var builder = ImmutableArray.CreateBuilder<XmlDocFormattingChange>();
        var seenTags = new HashSet<string>(StringComparer.Ordinal);
        var crossCuttingEmitted = false;

        WalkTopLevel(inputTokens, outputTokens, builder, seenTags, ref crossCuttingEmitted);

        // The tokenizer normalises whitespace inside inline-tag attribute sections, so token-level comparison
        // misses cases like `<see cref="X"\n     langword="null" />` → `<see cref="X" langword="null" />`. Do
        // a separate raw-string scan to catch those — the source substrings carry the original whitespace.
        ScanRawInlineTagDifferences(inputContent, outputContent, options.InlineTags, builder, seenTags);

        if (builder.Count == 0)
        {
            // Texts differ but the structured walk produced no attribution — emit a single cross-cutting
            // change so the analyzer still reports the difference (defensive fallback).
            builder.Add(CreateCrossCutting());
        }

        return builder.ToImmutable();
    }

    private static void ScanRawInlineTagDifferences(string inputContent, string outputContent, ImmutableHashSet<string> inlineTags, ImmutableArray<XmlDocFormattingChange>.Builder builder, HashSet<string> seenTags)
    {
        List<RawInlineOccurrence> inputInlines = FindInlineTagsInRawContent(inputContent, inlineTags);
        List<RawInlineOccurrence> outputInlines = FindInlineTagsInRawContent(outputContent, inlineTags);

        if (inputInlines.Count != outputInlines.Count) return;

        for (var i = 0; i < inputInlines.Count; i++)
        {
            RawInlineOccurrence input = inputInlines[i];
            RawInlineOccurrence output = outputInlines[i];

            if (!string.Equals(input.TagName, output.TagName, StringComparison.Ordinal)) continue;
            if (string.Equals(input.RawText, output.RawText, StringComparison.Ordinal)) continue;

            if (seenTags.Add(input.TagName))
            {
                builder.Add(new XmlDocFormattingChange(input.TagName, XmlDocFormatRangeKind.SelfClosingTagSpacing, $"<{input.TagName}> inline formatting updated to project policy."));
            }
        }
    }

    private static List<RawInlineOccurrence> FindInlineTagsInRawContent(string content, ImmutableHashSet<string> inlineTags)
    {
        var result = new List<RawInlineOccurrence>();
        var pos = 0;
        while (pos < content.Length)
        {
            var open = content.IndexOf('<', pos);
            if (open < 0) break;

            var nameStart = open + 1;
            if (nameStart < content.Length && content[nameStart] == '/')
            {
                pos = open + 1;
                continue;
            }

            var nameEnd = nameStart;
            while (nameEnd < content.Length && IsNameChar(content[nameEnd]))
            {
                nameEnd++;
            }

            if (nameEnd == nameStart)
            {
                pos = open + 1;
                continue;
            }

            var tagName = content.Substring(nameStart, nameEnd - nameStart);
            if (!inlineTags.Contains(tagName))
            {
                pos = open + 1;
                continue;
            }

            var openTagEnd = FindTagEnd(content, nameEnd);
            if (openTagEnd < 0)
            {
                pos = open + 1;
                continue;
            }

            var selfClosing = openTagEnd > 0 && content[openTagEnd - 1] == '/';
            if (selfClosing)
            {
                result.Add(new RawInlineOccurrence(tagName, content.Substring(open, openTagEnd + 1 - open)));
                pos = openTagEnd + 1;
                continue;
            }

            var closeSeq = "</" + tagName;
            var closeIdx = content.IndexOf(closeSeq, openTagEnd, StringComparison.Ordinal);
            if (closeIdx < 0)
            {
                pos = openTagEnd + 1;
                continue;
            }

            var closeEnd = content.IndexOf('>', closeIdx + closeSeq.Length);
            if (closeEnd < 0)
            {
                pos = openTagEnd + 1;
                continue;
            }

            result.Add(new RawInlineOccurrence(tagName, content.Substring(open, closeEnd + 1 - open)));
            pos = closeEnd + 1;
        }

        return result;
    }

    private static int FindTagEnd(string content, int searchStart)
    {
        var inQuote = false;
        var quoteChar = '\0';
        for (var i = searchStart; i < content.Length; i++)
        {
            var ch = content[i];
            if (inQuote)
            {
                if (ch == quoteChar) inQuote = false;
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                inQuote = true;
                quoteChar = ch;
                continue;
            }

            if (ch == '>') return i;
        }

        return -1;
    }

    private static bool IsNameChar(char ch) =>
        (ch >= 'A' && ch <= 'Z') ||
        (ch >= 'a' && ch <= 'z') ||
        (ch >= '0' && ch <= '9') ||
        ch == '-' || ch == '_' || ch == '.' || ch == ':';

    private readonly struct RawInlineOccurrence
    {
        public RawInlineOccurrence(string tagName, string rawText)
        {
            this.TagName = tagName;
            this.RawText = rawText;
        }

        public string TagName { get; }

        public string RawText { get; }
    }

    private static void WalkTopLevel(ImmutableArray<XmlDocToken> inputTokens, ImmutableArray<XmlDocToken> outputTokens, ImmutableArray<XmlDocFormattingChange>.Builder builder, HashSet<string> seenTags, ref bool crossCuttingEmitted)
    {
        List<TagScope> inputScopes = FindTopLevelScopes(inputTokens, 0, inputTokens.Length);
        List<TagScope> outputScopes = FindTopLevelScopes(outputTokens, 0, outputTokens.Length);

        if (inputScopes.Count != outputScopes.Count)
        {
            EmitCrossCutting(builder, ref crossCuttingEmitted);
            return;
        }

        var inputCursor = 0;
        var outputCursor = 0;

        for (var s = 0; s < inputScopes.Count; s++)
        {
            TagScope inScope = inputScopes[s];
            TagScope outScope = outputScopes[s];

            if (!string.Equals(inScope.TagName, outScope.TagName, StringComparison.Ordinal))
            {
                EmitCrossCutting(builder, ref crossCuttingEmitted);
                return;
            }

            // Compare top-level prose before this scope. Differences here are cross-cutting (text/whitespace
            // between top-level tags is not owned by any tag).
            ComparePreScopeProse(inputTokens, inputCursor, inScope.OpenIndex - 1, outputTokens, outputCursor, outScope.OpenIndex - 1, builder, ref crossCuttingEmitted);

            // Only attribute the change to this scope when the scope itself was restructured (different
            // tokens, different non-inline raw text, different lengths). Differences that live entirely
            // inside an InlineXml token are attributed to the inline tag by the raw-scan pass and should
            // not bubble up to the enclosing scope.
            if (ScopeStructureDiffers(inputTokens, inScope, outputTokens, outScope))
            {
                if (seenTags.Add(inScope.TagName))
                {
                    builder.Add(new XmlDocFormattingChange(inScope.TagName, XmlDocFormatRangeKind.BlockLayout, $"<{inScope.TagName}> layout updated to project policy."));
                }
            }

            inputCursor = inScope.CloseIndex + 1;
            outputCursor = outScope.CloseIndex + 1;
        }

        // Trailing top-level prose after the last scope.
        ComparePreScopeProse(inputTokens, inputCursor, inputTokens.Length - 1, outputTokens, outputCursor, outputTokens.Length - 1, builder, ref crossCuttingEmitted);
    }

    private static void ComparePreScopeProse(ImmutableArray<XmlDocToken> inputTokens, int inputStart, int inputEnd, ImmutableArray<XmlDocToken> outputTokens, int outputStart, int outputEnd, ImmutableArray<XmlDocFormattingChange>.Builder builder, ref bool crossCuttingEmitted)
    {
        var inputProse = ConcatRawText(inputTokens, inputStart, inputEnd);
        var outputProse = ConcatRawText(outputTokens, outputStart, outputEnd);

        if (!string.Equals(inputProse, outputProse, StringComparison.Ordinal))
        {
            EmitCrossCutting(builder, ref crossCuttingEmitted);
        }
    }

    private static bool ScopeStructureDiffers(ImmutableArray<XmlDocToken> inputTokens, TagScope inScope, ImmutableArray<XmlDocToken> outputTokens, TagScope outScope)
    {
        var inLen = inScope.CloseIndex - inScope.OpenIndex + 1;
        var outLen = outScope.CloseIndex - outScope.OpenIndex + 1;
        if (inLen != outLen) return true;

        for (var k = 0; k < inLen; k++)
        {
            XmlDocToken inT = inputTokens[inScope.OpenIndex + k];
            XmlDocToken outT = outputTokens[outScope.OpenIndex + k];

            if (inT.Kind != outT.Kind) return true;
            if (!string.Equals(inT.TagName, outT.TagName, StringComparison.Ordinal)) return true;

            // For InlineXml, only the kind + tag name matter for the scope-level decision; RawText
            // differences are attributed to the inline tag by the raw-scan pass instead.
            if (inT.Kind == XmlDocTokenKind.InlineXml) continue;

            if (!string.Equals(inT.RawText, outT.RawText, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static void EmitCrossCutting(ImmutableArray<XmlDocFormattingChange>.Builder builder, ref bool crossCuttingEmitted)
    {
        if (crossCuttingEmitted) return;
        builder.Add(CreateCrossCutting());
        crossCuttingEmitted = true;
    }

    private static XmlDocFormattingChange CreateCrossCutting() =>
        new XmlDocFormattingChange(
            tagName: null,
            kind: XmlDocFormatRangeKind.WhitespaceNormalisation,
            description: "Documentation comment prose, prefix, or indent updated to project policy.");

    private static List<TagScope> FindTopLevelScopes(ImmutableArray<XmlDocToken> tokens, int start, int end)
    {
        var scopes = new List<TagScope>();
        var depth = 0;
        var openIndex = -1;
        string? openTagName = null;

        for (var i = start; i < end; i++)
        {
            XmlDocToken token = tokens[i];

            if (token.Kind == XmlDocTokenKind.BlockStart && token.TagName is not null)
            {
                if (depth == 0)
                {
                    openIndex = i;
                    openTagName = token.TagName;
                }

                depth++;
            }
            else if (token.Kind == XmlDocTokenKind.BlockEnd && token.TagName is not null)
            {
                depth--;
                if (depth == 0 && openIndex >= 0 && openTagName is not null)
                {
                    scopes.Add(new TagScope(openTagName, openIndex, i));
                    openIndex = -1;
                    openTagName = null;
                }
            }
        }

        return scopes;
    }

    private static string ConcatRawText(ImmutableArray<XmlDocToken> tokens, int startInclusive, int endInclusive)
    {
        if (startInclusive > endInclusive) return string.Empty;

        var sb = new StringBuilder();
        for (var i = startInclusive; i <= endInclusive && i < tokens.Length; i++)
        {
            sb.Append(tokens[i].RawText);
        }

        return sb.ToString();
    }

    private readonly struct TagScope
    {
        public TagScope(string tagName, int openIndex, int closeIndex)
        {
            this.TagName = tagName;
            this.OpenIndex = openIndex;
            this.CloseIndex = closeIndex;
        }

        public string TagName { get; }

        public int OpenIndex { get; }

        public int CloseIndex { get; }
    }
}
