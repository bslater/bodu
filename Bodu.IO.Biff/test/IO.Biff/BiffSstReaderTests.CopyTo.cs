// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSstReaderTests.CopyTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffSstReaderTests
{
    /// <summary>
    /// Verifies that a contiguous string is copied into a caller buffer.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenContiguous_ShouldCopyCharacters()
    {
        var reader = new BiffReader(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("copy", wide: true)));
        Assert.IsTrue(reader.Read());
        var strings = new BiffSstReader(ref reader);
        Assert.IsTrue(strings.Read(ref reader));
        Span<char> buffer = stackalloc char[8];

        int written = strings.CopyTo(buffer);

        Assert.AreEqual(4, written);
        Assert.AreEqual("copy", new string(buffer.Slice(0, written)));
    }

    /// <summary>
    /// Verifies that a fragmented string is copied from the scratch buffer.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenFragmented_ShouldCopyStitchedCharacters()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, [0x03, 0x00, Compressed, (byte)'x']), BiffTestRecords.Continue(Compressed, (byte)'y', (byte)'z'));
        var reader = new BiffReader(stream);
        while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
        {
        }

        var strings = new BiffSstReader(ref reader);
        Assert.IsTrue(strings.Read(ref reader));
        Span<char> buffer = stackalloc char[3];

        Assert.AreEqual(3, strings.CopyTo(buffer));
        Assert.AreEqual("xyz", new string(buffer));
    }

    /// <summary>
    /// Verifies that a destination too small for the string is rejected.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("long")));
            _ = reader.Read();
            var strings = new BiffSstReader(ref reader);
            _ = strings.Read(ref reader);
            _ = strings.CopyTo(new char[2]);
        });
    }

    /// <summary>
    /// Verifies that a destination exactly the string's length is accepted for both contiguous and fragmented
    /// strings.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIsExactLength_ShouldCopy()
    {
        byte[] stream = Table(BiffTestRecords.Sst(2, 2, BiffTestRecords.UnicodeString("ab"), [0x02, 0x00, Compressed, (byte)'c']), BiffTestRecords.Continue(Compressed, (byte)'d'));
        var reader = new BiffReader(stream);
        while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
        {
        }

        var strings = new BiffSstReader(ref reader);
        Span<char> buffer = stackalloc char[2];

        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual(2, strings.CopyTo(buffer));
        Assert.AreEqual("ab", new string(buffer));
        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual(2, strings.CopyTo(buffer));
        Assert.AreEqual("cd", new string(buffer));
    }

    /// <summary>
    /// Verifies that an empty string copies nothing and accepts an empty destination.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenStringIsEmpty_ShouldWriteNothing()
    {
        var reader = new BiffReader(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString(string.Empty)));
        Assert.IsTrue(reader.Read());
        var strings = new BiffSstReader(ref reader);
        Assert.IsTrue(strings.Read(ref reader));

        Assert.AreEqual(0, strings.CopyTo(Span<char>.Empty));
    }

    /// <summary>
    /// Verifies that a fragmented string's copy rejects a destination one character too short.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenFragmentedAndDestinationTooSmall_ShouldThrowArgumentException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, [0x03, 0x00, Compressed, (byte)'x']), BiffTestRecords.Continue(Compressed, (byte)'y', (byte)'z'));

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var reader = new BiffReader(stream);
            while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
            {
            }

            var strings = new BiffSstReader(ref reader);
            _ = strings.Read(ref reader);
            _ = strings.CopyTo(new char[2]);
        });
    }
}
