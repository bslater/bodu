// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.TryReadContinuation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a following CONTINUE record is consumed and becomes current.
    /// </summary>
    [TestMethod]
    public void TryReadContinuation_WhenNextIsContinue_ShouldConsumeIt()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Record(0x01B6, [1]), BiffTestRecords.Continue(2, 3), BiffTestRecords.Eof());
        var reader = new BiffReader(stream);
        Assert.IsTrue(reader.Read());

        Assert.IsTrue(reader.TryReadContinuation(out ReadOnlySpan<byte> payload));

        CollectionAssert.AreEqual(new byte[] { 2, 3 }, payload.ToArray());
        Assert.IsTrue(reader.IsContinuation);
        Assert.AreEqual(11, reader.BytesConsumed);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffRecordType.Eof, reader.RecordType);
    }

    /// <summary>
    /// Verifies that a following record of another type is left unconsumed.
    /// </summary>
    [TestMethod]
    public void TryReadContinuation_WhenNextIsNotContinue_ShouldReturnFalseWithoutConsuming()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Record(0x01B6, [1]), BiffTestRecords.Eof());
        var reader = new BiffReader(stream);
        Assert.IsTrue(reader.Read());

        Assert.IsFalse(reader.TryReadContinuation(out ReadOnlySpan<byte> payload));

        Assert.IsTrue(payload.IsEmpty);
        Assert.AreEqual(0x01B6, reader.RecordId);
        Assert.AreEqual(5, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that the end of the stream yields no continuation.
    /// </summary>
    [TestMethod]
    public void TryReadContinuation_WhenAtEnd_ShouldReturnFalse()
    {
        var reader = new BiffReader(BiffTestRecords.Eof());
        Assert.IsTrue(reader.Read());

        Assert.IsFalse(reader.TryReadContinuation(out _));
    }

    /// <summary>
    /// Verifies that a truncated CONTINUE record in a final block is rejected.
    /// </summary>
    [TestMethod]
    public void TryReadContinuation_WhenContinueOverrunsFinalBlock_ShouldThrowBiffFormatException()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Eof(), [0x3C, 0x00, 0x05, 0x00, 0x01]);

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            var reader = new BiffReader(stream);
            _ = reader.Read();
            _ = reader.TryReadContinuation(out _);
        });
    }

    /// <summary>
    /// Verifies that a truncated CONTINUE record in a non-final block yields <see langword="false" /> and leaves the
    /// position for more data.
    /// </summary>
    [TestMethod]
    public void TryReadContinuation_WhenContinueOverrunsNonFinalBlock_ShouldReturnFalse()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Eof(), [0x3C, 0x00, 0x05, 0x00, 0x01]);
        var reader = new BiffReader(stream, isFinalBlock: false, default);
        Assert.IsTrue(reader.Read());

        Assert.IsFalse(reader.TryReadContinuation(out _));
        Assert.AreEqual(4, reader.BytesConsumed);
    }
}
