// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PositionedDiagnosticCollectionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies how <see cref="PositionedDiagnosticCollection" /> gives the diagnostics of a load the positions of the nodes
/// and declarations under examination.
/// </summary>
[TestClass]
public sealed partial class PositionedDiagnosticCollectionTests
{
    /// <summary>The namespace of the nodes the tests read.</summary>
    private static readonly XNamespace s_ns = "urn:bodu:globalization:calendar";

    /// <summary>
    /// Returns a node for the element <c>b</c>, whose name stands at line 2, position 4 of its document.
    /// </summary>
    /// <returns>A node read with line information.</returns>
    private static XmlDocumentNode PositionedNode()
    {
        var document = XDocument.Parse("<a>\n  <b />\n</a>", LoadOptions.SetLineInfo);
        return new XmlDocumentNode(document.Root!.Element("b")!, s_ns);
    }

    /// <summary>
    /// Returns a node for an element read without line information.
    /// </summary>
    /// <returns>A node that has no position.</returns>
    private static XmlDocumentNode UnpositionedNode() =>
        new(XDocument.Parse("<a><b /></a>").Root!, s_ns);

    /// <summary>
    /// Returns a diagnostic that carries no position.
    /// </summary>
    /// <returns>An error diagnostic.</returns>
    private static NotableDateValidationDiagnostic Diagnostic() =>
        new(NotableDateValidationSeverity.Error, "BODU-CAL-TEST", "something failed");
}
