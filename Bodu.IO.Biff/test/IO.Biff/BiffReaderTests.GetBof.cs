// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetBof.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF8 BOF record decodes its version, substream type, build fields, and history flags.
    /// </summary>
    [TestMethod]
    public void GetBof_WhenBiff8_ShouldDecodeAllFields()
    {
        var reader = new BiffReader(BiffTestRecords.Bof8(BiffSubstreamType.Worksheet));
        Assert.IsTrue(reader.Read());

        BiffBofRecord bof = reader.GetBof();

        Assert.AreEqual(0x0600, bof.RawVersion);
        Assert.AreEqual(BiffVersion.Biff8, bof.Version);
        Assert.AreEqual(BiffSubstreamType.Worksheet, bof.SubstreamType);
        Assert.AreEqual(0x0DBB, bof.Build);
        Assert.AreEqual(0x07CC, bof.Year);
        Assert.AreEqual(0x000040C1u, bof.FileHistoryFlags);
        Assert.AreEqual(0x00000206u, bof.LowestSaveVersion);
    }

    /// <summary>
    /// Verifies that a BIFF5 BOF record decodes its eight-byte layout with zeroed BIFF8-only fields.
    /// </summary>
    [TestMethod]
    public void GetBof_WhenBiff5_ShouldDecodeEightByteLayout()
    {
        var reader = new BiffReader(BiffTestRecords.Bof5(BiffSubstreamType.Chart));
        Assert.IsTrue(reader.Read());

        BiffBofRecord bof = reader.GetBof();

        Assert.AreEqual(BiffVersion.Biff5, bof.Version);
        Assert.AreEqual(BiffSubstreamType.Chart, bof.SubstreamType);
        Assert.AreEqual(0u, bof.FileHistoryFlags);
        Assert.AreEqual(0u, bof.LowestSaveVersion);
    }

    /// <summary>
    /// Verifies that a BOF record carrying only the version and substream type decodes with zero for the later fields
    /// and still establishes the version.
    /// </summary>
    [TestMethod]
    public void GetBof_WhenPayloadIsMinimal_ShouldDecodeWithZeroTrailingFields()
    {
        var reader = new BiffReader(BiffTestRecords.Record(BiffRecordType.Bof, [0x00, 0x06, 0x10, 0x00]));
        Assert.IsTrue(reader.Read());

        BiffBofRecord bof = reader.GetBof();

        Assert.AreEqual(BiffVersion.Biff8, reader.Version);
        Assert.AreEqual(BiffSubstreamType.Worksheet, bof.SubstreamType);
        Assert.AreEqual(0, bof.Build);
        Assert.AreEqual(0, bof.Year);
        Assert.AreEqual(0u, bof.FileHistoryFlags);
        Assert.AreEqual(0u, bof.LowestSaveVersion);
    }

    /// <summary>
    /// Verifies that a BOF record whose payload lies between the BIFF5 and BIFF8 lengths decodes the fields it
    /// carries and zero for the rest.
    /// </summary>
    [TestMethod]
    public void GetBof_WhenPayloadIsTwelveBytes_ShouldDecodeHistoryButNotLowestVersion()
    {
        byte[] payload = new byte[12];
        payload[1] = 0x06;
        payload[2] = 0x05;
        payload[8] = 0xC1;
        var reader = new BiffReader(BiffTestRecords.Record(BiffRecordType.Bof, payload));
        Assert.IsTrue(reader.Read());

        BiffBofRecord bof = reader.GetBof();

        Assert.AreEqual(0xC1u, bof.FileHistoryFlags);
        Assert.AreEqual(0u, bof.LowestSaveVersion);
    }

    /// <summary>
    /// Verifies that an unrecognized substream type is preserved as its raw value rather than mapped or rejected.
    /// </summary>
    [TestMethod]
    public void GetBof_WhenSubstreamTypeIsUnknown_ShouldPreserveRawValue()
    {
        var reader = new BiffReader(BiffTestRecords.Bof(BiffTestRecords.Biff8Marker, (BiffSubstreamType)0x0123));
        Assert.IsTrue(reader.Read());

        Assert.AreEqual((BiffSubstreamType)0x0123, reader.GetBof().SubstreamType);
    }
}
