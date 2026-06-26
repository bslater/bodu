// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatCodeFixProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bodu.CodeStyle.XmlDocumentation.Analyzers;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Bodu.CodeStyle.XmlDocumentation.CodeFixes;

/// <summary>
/// Applies the canonical XML documentation comment formatting computed by <see cref="XmlDocFormatAnalyzer" />.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocFormatCodeFixProvider))]
[Shared]
public sealed class XmlDocFormatCodeFixProvider : CodeFixProvider
{
    /// <summary>The equivalence key identifying the format fix for Fix All batching.</summary>
    private const string EquivalenceKey = "BoduFormatXmlDocComment";

    /// <summary>The code-action title presented for the format fix.</summary>
    private const string ActionTitle = "Format XML documentation comment";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DiagnosticIds.XmlDocSummary,
            DiagnosticIds.XmlDocRemarks,
            DiagnosticIds.XmlDocPara,
            DiagnosticIds.XmlDocExample,
            DiagnosticIds.XmlDocCode,
            DiagnosticIds.XmlDocList,
            DiagnosticIds.XmlDocItem,
            DiagnosticIds.XmlDocDescription,
            DiagnosticIds.XmlDocTerm,
            DiagnosticIds.XmlDocParam,
            DiagnosticIds.XmlDocTypeParam,
            DiagnosticIds.XmlDocReturns,
            DiagnosticIds.XmlDocException,
            DiagnosticIds.XmlDocValue,
            DiagnosticIds.XmlDocInlineCode,
            DiagnosticIds.XmlDocSee,
            DiagnosticIds.XmlDocParamRef,
            DiagnosticIds.XmlDocTypeParamRef,
            DiagnosticIds.XmlDocCrossCutting);

    /// <inheritdoc />
    /// <remarks>
    /// The formatting analyzer reports one diagnostic per changed tag, so a single documentation comment can carry
    /// several diagnostics at the same trivia. Each of those diagnostics, however, carries the identical whole-comment
    /// replacement (the canonical formatted text), so the batch fixer's merge collapses them to one edit — there is no
    /// overlapping or conflicting replacement. The
    /// <c>CodeFix_WhenTwoTagsMisformattedInOneComment_FixAll_ShouldReformatOnce</c> test guards this.
    /// </remarks>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(XmlDocFormatAnalyzer.FormattedTextPropertyKey, out var formattedText) || formattedText is null)
            {
                continue;
            }

            TextSpan span = diagnostic.Location.SourceSpan;
            SyntaxTrivia trivia = root.FindTrivia(span.Start, findInsideTrivia: false);
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                trivia = root.FindTrivia(span.Start, findInsideTrivia: true);
                if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    continue;
                }
            }

            // Preprocessor directives between the doc comment and the documented member are not a barrier
            // to the fix. The rewrite is a deterministic reformat of the doc trivia text driven only by
            // the trivia content, the surrounding indentation, and the configured wrap width — none of
            // which depend on which member the trivia ultimately attaches to under any given build
            // configuration. Replacing the trivia at its source position preserves correctness under all
            // configurations, including when a `#if`/`#elif`/`#else`/`#endif` block selects between
            // alternative declarations of the same member.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: ActionTitle,
                    createChangedDocument: cancellationToken => ApplyFixAsync(context.Document, trivia, formattedText, cancellationToken),
                    equivalenceKey: EquivalenceKey),
                diagnostic);
        }
    }

    /// <summary>
    /// Replaces the documentation-comment trivia with the canonical formatted text computed by the analyzer, rebuilding
    /// the owning token's leading trivia around the replacement.
    /// </summary>
    /// <param name="document">The document to reformat.</param>
    /// <param name="trivia">The documentation-comment trivia to replace.</param>
    /// <param name="formattedText">The canonical formatted documentation-comment text.</param>
    /// <param name="cancellationToken">
    /// A token that propagates notification that the operation should be canceled.
    /// </param>
    /// <returns>The updated document, or the original document when the replacement cannot be applied.</returns>
    private static async Task<Document> ApplyFixAsync(Document document, SyntaxTrivia trivia, string formattedText, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        SyntaxTriviaList replacement = SyntaxFactory.ParseLeadingTrivia(formattedText);
        if (replacement.Count == 0)
        {
            return document;
        }

        SyntaxToken token = trivia.Token;
        SyntaxTriviaList leading = token.LeadingTrivia;
        var index = leading.IndexOf(trivia);
        if (index < 0) return document;

        IEnumerable<SyntaxTrivia> rebuilt = leading.Take(index).Concat(replacement).Concat(leading.Skip(index + 1));
        SyntaxToken updatedToken = token.WithLeadingTrivia(rebuilt);

        SyntaxNode newRoot = root.ReplaceToken(token, updatedToken);
        return document.WithSyntaxRoot(newRoot);
    }
}
