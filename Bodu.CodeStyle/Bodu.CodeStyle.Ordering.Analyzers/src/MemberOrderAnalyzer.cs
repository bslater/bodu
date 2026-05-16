// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberOrderAnalyzer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using Bodu.CodeStyle.Ordering;
using Bodu.CodeStyle.Ordering.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bodu.CodeStyle.Ordering.Analyzers;

/// <summary>
/// Reports <c>BODUORD001</c> when a type's members are not declared in the canonical Bodu order
/// (constants → static fields → instance fields → constructors → events → properties → methods → nested
/// types, with public-before-private tie-breaking).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MemberOrderAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.MemberOrdering);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(
            AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.InterfaceDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        TypeDeclarationSyntax type = (TypeDeclarationSyntax)context.Node;
        if (type.Members.Count < 2)
        {
            return;
        }

        if (MemberOrderAnalysis.HasPreprocessorDirectives(type))
        {
            return;
        }

        MemberClassifier classifier = new MemberClassifier(DefaultBoduOrderingPolicy.Create());
        if (MemberOrderAnalysis.IsOrdered(type, classifier))
        {
            return;
        }

        Diagnostic diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.MemberOrdering,
            type.Identifier.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
