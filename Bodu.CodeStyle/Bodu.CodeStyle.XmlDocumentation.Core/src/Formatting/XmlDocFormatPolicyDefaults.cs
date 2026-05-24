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
    /// <returns>The default value of 120.</returns>
    public static int DefaultMaxLineLength => 120;

    /// <summary>
    /// Gets the default documentation prefix emitted at the start of every line.
    /// </summary>
    /// <returns>The literal <c>"/// "</c>.</returns>
    public static string DefaultDocumentationPrefix => "/// ";

    /// <summary>
    /// Gets the default indent unit applied beneath block tags.
    /// </summary>
    /// <returns>Four space characters.</returns>
    public static string DefaultIndentText => "    ";

    /// <summary>
    /// Gets the default set of block tag names.
    /// </summary>
    /// <returns>The ordinal immutable set of block tag names.</returns>
    public static ImmutableHashSet<string> DefaultBlockTags { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "summary",
        "remarks",
        "para",
        "example",
        "list",
        "item",
        "description",
        "term",
        "code");

    /// <summary>
    /// Gets the default set of inline-atomic tag names.
    /// </summary>
    /// <returns>The ordinal immutable set of inline tag names.</returns>
    public static ImmutableHashSet<string> DefaultInlineTags { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "c",
        "see",
        "paramref",
        "typeparamref");

    /// <summary>
    /// Gets the default set of force-multiline tag names.
    /// </summary>
    /// <returns>The ordinal immutable set of tags that always emit on their own lines.</returns>
    public static ImmutableHashSet<string> DefaultForceMultilineTags { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "summary",
        "remarks",
        "para",
        "example",
        "list",
        "item",
        "description",
        "code");

    /// <summary>
    /// Gets the default set of tags that may stay single-line when short enough.
    /// </summary>
    /// <returns>The ordinal immutable set of single-line-when-short tag names.</returns>
    public static ImmutableHashSet<string> DefaultSingleLineWhenShortTags { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "param",
        "typeparam",
        "returns",
        "exception",
        "value");

    /// <summary>
    /// Gets the default set of tags whose content must never wrap.
    /// </summary>
    /// <returns>The ordinal immutable set of never-split tag names.</returns>
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
    public static XmlDocFormatOptions CreateBoduDefaults() =>
        new XmlDocFormatOptions(
            maxLineLength: DefaultMaxLineLength,
            documentationPrefix: DefaultDocumentationPrefix,
            indentText: DefaultIndentText,
            collapseProseWhitespace: true,
            preserveBlankLines: false,
            preserveXmlTagAttributes: true,
            preserveCrefText: true,
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

        var multiline = new XmlDocTagPolicy(XmlDocTagLayout.MultilineBlock, null, allowLineBreakInside: true, selfClosingTrailingSpace: null);
        var singleLine = new XmlDocTagPolicy(XmlDocTagLayout.SingleLineWhenShort, maxSingleLineLength: DefaultMaxLineLength, allowLineBreakInside: true, selfClosingTrailingSpace: null);
        var inlineAtomic = new XmlDocTagPolicy(XmlDocTagLayout.InlineAtomic, null, allowLineBreakInside: false, selfClosingTrailingSpace: true);

        builder.Add("summary", multiline);
        builder.Add("remarks", multiline);
        builder.Add("para", multiline);
        builder.Add("example", multiline);
        builder.Add("list", multiline);
        builder.Add("item", multiline);
        builder.Add("description", multiline);

        builder.Add("param", singleLine);
        builder.Add("typeparam", singleLine);
        builder.Add("returns", singleLine);
        builder.Add("exception", singleLine);
        builder.Add("value", singleLine);

        builder.Add("c", inlineAtomic);
        builder.Add("see", inlineAtomic);
        builder.Add("paramref", inlineAtomic);
        builder.Add("typeparamref", inlineAtomic);

        return builder.ToImmutable();
    }
}
