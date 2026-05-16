// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticDescriptorsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Test.Diagnostics;

[TestClass]
public sealed class DiagnosticDescriptorsTests
{
    /// <summary>
    /// Verifies that the BODUXML001 descriptor exposes the documented identifier and metadata.
    /// </summary>
    [TestMethod]
    public void XmlDocumentationFormatting_ShouldExposeDocumentedMetadata()
    {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.XmlDocumentationFormatting;

        Assert.AreEqual("BODUXML001", descriptor.Id);
        Assert.AreEqual("Documentation", descriptor.Category);
        Assert.AreEqual(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.IsTrue(descriptor.IsEnabledByDefault);
    }

    /// <summary>
    /// Verifies that the BODUXML001 message format places a count argument in its template.
    /// </summary>
    [TestMethod]
    public void XmlDocumentationFormatting_MessageFormatShouldContainCountPlaceholder()
    {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.XmlDocumentationFormatting;

        StringAssert.Contains(descriptor.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture), "{0}");
    }

    /// <summary>
    /// Verifies that the BODUXML001 identifier constant matches the descriptor's identifier.
    /// </summary>
    [TestMethod]
    public void DiagnosticIds_ShouldMatchDescriptor()
    {
        Assert.AreEqual(DiagnosticIds.XmlDocumentationFormatting, DiagnosticDescriptors.XmlDocumentationFormatting.Id);
    }
}
