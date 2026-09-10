// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Test.Assertions;

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Verifies that a raw record is framed with a little-endian header and its payload copied verbatim.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void WriteRecord_WhenRawPayload_ShouldFrameRecord()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteRecord(0x1234, [0xAA, 0xBB, 0xCC]));

        CollectionAssert.AreEqual(new byte[] { 0x34, 0x12, 0x03, 0x00, 0xAA, 0xBB, 0xCC }, bytes);
    }

    /// <summary>
    /// Verifies that a typed identifier overload frames the record with that identifier.
    /// </summary>
    [TestMethod]
    public void WriteRecord_WhenRecordType_ShouldUseItsIdentifier()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteRecord(BiffRecordType.Eof, default));

        CollectionAssert.AreEqual(new byte[] { 0x0A, 0x00, 0x00, 0x00 }, bytes);
    }

    /// <summary>
    /// Verifies that the byte count advances by the header and payload of each record.
    /// </summary>
    [TestMethod]
    public void WriteRecord_WhenSeveralRecords_ShouldAdvanceBytesCommitted()
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);

        writer.WriteRecord(1, new byte[10]);
        writer.WriteRecord(2, default);

        Assert.AreEqual(18L, writer.BytesCommitted);
        Assert.AreEqual(18, output.WrittenCount);
    }

    /// <summary>
    /// Verifies that a payload at the version's maximum is accepted and one byte over is rejected.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="max">The maximum payload length.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5, 2080)]
    [DataRow(BiffVersion.Biff8, 8224)]
    public void WriteRecord_WhenPayloadAtMaximum_ShouldAcceptAndRejectOneOver(BiffVersion version, int max)
    {
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteRecord(0x0FFE, new byte[max]));
        Assert.AreEqual(4 + max, bytes.Length);

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit(version, (ref BiffWriter w) => w.WriteRecord(0x0FFE, new byte[max + 1])),
            "payload");
    }
}
