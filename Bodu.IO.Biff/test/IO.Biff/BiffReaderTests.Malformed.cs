// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that every typed accessor rejects a payload too short for its layout with
    /// <see cref="BiffFormatException" /> rather than an indexing exception.
    /// </summary>
    /// <param name="kat">The truncated record.</param>
    [TestMethod]
    [DynamicData(nameof(TruncatedKnownRecords), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Accessors_WhenPayloadIsTruncated_ShouldThrowBiffFormatException(InvalidKat<byte[]> kat)
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), kat.Input);

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            var reader = new BiffReader(stream);
            _ = reader.Read();
            _ = reader.Read();
            InvokeAccessor(ref reader);
        });
    }

    /// <summary>
    /// Verifies that a truncated payload is still traversable: the reader frames it and moves on, only the accessor
    /// rejects it.
    /// </summary>
    /// <param name="kat">The truncated record.</param>
    [TestMethod]
    [DynamicData(nameof(TruncatedKnownRecords), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenKnownRecordPayloadIsTruncated_ShouldStillFrameRecord(InvalidKat<byte[]> kat)
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), kat.Input, BiffTestRecords.Eof());
        var reader = new BiffReader(stream);

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffRecordType.Eof, reader.RecordType);
    }
}
