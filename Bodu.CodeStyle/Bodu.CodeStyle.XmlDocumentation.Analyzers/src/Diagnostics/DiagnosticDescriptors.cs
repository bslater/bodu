// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticDescriptors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;

/// <summary>
/// Provides the <see cref="DiagnosticDescriptor" /> instances surfaced by the Bodu XML documentation analyzer.
/// </summary>
internal static class DiagnosticDescriptors
{
    /// <summary>
    /// Gets the descriptor for <c>BODU1001</c> — XML documentation comment formatting differs from policy.
    /// </summary>
    public static DiagnosticDescriptor XmlDocumentationFormatting { get; } = new DiagnosticDescriptor(
        id: DiagnosticIds.XmlDocumentationFormatting,
        title: "XML documentation comment is not formatted according to Bodu style",
        messageFormat: "XML documentation comment formatting differs from project policy",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reformats XML documentation comments to match the Bodu code-style policy.",
        helpLinkUri: "https://github.com/bodu/bodu/blob/master/Bodu.CodeStyle/README.md#bodu1001");
}
