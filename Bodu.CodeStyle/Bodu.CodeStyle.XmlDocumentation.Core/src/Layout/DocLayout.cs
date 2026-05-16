// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocLayout.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        if (contentBudget <= 0) throw new ArgumentOutOfRangeException(nameof(contentBudget), "Content budget must be positive.");

        List<string> output = new List<string>();
        ComposeRange(tokens, 0, tokens.Count, options, contentBudget, output);
        return output;
    }

    private static int ComposeRange(IReadOnlyList<XmlDocToken> tokens, int start, int end, XmlDocFormatOptions options, int contentBudget, List<string> output)
    {
        List<XmlDocToken> currentRun = new List<XmlDocToken>();
        int position = start;

        while (position < end)
        {
            XmlDocToken token = tokens[position];

            if (token.Kind == XmlDocTokenKind.BlockStart)
            {
                if (TryFindMatchingEnd(tokens, position, end, token.TagName!, out int matchEnd))
                {
                    bool forceMultiline = options.ForceMultilineTags.Contains(token.TagName!) || options.GetTagPolicy(token.TagName!).Layout == XmlDocTagLayout.MultilineBlock;
                    bool singleLineCandidate = options.SingleLineWhenShortTags.Contains(token.TagName!) || options.GetTagPolicy(token.TagName!).Layout == XmlDocTagLayout.SingleLineWhenShort;

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

            if (token.Kind == XmlDocTokenKind.LineBreak)
            {
                if (currentRun.Count > 0)
                {
                    FlushRun(currentRun, options, contentBudget, output);
                }

                position++;
                continue;
            }

            currentRun.Add(token);
            position++;
        }

        FlushRun(currentRun, options, contentBudget, output);
        return position;
    }

    private static bool TryFindMatchingEnd(IReadOnlyList<XmlDocToken> tokens, int openIndex, int end, string tagName, out int closeIndex)
    {
        int depth = 1;
        for (int i = openIndex + 1; i < end; i++)
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

        StringBuilder candidate = new StringBuilder();
        candidate.Append(openToken.RawText);

        bool pendingWhitespace = false;
        bool hadContent = false;
        for (int i = openIndex + 1; i < closeIndex; i++)
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
        int singleLineLimit = policy.MaxSingleLineLength ?? options.MaxLineLength;
        string singleLine = candidate.ToString();
        if (singleLine.Length <= singleLineLimit && singleLine.Length <= contentBudget)
        {
            output.Add(singleLine);
            return;
        }

        // Expanded form: open on its own line, content on subsequent lines, close on its own line.
        output.Add(openToken.RawText);

        List<XmlDocToken> contentTokens = new List<XmlDocToken>();
        for (int i = openIndex + 1; i < closeIndex; i++)
        {
            contentTokens.Add(tokens[i]);
        }

        List<string> innerLines = new List<string>();
        ComposeRange(contentTokens, 0, contentTokens.Count, options, contentBudget, innerLines);
        foreach (string line in innerLines)
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

        List<string> atoms = new List<string>();
        bool pendingWhitespace = false;
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
        foreach (string line in wrapped)
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
        bool? wantTrailingSpace = policy.SelfClosingTrailingSpace;
        if (wantTrailingSpace is null)
        {
            return token.RawText;
        }

        return NormalizeSelfClosingTrailingSpace(token.RawText, wantTrailingSpace.Value);
    }

    private static string NormalizeSelfClosingTrailingSpace(string rawText, bool wantSpace)
    {
        int length = rawText.Length;
        if (length < 3 || rawText[length - 1] != '>' || rawText[length - 2] != '/')
        {
            return rawText;
        }

        int trimmedEnd = length - 2;
        while (trimmedEnd > 0 && (rawText[trimmedEnd - 1] == ' ' || rawText[trimmedEnd - 1] == '\t'))
        {
            trimmedEnd--;
        }

        string head = rawText.Substring(0, trimmedEnd);
        return wantSpace ? head + " />" : head + "/>";
    }
}
