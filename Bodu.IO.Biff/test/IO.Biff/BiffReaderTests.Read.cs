// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that reading a well-formed stream yields every physical record in order and then ends cleanly.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Read_WhenWellFormedStream_ShouldYieldEveryRecordInOrder()
    {
        byte[] stream = BiffTestRecords.Stream(
            BiffTestRecords.Bof8(),
            BiffTestRecords.Number(0, 0, 1.5),
            BiffTestRecords.Record(0x1234, [1, 2, 3]),
            BiffTestRecords.Eof());
        var reader = new BiffReader(stream);
        var seen = new List<ushort>();

        while (reader.Read())
            seen.Add(reader.RecordId);

        CollectionAssert.AreEqual(new ushort[] { 0x0809, 0x0203, 0x1234, 0x000A }, seen);
        Assert.AreEqual(stream.Length, reader.BytesConsumed);
        Assert.IsFalse(reader.HasRecord);
    }

    /// <summary>
    /// Verifies that a record the codec does not name is readable through its identifier and payload rather than
    /// rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenRecordIsUnknown_ShouldExposeIdAndPayload()
    {
        var reader = new BiffReader(BiffTestRecords.Record(0x0FFE, [0xAA, 0xBB]));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(0x0FFE, reader.RecordId);
        Assert.AreEqual(2, reader.RecordLength);
        Assert.IsFalse(Enum.IsDefined(reader.RecordType));
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB }, reader.ValueSpan.ToArray());
    }

    /// <summary>
    /// Verifies that a zero-length record is yielded with an empty payload.
    /// </summary>
    [TestMethod]
    public void Read_WhenRecordIsEmpty_ShouldYieldEmptyPayload()
    {
        var reader = new BiffReader(BiffTestRecords.Eof());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffRecordType.Eof, reader.RecordType);
        Assert.AreEqual(0, reader.RecordLength);
        Assert.IsTrue(reader.ValueSpan.IsEmpty);
        Assert.AreEqual(4, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that malformed framing in a final block throws <see cref="BiffFormatException" /> carrying the offset
    /// of the offending record.
    /// </summary>
    /// <param name="kat">The malformed stream.</param>
    [TestMethod]
    [DynamicData(nameof(MalformedFraming), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenFramingIsMalformed_ShouldThrowBiffFormatException(InvalidKat<byte[]> kat)
    {
        var ex = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            var reader = new BiffReader(kat.Input);
            while (reader.Read())
            {
            }
        });

        Assert.IsNotNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that with a non-final block an incomplete trailing record returns <see langword="false" /> without
    /// being consumed, so the caller can supply more data.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockIsNotFinalAndRecordIsIncomplete_ShouldReturnFalseWithoutConsuming()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Eof(), BiffTestRecords.Number(0, 0, 1));
        var reader = new BiffReader(stream.AsSpan(0, 10), isFinalBlock: false, default);

        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.Read());
        Assert.AreEqual(4, reader.BytesConsumed);
        Assert.IsFalse(reader.HasRecord);
    }

    /// <summary>
    /// Verifies that with a non-final block a trailing fragment shorter than a header also returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockIsNotFinalAndHeaderIsIncomplete_ShouldReturnFalse()
    {
        var reader = new BiffReader(new byte[] { 0x0A, 0x00 }, isFinalBlock: false, default);

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(0, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that a record declaring the maximum 16-bit length is framed when the buffer holds it.
    /// </summary>
    [TestMethod]
    public void Read_WhenRecordDeclaresMaximumLength_ShouldFrameIt()
    {
        byte[] record = BiffTestRecords.Record(0x0FFE, new byte[ushort.MaxValue]);
        var reader = new BiffReader(record);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(ushort.MaxValue, reader.RecordLength);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that reading past the end leaves the reader with no current record and an unchanged position.
    /// </summary>
    [TestMethod]
    public void Read_WhenCalledAfterEnd_ShouldKeepReturningFalse()
    {
        var reader = new BiffReader(BiffTestRecords.Eof());

        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.Read());
        Assert.IsFalse(reader.Read());
        Assert.AreEqual(4, reader.BytesConsumed);
        Assert.AreEqual(BiffRecordType.None, reader.RecordType);
    }
}
