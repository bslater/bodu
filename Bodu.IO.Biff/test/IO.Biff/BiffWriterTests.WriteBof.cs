// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteBof.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Verifies that a BIFF8 BOF record is sixteen bytes carrying the BIFF8 marker and substream type.
    /// </summary>
    [TestMethod]
    public void WriteBof_WhenBiff8_ShouldWriteSixteenByteRecord()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteBof(BiffSubstreamType.Worksheet, build: 0x1234, year: 0x07CC));

        BiffReader reader = Single(bytes);
        Assert.AreEqual(16, reader.RecordLength);
        BiffBofRecord bof = reader.GetBof();
        Assert.AreEqual(BiffVersion.Biff8, bof.Version);
        Assert.AreEqual(BiffSubstreamType.Worksheet, bof.SubstreamType);
        Assert.AreEqual(0x1234, bof.Build);
        Assert.AreEqual(0x07CC, bof.Year);
        Assert.AreEqual(6u, bof.LowestSaveVersion);
    }

    /// <summary>
    /// Verifies that a BIFF5 BOF record is eight bytes carrying the BIFF5 marker.
    /// </summary>
    [TestMethod]
    public void WriteBof_WhenBiff5_ShouldWriteEightByteRecord()
    {
        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteBof(BiffSubstreamType.WorkbookGlobals));

        BiffReader reader = Single(bytes, BiffVersion.Biff5);
        Assert.AreEqual(8, reader.RecordLength);
        Assert.AreEqual(BiffVersion.Biff5, reader.GetBof().Version);
    }

    /// <summary>
    /// Verifies that BOF and EOF track the open substream depth and that a stray EOF is rejected.
    /// </summary>
    [TestMethod]
    public void WriteEof_WhenBalanced_ShouldTrackDepthAndRejectStrayEof()
    {
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);

        writer.WriteBof(BiffSubstreamType.WorkbookGlobals);
        writer.WriteBof(BiffSubstreamType.Chart);
        Assert.AreEqual(2, writer.OpenSubstreamDepth);
        writer.WriteEof();
        writer.WriteEof();
        Assert.AreEqual(0, writer.OpenSubstreamDepth);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var stray = new BiffWriter(new System.Buffers.ArrayBufferWriter<byte>(), BiffVersion.Biff8);
            stray.WriteEof();
        });
    }

    /// <summary>
    /// Verifies that the CODEPAGE and DATEMODE records round-trip through the reader.
    /// </summary>
    [TestMethod]
    public void WriteCodePage_WhenWritten_ShouldRoundTripWithDateMode()
    {
        byte[] bytes = Emit8((ref BiffWriter w) =>
        {
            w.WriteCodePage(0x8000);
            w.WriteDateMode(true);
        });

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(0x8000, reader.GetCodePage().RawValue);
        Assert.AreEqual(10000, reader.CodePage);
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.GetDateMode().Is1904);
    }
}
