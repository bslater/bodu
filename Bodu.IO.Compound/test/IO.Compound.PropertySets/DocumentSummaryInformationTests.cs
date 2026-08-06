// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentSummaryInformationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies that <see cref="DocumentSummaryInformation" /> exposes the extended metadata of real Office documents,
/// including user-defined custom properties.
/// </summary>
[TestClass]
public partial class DocumentSummaryInformationTests
{
    /// <summary>
    /// Reads the document summary information from a reference fixture.
    /// </summary>
    /// <param name="relativePath">The corpus-relative fixture path.</param>
    /// <returns>The parsed document summary information.</returns>
    private static DocumentSummaryInformation Read(string relativePath)
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference(relativePath));
        Assert.IsTrue(file.TryGetDocumentSummaryInformation(out DocumentSummaryInformation? summary));
        return summary;
    }

    /// <summary>
    /// Verifies that the company of a Word document is decoded.
    /// </summary>
    [TestMethod]
    public void TryGetDocumentSummaryInformation_WhenWordDocument_ShouldDecodeCompany()
    {
        DocumentSummaryInformation summary = Read("valid/sample1.doc");

        Assert.AreEqual("ndg", summary.Company);
    }

    /// <summary>
    /// Verifies that the company of an Excel workbook is decoded.
    /// </summary>
    [TestMethod]
    public void TryGetDocumentSummaryInformation_WhenExcelWorkbook_ShouldDecodeCompany()
    {
        DocumentSummaryInformation summary = Read("valid/sample1.xls");

        Assert.AreEqual("Durham University", summary.Company);
    }

    /// <summary>
    /// Verifies that a document carrying a user-defined section exposes its custom named properties.
    /// </summary>
    [TestMethod]
    public void CustomProperties_WhenUserDefinedSectionPresent_ShouldExposeNamedProperties()
    {
        DocumentSummaryInformation summary = Read("valid/sample2.doc");

        Assert.AreEqual("Sun Microsystems", summary.Company);
        Assert.IsGreaterThanOrEqualTo(1, summary.CustomProperties.Count);
    }
}
