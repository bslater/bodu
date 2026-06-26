// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocCodeRequiresCDataAnalyzer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers;

/// <summary>
/// Reports <c>BODU1405</c> when a <c>&lt;code&gt;</c> documentation element does not begin with a
/// <c>&lt;![CDATA[…]]&gt;</c> section, or when that section's opener has whitespace separating it from the
/// preceding <c>///</c> doc-comment prefix. Wrapping the body in CDATA preserves XML-significant characters
/// and language samples verbatim; emitting <c>&lt;![CDATA[</c> immediately after <c>///</c> keeps the
/// rendered example flush-left without an accidental leading space.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XmlDocCodeRequiresCDataAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The local name of the documentation element this analyzer inspects.
    /// </summary>
    private const string CodeTagName = "code";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.XmlDocCodeRequiresCData);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeXmlElement, SyntaxKind.XmlElement);
        context.RegisterSyntaxNodeAction(AnalyzeXmlEmptyElement, SyntaxKind.XmlEmptyElement);
    }

    /// <summary>
    /// Inspects a <c>&lt;code&gt;</c> documentation element and reports the diagnostic when its first
    /// non-whitespace child is not a CDATA section, or when the CDATA opener is separated from the
    /// preceding <c>///</c> prefix by stray whitespace.
    /// </summary>
    /// <param name="context">The syntax-node analysis context for the <see cref="XmlElementSyntax" /> node.</param>
    private static void AnalyzeXmlElement(SyntaxNodeAnalysisContext context)
    {
        var element = (XmlElementSyntax)context.Node;
        if (!IsCodeElement(element.StartTag.Name)) return;
        if (IsInGeneratedCode(context)) return;

        XmlCDataSectionSyntax? firstCData = null;
        foreach (XmlNodeSyntax child in element.Content)
        {
            if (IsWhitespaceOnly(child)) continue;

            if (child is XmlCDataSectionSyntax cdata)
            {
                firstCData = cdata;
                break;
            }

            // First non-whitespace child is not CDATA — flag the element.
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.XmlDocCodeRequiresCData,
                element.StartTag.GetLocation()));
            return;
        }

        if (firstCData is null)
        {
            // Element body contains nothing or only whitespace — no CDATA child present.
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.XmlDocCodeRequiresCData,
                element.StartTag.GetLocation()));
            return;
        }

        if (HasSpaceBetweenDocPrefixAndCData(firstCData))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.XmlDocCodeRequiresCData,
                firstCData.GetLocation()));
        }
    }

    /// <summary>
    /// Reports the diagnostic for a self-closing <c>&lt;code/&gt;</c> documentation element, which can
    /// never contain the required CDATA child.
    /// </summary>
    /// <param name="context">The syntax-node analysis context for the <see cref="XmlEmptyElementSyntax" /> node.</param>
    private static void AnalyzeXmlEmptyElement(SyntaxNodeAnalysisContext context)
    {
        var element = (XmlEmptyElementSyntax)context.Node;
        if (!IsCodeElement(element.Name)) return;
        if (IsInGeneratedCode(context)) return;

        // A self-closing <code/> element has no children, so a CDATA child is impossible.
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.XmlDocCodeRequiresCData,
            element.GetLocation()));
    }

    /// <summary>
    /// Determines whether the supplied element name is an unprefixed <c>&lt;code&gt;</c> tag.
    /// </summary>
    /// <param name="name">The XML element name to test.</param>
    /// <returns><see langword="true" /> if the name is the unprefixed <c>code</c> tag; otherwise, <see langword="false" />.</returns>
    private static bool IsCodeElement(XmlNameSyntax name) =>
        name.Prefix is null
        && string.Equals(name.LocalName.ValueText, CodeTagName, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether the supplied content node consists solely of whitespace and newline tokens.
    /// </summary>
    /// <param name="node">The XML content node to test.</param>
    /// <returns><see langword="true" /> if the node is a text node containing only whitespace; otherwise, <see langword="false" />.</returns>
    private static bool IsWhitespaceOnly(XmlNodeSyntax node)
    {
        if (node is not XmlTextSyntax text) return false;

        foreach (SyntaxToken token in text.TextTokens)
        {
            if (token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken)) continue;
            if (token.IsKind(SyntaxKind.XmlTextLiteralToken) && string.IsNullOrWhiteSpace(token.ValueText)) continue;

            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the CDATA opener is separated from its preceding <c>///</c> doc-comment prefix
    /// by stray whitespace on the same line.
    /// </summary>
    /// <param name="cdata">The CDATA section whose opener position is examined.</param>
    /// <returns><see langword="true" /> if a stray space or tab sits between the <c>///</c> prefix and the opener; otherwise, <see langword="false" />.</returns>
    // Walks backward from the CDATA opener through space / tab characters on the same line. When those
    // whitespace characters are immediately preceded by exactly "///" (the doc-comment prefix), the opener
    // sits on its own /// line with a stray separating space — exactly the layout the rule forbids.
    private static bool HasSpaceBetweenDocPrefixAndCData(XmlCDataSectionSyntax cdata)
    {
        SyntaxTree? tree = cdata.SyntaxTree;
        if (tree is null) return false;

        SourceText text = tree.GetText();
        var position = cdata.SpanStart - 1;
        var whitespace = 0;

        while (position >= 0 && (text[position] == ' ' || text[position] == '\t'))
        {
            whitespace++;
            position--;
        }

        if (whitespace == 0) return false;
        if (position < 2) return false;
        if (text[position] != '/' || text[position - 1] != '/' || text[position - 2] != '/') return false;

        // Reject /// runs longer than three slashes (e.g. ////) — those aren't doc-comment prefixes.
        if (position - 3 >= 0 && text[position - 3] == '/') return false;

        return true;
    }

    /// <summary>
    /// Determines whether the node under analysis belongs to a generated source file.
    /// </summary>
    /// <param name="context">The syntax-node analysis context to inspect.</param>
    /// <returns><see langword="true" /> if the node resides in generated code; otherwise, <see langword="false" />.</returns>
    private static bool IsInGeneratedCode(SyntaxNodeAnalysisContext context) =>
        GeneratedCodeFilters.IsGenerated(context.Node.SyntaxTree);
}
