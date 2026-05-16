// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticIds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;

/// <summary>
/// Provides the well-known Roslyn diagnostic identifiers reported by this analyzer assembly.
/// </summary>
internal static class DiagnosticIds
{
    /// <summary>
    /// The diagnostic identifier reported when an XML documentation comment is not formatted according to the
    /// active project policy.
    /// </summary>
    public const string XmlDocumentationFormatting = "BODUXML001";
}
