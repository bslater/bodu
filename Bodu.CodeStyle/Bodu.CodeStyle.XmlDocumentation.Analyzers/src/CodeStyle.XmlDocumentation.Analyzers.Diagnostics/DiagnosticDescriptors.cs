// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticDescriptors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;

/// <summary>
/// Provides the <see cref="DiagnosticDescriptor" /> instances surfaced by the Bodu XML documentation analyzer.
/// </summary>
/// <remarks>
/// There is one descriptor per documented XML tag in the <c>BODU1001</c>–<c>BODU1018</c> range, plus a single
/// cross-cutting descriptor (<c>BODU1040</c>) for changes that fall outside any tag scope (line prefix, indent, prose
/// between top-level tags). All descriptors share the <c>Documentation</c> category, so a single
/// <c>dotnet_analyzer_diagnostic.category-Documentation.severity = …</c> entry in <c>.editorconfig</c> silences or
/// re-targets the entire XML-doc family at once.
/// </remarks>
internal static class DiagnosticDescriptors
{
    /// <summary>The diagnostic category shared by the XML documentation formatting descriptors.</summary>
    private const string Category = "Documentation";

    /// <summary>The diagnostic category used for configuration-file descriptors.</summary>
    private const string ConfigurationCategory = "BoduCodeStyle";

    /// <summary>The base URL for the help links attached to each descriptor, anchored per rule.</summary>
    private const string HelpLinkBase = "https://github.com/bodu/bodu/blob/master/Bodu.CodeStyle/README.md";

    /// <summary>
    /// Gets the descriptor for <c>BODU0001</c> — a <c>bodu.xmldocstyle.json</c> configuration file is invalid and was
    /// ignored.
    /// </summary>
    public static DiagnosticDescriptor XmlDocConfigInvalid { get; } = new DiagnosticDescriptor(
        id: DiagnosticIds.XmlDocConfigInvalid,
        title: "Bodu XML documentation configuration file is invalid",
        messageFormat: "bodu.xmldocstyle.json is invalid and was ignored; defaults are in effect: {0}",
        category: ConfigurationCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reports that a bodu.xmldocstyle.json configuration file could not be parsed or applied, so the analyzer fell back to the built-in defaults. Fix the file so the intended policy takes effect.",
        helpLinkUri: HelpLinkBase + "#bodu0001");

    /// <summary>
    /// Gets the descriptor for <c>BODU1001</c> — <c>&lt;summary&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocSummary { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocSummary, "summary");

    /// <summary>
    /// Gets the descriptor for <c>BODU1002</c> — <c>&lt;remarks&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocRemarks { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocRemarks, "remarks");

    /// <summary>
    /// Gets the descriptor for <c>BODU1003</c> — <c>&lt;para&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocPara { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocPara, "para");

    /// <summary>
    /// Gets the descriptor for <c>BODU1004</c> — <c>&lt;example&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocExample { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocExample, "example");

    /// <summary>
    /// Gets the descriptor for <c>BODU1005</c> — <c>&lt;code&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocCode { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocCode, "code");

    /// <summary>
    /// Gets the descriptor for <c>BODU1006</c> — <c>&lt;list&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocList { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocList, "list");

    /// <summary>
    /// Gets the descriptor for <c>BODU1007</c> — <c>&lt;item&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocItem { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocItem, "item");

    /// <summary>
    /// Gets the descriptor for <c>BODU1008</c> — <c>&lt;description&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocDescription { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocDescription, "description");

    /// <summary>
    /// Gets the descriptor for <c>BODU1009</c> — <c>&lt;term&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocTerm { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocTerm, "term");

    /// <summary>
    /// Gets the descriptor for <c>BODU1010</c> — <c>&lt;param&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocParam { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocParam, "param");

    /// <summary>
    /// Gets the descriptor for <c>BODU1011</c> — <c>&lt;typeparam&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocTypeParam { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocTypeParam, "typeparam");

    /// <summary>
    /// Gets the descriptor for <c>BODU1012</c> — <c>&lt;returns&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocReturns { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocReturns, "returns");

