// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocCodeRequiresCDataCodeFixProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bodu.CodeStyle.XmlDocumentation.CodeFixes;

/// <summary>
/// Provides code fixes for <c>BODU1405</c> — repairs <c>&lt;code&gt;</c> documentation elements so they
/// begin with a <c>&lt;![CDATA[…]]&gt;</c> section flush against the <c>///</c> doc-comment prefix.
/// </summary>
/// <remarks>
/// Three fixes are registered depending on the violating shape:
/// <list type="bullet">
///   <item><description>Removes stray whitespace between <c>///</c> and <c>&lt;![CDATA[</c> when the CDATA is present but offset by a space or tab.</description></item>
///   <item><description>Inserts an empty <c>&lt;![CDATA[]]&gt;</c> body for empty <c>&lt;code&gt;&lt;/code&gt;</c> elements and rewrites self-closing <c>&lt;code /&gt;</c> with the same.</description></item>
///   <item><description>Wraps existing text content in <c>&lt;![CDATA[…]]&gt;</c> when the <c>&lt;code&gt;</c> body is text or a mix of text and elements without a CDATA leader.</description></item>
/// </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocCodeRequiresCDataCodeFixProvider))]
[Shared]
public sealed class XmlDocCodeRequiresCDataCodeFixProvider : CodeFixProvider
{
    private const string RemoveSpaceTitle = "Remove space between /// and <![CDATA[";

    private const string WrapTitle = "Wrap <code> body in <![CDATA[…]]>";

    private const string RemoveSpaceEquivalenceKey = "BoduRemoveSpaceBeforeXmlDocCData";

    private const string WrapEquivalenceKey = "BoduWrapXmlDocCodeBodyInCData";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.XmlDocCodeRequiresCData);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            TextSpan span = diagnostic.Location.SourceSpan;
            SyntaxNode? node = root.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);

            switch (node)
            {
                case XmlCDataSectionSyntax cdata:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: RemoveSpaceTitle,
                            createChangedDocument: ct => RemoveLeadingWhitespaceAsync(context.Document, cdata, ct),
                            equivalenceKey: RemoveSpaceEquivalenceKey),
                        diagnostic);
                    break;

                case XmlEmptyElementSyntax emptyElement:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: WrapTitle,
                            createChangedDocument: ct => ReplaceSelfClosingWithCDataAsync(context.Document, emptyElement, ct),
                            equivalenceKey: WrapEquivalenceKey),
                        diagnostic);
                    break;

                case XmlElementStartTagSyntax startTag when startTag.Parent is XmlElementSyntax element:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: WrapTitle,
                            createChangedDocument: ct => WrapElementBodyInCDataAsync(context.Document, element, ct),
                            equivalenceKey: WrapEquivalenceKey),
                        diagnostic);
                    break;
            }
        }
    }

    private static async Task<Document> RemoveLeadingWhitespaceAsync(Document document, XmlCDataSectionSyntax cdata, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var position = cdata.SpanStart - 1;
        while (position >= 0 && (text[position] == ' ' || text[position] == '\t'))
        {
            position--;
        }

        var whitespaceStart = position + 1;
        var whitespaceEnd = cdata.SpanStart;
        if (whitespaceStart >= whitespaceEnd) return document;

        SourceText updated = text.WithChanges(new TextChange(TextSpan.FromBounds(whitespaceStart, whitespaceEnd), string.Empty));
        return document.WithText(updated);
    }

    private static async Task<Document> ReplaceSelfClosingWithCDataAsync(Document document, XmlEmptyElementSyntax element, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var localName = element.Name.LocalName.ValueText;
        var replacement = "<" + localName + "><![CDATA[]]></" + localName + ">";

        SourceText updated = text.WithChanges(new TextChange(element.Span, replacement));
        return document.WithText(updated);
    }

    private static async Task<Document> WrapElementBodyInCDataAsync(Document document, XmlElementSyntax element, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var bodyStart = element.StartTag.Span.End;
        var bodyEnd = element.EndTag.Span.Start;
        if (bodyStart > bodyEnd) return document;

        var existing = text.ToString(TextSpan.FromBounds(bodyStart, bodyEnd));
        var replacement = "<![CDATA[" + existing + "]]>";

        SourceText updated = text.WithChanges(new TextChange(TextSpan.FromBounds(bodyStart, bodyEnd), replacement));
        return document.WithText(updated);
    }
}
