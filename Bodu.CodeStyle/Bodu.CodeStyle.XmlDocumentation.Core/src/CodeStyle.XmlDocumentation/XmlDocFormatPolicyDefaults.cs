// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatPolicyDefaults.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Provides the canonical Bodu XML documentation formatting policy and the well-known tag-set constants used to
/// build it.
/// </summary>
public static class XmlDocFormatPolicyDefaults
{
    /// <summary>
    /// Gets the default maximum line length, in characters.
    /// </summary>
    /// <value>The default value of 120.</value>
    public static int DefaultMaxLineLength => 120;

    /// <summary>
    /// Gets the default documentation prefix emitted at the start of every line.
    /// </summary>
    /// <value>The literal <c>"/// "</c>.</value>
    public static string DefaultDocumentationPrefix => "/// ";

    /// <summary>
    /// Gets the default indent unit applied beneath block tags.
    /// </summary>
    /// <value>
    /// The empty string. The canonical Bodu layout keeps nested block content flush with its enclosing tag, so
    /// no per-level indentation is applied by default; set a non-empty indent to indent nested content.
    /// </value>
    public static string DefaultIndentText => string.Empty;

    /// <summary>
    /// Gets the default set of block tag names.
    /// </summary>
    /// <value>The ordinal immutable set of block tag names.</value>
    /// <remarks>
    /// This set matches <see cref="DefaultForceMultilineTags" />: every default block tag is also forced
    /// multiline. <c>term</c> and <c>description</c> are intentionally excluded — they are
    /// single-line-when-short (see <see cref="DefaultSingleLineWhenShortTags" />) so each row of a
    /// <c>&lt;list&gt;</c> reads as one line, wrapping only when its content overflows.
    /// </remarks>
    public static ImmutableHashSet<string> DefaultBlockTags { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "summary",
        "remarks",
        "para",
        "example",
        "list",
        "item",
        "listheader",
        "code");

    /// <summary>
    /// Gets the default set of inline-atomic tag names.
    /// </summary>
    /// <value>The ordinal immutable set of inline tag names.</value>
    public static ImmutableHashSet<string> DefaultInlineTags { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "c",
        "see",
        "paramref",
        "typeparamref");

    /// <summary>
    /// Gets the default set of force-multiline tag names.
    /// </summary>
    /// <value>The ordinal immutable set of tags that always emit on their own lines.</value>
    public static ImmutableHashSet<string> DefaultForceMultilineTags { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "summary",
        "remarks",
        "para",
        "example",
        "list",
        "item",
        "listheader",
        "code");

    /// <summary>
    /// Gets the default set of tags that may stay single-line when short enough.
    /// </summary>
    /// <value>The ordinal immutable set of single-line-when-short tag names.</value>
    /// <remarks>
    /// <c>term</c> and <c>description</c> are single-line-when-short so each row of a <c>&lt;list&gt;</c> renders
    /// as one line — <c>&lt;term&gt;Combination&lt;/term&gt;</c>, <c>&lt;description&gt;Yield&lt;/description&gt;</c>
    /// — and expands to the multiline block form only when the content overflows the line budget.
    /// </remarks>
    public static ImmutableHashSet<string> DefaultSingleLineWhenShortTags { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "param",
        "typeparam",
        "returns",
        "exception",
        "value",
        "term",
        "description");

    /// <summary>
    /// Gets the default set of tags whose content must never wrap.
    /// </summary>
    /// <value>The ordinal immutable set of never-split tag names.</value>
    public static ImmutableHashSet<string> DefaultNeverSplitTagContent { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "c",
        "see",
        "paramref",
        "typeparamref");

    /// <summary>
    /// Creates a new <see cref="XmlDocFormatOptions" /> populated with the canonical Bodu defaults.
    /// </summary>
    /// <returns>A fresh options instance carrying the Bodu profile.</returns>
    public static XmlDocFormatOptions CreateDefaults() =>
        new XmlDocFormatOptions(
            maxLineLength: DefaultMaxLineLength,
            documentationPrefix: DefaultDocumentationPrefix,
            indentText: DefaultIndentText,
            collapseProseWhitespace: true,
            preserveBlankLines: false,
            preserveXmlTagAttributes: false,
            preserveCrefText: true,
            keepFieldSummaryOnSingleLine: false,
            blockTags: DefaultBlockTags,
            inlineTags: DefaultInlineTags,
            forceMultilineTags: DefaultForceMultilineTags,
            singleLineWhenShortTags: DefaultSingleLineWhenShortTags,
            neverSplitTagContent: DefaultNeverSplitTagContent,
            tagPolicies: CreateDefaultTagPolicies());

    private static ImmutableDictionary<string, XmlDocTagPolicy> CreateDefaultTagPolicies()
    {
        ImmutableDictionary<string, XmlDocTagPolicy>.Builder builder =
            ImmutableDictionary.CreateBuilder<string, XmlDocTagPolicy>(StringComparer.Ordinal);

        // The default per-tag policies carry only supplementary metadata and leave Layout = Auto, so they defer
        // to the convenience sets (ForceMultilineTags / SingleLineWhenShortTags / InlineTags / BlockTags) for the
        // layout decision. This keeps the sets as the single shorthand source for the default profile; an
        // explicit, non-Auto tagPolicies.layout in user configuration is what authoritatively overrides them.
        var multiline = new XmlDocTagPolicy(XmlDocTagLayout.Auto, null, allowLineBreakInside: true, selfClosingTrailingSpace: null);
        var singleLine = new XmlDocTagPolicy(XmlDocTagLayout.Auto, maxSingleLineLength: DefaultMaxLineLength, allowLineBreakInside: true, selfClosingTrailingSpace: null);
        var inlineAtomic = new XmlDocTagPolicy(XmlDocTagLayout.Auto, null, allowLineBreakInside: false, selfClosingTrailingSpace: true);

        builder.Add("summary", multiline);
        builder.Add("remarks", multiline);
        builder.Add("para", multiline);
        builder.Add("example", multiline);
        builder.Add("list", multiline);
        builder.Add("item", multiline);
        builder.Add("listheader", multiline);

        builder.Add("param", singleLine);
        builder.Add("typeparam", singleLine);
        builder.Add("returns", singleLine);
        builder.Add("exception", singleLine);
        builder.Add("value", singleLine);
        builder.Add("term", singleLine);
        builder.Add("description", singleLine);

        builder.Add("c", inlineAtomic);
        builder.Add("see", inlineAtomic);
        builder.Add("paramref", inlineAtomic);
        builder.Add("typeparamref", inlineAtomic);

        return builder.ToImmutable();
    }
}
