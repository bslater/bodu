// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocCodeRequiresCDataAnalyzer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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

    private static bool IsCodeElement(XmlNameSyntax name) =>
        name.Prefix is null
        && string.Equals(name.LocalName.ValueText, CodeTagName, StringComparison.Ordinal);

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

    private static bool IsInGeneratedCode(SyntaxNodeAnalysisContext context) =>
        GeneratedCodeFilters.IsGenerated(context.Node.SyntaxTree);
}
