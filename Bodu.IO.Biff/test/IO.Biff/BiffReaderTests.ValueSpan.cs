// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.ValueSpan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that the payload, header, length, identifier, and start offset describe the current record.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenRecordIsCurrent_ShouldDescribeRecord()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Eof(), BiffTestRecords.Record(0x0203, [1, 2, 3, 4, 5]));
        var reader = new BiffReader(stream);

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, reader.ValueSpan.ToArray());
        Assert.AreEqual(5, reader.RecordLength);
        Assert.AreEqual(0x0203, reader.RecordId);
        Assert.AreEqual(BiffRecordType.Number, reader.RecordType);
        Assert.AreEqual(4, reader.RecordStartIndex);
        Assert.AreEqual(new BiffRecordHeader(0x0203, 5), reader.Header);
        Assert.AreEqual(13, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that the payload is a view over the source bytes rather than a copy.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenRead_ShouldBeSliceOfSource()
    {
        byte[] stream = BiffTestRecords.Record(0x0203, [1, 2, 3]);
        var reader = new BiffReader(stream);
        Assert.IsTrue(reader.Read());

        stream[4] = 9;

        Assert.AreEqual(9, reader.ValueSpan[0]);
    }

    /// <summary>
    /// Verifies that the continuation flag is set only for a CONTINUE record.
    /// </summary>
    [TestMethod]
    public void IsContinuation_WhenRecordIsContinue_ShouldBeTrue()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Eof(), BiffTestRecords.Continue(1));
        var reader = new BiffReader(stream);

        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.IsContinuation);
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.IsContinuation);
    }

    /// <summary>
    /// Verifies that a typed accessor called before any record has been read throws.
    /// </summary>
    [TestMethod]
    public void GetNumber_WhenNoRecordIsCurrent_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Number(0, 0, 1));
            _ = reader.GetNumber();
        });
    }

    /// <summary>
    /// Verifies that a typed accessor called on a record of a different type throws.
    /// </summary>
    [TestMethod]
    public void GetNumber_WhenCurrentRecordIsDifferentType_ShouldThrowInvalidOperationException()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Eof());
            _ = reader.Read();
            _ = reader.GetNumber();
        });

        Assert.IsTrue(ex.Message.Contains("Eof", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("Number", StringComparison.Ordinal));
    }
}
