// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocLayout.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Bodu.CodeStyle.XmlDocumentation.Tokens;

namespace Bodu.CodeStyle.XmlDocumentation.Layout;

/// <summary>
/// Converts a token stream into a flat list of content lines that <see cref="DocIndent.Reapply" /> can
/// re-prefix with <c>"/// "</c>.
/// </summary>
/// <remarks>
/// <para>
/// The composer applies tag-level layout policy: force-multiline tags emit their open and close tokens on their
/// own lines, single-line-when-short tags collapse to a single line when the joined length fits, and inline
/// atomic tokens (<c>&lt;see /&gt;</c>, <c>&lt;c&gt;…&lt;/c&gt;</c>, etc.) remain intact.
/// </para>
/// </remarks>
internal static class DocLayout
{
    /// <summary>
    /// Composes the given token stream into a flat list of physical content lines.
    /// </summary>
    /// <param name="tokens">The token stream produced by <see cref="XmlDocTokenizer" />.</param>
    /// <param name="options">The active formatting policy.</param>
    /// <param name="contentBudget">The maximum content length per line, excluding the documentation prefix and base indent.</param>
    /// <returns>The ordered list of content lines, ready for the documentation prefix to be applied.</returns>
    public static IReadOnlyList<string> Compose(IReadOnlyList<XmlDocToken> tokens, XmlDocFormatOptions options, int contentBudget)
    {
        if (tokens is null) throw new ArgumentNullException(nameof(tokens));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (contentBudget <= 0) throw new ArgumentOutOfRangeException(nameof(contentBudget), XmlDocResourceStrings.Arg_OutOfRange_ContentBudgetNotPositive);

        var output = new List<string>();
        ComposeRange(tokens, 0, tokens.Count, options, contentBudget, output);
        return output;
    }

    private static int ComposeRange(IReadOnlyList<XmlDocToken> tokens, int start, int end, XmlDocFormatOptions options, int contentBudget, List<string> output)
    {
        var currentRun = new List<XmlDocToken>();
        var position = start;

        while (position < end)
        {
            XmlDocToken token = tokens[position];

            // CDATA sections are preserved verbatim. Multi-line CDATA (the open `<![CDATA[`, body, and
            // closing `]]>` on separate lines) is emitted as a sequence of physical content lines, bypassing
            // the wrapper so each body line gets its own `///` prefix and the delimiters keep the no-space
            // convention. Single-line CDATA stays inline with the surrounding prose and flows through the
            // normal run / wrap path as an indivisible atom.
            if (token.Kind == XmlDocTokenKind.CData)
            {
                if (token.RawText.IndexOf('\n') >= 0)
                {
                    FlushRun(currentRun, options, contentBudget, output);
                    EmitCDataLines(token.RawText, output);
                    position++;
                    continue;
                }

                currentRun.Add(token);
                position++;
                continue;
            }

            if (token.Kind == XmlDocTokenKind.BlockStart)
            {
                if (TryFindMatchingEnd(tokens, position, end, token.TagName!, out var matchEnd))
                {
                    // XmlDocFormatOptions.ResolveLayout is the single authoritative layout source: an explicit
                    // per-tag policy layout wins, otherwise the convenience sets (ForceMultilineTags,
                    // SingleLineWhenShortTags, InlineTags, BlockTags) are consulted in precedence order. This
                    // makes tagPolicies.layout authoritative and gives BlockTags a real effect while leaving the
                    // default Bodu profile byte-identical.
                    XmlDocTagLayout layout = options.ResolveLayout(token.TagName!);
                    var forceMultiline = layout == XmlDocTagLayout.MultilineBlock;
                    var singleLineCandidate = layout == XmlDocTagLayout.SingleLineWhenShort;

                    if (forceMultiline)
                    {
                        FlushRun(currentRun, options, contentBudget, output);
                        output.Add(token.RawText);
                        ComposeRange(tokens, position + 1, matchEnd, options, contentBudget, output);
                        output.Add(tokens[matchEnd].RawText);
                        position = matchEnd + 1;
                        continue;
                    }

                    if (singleLineCandidate)
                    {
                        FlushRun(currentRun, options, contentBudget, output);
                        ComposeSingleLineCandidate(tokens, position, matchEnd, options, contentBudget, output);
                        position = matchEnd + 1;
                        continue;
                    }
                }

                currentRun.Add(token);
                position++;
                continue;
            }

            // When blank-line preservation is enabled, a run of two or more line breaks (optionally separated by
            // whitespace) is an authored blank line. Flush the pending content, emit one empty content line per
            // blank line, and skip the break run so the blank line survives instead of collapsing into a space.
            if (token.Kind == XmlDocTokenKind.LineBreak && options.PreserveBlankLines)
            {
                var lookahead = position + 1;
                var breaks = 1;
                while (lookahead < end)
                {
                    XmlDocTokenKind kind = tokens[lookahead].Kind;
                    if (kind == XmlDocTokenKind.Whitespace)
                    {
                        lookahead++;
                        continue;
                    }

                    if (kind == XmlDocTokenKind.LineBreak)
                    {
                        breaks++;
                        lookahead++;
                        continue;
                    }

                    break;
                }

                if (breaks >= 2)
                {
                    FlushRun(currentRun, options, contentBudget, output);
                    for (var b = 0; b < breaks - 1; b++)
                    {
                        if (output.Count > 0)
                        {
                            output.Add(string.Empty);
                        }
                    }

                    position = lookahead;
                    continue;
                }
            }

            currentRun.Add(token);
            position++;
        }

        FlushRun(currentRun, options, contentBudget, output);
        return position;
    }

