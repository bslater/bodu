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
}
