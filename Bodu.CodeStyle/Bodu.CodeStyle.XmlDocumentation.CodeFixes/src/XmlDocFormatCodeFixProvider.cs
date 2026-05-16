// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatCodeFixProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// Applies the canonical XML documentation comment formatting computed by
/// <see cref="XmlDocFormatAnalyzer" />.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocFormatCodeFixProvider))]
[Shared]
public sealed class XmlDocFormatCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "BoduFormatXmlDocComment";

    private const string ActionTitle = "Format XML documentation comment";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.XmlDocumentationFormatting);

    /// <inheritdoc />
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

            if (HasPreprocessorDirectiveBetweenTriviaAndMember(trivia))
            {
                // A preprocessor directive between the doc comment and the documented member makes any rewrite
                // unsafe — we cannot guarantee the documented member is the same across configurations. Skip
                // registering a fix for this diagnostic; the squiggle remains, but no code action is offered.
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: ActionTitle,
                    createChangedDocument: cancellationToken => ApplyFixAsync(context.Document, trivia, formattedText, cancellationToken),
                    equivalenceKey: EquivalenceKey),
                diagnostic);
        }
    }

    private static bool HasPreprocessorDirectiveBetweenTriviaAndMember(SyntaxTrivia trivia)
    {
        SyntaxToken token = trivia.Token;
        SyntaxTriviaList leading = token.LeadingTrivia;
        var index = leading.IndexOf(trivia);
        if (index < 0)
        {
            return false;
        }

        for (var i = index + 1; i < leading.Count; i++)
        {
            if (leading[i].IsDirective)
            {
                return true;
            }
        }

        return false;
    }

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
