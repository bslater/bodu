// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticDescriptorsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// Verifies that the BODU1001 (summary) descriptor exposes the documented identifier and metadata.
    /// </summary>
    [TestMethod]
    public void XmlDocSummary_ShouldExposeDocumentedMetadata()
    {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.XmlDocSummary;

        Assert.AreEqual("BODU1001", descriptor.Id);
        Assert.AreEqual("Documentation", descriptor.Category);
        Assert.AreEqual(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.IsTrue(descriptor.IsEnabledByDefault);
    }

    /// <summary>
    /// Verifies that every tag-attributed descriptor's message format mentions the project policy.
    /// </summary>
    [TestMethod]
    public void All_MessageFormatShouldMentionProjectPolicy()
    {
        foreach (DiagnosticDescriptor descriptor in DiagnosticDescriptors.All)
        {
            StringAssert.Contains(descriptor.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture), "project policy");
        }
    }

    /// <summary>
    /// Verifies that the BODU1001 identifier constant matches the summary descriptor's identifier.
    /// </summary>
    [TestMethod]
    public void DiagnosticIds_ShouldMatchDescriptor()
    {
        Assert.AreEqual(DiagnosticIds.XmlDocSummary, DiagnosticDescriptors.XmlDocSummary.Id);
    }

    /// <summary>
    /// Verifies that <see cref="DiagnosticDescriptors.ForTag(string)" /> returns the matching per-tag
    /// descriptor for every known tag name and the cross-cutting descriptor for unknown / null inputs.
    /// </summary>
    [TestMethod]
    public void ForTag_ShouldReturnMatchingDescriptor()
    {
        Assert.AreSame(DiagnosticDescriptors.XmlDocSummary, DiagnosticDescriptors.ForTag("summary"));
        Assert.AreSame(DiagnosticDescriptors.XmlDocSee, DiagnosticDescriptors.ForTag("see"));
        Assert.AreSame(DiagnosticDescriptors.XmlDocParamRef, DiagnosticDescriptors.ForTag("paramref"));
        Assert.AreSame(DiagnosticDescriptors.XmlDocCrossCutting, DiagnosticDescriptors.ForTag(tagName: null));
        Assert.AreSame(DiagnosticDescriptors.XmlDocCrossCutting, DiagnosticDescriptors.ForTag("unknownTag"));
    }

    /// <summary>
    /// Verifies that the <see cref="DiagnosticDescriptors.All" /> collection includes one descriptor per
    /// supported diagnostic ID.
    /// </summary>
    [TestMethod]
    public void All_ShouldContainEveryKnownDescriptor()
    {
        Assert.AreEqual(21, DiagnosticDescriptors.All.Length);
    }

    /// <summary>
    /// Verifies that the BODU1405 (code-requires-CDATA) descriptor exposes the documented identifier and metadata.
    /// </summary>
    [TestMethod]
    public void XmlDocCodeRequiresCData_ShouldExposeDocumentedMetadata()
    {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.XmlDocCodeRequiresCData;

        Assert.AreEqual("BODU1405", descriptor.Id);
        Assert.AreEqual("Documentation", descriptor.Category);
        Assert.AreEqual(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.IsTrue(descriptor.IsEnabledByDefault);
    }

    /// <summary>
    /// Verifies that the BODU1406 (typeparam-requires-short-content) descriptor exposes the documented
    /// identifier and metadata.
    /// </summary>
    [TestMethod]
    public void XmlDocTypeParamRequiresShortContent_ShouldExposeDocumentedMetadata()
    {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.XmlDocTypeParamRequiresShortContent;

        Assert.AreEqual("BODU1406", descriptor.Id);
        Assert.AreEqual("Documentation", descriptor.Category);
        Assert.AreEqual(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.IsTrue(descriptor.IsEnabledByDefault);
    }
}
