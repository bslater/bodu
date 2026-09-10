// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSstReaderTests.Current.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffSstReaderTests
{
    /// <summary>
    /// Verifies that a contiguous string exposes a span-backed view with its trailers.
    /// </summary>
    [TestMethod]
    public void Current_WhenStringIsContiguous_ShouldExposeView()
    {
        var reader = new BiffReader(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("rich", richRuns: 1, extendedSize: 2)));
        Assert.IsTrue(reader.Read());
        var strings = new BiffSstReader(ref reader);
        Assert.IsTrue(strings.Read(ref reader));

        BiffString current = strings.Current;

        Assert.AreEqual("rich", current.GetString());
        Assert.AreEqual(4, current.Length);
        Assert.AreEqual(1, current.RichRunCount);
        Assert.AreEqual(4, current.RichRuns.Length);
        Assert.IsTrue(current.HasExtendedData);
        Assert.AreEqual(2, current.ExtendedData.Length);
        Assert.AreEqual(2 + 1 + 2 + 4 + 4 + 4 + 2, current.EncodedLength);
        Assert.IsTrue(strings.HasRichRuns);
        Assert.IsTrue(strings.HasExtendedData);
    }

    /// <summary>
    /// Verifies that a fragmented string has no contiguous view.
    /// </summary>
    [TestMethod]
    public void Current_WhenStringIsFragmented_ShouldThrowInvalidOperationException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, [0x02, 0x00, Compressed, (byte)'A']), BiffTestRecords.Continue(Compressed, (byte)'B'));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(stream);
            while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
            {
            }

            var strings = new BiffSstReader(ref reader);
            _ = strings.Read(ref reader);
            _ = strings.Current;
        });
    }

    /// <summary>
    /// Verifies that the view and the per-string properties are unavailable before the first read.
    /// </summary>
    [TestMethod]
    public void Current_WhenNoStringIsCurrent_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("a")));
            _ = reader.Read();
            var strings = new BiffSstReader(ref reader);
            _ = strings.Current;
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("a")));
            _ = reader.Read();
            var strings = new BiffSstReader(ref reader);
            _ = strings.Length;
        });
    }

    /// <summary>
    /// Verifies that every per-string member rejects a reader with no current string.
    /// </summary>
    /// <param name="member">The member to invoke.</param>
    [TestMethod]
    [DataRow("IsFragmented")]
    [DataRow("HasRichRuns")]
    [DataRow("HasExtendedData")]
    [DataRow("GetString")]
    [DataRow("CopyTo")]
    public void Current_WhenNoStringIsCurrent_ShouldThrowFromEveryMember(string member)
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("a")));
            _ = reader.Read();
            var strings = new BiffSstReader(ref reader);
            switch (member)
            {
                case "IsFragmented": _ = strings.IsFragmented; break;
                case "HasRichRuns": _ = strings.HasRichRuns; break;
                case "HasExtendedData": _ = strings.HasExtendedData; break;
                case "GetString": _ = strings.GetString(); break;
                default: _ = strings.CopyTo(new char[4]); break;
            }
        });
    }

    /// <summary>
    /// Verifies that once the table is exhausted the per-string members reject the reader again.
    /// </summary>
    [TestMethod]
    public void Current_WhenTableExhausted_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("a")));
            _ = reader.Read();
            var strings = new BiffSstReader(ref reader);
            Assert.IsTrue(strings.Read(ref reader));
            Assert.IsFalse(strings.Read(ref reader));
            _ = strings.GetString();
        });
    }

    /// <summary>
    /// Verifies that the view of a contiguous string reports the header flags exactly as declared.
    /// </summary>
    [TestMethod]
    public void Current_WhenPlainString_ShouldReportNoTrailers()
    {
        var reader = new BiffReader(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("plain")));
        Assert.IsTrue(reader.Read());
        var strings = new BiffSstReader(ref reader);
        Assert.IsTrue(strings.Read(ref reader));

        BiffString current = strings.Current;

        Assert.IsFalse(strings.HasRichRuns);
        Assert.IsFalse(strings.HasExtendedData);
        Assert.IsFalse(current.HasRichRuns);
        Assert.IsFalse(current.HasExtendedData);
        Assert.IsFalse(current.IsHighByte);
        Assert.AreEqual(5, strings.Length);
        Assert.AreEqual(2 + 1 + 5, current.EncodedLength);
    }
}
