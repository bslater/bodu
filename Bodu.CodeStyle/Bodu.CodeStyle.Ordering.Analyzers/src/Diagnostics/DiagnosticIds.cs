// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticIds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.Ordering.Analyzers.Diagnostics;

/// <summary>
/// Provides the diagnostic identifiers reported by the Bodu member-ordering analyzer assembly.
/// </summary>
internal static class DiagnosticIds
{
    /// <summary>
    /// The diagnostic identifier reported when a type's members are not declared in the canonical Bodu order.
    /// </summary>
    public const string MemberOrdering = "BODUORD001";
}
