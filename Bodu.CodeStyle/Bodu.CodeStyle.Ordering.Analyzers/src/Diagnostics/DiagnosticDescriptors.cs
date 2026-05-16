// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticDescriptors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Bodu.CodeStyle.Ordering.Analyzers.Diagnostics;

/// <summary>
/// Provides the <see cref="DiagnosticDescriptor" /> instances surfaced by the Bodu member-ordering analyzer.
/// </summary>
internal static class DiagnosticDescriptors
{
    /// <summary>
    /// Gets the descriptor for <c>BODUORD001</c> — members are not declared in the canonical Bodu order.
    /// </summary>
    public static DiagnosticDescriptor MemberOrdering { get; } = new DiagnosticDescriptor(
        id: DiagnosticIds.MemberOrdering,
        title: "Type members are not ordered according to Bodu declaration order policy",
        messageFormat: "Type members are not ordered according to Bodu declaration order policy",
        category: "Ordering",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Reorders type members to match the Bodu code-style declaration order policy.",
        helpLinkUri: "https://github.com/bodu/bodu/blob/master/Bodu.CodeStyle/README.md#boduord001");
}
