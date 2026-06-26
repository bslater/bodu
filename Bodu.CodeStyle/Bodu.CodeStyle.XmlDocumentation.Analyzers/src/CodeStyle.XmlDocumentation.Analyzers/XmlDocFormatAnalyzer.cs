// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatAnalyzer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
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
/// Reports one of the <c>BODU1001</c>–<c>BODU1040</c> diagnostics when an XML documentation comment's formatting
/// differs from the active project policy. Each per-tag rule has its own diagnostic ID so that individual tags can be
/// silenced or re-targeted in <c>.editorconfig</c> independently.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XmlDocFormatAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Gets the property key used to surface the canonical formatted text on the emitted diagnostic.
    /// </summary>
    /// <value>The property name consumed by the code fix provider.</value>
    public static string FormattedTextPropertyKey => "FormattedText";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        DiagnosticDescriptors.FormattingDescriptors;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    /// <summary>
    /// Loads the compilation-wide formatting options and registers the per-tree formatting analysis.
    /// </summary>
    /// <param name="compilationContext">
    /// The compilation-start analysis context used to read additional files and register the tree action.
    /// </param>
    private static void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
    {
        XmlDocFormatOptions compilationOptions = XmlDocConfigurationLoader.LoadCompilationOptions(
            compilationContext.Options.AdditionalFiles,
            compilationContext.CancellationToken);

        var formatter = new XmlDocFormatter();

        compilationContext.RegisterSyntaxTreeAction(treeContext =>
            AnalyzeTree(treeContext, compilationOptions, formatter));
    }

    /// <summary>
    /// Formats every documentation-comment trivia in the syntax tree and emits one diagnostic per attributed formatting
    /// change so each tag and the cross-cutting bucket can be silenced independently.
    /// </summary>
    /// <param name="treeContext">The syntax-tree analysis context for the source file under analysis.</param>
    /// <param name="compilationOptions">The compilation-wide formatting options, before any per-tree overrides.</param>
    /// <param name="formatter">The formatter that produces the canonical rendering of each doc-comment trivia.</param>
    private static void AnalyzeTree(
        SyntaxTreeAnalysisContext treeContext,
        XmlDocFormatOptions compilationOptions,
        XmlDocFormatter formatter)
    {
        if (GeneratedCodeFilters.IsGenerated(treeContext.Tree)) return;

        AnalyzerConfigOptions treeOptions = treeContext.Options.AnalyzerConfigOptionsProvider.GetOptions(treeContext.Tree);
        XmlDocFormatOptions options = XmlDocConfigurationLoader.ApplyEditorConfigOverrides(compilationOptions, treeOptions);
        SourceText sourceText = treeContext.Tree.GetText(treeContext.CancellationToken);
        var lineEnding = XmlDocConfigurationLoader.ResolveLineEnding(treeOptions, sourceText);

        SyntaxNode root = treeContext.Tree.GetRoot(treeContext.CancellationToken);
        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            treeContext.CancellationToken.ThrowIfCancellationRequested();

            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                continue;
            }

            var triviaText = trivia.ToFullString();
            var baseIndent = ResolveBaseIndent(trivia);

            var formatContext = new XmlDocFormatContext(baseIndent, lineEnding, ResolveMemberKind(trivia));
            XmlDocFormatResult result = formatter.FormatTrivia(triviaText, formatContext, options);

            if (!result.Changed) continue;

            ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
                .Add(FormattedTextPropertyKey, result.FormattedText);

            // Use FullSpan to include the leading "///" characters in the diagnostic location so that the
            // editor squiggle covers the full doc trivia text rather than just the structured XML payload.
            var location = Location.Create(treeContext.Tree, trivia.FullSpan);

            // The attributor deduplicates per-tag changes; emit one diagnostic per attributed change so each
            // tag and the cross-cutting bucket can be silenced independently in .editorconfig.
            ImmutableArray<XmlDocFormattingChange> changes = result.Changes;
            if (changes.IsDefaultOrEmpty)
            {
                // Defensive: the formatter reported a change but the attributor produced no records — emit
                // the cross-cutting bucket so the diagnostic surfaces in the editor.
                treeContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.XmlDocCrossCutting, location, properties));
                continue;
            }

            foreach (XmlDocFormattingChange change in changes)
            {
                DiagnosticDescriptor descriptor = DiagnosticDescriptors.ForTag(change.TagName);
                treeContext.ReportDiagnostic(Diagnostic.Create(descriptor, location, properties));
            }
        }
    }

    /// <summary>
    /// Resolves the member-kind hint for the member that the supplied documentation trivia documents.
    /// </summary>
    /// <param name="trivia">The documentation-comment trivia whose owning member is classified.</param>
    /// <returns>
    /// The <see cref="XmlDocMemberKindHint" /> matching the owning member, or
    /// <see cref="XmlDocMemberKindHint.Unknown" /> when no member is found.
    /// </returns>
    private static XmlDocMemberKindHint ResolveMemberKind(SyntaxTrivia trivia)
    {
        SyntaxNode? owner = trivia.Token.Parent?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        return owner switch
        {
            BaseTypeDeclarationSyntax => XmlDocMemberKindHint.Type,
            DelegateDeclarationSyntax => XmlDocMemberKindHint.Method,
            MethodDeclarationSyntax => XmlDocMemberKindHint.Method,
            ConstructorDeclarationSyntax => XmlDocMemberKindHint.Constructor,
            PropertyDeclarationSyntax => XmlDocMemberKindHint.Property,
            IndexerDeclarationSyntax => XmlDocMemberKindHint.Property,
            FieldDeclarationSyntax => XmlDocMemberKindHint.Field,
            EventFieldDeclarationSyntax => XmlDocMemberKindHint.Event,
            EventDeclarationSyntax => XmlDocMemberKindHint.Event,
            _ => XmlDocMemberKindHint.Unknown,
        };
    }

    /// <summary>
    /// Resolves the leading whitespace indentation that precedes the supplied documentation trivia on its line.
    /// </summary>
    /// <param name="trivia">The documentation-comment trivia whose base indent is resolved.</param>
    /// <returns>
    /// The whitespace indent string, or an empty string when the trivia is not preceded by whitespace trivia.
    /// </returns>
    private static string ResolveBaseIndent(SyntaxTrivia trivia)
    {
        SyntaxToken token = trivia.Token;
        SyntaxTriviaList leading = token.LeadingTrivia;
        var index = leading.IndexOf(trivia);
        if (index <= 0) return string.Empty;

        SyntaxTrivia previous = leading[index - 1];
        if (!previous.IsKind(SyntaxKind.WhitespaceTrivia)) return string.Empty;

        return previous.ToFullString();
    }
}
