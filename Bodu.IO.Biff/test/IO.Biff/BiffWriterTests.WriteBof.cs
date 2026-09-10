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

    /// <summary>
    /// Verifies that the default build and year are zero and every substream type is written verbatim.
    /// </summary>
    /// <param name="substream">The substream type.</param>
    [TestMethod]
    [DataRow(BiffSubstreamType.WorkbookGlobals)]
    [DataRow(BiffSubstreamType.Worksheet)]
    [DataRow(BiffSubstreamType.Chart)]
    [DataRow((BiffSubstreamType)0x0123)]
    public void WriteBof_WhenSubstreamType_ShouldWriteVerbatimWithZeroBuild(BiffSubstreamType substream)
    {
        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteBof(substream));

        BiffBofRecord bof = Single(bytes, BiffVersion.Biff5).GetBof();
        Assert.AreEqual(substream, bof.SubstreamType);
        Assert.AreEqual(0, bof.Build);
        Assert.AreEqual(0, bof.Year);
    }

    /// <summary>
    /// Verifies that the BOF a writer emits establishes the same version in a reader created without options.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteBof_WhenReadWithoutOptions_ShouldEstablishVersion(BiffVersion version)
    {
        byte[] bytes = Emit(version, (ref BiffWriter w) =>
        {
            w.WriteBof(BiffSubstreamType.WorkbookGlobals);
            w.WriteEof();
        });

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(version, reader.Version);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffRecordType.Eof, reader.RecordType);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a second stray EOF after balancing is rejected and the depth never goes negative.
    /// </summary>
    [TestMethod]
    public void WriteEof_WhenCalledTwiceAfterOneBof_ShouldRejectSecond()
    {
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);
        writer.WriteBof(BiffSubstreamType.Worksheet);
        writer.WriteEof();

        bool rejected = false;
        try
        {
            writer.WriteEof();
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Assert.IsTrue(rejected);
        Assert.AreEqual(0, writer.OpenSubstreamDepth);
        Assert.AreEqual(24L, writer.BytesCommitted, "The rejected EOF wrote nothing.");
    }

    /// <summary>
    /// Verifies that CODEPAGE and DATEMODE are written in both versions with their fixed two-byte layout.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteCodePage_WhenAnyVersion_ShouldWriteTwoByteRecords(BiffVersion version)
    {
        byte[] bytes = Emit(version, (ref BiffWriter w) =>
        {
            w.WriteCodePage(1252);
            w.WriteDateMode(false);
        });

        Assert.AreEqual(12, bytes.Length);
        var reader = new BiffReader(bytes, new BiffReaderOptions { Version = version });
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1252, reader.GetCodePage().CodePage);
        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.GetDateMode().Is1904);
    }
}
