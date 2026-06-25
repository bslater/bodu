// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Provides the immutable policy that drives <see cref="XmlDocFormatter" /> output. Use
/// <see cref="XmlDocFormatPolicyDefaults.CreateDefaults" /> for the standard Bodu profile or
/// construct a custom instance to override individual rules.
/// </summary>
/// <remarks>
/// <para>
/// All sets and dictionaries are immutable. Comparisons against tag names are performed using
/// <see cref="System.StringComparer.Ordinal" /> because XML element names are case-sensitive.
/// </para>
/// </remarks>
public sealed class XmlDocFormatOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocFormatOptions" /> class with the given policy.
    /// </summary>
    /// <param name="maxLineLength">The maximum physical line length, including the documentation prefix and indent.</param>
    /// <param name="documentationPrefix">The prefix emitted at the start of every doc-comment line.</param>
    /// <param name="indentText">The indent unit used for content beneath block tags.</param>
    /// <param name="collapseProseWhitespace">Whether sequences of prose whitespace are collapsed to a single space.</param>
    /// <param name="preserveBlankLines">Whether existing blank documentation lines are preserved.</param>
    /// <param name="preserveXmlTagAttributes">Whether attribute spacing inside XML tags is preserved verbatim.</param>
    /// <param name="preserveCrefText">Whether <c>cref</c> attribute values are preserved verbatim.</param>
    /// <param name="keepFieldSummaryOnSingleLine">Whether a field's single-line <c>&lt;summary&gt;</c> is kept on one line even when its content overflows the line budget.</param>
    /// <param name="blockTags">The set of element names treated as block tags.</param>
    /// <param name="inlineTags">The set of element names treated as inline-atomic tags.</param>
    /// <param name="forceMultilineTags">The set of element names that must always emit on their own lines.</param>
    /// <param name="singleLineWhenShortTags">The set of element names that may stay single-line when short enough.</param>
    /// <param name="neverSplitTagContent">The set of element names whose content must never wrap.</param>
    /// <param name="tagPolicies">The per-tag fine-grained policy overrides.</param>
    public XmlDocFormatOptions(
        int maxLineLength,
        string documentationPrefix,
        string indentText,
        bool collapseProseWhitespace,
        bool preserveBlankLines,
        bool preserveXmlTagAttributes,
        bool preserveCrefText,
        bool keepFieldSummaryOnSingleLine,
        ImmutableHashSet<string> blockTags,
        ImmutableHashSet<string> inlineTags,
        ImmutableHashSet<string> forceMultilineTags,
        ImmutableHashSet<string> singleLineWhenShortTags,
        ImmutableHashSet<string> neverSplitTagContent,
        ImmutableDictionary<string, XmlDocTagPolicy> tagPolicies)
    {
        if (documentationPrefix is null) throw new System.ArgumentNullException(nameof(documentationPrefix));
        if (indentText is null) throw new System.ArgumentNullException(nameof(indentText));
        if (blockTags is null) throw new System.ArgumentNullException(nameof(blockTags));
        if (inlineTags is null) throw new System.ArgumentNullException(nameof(inlineTags));
        if (forceMultilineTags is null) throw new System.ArgumentNullException(nameof(forceMultilineTags));
        if (singleLineWhenShortTags is null) throw new System.ArgumentNullException(nameof(singleLineWhenShortTags));
        if (neverSplitTagContent is null) throw new System.ArgumentNullException(nameof(neverSplitTagContent));
        if (tagPolicies is null) throw new System.ArgumentNullException(nameof(tagPolicies));
        if (maxLineLength <= 0) throw new System.ArgumentOutOfRangeException(nameof(maxLineLength), "Maximum line length must be positive.");

        this.MaxLineLength = maxLineLength;
        this.DocumentationPrefix = documentationPrefix;
        this.IndentText = indentText;
        this.CollapseProseWhitespace = collapseProseWhitespace;
        this.PreserveBlankLines = preserveBlankLines;
        this.PreserveXmlTagAttributes = preserveXmlTagAttributes;
        this.PreserveCrefText = preserveCrefText;
        this.KeepFieldSummaryOnSingleLine = keepFieldSummaryOnSingleLine;
        this.BlockTags = blockTags;
        this.InlineTags = inlineTags;
        this.ForceMultilineTags = forceMultilineTags;
        this.SingleLineWhenShortTags = singleLineWhenShortTags;
        this.NeverSplitTagContent = neverSplitTagContent;
        this.TagPolicies = tagPolicies;
    }

    /// <summary>
    /// Gets the maximum physical line length, including the documentation prefix and base indent.
    /// </summary>
    /// <value>The configured maximum line length in characters.</value>
    public int MaxLineLength { get; }

    /// <summary>
    /// Gets the prefix emitted at the start of every documentation line.
    /// </summary>
    /// <value>The configured documentation prefix; typically <c>"/// "</c>.</value>
    public string DocumentationPrefix { get; }

    /// <summary>
    /// Gets the indent unit used for content beneath block tags.
    /// </summary>
    /// <value>The configured indent text; typically four spaces.</value>
    public string IndentText { get; }

    /// <summary>
    /// Gets a value indicating whether sequences of prose whitespace are collapsed to a single space.
    /// </summary>
    /// <value><see langword="true" /> when prose whitespace is collapsed; otherwise <see langword="false" />.</value>
    public bool CollapseProseWhitespace { get; }

    /// <summary>
    /// Gets a value indicating whether existing blank documentation lines are preserved.
    /// </summary>
    /// <value><see langword="true" /> when blank lines inside the comment are preserved; otherwise <see langword="false" />.</value>
    public bool PreserveBlankLines { get; }

    /// <summary>
    /// Gets a value indicating whether attribute spacing inside XML tags is preserved verbatim.
    /// </summary>
    /// <value><see langword="true" /> when attribute formatting is preserved; otherwise <see langword="false" />.</value>
    public bool PreserveXmlTagAttributes { get; }

    /// <summary>
    /// Gets a value indicating whether <c>cref</c> attribute values are preserved verbatim.
    /// </summary>
    /// <value><see langword="true" /> when <c>cref</c> text is preserved; otherwise <see langword="false" />.</value>
    public bool PreserveCrefText { get; }

    /// <summary>
    /// Gets a value indicating whether a field's single-line <c>&lt;summary&gt;</c> is kept on one line even when
    /// its content overflows the line budget.
    /// </summary>
    /// <value><see langword="true" /> when a field summary is never wrapped; otherwise <see langword="false" />.</value>
    /// <remarks>
    /// When enabled, a field's <c>&lt;summary&gt;</c> is treated as single-line content whose wrapping is
    /// forbidden, so it stays on one physical line regardless of length rather than expanding to the multiline
    /// block form. Other member kinds, and summaries that contain genuinely multi-line content, are unaffected.
    /// </remarks>
    public bool KeepFieldSummaryOnSingleLine { get; }

    /// <summary>
    /// Gets the set of element names treated as block tags.
    /// </summary>
    /// <value>The immutable set of block tag names.</value>
    public ImmutableHashSet<string> BlockTags { get; }

    /// <summary>
    /// Gets the set of element names treated as inline-atomic tags.
    /// </summary>
    /// <value>The immutable set of inline tag names.</value>
    public ImmutableHashSet<string> InlineTags { get; }

    /// <summary>
    /// Gets the set of element names that must always emit on their own lines.
    /// </summary>
    /// <value>The immutable set of force-multiline tag names.</value>
    public ImmutableHashSet<string> ForceMultilineTags { get; }

    /// <summary>
    /// Gets the set of element names that may stay single-line when short enough.
    /// </summary>
    /// <value>The immutable set of single-line-when-short tag names.</value>
    public ImmutableHashSet<string> SingleLineWhenShortTags { get; }

    /// <summary>
    /// Gets the set of element names whose content must never wrap.
    /// </summary>
    /// <value>The immutable set of never-split tag names.</value>
    public ImmutableHashSet<string> NeverSplitTagContent { get; }

    /// <summary>
    /// Gets the per-tag fine-grained policy overrides keyed by element name.
    /// </summary>
    /// <value>The immutable dictionary of per-tag policies.</value>
    public ImmutableDictionary<string, XmlDocTagPolicy> TagPolicies { get; }

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="MaxLineLength" /> replaced.
    /// </summary>
    /// <param name="maxLineLength">The new maximum line length.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithMaxLineLength(int maxLineLength) =>
        this.With(maxLineLength: maxLineLength);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="BlockTags" /> replaced.
    /// </summary>
    /// <param name="blockTags">The new set of block tag names.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithBlockTags(ImmutableHashSet<string> blockTags) =>
        this.With(blockTags: blockTags);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="InlineTags" /> replaced.
    /// </summary>
    /// <param name="inlineTags">The new set of inline-atomic tag names.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithInlineTags(ImmutableHashSet<string> inlineTags) =>
        this.With(inlineTags: inlineTags);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="ForceMultilineTags" /> replaced.
    /// </summary>
    /// <param name="forceMultilineTags">The new set of force-multiline tag names.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithForceMultilineTags(ImmutableHashSet<string> forceMultilineTags) =>
        this.With(forceMultilineTags: forceMultilineTags);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="TagPolicies" /> replaced.
    /// </summary>
    /// <param name="tagPolicies">The new per-tag policy dictionary.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithTagPolicies(ImmutableDictionary<string, XmlDocTagPolicy> tagPolicies) =>
        this.With(tagPolicies: tagPolicies);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="NeverSplitTagContent" /> replaced.
    /// </summary>
    /// <param name="neverSplitTagContent">The new set of never-split tag names.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithNeverSplitTagContent(ImmutableHashSet<string> neverSplitTagContent) =>
        this.With(neverSplitTagContent: neverSplitTagContent);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="PreserveBlankLines" /> replaced.
    /// </summary>
    /// <param name="preserveBlankLines">The new preserve-blank-lines setting.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithPreserveBlankLines(bool preserveBlankLines) =>
        this.With(preserveBlankLines: preserveBlankLines);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="CollapseProseWhitespace" /> replaced.
    /// </summary>
    /// <param name="collapseProseWhitespace">The new collapse-prose-whitespace setting.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithCollapseProseWhitespace(bool collapseProseWhitespace) =>
        this.With(collapseProseWhitespace: collapseProseWhitespace);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="IndentText" /> replaced.
    /// </summary>
    /// <param name="indentText">The new indent unit applied beneath block tags.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithIndentText(string indentText) =>
        this.With(indentText: indentText);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="PreserveXmlTagAttributes" /> replaced.
    /// </summary>
    /// <param name="preserveXmlTagAttributes">The new preserve-attribute-spacing setting.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithPreserveXmlTagAttributes(bool preserveXmlTagAttributes) =>
        this.With(preserveXmlTagAttributes: preserveXmlTagAttributes);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="PreserveCrefText" /> replaced.
    /// </summary>
    /// <param name="preserveCrefText">The new preserve-cref-text setting.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithPreserveCrefText(bool preserveCrefText) =>
        this.With(preserveCrefText: preserveCrefText);

    /// <summary>
    /// Returns a new <see cref="XmlDocFormatOptions" /> instance with <see cref="KeepFieldSummaryOnSingleLine" />
    /// replaced.
    /// </summary>
    /// <param name="keepFieldSummaryOnSingleLine">The new keep-field-summary-on-single-line setting.</param>
    /// <returns>A new instance with the requested override applied.</returns>
    public XmlDocFormatOptions WithKeepFieldSummaryOnSingleLine(bool keepFieldSummaryOnSingleLine) =>
        this.With(keepFieldSummaryOnSingleLine: keepFieldSummaryOnSingleLine);

    /// <summary>
    /// Returns the per-tag policy for the given element name, or <see cref="XmlDocTagPolicy.Default" /> when no
    /// explicit policy is configured.
    /// </summary>
    /// <param name="tagName">The element name to look up.</param>
    /// <returns>The configured per-tag policy, or the default policy when none is configured.</returns>
    public XmlDocTagPolicy GetTagPolicy(string tagName)
    {
        if (tagName is null) throw new System.ArgumentNullException(nameof(tagName));

        return this.TagPolicies.TryGetValue(tagName, out XmlDocTagPolicy? policy) && policy is not null
            ? policy
            : XmlDocTagPolicy.Default;
    }

    /// <summary>
    /// Resolves the authoritative layout for the given element name.
    /// </summary>
    /// <param name="tagName">The element name to classify.</param>
    /// <returns>The effective <see cref="XmlDocTagLayout" /> for the tag.</returns>
    /// <remarks>
    /// <para>
    /// An explicit per-tag policy whose <see cref="XmlDocTagPolicy.Layout" /> is not
    /// <see cref="XmlDocTagLayout.Auto" /> wins outright; this makes <c>tagPolicies</c> the single authoritative
    /// layout source. The convenience sets act as shorthand consulted only when no explicit policy layout
    /// applies, in precedence order: <see cref="ForceMultilineTags" /> (block), <see cref="SingleLineWhenShortTags" />,
    /// <see cref="InlineTags" /> (inline atomic), then <see cref="BlockTags" /> (block). When none match the tag
    /// flows inline with the surrounding prose (<see cref="XmlDocTagLayout.Auto" />).
    /// </para>
    /// </remarks>
    public XmlDocTagLayout ResolveLayout(string tagName)
    {
        if (tagName is null) throw new System.ArgumentNullException(nameof(tagName));

        if (this.TagPolicies.TryGetValue(tagName, out XmlDocTagPolicy? policy) && policy is not null && policy.Layout != XmlDocTagLayout.Auto)
        {
            return policy.Layout;
        }

        if (this.ForceMultilineTags.Contains(tagName)) return XmlDocTagLayout.MultilineBlock;
        if (this.SingleLineWhenShortTags.Contains(tagName)) return XmlDocTagLayout.SingleLineWhenShort;
        if (this.InlineTags.Contains(tagName)) return XmlDocTagLayout.InlineAtomic;
        if (this.BlockTags.Contains(tagName)) return XmlDocTagLayout.MultilineBlock;

        return XmlDocTagLayout.Auto;
    }

    /// <summary>
    /// Determines whether the content of the given element may wrap across lines.
    /// </summary>
    /// <param name="tagName">The element name to test.</param>
    /// <returns>
    /// <see langword="false" /> when the tag is in <see cref="NeverSplitTagContent" /> or its per-tag policy sets
    /// <see cref="XmlDocTagPolicy.AllowLineBreakInside" /> to <see langword="false" />; otherwise <see langword="true" />.
    /// </returns>
    /// <remarks>
    /// When wrapping is forbidden, a single-line-when-short tag whose content overflows the budget is emitted on
    /// one line rather than expanded to a multiline block, keeping the element intact.
    /// </remarks>
    public bool AllowsContentWrapping(string tagName)
    {
        if (tagName is null) throw new System.ArgumentNullException(nameof(tagName));

        if (this.NeverSplitTagContent.Contains(tagName)) return false;
        if (this.TagPolicies.TryGetValue(tagName, out XmlDocTagPolicy? policy) && policy is not null && policy.AllowLineBreakInside == false)
        {
            return false;
        }

        return true;
    }

    private XmlDocFormatOptions With(
        int? maxLineLength = null,
        string? documentationPrefix = null,
        string? indentText = null,
        bool? collapseProseWhitespace = null,
        bool? preserveBlankLines = null,
        bool? preserveXmlTagAttributes = null,
        bool? preserveCrefText = null,
        bool? keepFieldSummaryOnSingleLine = null,
        ImmutableHashSet<string>? blockTags = null,
        ImmutableHashSet<string>? inlineTags = null,
        ImmutableHashSet<string>? forceMultilineTags = null,
        ImmutableHashSet<string>? singleLineWhenShortTags = null,
        ImmutableHashSet<string>? neverSplitTagContent = null,
        ImmutableDictionary<string, XmlDocTagPolicy>? tagPolicies = null) =>
        new XmlDocFormatOptions(
            maxLineLength ?? this.MaxLineLength,
            documentationPrefix ?? this.DocumentationPrefix,
            indentText ?? this.IndentText,
            collapseProseWhitespace ?? this.CollapseProseWhitespace,
            preserveBlankLines ?? this.PreserveBlankLines,
            preserveXmlTagAttributes ?? this.PreserveXmlTagAttributes,
            preserveCrefText ?? this.PreserveCrefText,
            keepFieldSummaryOnSingleLine ?? this.KeepFieldSummaryOnSingleLine,
            blockTags ?? this.BlockTags,
            inlineTags ?? this.InlineTags,
            forceMultilineTags ?? this.ForceMultilineTags,
            singleLineWhenShortTags ?? this.SingleLineWhenShortTags,
            neverSplitTagContent ?? this.NeverSplitTagContent,
            tagPolicies ?? this.TagPolicies);
}