    private static void EmitCDataLines(string cdataRawText, List<string> output)
    {
        // Split on '\n' (the tokenizer's logical line marker after DocIndent.Strip). Each piece becomes one
        // entry in `output`, ready for DocIndent.Reapply to add the `///` prefix. The opening `<![CDATA[`
        // and closing `]]>` lines are detected by Reapply and emitted with the no-space prefix per Bodu
        // convention; body lines use the normal `/// ` prefix so any interior indentation is preserved.
        var start = 0;
        for (var i = 0; i < cdataRawText.Length; i++)
        {
            if (cdataRawText[i] == '\n')
            {
                output.Add(cdataRawText.Substring(start, i - start));
                start = i + 1;
            }
        }

        if (start <= cdataRawText.Length)
        {
            output.Add(cdataRawText.Substring(start));
        }
    }

    private static bool TryFindMatchingEnd(IReadOnlyList<XmlDocToken> tokens, int openIndex, int end, string tagName, out int closeIndex)
    {
        var depth = 1;
        for (var i = openIndex + 1; i < end; i++)
        {
            XmlDocToken t = tokens[i];
            if (t.Kind == XmlDocTokenKind.BlockStart && string.Equals(t.TagName, tagName, StringComparison.Ordinal))
            {
                depth++;
            }
            else if (t.Kind == XmlDocTokenKind.BlockEnd && string.Equals(t.TagName, tagName, StringComparison.Ordinal))
            {
                depth--;
                if (depth == 0)
                {
                    closeIndex = i;
                    return true;
                }
            }
        }

        closeIndex = -1;
        return false;
    }

    private static void ComposeSingleLineCandidate(IReadOnlyList<XmlDocToken> tokens, int openIndex, int closeIndex, XmlDocFormatOptions options, int contentBudget, List<string> output)
    {
        XmlDocToken openToken = tokens[openIndex];
        XmlDocToken closeToken = tokens[closeIndex];

        // Multi-line CDATA sections cannot be represented on a single line. When the body carries one, skip
        // the candidate stage entirely and fall through to the expanded form, which dispatches to
        // ComposeRange's CDATA handler. Single-line CDATA flows through the candidate as an indivisible atom.
        for (var k = openIndex + 1; k < closeIndex; k++)
        {
            if (tokens[k].Kind == XmlDocTokenKind.CData && tokens[k].RawText.IndexOf('\n') >= 0)
            {
                EmitExpandedSingleLineCandidate(tokens, openIndex, closeIndex, options, contentBudget, output);
                return;
            }
        }

        var candidate = new StringBuilder();
        candidate.Append(openToken.RawText);

        var pendingWhitespace = false;
        var hadContent = false;
        for (var i = openIndex + 1; i < closeIndex; i++)
        {
            XmlDocToken t = tokens[i];
            switch (t.Kind)
            {
                case XmlDocTokenKind.LineBreak:
                case XmlDocTokenKind.Whitespace:
                    if (hadContent)
                    {
                        pendingWhitespace = true;
                    }

                    break;

                case XmlDocTokenKind.Text:
                case XmlDocTokenKind.InlineXml:
                case XmlDocTokenKind.BlockStart:
                case XmlDocTokenKind.BlockEnd:
                case XmlDocTokenKind.CData:
                    if (pendingWhitespace)
                    {
                        candidate.Append(' ');
                        pendingWhitespace = false;
                    }

                    candidate.Append(NormalizeTagRaw(t, options));
                    hadContent = true;
                    break;
            }
        }

        candidate.Append(closeToken.RawText);

        XmlDocTagPolicy policy = options.GetTagPolicy(openToken.TagName!);
        var singleLineLimit = policy.MaxSingleLineLength ?? options.MaxLineLength;
        var singleLine = candidate.ToString();
        if (singleLine.Length <= singleLineLimit && singleLine.Length <= contentBudget)
        {
            output.Add(singleLine);
            return;
        }

        // The candidate overflows the budget. Normally it expands to a multiline block, but when the tag's
        // content must never wrap (NeverSplitTagContent or a policy AllowLineBreakInside == false) it is kept
        // intact on a single line, accepting the overflow rather than splitting it.
        if (!options.AllowsContentWrapping(openToken.TagName!))
        {
            output.Add(singleLine);
            return;
        }

        EmitExpandedSingleLineCandidate(tokens, openIndex, closeIndex, options, contentBudget, output);
    }

