// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocCodeRequiresCDataCodeFixProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
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
/// Provides code fixes for <c>BODU1405</c> — repairs <c>&lt;code&gt;</c> documentation elements so they begin with a
/// <c>&lt;![CDATA[…]]&gt;</c> section flush against the <c>///</c> doc-comment prefix.
/// </summary>
/// <remarks>
/// Three fixes are registered depending on the violating shape:
/// <list type="bullet">
/// <item>
/// <description>
/// Removes stray whitespace between <c>///</c> and <c>&lt;![CDATA[</c> when the CDATA is present but offset by a space
/// or tab.
/// </description>
/// </item>
/// <item>
/// <description>
/// Rewrites a self-closing <c>&lt;code /&gt;</c> as a multi-line <c>&lt;code&gt;</c> element whose body is an empty
/// <c>&lt;![CDATA[…]]&gt;</c> block, with the opener and closer each on their own line flush against <c>///</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Wraps the existing body of a non-empty <c>&lt;code&gt;</c> element in a multi-line <c>&lt;![CDATA[…]]&gt;</c> block,
/// placing the opener and closer on their own lines flush against <c>///</c> and preserving the existing body content
/// between them.
/// </description>
/// </item>
/// </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocCodeRequiresCDataCodeFixProvider))]
[Shared]
public sealed class XmlDocCodeRequiresCDataCodeFixProvider : CodeFixProvider
{
    /// <summary>The code-action title for the fix that removes whitespace between <c>///</c> and <c>&lt;![CDATA[</c>.</summary>
    private const string RemoveSpaceTitle = "Remove space between /// and <![CDATA[";

    /// <summary>The code-action title for the fixes that wrap a <c>&lt;code&gt;</c> body in a <c>&lt;![CDATA[…]]&gt;</c> block.</summary>
    private const string WrapTitle = "Wrap <code> body in <![CDATA[…]]>";

    /// <summary>The equivalence key identifying the remove-leading-whitespace fix for Fix All batching.</summary>
    private const string RemoveSpaceEquivalenceKey = "BoduRemoveSpaceBeforeXmlDocCData";

    /// <summary>The equivalence key identifying the wrap-body-in-CDATA fixes for Fix All batching.</summary>
    private const string WrapEquivalenceKey = "BoduWrapXmlDocCodeBodyInCData";

    /// <summary>The documentation-comment prefix, including the trailing space, used when emitting body lines.</summary>
    private const string DocCommentPrefix = "/// ";

    /// <summary>The documentation-comment prefix without a trailing space, used for blank body lines and flush markers.</summary>
    private const string DocCommentPrefixNoSpace = "///";

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

    /// <summary>
    /// Removes the run of whitespace between the <c>///</c> prefix and an existing <c>&lt;![CDATA[</c> section so the
    /// CDATA opener sits flush against the doc-comment prefix.
    /// </summary>
    /// <param name="document">The document to repair.</param>
    /// <param name="cdata">The CDATA section that is offset from the prefix.</param>
    /// <param name="cancellationToken">
    /// A token that propagates notification that the operation should be canceled.
    /// </param>
    /// <returns>The updated document, or the original document when there is no whitespace to remove.</returns>
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

    /// <summary>
    /// Rewrites a self-closing <c>&lt;code /&gt;</c> element as a multi-line <c>&lt;code&gt;</c> element whose body is
    /// an empty <c>&lt;![CDATA[…]]&gt;</c> block, with the opener and closer each on their own line flush against the
    /// <c>///</c> prefix.
    /// </summary>
    /// <param name="document">The document to repair.</param>
    /// <param name="element">The self-closing element to rewrite.</param>
    /// <param name="cancellationToken">
    /// A token that propagates notification that the operation should be canceled.
    /// </param>
    /// <returns>The updated document containing the expanded multi-line element.</returns>
    private static async Task<Document> ReplaceSelfClosingWithCDataAsync(Document document, XmlEmptyElementSyntax element, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var localName = element.Name.LocalName.ValueText;

        var lineEnding = DocCommentSource.ResolveLineEnding(text);
        var indent = DocCommentSource.ExtractIndent(text, element.SpanStart);

        var sb = new StringBuilder();
        sb.Append('<').Append(localName).Append('>');
        sb.Append(lineEnding);
        sb.Append(indent).Append("///<![CDATA[");
        sb.Append(lineEnding);
        sb.Append(indent).Append("///]]>");
        sb.Append(lineEnding);
        sb.Append(indent).Append("/// </").Append(localName).Append('>');

        SourceText updated = text.WithChanges(new TextChange(element.Span, sb.ToString()));
        return document.WithText(updated);
    }

    /// <summary>
    /// Wraps the existing body of a non-empty <c>&lt;code&gt;</c> element in a multi-line <c>&lt;![CDATA[…]]&gt;</c>
    /// block, placing the opener and closer on their own lines flush against the <c>///</c> prefix and re-emitting each
    /// logical body line under the standard prefix.
    /// </summary>
    /// <param name="document">The document to repair.</param>
    /// <param name="element">The <c>&lt;code&gt;</c> element whose body is wrapped.</param>
    /// <param name="cancellationToken">
    /// A token that propagates notification that the operation should be canceled.
    /// </param>
    /// <returns>The updated document, or the original document when the element body span is degenerate.</returns>
    private static async Task<Document> WrapElementBodyInCDataAsync(Document document, XmlElementSyntax element, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var bodyStart = element.StartTag.Span.End;
        var bodyEnd = element.EndTag.Span.Start;
        if (bodyStart > bodyEnd) return document;

        var lineEnding = DocCommentSource.ResolveLineEnding(text);
        var indent = DocCommentSource.ExtractIndent(text, element.StartTag.SpanStart);

        var existing = text.ToString(TextSpan.FromBounds(bodyStart, bodyEnd));
        IReadOnlyList<string> bodyLines = XmlDocProseText.ToLogicalBodyLines(existing);

        // Compose the multi-line CDATA replacement: opener and closer each on their own line flush
        // against `///`. Every logical body line is re-emitted under the standard `/// ` prefix, so the
        // physical doc-comment prefixes that introduced each source line are never embedded into — nor
        // duplicated within — the CDATA content. Blank body lines collapse to a bare `///` with no
        // trailing whitespace.
        var sb = new StringBuilder();
        sb.Append(lineEnding);
        sb.Append(indent).Append("///<![CDATA[");
        sb.Append(lineEnding);
        foreach (var line in bodyLines)
        {
            sb.Append(line.Length == 0 ? indent + DocCommentPrefixNoSpace : indent + DocCommentPrefix + line);
            sb.Append(lineEnding);
        }

        sb.Append(indent).Append("///]]>");
        sb.Append(lineEnding);
        sb.Append(indent).Append(DocCommentPrefix);

        SourceText updated = text.WithChanges(new TextChange(TextSpan.FromBounds(bodyStart, bodyEnd), sb.ToString()));
        return document.WithText(updated);
    }
}
