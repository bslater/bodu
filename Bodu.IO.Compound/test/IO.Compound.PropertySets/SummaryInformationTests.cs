// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SummaryInformationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;
using Bodu.Test;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies that <see cref="SummaryInformation" /> exposes the standard summary metadata of real Office documents.
/// </summary>
[TestClass]
public class SummaryInformationTests
{
    /// <summary>
    /// Reads the summary information from a reference fixture.
    /// </summary>
    /// <param name="relativePath">The corpus-relative fixture path.</param>
    /// <returns>The parsed summary information.</returns>
    private static SummaryInformation Read(string relativePath)
    {
        using CompoundFile file = CompoundFile.Open(CompoundFixtures.OpenReference(relativePath));
        Assert.IsTrue(file.TryGetSummaryInformation(out SummaryInformation? summary));
        return summary;
    }

    /// <summary>
    /// Verifies that the title and author of a Word document are decoded.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void TryGetSummaryInformation_WhenWordDocument_ShouldDecodeTitleAndAuthor()
    {
        SummaryInformation summary = Read("valid/sample1.doc");

        Assert.AreEqual("Sample document created with MS Word", summary.Title);
        Assert.AreEqual("steve", summary.Author);
        Assert.AreEqual(288, summary.WordCount);
    }

    /// <summary>
    /// Verifies that the title and author of an Excel workbook are decoded.
    /// </summary>
    [TestMethod]
    public void TryGetSummaryInformation_WhenExcelWorkbook_ShouldDecodeTitleAndAuthor()
    {
        SummaryInformation summary = Read("valid/sample1.xls");

        Assert.AreEqual("Sample Excel Worksheet", summary.Title);
        Assert.AreEqual("Dr Malcolm Murray", summary.Author);
    }

    /// <summary>
    /// Verifies that the creation time stamp of a document is decoded as a representable instant.
    /// </summary>
    [TestMethod]
    public void TryGetSummaryInformation_WhenWordDocument_ShouldDecodeCreationTime()
    {
        SummaryInformation summary = Read("valid/sample1.doc");

        Assert.IsNotNull(summary.CreateTime);
        Assert.AreEqual(2001, summary.CreateTime.Value.Year);
    }

    /// <summary>
    /// Verifies that a document without a summary-information stream reports its absence.
    /// </summary>
    [TestMethod]
    public void TryGetSummaryInformation_WhenStreamAbsent_ShouldReturnFalse()
    {
        using CompoundFile file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/clean.dat"));

        Assert.IsFalse(file.TryGetSummaryInformation(out _));
    }
}
