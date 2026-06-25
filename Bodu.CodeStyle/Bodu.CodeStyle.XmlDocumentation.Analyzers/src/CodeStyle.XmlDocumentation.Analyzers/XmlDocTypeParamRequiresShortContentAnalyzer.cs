// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTypeParamRequiresShortContentAnalyzer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Text;
using Bodu.CodeStyle.XmlDocumentation;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Configuration;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers;

/// <summary>
/// Reports <c>BODU1406</c> when a <c>&lt;typeparam&gt;</c> documentation element's single-line rendering
/// exceeds the configured line budget and the content can be split at the first sentence boundary so the
/// trailing prose moves to a <c>&lt;remarks&gt;&lt;para&gt;…&lt;/para&gt;&lt;/remarks&gt;</c> block.
/// </summary>
/// <remarks>
/// <para>
/// The rule fires when all three conditions hold: (1) the element is <c>&lt;typeparam&gt;</c>; (2) the
/// rendered source line — leading indent, <c>///</c> prefix, start tag, content, end tag — exceeds
/// <see cref="XmlDocFormatOptions.MaxLineLength" />; (3) the content contains at least one period-space
/// boundary where splitting the first sentence would bring the line under budget. The third condition
/// guarantees the companion code fix can always succeed when the diagnostic fires.
/// </para>
/// <para>
/// Multi-line authoring of <c>&lt;typeparam&gt;</c> is governed by the regular format analyzer
/// (<c>BODU1011</c>), which is content-shape agnostic. Single-line authoring is the canonical Bodu form, so
/// this content-quality rule measures the source span and the first-sentence split candidate using that
/// canonical layout.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XmlDocTypeParamRequiresShortContentAnalyzer : DiagnosticAnalyzer
{
    private const string TypeParamTagName = "typeparam";

    private const string DocCommentPrefix = "/// ";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.XmlDocTypeParamRequiresShortContent);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
    {
        XmlDocFormatOptions compilationOptions = XmlDocConfigurationLoader.LoadCompilationOptions(
            compilationContext.Options.AdditionalFiles,
            compilationContext.CancellationToken);

        compilationContext.RegisterSyntaxNodeAction(
            ctx => AnalyzeXmlElement(ctx, compilationOptions),
            SyntaxKind.XmlElement);
    }

    private static void AnalyzeXmlElement(SyntaxNodeAnalysisContext context, XmlDocFormatOptions compilationOptions)
    {
        var element = (XmlElementSyntax)context.Node;
        if (!IsTypeParamElement(element.StartTag.Name)) return;
        if (IsInGeneratedCode(context)) return;

        AnalyzerConfigOptions treeOptions = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        XmlDocFormatOptions options = XmlDocConfigurationLoader.ApplyEditorConfigOverrides(compilationOptions, treeOptions);
        var budget = options.MaxLineLength;

        // Measure the canonical single-line rendering — the form BODU1011 would reflow the element to — rather
        // than the raw source span. This avoids over-estimating a multi-line-authored element and re-firing
        // after the formatter has joined it onto one line.
        var canonicalContent = XmlDocProseText.Canonicalize(GetContentText(element));
        var fixedOverhead = ComputeFixedOverhead(element);
        if (fixedOverhead + canonicalContent.Length <= budget) return;

        var splitIndex = XmlDocSentenceBoundary.FindFirstSentenceBoundary(canonicalContent);
        if (splitIndex < 0) return;

        // The line after the fix keeps the first sentence; confirm that brings the canonical line under budget.
        var firstSentenceLineLength = fixedOverhead + (splitIndex + 1);
        if (firstSentenceLineLength > budget) return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.XmlDocTypeParamRequiresShortContent,
            element.StartTag.GetLocation()));
    }

    private static bool IsTypeParamElement(XmlNameSyntax name) =>
        name.Prefix is null
        && string.Equals(name.LocalName.ValueText, TypeParamTagName, StringComparison.Ordinal);

    // Computes the non-content width of the canonical single-line rendering: the rendered prefix (indent plus
    // the "/// " doc-comment marker, taken as the start tag's column) plus the canonicalized start and end tags.
    private static int ComputeFixedOverhead(XmlElementSyntax element)
    {
        SourceText text = element.SyntaxTree!.GetText();
        var lineStart = element.StartTag.SpanStart;
        while (lineStart > 0 && text[lineStart - 1] != '\n' && text[lineStart - 1] != '\r')
        {
            lineStart--;
        }

        var prefixWidth = element.StartTag.SpanStart - lineStart;
        var startTagWidth = XmlDocProseText.Canonicalize(element.StartTag.ToString()).Length;
        var endTagWidth = XmlDocProseText.Canonicalize(element.EndTag.ToString()).Length;
        return prefixWidth + startTagWidth + endTagWidth;
    }

    // Concatenates the element's content tokens into a single string. Inline child elements (e.g.
    // <see cref="…" />) contribute their verbatim source text; text tokens contribute their raw text.
    // Whitespace is preserved as-is so the first-sentence boundary detection can rely on the canonical
    // ". " marker.
    private static string GetContentText(XmlElementSyntax element)
    {
        var sb = new StringBuilder();
        foreach (XmlNodeSyntax node in element.Content)
        {
            sb.Append(node.ToString());
        }

        return sb.ToString();
    }

    private static bool IsInGeneratedCode(SyntaxNodeAnalysisContext context) =>
        GeneratedCodeFilters.IsGenerated(context.Node.SyntaxTree);
}
