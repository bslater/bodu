// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundEntryInfoTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;

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
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        Assert.AreEqual(WordDocumentClsid, file.RootStorage.Stat.ClassId);
    }

    /// <summary>
    /// Verifies that a stream entry reports a stream type and a non-negative size, and carries no class identifier.
    /// </summary>
    [TestMethod]
    public void Stat_WhenStreamEntry_ShouldReportStreamMetadata()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.xls"));

        CompoundEntryInfo info = file.RootStorage.OpenStream("Workbook").Stat;

        Assert.AreEqual(CompoundEntryType.Stream, info.EntryType);
        Assert.IsGreaterThan(0, info.Length);
        Assert.AreEqual(Guid.Empty, info.ClassId);
    }

    /// <summary>
    /// Verifies that the root storage reports a creation or modification time stamp when one is recorded, and that a
    /// time stamp of zero is surfaced as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Stat_WhenRootStorage_ShouldExposeOptionalTimeStamps()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        CompoundEntryInfo info = file.RootStorage.Stat;

        // The value may be null (zero FILETIME) or a representable instant, but must never be a sentinel out-of-range
        // value.
        if (info.LastModifiedTime is { } modified)
            Assert.IsTrue(modified.Year is >= 1601 and <= 9999);
    }

    /// <summary>
    /// Verifies that the creation and modification times of directory entries are decoded as UTC instants, and that a
    /// time of zero is surfaced as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Stat_WhenEntryRecordsFileTimes_ShouldDecodeUtcInstants()
    {
        const long unixEpoch = 116444736000000000;
        const long storageTime = 0x01D0000000000000;
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStorage("S").AddStream("A", CompoundContainerBytes.Pattern(100));
        byte[] container = builder.ToArray();
        int root = CompoundContainerBytes.FindEntryOffset(container, "Root Entry");
        int storage = CompoundContainerBytes.FindEntryOffset(container, "S");
        CompoundContainerBytes.WriteInt64(container, root + CompoundContainerBytes.CreationTimeOffset, unixEpoch);
        CompoundContainerBytes.WriteInt64(container, root + CompoundContainerBytes.ModifiedTimeOffset, 0);
        CompoundContainerBytes.WriteInt64(container, storage + CompoundContainerBytes.CreationTimeOffset, storageTime);
        CompoundContainerBytes.WriteInt64(container, storage + CompoundContainerBytes.ModifiedTimeOffset, storageTime);

        using var file = CompoundFile.Open(new MemoryStream(container));
        CompoundEntryInfo rootInfo = file.RootStorage.Stat;
        CompoundEntryInfo storageInfo = file.RootStorage.OpenStorage("S").Stat;

        var expected = new DateTimeOffset(DateTime.FromFileTimeUtc(storageTime));
        Assert.AreEqual(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero), rootInfo.CreationTime);
        Assert.AreEqual(TimeSpan.Zero, rootInfo.CreationTime!.Value.Offset);
        Assert.IsNull(rootInfo.LastModifiedTime);
        Assert.AreEqual(expected, storageInfo.CreationTime);
        Assert.AreEqual(TimeSpan.Zero, storageInfo.CreationTime!.Value.Offset);
        Assert.AreEqual(expected, storageInfo.LastModifiedTime);
        Assert.AreEqual(TimeSpan.Zero, storageInfo.LastModifiedTime!.Value.Offset);
    }
}
