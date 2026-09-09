// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a newly constructed reader has no current record and reports the unknown version.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldHaveNoCurrentRecord()
    {
        var reader = new BiffReader(BiffTestRecords.Bof8());

        Assert.IsFalse(reader.HasRecord);
        Assert.AreEqual(BiffRecordType.None, reader.RecordType);
        Assert.AreEqual(0, reader.RecordId);
        Assert.AreEqual(0, reader.RecordLength);
        Assert.AreEqual(0, reader.BytesConsumed);
        Assert.AreEqual(BiffVersion.Unknown, reader.Version);
        Assert.IsTrue(reader.IsFinalBlock);
        Assert.IsTrue(reader.ValueSpan.IsEmpty);
    }

    /// <summary>
    /// Verifies that options seed the version and code page before any record has been read.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionsSupplied_ShouldSeedVersionAndCodePage()
    {
        var reader = new BiffReader(BiffTestRecords.Eof(), new BiffReaderOptions { Version = BiffVersion.Biff5, CodePage = 850 });

        Assert.AreEqual(BiffVersion.Biff5, reader.Version);
        Assert.AreEqual(850, reader.CodePage);
    }

    /// <summary>
    /// Verifies that the default code page is Windows-1252 when neither options nor a CODEPAGE record supply one.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNoCodePage_ShouldDefaultTo1252()
    {
        var reader = new BiffReader(BiffTestRecords.Eof());

        Assert.AreEqual(BiffLimits.DefaultCodePage, reader.CodePage);
    }

    /// <summary>
    /// Verifies that a reader continued from a captured state carries the established version and code page.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenContinuedFromState_ShouldCarryVersionAndCodePage()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.CodePage(1251), BiffTestRecords.Eof());
        var first = new BiffReader(stream.AsSpan(0, 26), isFinalBlock: false, default);
        Assert.IsTrue(first.Read());
        Assert.IsTrue(first.Read());
        Assert.IsFalse(first.Read(), "The EOF header is not within the first block.");

        var second = new BiffReader(stream.AsSpan(first.BytesConsumed), isFinalBlock: true, first.CurrentState);

        Assert.AreEqual(BiffVersion.Biff8, second.Version);
        Assert.AreEqual(1251, second.CodePage);
        Assert.IsTrue(second.Read());
        Assert.AreEqual(BiffRecordType.Eof, second.RecordType);
        Assert.IsFalse(second.Read());
    }

    /// <summary>
    /// Verifies that an empty buffer yields no records without error.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEmpty_ShouldReadNothing()
    {
        var reader = new BiffReader(ReadOnlySpan<byte>.Empty);

        Assert.IsFalse(reader.Read());
        Assert.IsFalse(reader.HasRecord);
    }
}