    private static void EmitExpandedSingleLineCandidate(IReadOnlyList<XmlDocToken> tokens, int openIndex, int closeIndex, XmlDocFormatOptions options, int contentBudget, List<string> output)
    {
        // Expanded form: open on its own line, content on subsequent lines, close on its own line.
        XmlDocToken openToken = tokens[openIndex];
        XmlDocToken closeToken = tokens[closeIndex];

        output.Add(openToken.RawText);

        var contentTokens = new List<XmlDocToken>();
        for (var i = openIndex + 1; i < closeIndex; i++)
        {
            contentTokens.Add(tokens[i]);
        }

        var innerLines = new List<string>();
        ComposeRange(contentTokens, 0, contentTokens.Count, options, contentBudget, innerLines);
        foreach (var line in innerLines)
        {
            output.Add(line);
        }

        output.Add(closeToken.RawText);
    }

    private static void FlushRun(List<XmlDocToken> run, XmlDocFormatOptions options, int contentBudget, List<string> output)
    {
        if (run.Count == 0)
        {
            return;
        }

        var atoms = new List<string>();
        var pendingWhitespace = false;
        foreach (XmlDocToken token in run)
        {
            switch (token.Kind)
            {
                case XmlDocTokenKind.Whitespace:
                    if (atoms.Count > 0)
                    {
                        pendingWhitespace = true;
                    }

                    break;

                case XmlDocTokenKind.Text:
                case XmlDocTokenKind.InlineXml:
                case XmlDocTokenKind.BlockStart:
                case XmlDocTokenKind.BlockEnd:
                case XmlDocTokenKind.CData:
                    if (pendingWhitespace)
                    {
                        atoms.Add(" ");
                        pendingWhitespace = false;
                    }

                    atoms.Add(NormalizeTagRaw(token, options));
                    break;

                case XmlDocTokenKind.LineBreak:
                    // Soft line break inside the run; treat as a whitespace boundary.
                    if (atoms.Count > 0)
                    {
                        pendingWhitespace = true;
                    }

                    break;
            }
        }

        run.Clear();

        if (atoms.Count == 0)
        {
            return;
        }

        IEnumerable<string> wrapped = DocWrapper.Wrap(atoms, contentBudget);
        foreach (var line in wrapped)
        {
            output.Add(line);
        }
    }

    private static string NormalizeTagRaw(XmlDocToken token, XmlDocFormatOptions options)
    {
        if (token.Kind != XmlDocTokenKind.InlineXml || !token.IsSelfClosing || token.TagName is null)
        {
            return token.RawText;
        }

        XmlDocTagPolicy policy = options.GetTagPolicy(token.TagName);
        var wantTrailingSpace = policy.SelfClosingTrailingSpace;
        if (wantTrailingSpace is null)
        {
            return token.RawText;
        }

        return NormalizeSelfClosingTrailingSpace(token.RawText, wantTrailingSpace.Value);
    }

    private static string NormalizeSelfClosingTrailingSpace(string rawText, bool wantSpace)
    {
        var length = rawText.Length;
        if (length < 3 || rawText[length - 1] != '>' || rawText[length - 2] != '/')
        {
            return rawText;
        }

        var trimmedEnd = length - 2;
        while (trimmedEnd > 0 && (rawText[trimmedEnd - 1] == ' ' || rawText[trimmedEnd - 1] == '\t'))
        {
            trimmedEnd--;
        }

        var head = rawText.Substring(0, trimmedEnd);
        return wantSpace ? head + " />" : head + "/>";
    }
}
