// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteSst.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Reads every string of the first SST in the stream through the reader.
    /// </summary>
    /// <param name="bytes">The stream bytes.</param>
    /// <param name="recordCount">When this method returns, the number of physical records in the stream.</param>
    /// <returns>The strings.</returns>
    private static List<string> ReadSst(byte[] bytes, out int recordCount)
    {
        recordCount = 0;
        var counter = new BiffReader(bytes);
        while (counter.Read())
            recordCount++;

        var reader = new BiffReader(bytes, new BiffReaderOptions { Version = BiffVersion.Biff8 });
        Assert.IsTrue(reader.Read());
        var strings = new BiffSstReader(ref reader);
        var result = new List<string>();
        while (strings.Read(ref reader))
            result.Add(strings.GetString());

        return result;
    }

    /// <summary>
    /// Verifies that a small table is written as a single SST record with its counts.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenTableFits_ShouldWriteSingleRecord()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(["one", "two", "日本"], totalReferenceCount: 9));

        List<string> strings = ReadSst(bytes, out int records);
        Assert.AreEqual(1, records);
        CollectionAssert.AreEqual(new[] { "one", "two", "日本" }, strings);

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(9u, reader.GetSstHeader().TotalCount);
        Assert.AreEqual(3u, reader.GetSstHeader().UniqueCount);
    }

    /// <summary>
    /// Verifies that an empty table writes the counts only.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenEmpty_ShouldWriteCountsOnly()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(default));

        BiffReader reader = Single(bytes);
        Assert.AreEqual(8, reader.RecordLength);
        Assert.IsEmpty(ReadSst(bytes, out _));
    }

    /// <summary>
    /// Verifies that many strings overflow into CONTINUE records at string boundaries and read back intact.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void WriteSst_WhenManyStrings_ShouldContinueAtStringBoundaries()
    {
        string[] strings = [.. Enumerable.Range(0, 3000).Select(i => $"string-{i:D5}")];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        List<string> read = ReadSst(bytes, out int records);
        Assert.IsGreaterThan(1, records);
        CollectionAssert.AreEqual(strings, read);
    }

    /// <summary>
    /// Verifies that a string longer than a record is split mid-characters with a flags byte opening each
    /// continuation, for both compressed and 16-bit text.
    /// </summary>
    /// <param name="wide">Whether to use a character outside the 8-bit range.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void WriteSst_WhenStringExceedsRecord_ShouldSplitCharacters(bool wide)
    {
        string prefix = wide ? "日" : "a";
        string longString = prefix + new string('b', 20000);
        string[] strings = ["head", longString, "tail"];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        List<string> read = ReadSst(bytes, out int records);
        Assert.IsGreaterThan(2, records);
        CollectionAssert.AreEqual(strings, read);
    }

    /// <summary>
    /// Verifies that a string header never straddles a record boundary: when fewer than three bytes remain, the
    /// header opens a new record.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenHeaderWouldStraddle_ShouldStartNewRecord()
    {
        // Fill the first record so exactly two bytes remain before the next header.
        int fill = BiffLimits.Biff8MaxPayloadLength - 8 - 3 - 2;
        string[] strings = [new string('x', fill), "y", "z"];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffLimits.Biff8MaxPayloadLength - 2, reader.RecordLength);
        CollectionAssert.AreEqual(strings, ReadSst(bytes, out _));
    }

    /// <summary>
    /// Verifies that the table is rejected under BIFF5.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenBiff5_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => Emit5((ref BiffWriter w) => w.WriteSst(["a"])));
    }

    /// <summary>
    /// Verifies that a null string in the table is rejected.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenStringIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => Emit8((ref BiffWriter w) => w.WriteSst(["a", null!])));
    }
}
