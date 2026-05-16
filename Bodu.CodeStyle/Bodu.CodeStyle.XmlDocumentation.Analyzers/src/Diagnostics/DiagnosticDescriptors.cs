// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticDescriptors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// cross-cutting descriptor (<c>BODU1040</c>) for changes that fall outside any tag scope (line prefix, indent,
/// prose between top-level tags). All descriptors share the <c>Documentation</c> category, so a single
/// <c>dotnet_analyzer_diagnostic.category-Documentation.severity = …</c> entry in <c>.editorconfig</c>
/// silences or re-targets the entire XML-doc family at once.
/// </remarks>
internal static class DiagnosticDescriptors
{
    private const string Category = "Documentation";

    private const string HelpLinkBase = "https://github.com/bodu/bodu/blob/master/Bodu.CodeStyle/README.md";

    /// <summary>Gets the descriptor for <c>BODU1001</c> — <c>&lt;summary&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocSummary { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocSummary, "summary");

    /// <summary>Gets the descriptor for <c>BODU1002</c> — <c>&lt;remarks&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocRemarks { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocRemarks, "remarks");

    /// <summary>Gets the descriptor for <c>BODU1003</c> — <c>&lt;para&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocPara { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocPara, "para");

    /// <summary>Gets the descriptor for <c>BODU1004</c> — <c>&lt;example&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocExample { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocExample, "example");

    /// <summary>Gets the descriptor for <c>BODU1005</c> — <c>&lt;code&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocCode { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocCode, "code");

    /// <summary>Gets the descriptor for <c>BODU1006</c> — <c>&lt;list&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocList { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocList, "list");

    /// <summary>Gets the descriptor for <c>BODU1007</c> — <c>&lt;item&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocItem { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocItem, "item");

    /// <summary>Gets the descriptor for <c>BODU1008</c> — <c>&lt;description&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocDescription { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocDescription, "description");

    /// <summary>Gets the descriptor for <c>BODU1009</c> — <c>&lt;term&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocTerm { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocTerm, "term");

    /// <summary>Gets the descriptor for <c>BODU1010</c> — <c>&lt;param&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocParam { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocParam, "param");

    /// <summary>Gets the descriptor for <c>BODU1011</c> — <c>&lt;typeparam&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocTypeParam { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocTypeParam, "typeparam");

    /// <summary>Gets the descriptor for <c>BODU1012</c> — <c>&lt;returns&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocReturns { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocReturns, "returns");

    /// <summary>Gets the descriptor for <c>BODU1013</c> — <c>&lt;exception&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocException { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocException, "exception");

    /// <summary>Gets the descriptor for <c>BODU1014</c> — <c>&lt;value&gt;</c> formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocValue { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocValue, "value");

    /// <summary>Gets the descriptor for <c>BODU1015</c> — <c>&lt;c&gt;</c> inline formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocInlineCode { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocInlineCode, "c");

    /// <summary>Gets the descriptor for <c>BODU1016</c> — <c>&lt;see&gt;</c> inline formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocSee { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocSee, "see");

    /// <summary>Gets the descriptor for <c>BODU1017</c> — <c>&lt;paramref&gt;</c> inline formatting differs from policy.</summary>
    public static DiagnosticDescriptor XmlDocParamRef { get; } = CreateTagDescriptor(DiagnosticIds.XmlDocParamRef, "paramref");

    /// <summary>Gets the descriptor for <c>BODU1018</c> — <c>&lt;typeparamref&gt;</c> inline formatting differs from policy.</summary>
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

    /// <summary>Gets the immutable collection of every descriptor surfaced by this analyzer.</summary>
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
        XmlDocCrossCutting);

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
    /// Returns the descriptor associated with the given tag name, or the cross-cutting descriptor when no
    /// per-tag descriptor exists for that name.
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
