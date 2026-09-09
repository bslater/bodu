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
}
