// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteContinuedRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Verifies that a payload within the maximum is written as one record.
    /// </summary>
    [TestMethod]
    public void WriteContinuedRecord_WhenPayloadFits_ShouldWriteSingleRecord()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteContinuedRecord(0x01B6, new byte[100]));

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(0x01B6, reader.RecordId);
        Assert.AreEqual(100, reader.RecordLength);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an oversized payload is split at the maximum into the record and CONTINUE records that
    /// concatenate back to the original.
    /// </summary>
    [TestMethod]
    public void WriteContinuedRecord_WhenPayloadExceedsMaximum_ShouldSplitIntoContinues()
    {
        byte[] payload = new byte[(2 * BiffLimits.Biff5MaxPayloadLength) + 7];
        for (int i = 0; i < payload.Length; i++)
            payload[i] = (byte)i;

        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteContinuedRecord(0x01B6, payload));

        var reader = new BiffReader(bytes);
        var reassembled = new List<byte>();
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(0x01B6, reader.RecordId);
        Assert.AreEqual(BiffLimits.Biff5MaxPayloadLength, reader.RecordLength);
        reassembled.AddRange(reader.ValueSpan.ToArray());

        int continues = 0;
        while (reader.TryReadContinuation(out ReadOnlySpan<byte> part))
        {
            continues++;
            reassembled.AddRange(part.ToArray());
        }

        Assert.AreEqual(2, continues);
        CollectionAssert.AreEqual(payload, reassembled);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an empty payload is written as a single empty record.
    /// </summary>
    [TestMethod]
    public void WriteContinuedRecord_WhenPayloadIsEmpty_ShouldWriteSingleEmptyRecord()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteContinuedRecord(0x01B6, default));

        CollectionAssert.AreEqual(new byte[] { 0xB6, 0x01, 0x00, 0x00 }, bytes);
    }

    /// <summary>
    /// Verifies that a payload exactly at the maximum is a single record, and one byte over yields a one-byte
    /// continuation, under each version.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteContinuedRecord_WhenPayloadAtMaximum_ShouldSplitOnlyBeyondIt(BiffVersion version)
    {
        int max = BiffLimits.GetMaxPayloadLength(version);

        byte[] exact = Emit(version, (ref BiffWriter w) => w.WriteContinuedRecord(0x01B6, new byte[max]));
        Assert.AreEqual(4 + max, exact.Length);

        byte[] over = Emit(version, (ref BiffWriter w) => w.WriteContinuedRecord(0x01B6, new byte[max + 1]));
        var reader = new BiffReader(over);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(max, reader.RecordLength);
        Assert.IsTrue(reader.TryReadContinuation(out ReadOnlySpan<byte> tail));
        Assert.AreEqual(1, tail.Length);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that the continued record advances the byte count by every record it emits.
    /// </summary>
    [TestMethod]
    public void WriteContinuedRecord_WhenSplit_ShouldCountEveryRecord()
    {
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);

        writer.WriteContinuedRecord(0x01B6, new byte[(BiffLimits.Biff8MaxPayloadLength * 2) + 1]);

        Assert.AreEqual((3 * 4) + (BiffLimits.Biff8MaxPayloadLength * 2) + 1, writer.BytesCommitted);
        Assert.AreEqual(writer.BytesCommitted, output.WrittenCount);
    }
}
