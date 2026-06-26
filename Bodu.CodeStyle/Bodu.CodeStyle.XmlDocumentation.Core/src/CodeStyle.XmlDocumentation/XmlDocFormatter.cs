// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Bodu.CodeStyle.XmlDocumentation.Layout;
using Bodu.CodeStyle.XmlDocumentation.Tokens;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Provides the entry point for formatting an XML documentation comment to the configured Bodu policy.
/// </summary>
/// <remarks>
/// <para>
/// The formatter takes the raw <c>///</c> trivia text, the per-comment <see cref="XmlDocFormatContext" />, and an
/// <see cref="XmlDocFormatOptions" /> policy and returns a canonical replacement. The operation is idempotent: running
/// the formatter on its own output returns byte-identical text.
/// </para>
/// <para>
/// The formatter is intentionally Roslyn-free so it can be reused by future CLI, VSIX, and unit-test scenarios without
/// taking a dependency on <c>Microsoft.CodeAnalysis</c>.
/// </para>
/// </remarks>
public sealed class XmlDocFormatter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocFormatter" /> class.
    /// </summary>
    public XmlDocFormatter()
    {
    }

    /// <summary>
    /// Formats the given XML documentation comment trivia to the configured Bodu policy.
    /// </summary>
    /// <param name="triviaText">
    /// The raw documentation comment trivia text, including the <c>"/// "</c> prefix on every line.
    /// </param>
    /// <param name="context">The per-comment context describing indentation, line ending, and member-kind hint.</param>
    /// <param name="options">The active formatting policy.</param>
    /// <returns>The formatting result describing whether the input changed and exposing the canonical text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null" />.</exception>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance method by API design; future versions may carry per-instance state.")]
    public XmlDocFormatResult FormatTrivia(string triviaText, XmlDocFormatContext context, XmlDocFormatOptions options)
    {
        if (triviaText is null) throw new ArgumentNullException(nameof(triviaText));
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (options is null) throw new ArgumentNullException(nameof(options));

        XmlDocFormatResult firstPass = FormatCore(triviaText, context, options);

#if DEBUG
        if (firstPass.Changed)
        {
            XmlDocFormatResult secondPass = FormatCore(firstPass.FormattedText, context, options);
            Debug.Assert(
                string.Equals(secondPass.FormattedText, firstPass.FormattedText, StringComparison.Ordinal),
                "XmlDocFormatter output is not idempotent for the given input and policy.");
        }
#endif

        return firstPass;
    }

    /// <summary>
    /// Performs a single formatting pass over the given trivia, producing the canonical text and the set of attributed
    /// formatting changes.
    /// </summary>
    /// <param name="triviaText">The raw documentation comment trivia text.</param>
    /// <param name="context">The per-comment context describing indentation, line ending, and member-kind hint.</param>
    /// <param name="options">The active formatting policy.</param>
    /// <returns>The result describing whether the input changed and exposing the canonical text.</returns>
    private static XmlDocFormatResult FormatCore(string triviaText, XmlDocFormatContext context, XmlDocFormatOptions options)
    {
        options = ApplyMemberKindLayout(options, context.MemberKindHint);

        var content = DocIndent.Strip(triviaText, context.BaseIndent, options.DocumentationPrefix);
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize(content, options.InlineTags, options.PreserveXmlTagAttributes, options.PreserveCrefText);

        if (tokens.Length == 0 || HasNoMeaningfulTokens(tokens))
        {
            return new XmlDocFormatResult(changed: false, triviaText, ImmutableArray<XmlDocFormattingChange>.Empty);
        }

        if (!HasMatchedTags(tokens))
        {
            // Malformed XML: leave unchanged.
            return new XmlDocFormatResult(changed: false, triviaText, ImmutableArray<XmlDocFormattingChange>.Empty);
        }

        var contentBudget = Math.Max(1, options.MaxLineLength - context.BaseIndent.Length - options.DocumentationPrefix.Length);
        IReadOnlyList<string> contentLines = DocLayout.Compose(tokens, options, contentBudget);
        var formatted = DocIndent.Reapply(contentLines, context.BaseIndent, options.DocumentationPrefix, context.LineEnding);

        var changed = !string.Equals(formatted, triviaText, StringComparison.Ordinal);
        ImmutableArray<XmlDocFormattingChange> changes = changed
            ? XmlDocChangeAttributor.Attribute(triviaText, formatted, context, options)
            : ImmutableArray<XmlDocFormattingChange>.Empty;

        return new XmlDocFormatResult(changed, formatted, changes);
    }

    /// <summary>
    /// Adjusts the formatting policy for the documented member kind, keeping a field's <c>&lt;summary&gt;</c> on a
    /// single line per the Bodu profile and leaving all other member kinds unchanged.
    /// </summary>
    /// <param name="options">The base formatting policy.</param>
    /// <param name="memberKind">The member-kind hint for the comment being formatted.</param>
    /// <returns>The policy to use for this member; the original instance when no adjustment applies.</returns>
    private static XmlDocFormatOptions ApplyMemberKindLayout(XmlDocFormatOptions options, XmlDocMemberKindHint memberKind)
    {
        // A field's <summary> stays on a single line unless its content exceeds the width budget, matching the
        // way Bodu documents simple field values. Other member kinds keep the default force-multiline <summary>.
        if (memberKind != XmlDocMemberKindHint.Field)
        {
            return options;
        }

        XmlDocTagPolicy current = options.GetTagPolicy("summary");

        // When KeepFieldSummaryOnSingleLine is set, the field summary must never wrap: forbid breaking inside it so
        // an overflowing single line is kept intact rather than expanded to the multiline block form. Otherwise it
        // keeps the current setting and wraps as normal once it exceeds the budget.
        bool? allowLineBreakInside = options.KeepFieldSummaryOnSingleLine ? false : current.AllowLineBreakInside;
        if (current.Layout == XmlDocTagLayout.SingleLineWhenShort && current.AllowLineBreakInside == allowLineBreakInside)
        {
            return options;
        }

        var fieldSummaryPolicy = new XmlDocTagPolicy(
            XmlDocTagLayout.SingleLineWhenShort,
            current.MaxSingleLineLength,
            allowLineBreakInside,
            current.SelfClosingTrailingSpace);

        return options.WithTagPolicies(options.TagPolicies.SetItem("summary", fieldSummaryPolicy));
    }

    /// <summary>
    /// Determines whether the token stream contains only whitespace and line breaks, with no documentable content.
    /// </summary>
    /// <param name="tokens">The tokenized doc-comment content.</param>
    /// <returns>
    /// <see langword="true" /> when every token is whitespace or a line break; otherwise <see langword="false" />.
    /// </returns>
    private static bool HasNoMeaningfulTokens(ImmutableArray<XmlDocToken> tokens)
    {
        foreach (XmlDocToken token in tokens)
        {
            if (token.Kind == XmlDocTokenKind.LineBreak || token.Kind == XmlDocTokenKind.Whitespace)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether every block-start tag in the token stream is paired with a correctly nested block-end tag,
    /// indicating well-formed XML the formatter may safely rewrite.
    /// </summary>
    /// <param name="tokens">The tokenized doc-comment content.</param>
    /// <returns>
    /// <see langword="true" /> when all block tags are matched and balanced; otherwise <see langword="false" />.
    /// </returns>
    private static bool HasMatchedTags(ImmutableArray<XmlDocToken> tokens)
    {
        var openTags = new Stack<string>();
        foreach (XmlDocToken token in tokens)
        {
            if (token.Kind == XmlDocTokenKind.BlockStart && token.TagName is not null)
            {
                openTags.Push(token.TagName);
                continue;
            }

            if (token.Kind == XmlDocTokenKind.BlockEnd && token.TagName is not null)
            {
                if (openTags.Count == 0 || !string.Equals(openTags.Peek(), token.TagName, StringComparison.Ordinal))
                {
                    return false;
                }

                openTags.Pop();
            }
        }

        return openTags.Count == 0;
    }
}
