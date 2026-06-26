// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocLayout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Bodu.CodeStyle.XmlDocumentation.Tokens;

namespace Bodu.CodeStyle.XmlDocumentation.Layout;

/// <summary>
/// Converts a token stream into a flat list of content lines that <see cref="DocIndent.Reapply" /> can re-prefix with
/// <c>"/// "</c>.
/// </summary>
/// <remarks>
/// <para>
/// The composer applies tag-level layout policy: force-multiline tags emit their open and close tokens on their own
/// lines, single-line-when-short tags collapse to a single line when the joined length fits, and inline atomic tokens (<c>&lt;see /&gt;</c>,
/// <c>&lt;c&gt;…&lt;/c&gt;</c>, etc.) remain intact.
/// </para>
/// </remarks>
internal static class DocLayout
{
    /// <summary>
    /// Composes the given token stream into a flat list of physical content lines.
    /// </summary>
    /// <param name="tokens">The token stream produced by <see cref="XmlDocTokenizer" />.</param>
    /// <param name="options">The active formatting policy.</param>
    /// <param name="contentBudget">
    /// The maximum content length per line, excluding the documentation prefix and base indent.
    /// </param>
    /// <returns>The ordered list of content lines, ready for the documentation prefix to be applied.</returns>
    public static IReadOnlyList<string> Compose(IReadOnlyList<XmlDocToken> tokens, XmlDocFormatOptions options, int contentBudget)
    {
        if (tokens is null) throw new ArgumentNullException(nameof(tokens));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (contentBudget <= 0) throw new ArgumentOutOfRangeException(nameof(contentBudget), XmlDocResourceStrings.Arg_OutOfRange_ContentBudgetNotPositive);

        var output = new List<string>();
        ComposeRange(tokens, 0, tokens.Count, options, contentBudget, string.Empty, output);
        return output;
    }

