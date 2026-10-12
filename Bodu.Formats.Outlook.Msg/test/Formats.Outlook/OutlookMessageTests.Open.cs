// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>
    /// Verifies that a message read from a <see cref="MemoryStream" /> over a segment of a larger array, between filler
    /// bytes, is read from the segment's start rather than from the start of the array.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsAnArraySegment_ShouldReadFromItsStart()
    {
        using MemoryStream container = new MsgFixtureBuilder().AddUnicode(MapiPropertyIds.Subject, "Segment").Build();
        byte[] message = container.ToArray();
        var buffer = new byte[message.Length + 2048];
        Array.Fill(buffer, (byte)0xA5);
        message.CopyTo(buffer, 1024);

        using var segment = new MemoryStream(buffer, 1024, message.Length, writable: false, publiclyVisible: true);
        using var opened = OutlookMessage.Open(segment, new OutlookMessageReaderOptions());

        Assert.AreEqual("Segment", opened.Subject);
    }

    /// <summary>
    /// Verifies that a source that is not a compound file at all - empty, or without the compound-file signature - is
    /// reported as not being a compound file, with the container's exception as the inner exception.
    /// </summary>
    /// <param name="testName">The source, by name.</param>
    /// <param name="length">The length of the zero-filled source.</param>
    /// <param name="category">The category the container reports.</param>
    [TestMethod]
    [DataRow("Empty", 0, CompoundFileError.TruncatedFile)]
    [DataRow("ZeroFilled", 1024, CompoundFileError.InvalidSignature)]
    public void Open_WhenSourceIsNotACompoundFile_ShouldReportItIsNotACompoundFile(
        string testName,
        int length,
        CompoundFileError category)
    {
        using var source = new MemoryStream(new byte[length]);

        var ex = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = OutlookMessage.Open(source, new OutlookMessageReaderOptions());
        });

        Assert.AreEqual(OutlookMsgResourceStrings.Format_Invalid_MsgNotCompound, ex.Message, testName);
        Assert.IsInstanceOfType<CompoundFileFormatException>(ex.InnerException, testName);
        Assert.AreEqual(category, ((CompoundFileFormatException)ex.InnerException).Category, testName);
    }

    /// <summary>
    /// Verifies that a compound file with a broken sector chain - the FAT entry that starts the chain holding the
    /// property stream points past the end of the file - is reported as a malformed container rather than as not being
    /// a compound file, with the container's exception as the inner exception.
    /// </summary>
    [TestMethod]
    public void Open_WhenCompoundFileHasABrokenSectorChain_ShouldReportAMalformedContainer()
    {
        using var source = new MemoryStream(CreateWithBrokenSectorChain());

        var ex = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = OutlookMessage.Open(source, new OutlookMessageReaderOptions());
        });

        Assert.AreNotEqual(OutlookMsgResourceStrings.Format_Invalid_MsgNotCompound, ex.Message);
        Assert.IsTrue(ex.Message.Contains("malformed", StringComparison.Ordinal), ex.Message);
        Assert.IsInstanceOfType<CompoundFileFormatException>(ex.InnerException);
        Assert.AreEqual(CompoundFileError.SectorOutOfRange, ((CompoundFileFormatException)ex.InnerException).Category);
    }

    /// <summary>
    /// Builds a minimal message whose root property stream, stored in the mini stream, can no longer be reached: the
    /// FAT entry that starts the root entry's mini-stream chain points at a sector past the end of the file.
    /// </summary>
    /// <returns>The corrupted file bytes.</returns>
    private static byte[] CreateWithBrokenSectorChain()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal().Build();
        byte[] file = container.ToArray();

        // MS-CFB 2.2: the header gives the sector shift (offset 30), the first directory sector (48) and the first FAT
        // sector (76); sector n starts at (n + 1) * sector size, the root entry is the first directory entry, and its
        // starting sector (entry offset 116) begins the mini-stream chain.
        int sectorSize = 1 << BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(30));
        int directory = checked((int)(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(48)) + 1) * sectorSize);
        int fat = checked((int)(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(76)) + 1) * sectorSize);
        uint miniStreamStart = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(directory + 116));
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(fat + checked((int)miniStreamStart * 4)), 0x00FFFFF0);

        return file;
    }

    /// <summary>
    /// Verifies that a compound file whose root has no <c>__properties_version1.0</c> stream is reported as missing
    /// that stream, the one case the missing-stream message describes, with no inner exception.
    /// </summary>
    [TestMethod]
    public void Open_WhenRootHasNoPropertyStream_ShouldReportTheMissingStream()
    {
        using MemoryStream container = new MsgFixtureBuilder().OmitPropertiesStream().Build();

        var ex = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = OutlookMessage.Open(container, new OutlookMessageReaderOptions());
        });

        Assert.AreEqual(OutlookMsgResourceStrings.Format_Invalid_MsgContainer, ex.Message);
        Assert.IsNull(ex.InnerException);
    }
}
