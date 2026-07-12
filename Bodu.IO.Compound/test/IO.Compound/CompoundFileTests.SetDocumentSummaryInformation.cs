// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.SetDocumentSummaryInformation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that a document-summary set written to a new file reads back through
    /// <see cref="CompoundFile.TryGetDocumentSummaryInformation(out DocumentSummaryInformation)" /> after commit and
    /// reopen.
    /// </summary>
    [TestMethod]
    public void SetDocumentSummaryInformation_WhenWritable_ShouldRoundTripThroughReopen()
    {
        var builder = new DocumentSummaryInformationBuilder
        {
            Company = "Bodu Pty. Ltd.",
            LineCount = 42,
        };
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.SetDocumentSummaryInformation(new DocumentSummaryInformation(builder.ToPropertySet()));
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.IsTrue(reopened.TryGetDocumentSummaryInformation(out DocumentSummaryInformation? summary));
        Assert.AreEqual("Bodu Pty. Ltd.", summary.Company);
        Assert.AreEqual(42, summary.LineCount);
    }

    /// <summary>
    /// Verifies that vector-valued properties — the heading-pair and titles-of-parts shape real documents carry —
    /// survive the write-back path end to end.
    /// </summary>
    [TestMethod]
    public void SetDocumentSummaryInformation_WhenVectorProperties_ShouldRoundTrip()
    {
        OlePropertySet set = new DocumentSummaryInformationBuilder().ToPropertySet();
        set.Sections[0].Set(12, new OlePropertyValue
        {
            Type = OlePropertyType.Variant,
            IsVector = true,
            Value = new object?[] { "Worksheets", 2 },
        });
        set.Sections[0].Set(13, new OlePropertyValue
        {
            Type = OlePropertyType.AnsiString,
            IsVector = true,
            Value = new object?[] { "Sheet1", "Sheet2" },
        });
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.SetDocumentSummaryInformation(new DocumentSummaryInformation(set));
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.IsTrue(reopened.TryGetDocumentSummaryInformation(out DocumentSummaryInformation? summary));
        Assert.IsTrue(summary.PropertySet.TryGetValue(12, out OlePropertyValue? headingPairs));
        CollectionAssert.AreEqual(new object?[] { "Worksheets", 2 }, (object?[])headingPairs.Value!);
        Assert.IsTrue(summary.PropertySet.TryGetValue(13, out OlePropertyValue? titles));
        CollectionAssert.AreEqual(new object?[] { "Sheet1", "Sheet2" }, (object?[])titles.Value!);
    }

    /// <summary>
    /// Verifies that writing document-summary information on a read-only file throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void SetDocumentSummaryInformation_WhenReadOnly_ShouldThrowInvalidOperationException()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            file.SetDocumentSummaryInformation(new DocumentSummaryInformation(new DocumentSummaryInformationBuilder().ToPropertySet()));
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> summary throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void SetDocumentSummaryInformation_WhenNull_ShouldThrowArgumentNullException()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            file.SetDocumentSummaryInformation(null!);
        });

        Assert.AreEqual("summary", ex.ParamName);
    }
}
