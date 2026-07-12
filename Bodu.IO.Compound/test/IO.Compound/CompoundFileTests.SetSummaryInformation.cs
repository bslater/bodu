// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.SetSummaryInformation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.PropertySets;
using Bodu.Test;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that a summary-information set written to a new file reads back through
    /// <see cref="CompoundFile.TryGetSummaryInformation(out SummaryInformation)" /> after commit and reopen.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void SetSummaryInformation_WhenWritable_ShouldRoundTripThroughReopen()
    {
        var builder = new SummaryInformationBuilder
        {
            Title = "Quarterly Report",
            Author = "Bodu",
            PageCount = 12,
        };
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.SetSummaryInformation(new SummaryInformation(builder.ToPropertySet()));
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.IsTrue(reopened.TryGetSummaryInformation(out SummaryInformation? summary));
        Assert.AreEqual("Quarterly Report", summary.Title);
        Assert.AreEqual("Bodu", summary.Author);
        Assert.AreEqual(12, summary.PageCount);
    }

    /// <summary>
    /// Verifies that a summary-information set read from an existing document can be modified and written back in
    /// update mode.
    /// </summary>
    [TestMethod]
    public void SetSummaryInformation_WhenReadModifyWrite_ShouldPersistUpdatedValues()
    {
        var buffer = new MemoryStream();
        buffer.Write(CompoundFixtures.ReadReferenceBytes("valid/sample1.doc"));
        buffer.Position = 0;

        using (var file = CompoundFile.Open(buffer, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            Assert.IsTrue(file.TryGetSummaryInformation(out SummaryInformation? original));
            original.PropertySet.Sections[0].Set(2, OlePropertyValue.Create("Updated Title"));
            file.SetSummaryInformation(original);
            file.Commit();
        }

        buffer.Position = 0;
        using var reopened = CompoundFile.Open(buffer);

        Assert.IsTrue(reopened.TryGetSummaryInformation(out SummaryInformation? summary));
        Assert.AreEqual("Updated Title", summary.Title);
    }

    /// <summary>
    /// Verifies that writing summary information on a read-only file throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void SetSummaryInformation_WhenReadOnly_ShouldThrowInvalidOperationException()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            file.SetSummaryInformation(new SummaryInformation(new SummaryInformationBuilder().ToPropertySet()));
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> summary throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void SetSummaryInformation_WhenNull_ShouldThrowArgumentNullException()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            file.SetSummaryInformation(null!);
        });

        Assert.AreEqual("summary", ex.ParamName);
    }

    /// <summary>
    /// Verifies that writing summary information on a disposed file throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void SetSummaryInformation_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        using var destination = new MemoryStream();
        var file = CompoundFile.Create(destination, leaveOpen: true);
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            file.SetSummaryInformation(new SummaryInformation(new SummaryInformationBuilder().ToPropertySet()));
        });
    }
}
