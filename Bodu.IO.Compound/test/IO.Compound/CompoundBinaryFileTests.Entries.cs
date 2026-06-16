// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundBinaryFileTests.Entries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.IO.Compound;

public partial class CompoundBinaryFileTests
{
    /// <summary>
    /// Verifies that the directory of the sample workbook exposes the root storage entry.
    /// </summary>
    [TestMethod]
    public void Entries_WhenSampleOpened_ShouldExposeRootStorage()
    {
        using CompoundBinaryFile file = OpenSample();

        CompoundDirectoryEntry? root = file.Entries.SingleOrDefault(e => e.Type == CompoundDirectoryEntryType.RootStorage);

        Assert.IsNotNull(root);
        Assert.AreEqual("Root Entry", root.Name);
    }

    /// <summary>
    /// Verifies that the directory of the sample workbook exposes the <c>Workbook</c> stream with its declared size.
    /// </summary>
    [TestMethod]
    public void Entries_WhenSampleOpened_ShouldExposeWorkbookStreamWithDeclaredSize()
    {
        using CompoundBinaryFile file = OpenSample();

        CompoundDirectoryEntry workbook = file.Entries.Single(e => e.Name == "Workbook");

        Assert.AreEqual(CompoundDirectoryEntryType.Stream, workbook.Type);
        Assert.AreEqual(461504L, workbook.Size);
    }

    /// <summary>
    /// Verifies that the directory exposes both OLE summary-information streams, whose names carry a leading 0x05
    /// control prefix that the reader preserves verbatim.
    /// </summary>
    [TestMethod]
    public void Entries_WhenSampleOpened_ShouldExposeSummaryInformationStreams()
    {
        using CompoundBinaryFile file = OpenSample();

        int summaryStreamCount = file.Entries.Count(e => e.Name.Contains("SummaryInformation", StringComparison.Ordinal));

        Assert.AreEqual(2, summaryStreamCount);
    }
}
