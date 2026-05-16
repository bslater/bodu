// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberOrderCodeFixProvider.cs" company="PlaceholderCompany">
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
using Bodu.CodeStyle.Ordering;
using Bodu.CodeStyle.Ordering.Analyzers;
using Bodu.CodeStyle.Ordering.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bodu.CodeStyle.Ordering.CodeFixes;

/// <summary>
/// Reorders type members so they follow the canonical Bodu declaration-order policy.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MemberOrderCodeFixProvider))]
[Shared]
public sealed class MemberOrderCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "BoduOrderMembers";

    private const string ActionTitle = "Order members according to Bodu policy";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.MemberOrdering);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            SyntaxToken identifier = root.FindToken(diagnostic.Location.SourceSpan.Start);
            TypeDeclarationSyntax? type = identifier.Parent as TypeDeclarationSyntax;
            if (type is null) continue;

            if (MemberOrderAnalysis.HasPreprocessorDirectives(type))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: ActionTitle,
                    createChangedDocument: cancellationToken => ApplyFixAsync(context.Document, type, cancellationToken),
                    equivalenceKey: EquivalenceKey),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyFixAsync(Document document, TypeDeclarationSyntax type, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        MemberClassifier classifier = new MemberClassifier(DefaultBoduOrderingPolicy.Create());
        IReadOnlyList<MemberDeclarationSyntax> originalMembers = type.Members;
        List<(MemberDeclarationSyntax Member, (int GroupRank, int AccessibilityRank, int OriginalIndex) Key)> annotated =
            new List<(MemberDeclarationSyntax, (int, int, int))>(originalMembers.Count);
        for (int i = 0; i < originalMembers.Count; i++)
        {
            annotated.Add((originalMembers[i], MemberOrderAnalysis.ResolveOrderKey(originalMembers[i], i, classifier)));
        }

        annotated.Sort((a, b) =>
        {
            int g = a.Key.GroupRank.CompareTo(b.Key.GroupRank);
            if (g != 0) return g;

            int acc = a.Key.AccessibilityRank.CompareTo(b.Key.AccessibilityRank);
            if (acc != 0) return acc;

            return a.Key.OriginalIndex.CompareTo(b.Key.OriginalIndex);
        });

        string indent = ResolveMemberIndent(originalMembers);
        List<MemberDeclarationSyntax> sortedMembers = annotated.Select(pair => pair.Member).ToList();
        List<MemberDeclarationSyntax> normalized = new List<MemberDeclarationSyntax>(sortedMembers.Count);
        for (int i = 0; i < sortedMembers.Count; i++)
        {
            normalized.Add(NormalizeMemberLeadingTrivia(sortedMembers[i], isFirst: i == 0, indent));
        }

        TypeDeclarationSyntax reorderedType = type.WithMembers(SyntaxFactory.List(normalized));
        SyntaxNode newRoot = root.ReplaceNode(type, reorderedType);
        return document.WithSyntaxRoot(newRoot);
    }

    private static string ResolveMemberIndent(IReadOnlyList<MemberDeclarationSyntax> members)
    {
        foreach (MemberDeclarationSyntax member in members)
        {
            SyntaxTriviaList leading = member.GetLeadingTrivia();
            for (int i = leading.Count - 1; i >= 0; i--)
            {
                if (leading[i].IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    return leading[i].ToFullString();
                }

                if (!leading[i].IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    break;
                }
            }
        }

        return "    ";
    }

    private static MemberDeclarationSyntax NormalizeMemberLeadingTrivia(MemberDeclarationSyntax member, bool isFirst, string indent)
    {
        SyntaxTriviaList leading = member.GetLeadingTrivia();
        int firstSignificant = -1;
        for (int i = 0; i < leading.Count; i++)
        {
            if (!leading[i].IsKind(SyntaxKind.WhitespaceTrivia) && !leading[i].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                firstSignificant = i;
                break;
            }
        }

        SyntaxTriviaList significant = firstSignificant < 0
            ? SyntaxTriviaList.Empty
            : SyntaxFactory.TriviaList(leading.Skip(firstSignificant));

        // Roslyn places end-of-line characters as TRAILING trivia of the previous token (the `{` or the previous
        // member's `}`). For the first member we therefore emit only the indent; for non-first members we add
        // one extra newline so that the previous `\r\n` + our `\r\n` renders a single blank-line gap.
        // Significant trivia (doc comments, attributes) is preserved with the indent inserted before it so the
        // first physical line of the member declaration is correctly indented.
        List<SyntaxTrivia> rebuilt = new List<SyntaxTrivia>();
        if (!isFirst)
        {
            rebuilt.Add(SyntaxFactory.EndOfLine("\r\n"));
        }

        rebuilt.Add(SyntaxFactory.Whitespace(indent));

        if (significant.Count > 0)
        {
            rebuilt.AddRange(significant);
            if (!significant.Last().IsKind(SyntaxKind.WhitespaceTrivia))
            {
                rebuilt.Add(SyntaxFactory.Whitespace(indent));
            }
        }

        return member.WithLeadingTrivia(SyntaxFactory.TriviaList(rebuilt));
    }
}
