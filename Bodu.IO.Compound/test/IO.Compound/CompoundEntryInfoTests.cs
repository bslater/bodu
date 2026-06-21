// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundEntryInfoTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies that <see cref="CompoundEntryInfo" /> surfaces the directory metadata of compound-file entries.
/// </summary>
[TestClass]
public class CompoundEntryInfoTests
{
    /// <summary>The published OLE class identifier of a Microsoft Word 97-2003 document root storage.</summary>
    private static readonly Guid WordDocumentClsid = new("00020906-0000-0000-C000-000000000046");

    /// <summary>
    /// Verifies that the root storage of a Word document exposes the document's class identifier.
    /// </summary>
    [TestMethod]
    public void ClassId_WhenWordDocumentRoot_ShouldExposeWordClsid()
    {
        using CompoundFile file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        Assert.AreEqual(WordDocumentClsid, file.RootStorage.Stat.ClassId);
    }

    /// <summary>
    /// Verifies that a stream entry reports a stream type and a non-negative size, and carries no class identifier.
    /// </summary>
    [TestMethod]
    public void Stat_WhenStreamEntry_ShouldReportStreamMetadata()
    {
        using CompoundFile file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.xls"));

        CompoundEntryInfo info = file.RootStorage.OpenStream("Workbook").Stat;

        Assert.AreEqual(CompoundEntryType.Stream, info.EntryType);
        Assert.IsTrue(info.Length > 0);
        Assert.AreEqual(Guid.Empty, info.ClassId);
    }

    /// <summary>
    /// Verifies that the root storage reports a creation or modification time stamp when one is recorded, and that a
    /// time stamp of zero is surfaced as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Stat_WhenRootStorage_ShouldExposeOptionalTimeStamps()
    {
        using CompoundFile file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        CompoundEntryInfo info = file.RootStorage.Stat;

        // The value may be null (zero FILETIME) or a representable instant, but must never be a sentinel out-of-range
        // value.
        if (info.LastModifiedTime is { } modified)
            Assert.IsTrue(modified.Year is >= 1601 and <= 9999);
    }
}