    /// <summary>
    /// Gets the descriptor for <c>BODU1013</c> — <c>&lt;exception&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocException { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocException, "exception");

    /// <summary>
    /// Gets the descriptor for <c>BODU1014</c> — <c>&lt;value&gt;</c> formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocValue { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocValue, "value");

    /// <summary>
    /// Gets the descriptor for <c>BODU1015</c> — <c>&lt;c&gt;</c> inline formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocInlineCode { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocInlineCode, "c");

    /// <summary>
    /// Gets the descriptor for <c>BODU1016</c> — <c>&lt;see&gt;</c> inline formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocSee { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocSee, "see");

    /// <summary>
    /// Gets the descriptor for <c>BODU1017</c> — <c>&lt;paramref&gt;</c> inline formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocParamRef { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocParamRef, "paramref");

    /// <summary>
    /// Gets the descriptor for <c>BODU1018</c> — <c>&lt;typeparamref&gt;</c> inline formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocTypeParamRef { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocTypeParamRef, "typeparamref");

    /// <summary>
    /// Gets the descriptor for <c>BODU1040</c> — documentation prose, prefix, or indent differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocCrossCutting { get; } = new DiagnosticDescriptor(
        id: DiagnosticIds.XmlDocCrossCutting,
        title: "XML documentation prose or prefix is not formatted according to Bodu style",
        messageFormat: "XML documentation prose, prefix, or indent differs from project policy",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reformats documentation content outside any tag scope (line prefix, indent, prose between tags) to match the Bodu code-style policy.",
        helpLinkUri: HelpLinkBase + "#bodu1040");

    /// <summary>
    /// Gets the descriptor for <c>BODU1405</c> — a <c>&lt;code&gt;</c> element does not contain a
    /// <c>&lt;![CDATA[…]]&gt;</c> section as its first non-whitespace child, or that section's opener has stray
    /// whitespace separating it from the preceding <c>///</c> doc-comment prefix.
    /// </summary>
    public static DiagnosticDescriptor XmlDocCodeRequiresCData { get; } = new DiagnosticDescriptor(
        id: DiagnosticIds.XmlDocCodeRequiresCData,
        title: "XML documentation <code> element must begin with a CDATA section flush against ///",
        messageFormat: "<code> element must begin with <![CDATA[ immediately after the /// doc-comment prefix per project policy",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Requires <code> documentation blocks to wrap their body in a <![CDATA[…]]> section so XML-significant characters and language samples render verbatim, and requires the opener to butt directly against the /// prefix with no separating space.",
        helpLinkUri: HelpLinkBase + "#bodu1405");

    /// <summary>
    /// Gets the descriptor for <c>BODU1406</c> — a <c>&lt;typeparam&gt;</c> element carries explanatory prose that
    /// overflows the single-line budget; the trailing sentences should be relocated into a
    /// <c>&lt;remarks&gt;&lt;para&gt;…&lt;/para&gt;&lt;/remarks&gt;</c> block so the type-parameter description remains
    /// a single concise statement of what the parameter represents.
    /// </summary>
    public static DiagnosticDescriptor XmlDocTypeParamRequiresShortContent { get; } = new DiagnosticDescriptor(
        id: DiagnosticIds.XmlDocTypeParamRequiresShortContent,
        title: "XML documentation <typeparam> content overflows the single-line budget",
        messageFormat: "<typeparam> content overflows the single-line budget per project policy; trailing prose must move to <remarks>",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Requires <typeparam> documentation elements to fit on a single line within the comment width. Explanatory prose beyond a concise statement of the type parameter is relocated into a <para> block inside the type-level <remarks>.",
        helpLinkUri: HelpLinkBase + "#bodu1406");

    /// <summary>
    /// Gets the descriptors emitted by <see cref="XmlDocFormatAnalyzer" /> — the per-tag formatting diagnostics (<c>BODU1001</c>–<c>BODU1018</c>)
    /// plus the cross-cutting <c>BODU1040</c>. This is the set the formatting analyzer advertises; the content-quality
    /// (<c>BODU1405</c>/<c>BODU1406</c>) and configuration (<c>BODU0001</c>) diagnostics are owned by their own
    /// analyzers.
    /// </summary>
    public static ImmutableArray<DiagnosticDescriptor> FormattingDescriptors { get; } = ImmutableArray.Create(
        XmlDocSummary,
        XmlDocRemarks,
        XmlDocPara,
        XmlDocExample,
        XmlDocCode,
        XmlDocList,
        XmlDocItem,
        XmlDocDescription,
        XmlDocTerm,
        XmlDocParam,
        XmlDocTypeParam,
        XmlDocReturns,
        XmlDocException,
        XmlDocValue,
        XmlDocInlineCode,
        XmlDocSee,
        XmlDocParamRef,
        XmlDocTypeParamRef,
        XmlDocCrossCutting);

    /// <summary>
    /// Gets the immutable collection of every descriptor surfaced by this analyzer package. Intended for documentation
    /// and tests, not for an individual analyzer's <c>SupportedDiagnostics</c>.
    /// </summary>
    public static ImmutableArray<DiagnosticDescriptor> All { get; } = ImmutableArray.Create(
        XmlDocSummary,
        XmlDocRemarks,
        XmlDocPara,
        XmlDocExample,
        XmlDocCode,
        XmlDocList,
        XmlDocItem,
        XmlDocDescription,
        XmlDocTerm,
        XmlDocParam,
        XmlDocTypeParam,
        XmlDocReturns,
        XmlDocException,
        XmlDocValue,
        XmlDocInlineCode,
        XmlDocSee,
        XmlDocParamRef,
        XmlDocTypeParamRef,
        XmlDocCrossCutting,
        XmlDocCodeRequiresCData,
        XmlDocTypeParamRequiresShortContent);

    /// <summary>Maps each supported XML doc tag name to its per-tag formatting descriptor.</summary>
    private static readonly Dictionary<string, DiagnosticDescriptor> s_byTagName =
        new(StringComparer.Ordinal)
        {
            ["summary"] = XmlDocSummary,
            ["remarks"] = XmlDocRemarks,
            ["para"] = XmlDocPara,
            ["example"] = XmlDocExample,
            ["code"] = XmlDocCode,
            ["list"] = XmlDocList,
            ["item"] = XmlDocItem,
            ["description"] = XmlDocDescription,
            ["term"] = XmlDocTerm,
            ["param"] = XmlDocParam,
            ["typeparam"] = XmlDocTypeParam,
            ["returns"] = XmlDocReturns,
            ["exception"] = XmlDocException,
            ["value"] = XmlDocValue,
            ["c"] = XmlDocInlineCode,
            ["see"] = XmlDocSee,
            ["paramref"] = XmlDocParamRef,
            ["typeparamref"] = XmlDocTypeParamRef,
        };

    /// <summary>
    /// Returns the descriptor associated with the given tag name, or the cross-cutting descriptor when no per-tag
    /// descriptor exists for that name.
    /// </summary>
    /// <param name="tagName">
    /// The XML doc tag name (e.g. <c>"summary"</c>), or <see langword="null" /> for a cross-cutting change.
    /// </param>
    /// <returns>The descriptor to report; never <see langword="null" />.</returns>
    public static DiagnosticDescriptor ForTag(string? tagName)
    {
        if (tagName is null) return XmlDocCrossCutting;
        return s_byTagName.TryGetValue(tagName, out DiagnosticDescriptor descriptor) ? descriptor : XmlDocCrossCutting;
    }

    /// <summary>
    /// Creates a per-tag formatting descriptor with the standard title, message, category, severity, and help link.
    /// </summary>
    /// <param name="id">The diagnostic identifier (e.g. <c>BODU1001</c>).</param>
    /// <param name="tagName">The XML doc tag name the descriptor reports on.</param>
    /// <returns>The constructed descriptor.</returns>
    private static DiagnosticDescriptor CreateTagDescriptor(string id, string tagName) =>
        new DiagnosticDescriptor(
            id: id,
            title: $"XML documentation <{tagName}> is not formatted according to Bodu style",
            messageFormat: $"<{tagName}> formatting differs from project policy",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: $"Reformats <{tagName}> documentation tags to match the Bodu code-style policy.",
            helpLinkUri: HelpLinkBase + "#" + id.ToLowerInvariant());
}
