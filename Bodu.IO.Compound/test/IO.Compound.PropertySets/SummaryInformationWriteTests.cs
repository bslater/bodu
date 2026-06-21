// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SummaryInformationWriteTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;
using Bodu.IO.Compound.Nodes;
using Bodu.Test;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies authoring and embedding of the summary-information and document-summary-information property sets.
/// </summary>
[TestClass]
public class SummaryInformationWriteTests
{
    /// <summary>
    /// Verifies that an authored summary-information set reads back through <see cref="SummaryInformation" />.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void ToPropertySet_WhenAuthored_ShouldReadBackFields()
    {
        var builder = new SummaryInformationBuilder
        {
            Title = "My Title",
            Author = "Ada",
            WordCount = 42,
            CreateTime = new DateTimeOffset(2021, 5, 6, 7, 8, 9, TimeSpan.Zero),
        };

        var summary = new SummaryInformation(OlePropertySet.Parse(builder.ToArray()));

        Assert.AreEqual("My Title", summary.Title);
        Assert.AreEqual("Ada", summary.Author);
        Assert.AreEqual(42, summary.WordCount);
        Assert.AreEqual(new DateTimeOffset(2021, 5, 6, 7, 8, 9, TimeSpan.Zero), summary.CreateTime);
    }

    /// <summary>
    /// Verifies that summary information embedded in a written compound file is read back by the reader.
    /// </summary>
    [TestMethod]
    public void Save_WhenSummaryInformationEmbedded_ShouldBeReadByCompoundFile()
    {
        var builder = new SummaryInformationBuilder { Title = "Report", Author = "Grace" };
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStream(SummaryInformation.StreamName, builder.ToArray());

        using CompoundFile file = CompoundFile.Open(new MemoryStream(root.ToArray()));

        Assert.IsTrue(file.TryGetSummaryInformation(out SummaryInformation? summary));
        Assert.AreEqual("Report", summary.Title);
        Assert.AreEqual("Grace", summary.Author);
    }

    /// <summary>
    /// Verifies that document summary information, including a user-defined custom property, round-trips through a
    /// written compound file.
    /// </summary>
    [TestMethod]
    public void Save_WhenDocumentSummaryInformationEmbedded_ShouldRoundTripCustomProperties()
    {
        var builder = new DocumentSummaryInformationBuilder { Company = "Bodu", SlideCount = 3 };
        builder.AddCustomProperty("Project", OlePropertyValue.Create("Apollo"));

        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStream(DocumentSummaryInformation.StreamName, builder.ToArray());

        using CompoundFile file = CompoundFile.Open(new MemoryStream(root.ToArray()));

        Assert.IsTrue(file.TryGetDocumentSummaryInformation(out DocumentSummaryInformation? summary));
        Assert.AreEqual("Bodu", summary.Company);
        Assert.AreEqual(3, summary.SlideCount);
        Assert.IsTrue(summary.CustomProperties.TryGetValue("Project", out OlePropertyValue? project));
        Assert.AreEqual("Apollo", project.AsString());
    }
}