    /// <summary>
    /// Composes the tokens in the half-open range into content lines, recursing into force-multiline and
    /// single-line-candidate tag bodies at the appropriate indent.
    /// </summary>
    /// <param name="tokens">The token stream being composed.</param>
    /// <param name="start">The inclusive start index of the range to compose.</param>
    /// <param name="end">The exclusive end index of the range to compose.</param>
    /// <param name="options">The active formatting policy.</param>
    /// <param name="contentBudget">
    /// The maximum content length per line, excluding the documentation prefix and base indent.
    /// </param>
    /// <param name="currentIndent">The nesting indent to prepend to each emitted line.</param>
    /// <param name="output">The list receiving the composed content lines.</param>
    /// <returns>The index immediately after the last token consumed.</returns>
    private static int ComposeRange(IReadOnlyList<XmlDocToken> tokens, int start, int end, XmlDocFormatOptions options, int contentBudget, string currentIndent, List<string> output)
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
                    FlushRun(currentRun, options, contentBudget, currentIndent, output);
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
                        FlushRun(currentRun, options, contentBudget, currentIndent, output);
                        output.Add(currentIndent + token.RawText);
                        ComposeRange(tokens, position + 1, matchEnd, options, contentBudget, currentIndent + options.IndentText, output);
                        output.Add(currentIndent + tokens[matchEnd].RawText);
                        position = matchEnd + 1;
                        continue;
                    }

                    if (singleLineCandidate)
                    {
                        FlushRun(currentRun, options, contentBudget, currentIndent, output);
                        ComposeSingleLineCandidate(tokens, position, matchEnd, options, contentBudget, currentIndent, output);
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
                    FlushRun(currentRun, options, contentBudget, currentIndent, output);
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

        FlushRun(currentRun, options, contentBudget, currentIndent, output);
        return position;
    }

    /// <summary>
    /// Emits each physical line of a multi-line CDATA section as a separate content line.
    /// </summary>
    /// <param name="cdataRawText">
    /// The raw CDATA section text, including delimiters, with logical lines separated by <c>'\n'</c>.
    /// </param>
    /// <param name="output">The list receiving the emitted content lines.</param>
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

        // Emit the trailing segment only when there is content after the final newline. A well-formed CDATA
        // literal ends with "]]>", so this is normally taken; the guard ensures a literal that happened to end
        // with a newline would not append a spurious empty content line.
        if (start < cdataRawText.Length)
        {
            output.Add(cdataRawText.Substring(start));
        }
    }

    /// <summary>
    /// Locates the matching block-end token for an opening block token, honouring nesting of identically named tags.
    /// </summary>
    /// <param name="tokens">The token stream being scanned.</param>
    /// <param name="openIndex">The index of the opening block-start token.</param>
    /// <param name="end">The exclusive upper bound of the search range.</param>
    /// <param name="tagName">The element name whose matching close is sought.</param>
    /// <param name="closeIndex">
    /// When this method returns, the index of the matching block-end token; otherwise <c>-1</c>.
    /// </param>
    /// <returns><see langword="true" /> if a matching close was found; otherwise <see langword="false" />.</returns>
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

    /// <summary>
    /// Attempts to render a single-line-when-short tag as one collapsed line, falling back to the expanded multiline
    /// form when the joined content overflows or contains a multi-line atom.
    /// </summary>
    /// <param name="tokens">The token stream being composed.</param>
    /// <param name="openIndex">The index of the opening block-start token.</param>
    /// <param name="closeIndex">The index of the matching block-end token.</param>
    /// <param name="options">The active formatting policy.</param>
    /// <param name="contentBudget">
    /// The maximum content length per line, excluding the documentation prefix and base indent.
    /// </param>
    /// <param name="currentIndent">The nesting indent to prepend to each emitted line.</param>
    /// <param name="output">The list receiving the composed content lines.</param>
    private static void ComposeSingleLineCandidate(IReadOnlyList<XmlDocToken> tokens, int openIndex, int closeIndex, XmlDocFormatOptions options, int contentBudget, string currentIndent, List<string> output)
    {
        XmlDocToken openToken = tokens[openIndex];
        XmlDocToken closeToken = tokens[closeIndex];

        // A single-line candidate cannot represent any content token that spans multiple lines — a multi-line
        // CDATA section, or a tag preserved verbatim under PreserveXmlTagAttributes. When the body carries one,
        // skip the candidate stage and fall through to the expanded form so the multi-line content is emitted
        // across its own lines.
        for (var k = openIndex + 1; k < closeIndex; k++)
        {
            // Structural line breaks do not count — only a content token that itself spans lines (a multi-line
            // CDATA section or a verbatim-preserved tag) forces the expanded form.
            if (tokens[k].Kind != XmlDocTokenKind.LineBreak &&
                tokens[k].Kind != XmlDocTokenKind.Whitespace &&
                tokens[k].RawText.IndexOf('\n') >= 0)
            {
                EmitExpandedSingleLineCandidate(tokens, openIndex, closeIndex, options, contentBudget, currentIndent, output);
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
        var effectiveBudget = Math.Max(1, contentBudget - currentIndent.Length);
        var singleLineLimit = policy.MaxSingleLineLength ?? options.MaxLineLength;
        var singleLine = candidate.ToString();
        if (singleLine.Length <= singleLineLimit && singleLine.Length <= effectiveBudget)
        {
            output.Add(currentIndent + singleLine);
            return;
        }

        // The candidate overflows the budget. Normally it expands to a multiline block, but when the tag's
        // content must never wrap (NeverSplitTagContent or a policy AllowLineBreakInside == false) it is kept
        // intact on a single line, accepting the overflow rather than splitting it.
        if (!options.AllowsContentWrapping(openToken.TagName!))
        {
            output.Add(currentIndent + singleLine);
            return;
        }

        EmitExpandedSingleLineCandidate(tokens, openIndex, closeIndex, options, contentBudget, currentIndent, output);
    }

    /// <summary>
    /// Emits a tag in expanded form: the opening token on its own line, the body composed at the next indent level, and
    /// the closing token on its own line.
    /// </summary>
    /// <param name="tokens">The token stream being composed.</param>
    /// <param name="openIndex">The index of the opening block-start token.</param>
    /// <param name="closeIndex">The index of the matching block-end token.</param>
    /// <param name="options">The active formatting policy.</param>
    /// <param name="contentBudget">
    /// The maximum content length per line, excluding the documentation prefix and base indent.
    /// </param>
    /// <param name="currentIndent">The nesting indent to prepend to each emitted line.</param>
    /// <param name="output">The list receiving the composed content lines.</param>
    private static void EmitExpandedSingleLineCandidate(IReadOnlyList<XmlDocToken> tokens, int openIndex, int closeIndex, XmlDocFormatOptions options, int contentBudget, string currentIndent, List<string> output)
    {
        // Expanded form: open on its own line, content on subsequent lines, close on its own line.
        XmlDocToken openToken = tokens[openIndex];
        XmlDocToken closeToken = tokens[closeIndex];

        output.Add(currentIndent + openToken.RawText);

        var contentTokens = new List<XmlDocToken>();
        for (var i = openIndex + 1; i < closeIndex; i++)
        {
            contentTokens.Add(tokens[i]);
        }

        var innerLines = new List<string>();
        ComposeRange(contentTokens, 0, contentTokens.Count, options, contentBudget, currentIndent + options.IndentText, innerLines);
        foreach (var line in innerLines)
        {
            output.Add(line);
        }

        output.Add(currentIndent + closeToken.RawText);
    }

    /// <summary>
    /// Joins the pending run of prose tokens into atoms, wraps them to the effective budget, and emits the resulting
    /// lines. The run is cleared on return.
    /// </summary>
    /// <param name="run">The accumulated run of prose tokens to flush.</param>
    /// <param name="options">The active formatting policy.</param>
    /// <param name="contentBudget">
    /// The maximum content length per line, excluding the documentation prefix and base indent.
    /// </param>
    /// <param name="currentIndent">The nesting indent to prepend to each emitted line.</param>
    /// <param name="output">The list receiving the wrapped content lines.</param>
    private static void FlushRun(List<XmlDocToken> run, XmlDocFormatOptions options, int contentBudget, string currentIndent, List<string> output)
    {
        if (run.Count == 0)
        {
            return;
        }

        var atoms = new List<string>();
        string? pendingWhitespace = null;
        var gapHasLineBreak = false;
        foreach (XmlDocToken token in run)
        {
            switch (token.Kind)
            {
                case XmlDocTokenKind.Whitespace:
                    if (atoms.Count > 0)
                    {
                        // Collapse to a single space, or carry the exact run when collapsing is disabled.
                        pendingWhitespace = options.CollapseProseWhitespace ? " " : token.RawText;
                    }

                    break;

                case XmlDocTokenKind.Text:
                case XmlDocTokenKind.InlineXml:
                case XmlDocTokenKind.BlockStart:
                case XmlDocTokenKind.BlockEnd:
                case XmlDocTokenKind.CData:
                    if (atoms.Count > 0 && (pendingWhitespace != null || gapHasLineBreak))
                    {
                        // A soft line break in the gap always joins as a single space; otherwise reproduce the
                        // pending horizontal whitespace (a single space when collapsing, the exact run when not).
                        atoms.Add(gapHasLineBreak ? " " : pendingWhitespace ?? " ");
                    }

                    atoms.Add(NormalizeTagRaw(token, options));
                    pendingWhitespace = null;
                    gapHasLineBreak = false;
                    break;

                case XmlDocTokenKind.LineBreak:
                    // Soft line break inside the run; treat as a whitespace boundary that joins as one space.
                    if (atoms.Count > 0)
                    {
                        gapHasLineBreak = true;
                    }

                    break;
            }
        }

        run.Clear();

        if (atoms.Count == 0)
        {
            return;
        }

        // Reduce the available width by the nesting indent, then re-prepend that indent to each wrapped line so
        // the total line still fits the configured budget. With the default empty IndentText the indent is empty
        // and this is a no-op, leaving the canonical flush layout byte-identical.
        var effectiveBudget = Math.Max(1, contentBudget - currentIndent.Length);
        IEnumerable<string> wrapped = DocWrapper.Wrap(atoms, effectiveBudget);
        foreach (var line in wrapped)
        {
            output.Add(currentIndent + line);
        }
    }

    /// <summary>
    /// Returns the token's raw text, adjusting the trailing-space convention of a self-closing inline tag to match its
    /// tag policy.
    /// </summary>
    /// <param name="token">The token whose raw text is normalized.</param>
    /// <param name="options">The active formatting policy supplying the per-tag self-closing convention.</param>
    /// <returns>The normalized raw text for the token.</returns>
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

    /// <summary>
    /// Rewrites a self-closing tag's closing delimiter to either <c>" /&gt;"</c> or <c>"/&gt;"</c> according to the
    /// requested convention.
    /// </summary>
    /// <param name="rawText">The raw self-closing tag text.</param>
    /// <param name="wantSpace">
    /// When <see langword="true" />, a single space precedes the <c>/&gt;</c>; otherwise no space is emitted.
    /// </param>
    /// <returns>The tag text with its trailing-space convention applied.</returns>
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
