// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Resumable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.Kat;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the tests of the resumable reading contract of <see cref="Utf8DelimitedReader" />: an input read in blocks,
/// each block's reader resuming from the state of the one before, reports exactly what a single read of the whole input
/// reports.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// A document that exercises what the resumable reader must survive being split inside: a byte-order mark, CRLF, LF
    /// and lone CR line endings, a quoted field holding the delimiter, doubled quotes and a line break, a comment line,
    /// a blank line, multi-byte UTF-8, and a last record without a line ending.
    /// </summary>
    private static readonly byte[] s_tortureDocument =
    [
        0xEF, 0xBB, 0xBF,
        .. Encoding.UTF8.GetBytes(
            "name,city,note\r\n" +
            "\"Smith, J\",\"Boston \"\"MA\"\"\",\"line one\nline two\"\r\n" +
            "# a comment line\n" +
            "\n" +
            "Doe,Zürich,café ☃\r" +
            "x,\"\",\"\"\"\"\n" +
            "last,row,without ending"),
    ];

    /// <summary>
    /// Verifies that a block that ends inside a record makes <see cref="Utf8DelimitedReader.Read" /> return
    /// <see langword="false" /> with the reader left as it was: the previous token is still current, and
    /// <see cref="Utf8DelimitedReader.BytesConsumed" /> ends at the start of the record.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockEndsInsideARecord_ShouldReturnFalseAndLeaveTheRecordUnread()
    {
        var reader = new Utf8DelimitedReader("a,b\nc,\"d"u8, isFinalBlock: false, new DelimitedReaderState(new DelimitedReaderOptions { NoHeader = true }));

        while (reader.TokenType != DelimitedTokenType.EndArray)
            Assert.IsTrue(reader.Read());

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(DelimitedTokenType.EndArray, reader.TokenType);
        Assert.AreEqual(4, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that a carriage return that ends a block that is not the last is left for the next block, which may
    /// begin with the line feed of a CRLF, so the pair counts as one line ending.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockEndsWithACarriageReturn_ShouldWaitForTheByteAfterIt()
    {
        byte[] input = "a\r\nb\n"u8.ToArray();

        List<string> expected = DelimitedReaderTranscript.ReadWhole(input, new DelimitedReaderOptions { NoHeader = true });
        List<string> split = DelimitedReaderTranscript.ReadInBlocks(input, new DelimitedReaderOptions { NoHeader = true }, [2]);

        CollectionAssert.AreEqual(expected, split);
    }

    /// <summary>
    /// Verifies that a quote that ends a block that is not the last is left for the next block, which may begin with
    /// the second quote of a doubled pair.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockEndsWithAQuote_ShouldWaitForTheByteAfterIt()
    {
        var options = new DelimitedReaderOptions { NoHeader = true };
        var first = new Utf8DelimitedReader("\"a\""u8, isFinalBlock: false, new DelimitedReaderState(options));

        Assert.IsTrue(first.Read());
        Assert.IsFalse(first.Read());
        Assert.AreEqual(0, first.BytesConsumed);

        var next = new Utf8DelimitedReader("\"a\"\"b\"\n"u8, isFinalBlock: true, first.CurrentState);
        Assert.IsTrue(next.Read());
        Assert.IsTrue(next.Read());
        Assert.AreEqual("a\"b", next.GetString());
    }

    /// <summary>
    /// Verifies that a byte-order mark split across blocks is skipped once the block that completes it arrives.
    /// </summary>
    [TestMethod]
    public void Read_WhenByteOrderMarkSplitAcrossBlocks_ShouldSkipIt()
    {
        var first = new Utf8DelimitedReader([0xEF, 0xBB], isFinalBlock: false, new DelimitedReaderState());

        Assert.IsFalse(first.Read());
        Assert.AreEqual(0, first.BytesConsumed);

        var next = new Utf8DelimitedReader([0xEF, 0xBB, 0xBF, .. "id\n1\n"u8.ToArray()], isFinalBlock: true, first.CurrentState);
        Assert.IsTrue(next.Read());
        CollectionAssert.AreEqual(new[] { "id" }, next.Headers.ToArray());
    }

    /// <summary>
    /// Verifies that an empty block that is not the last reports no token and consumes nothing.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockIsEmptyAndNotTheLast_ShouldReturnFalse()
    {
        var reader = new Utf8DelimitedReader(ReadOnlySpan<byte>.Empty, isFinalBlock: false, new DelimitedReaderState());

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(0, reader.BytesConsumed);
        Assert.AreEqual(DelimitedTokenType.None, reader.TokenType);
    }

    /// <summary>
    /// Verifies that a final block that ends inside a quoted field raises <see cref="DelimitedFormatException" />, as a
    /// single read of the same bytes does.
    /// </summary>
    [TestMethod]
    public void Read_WhenFinalBlockEndsInsideAQuotedField_ShouldThrowDelimitedFormatException()
    {
        Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            var reader = new Utf8DelimitedReader("a,\"b"u8, isFinalBlock: true, new DelimitedReaderState(new DelimitedReaderOptions { NoHeader = true }));
            while (reader.Read())
            {
            }
        });
    }

    /// <summary>
    /// Verifies that reading the torture document in chunks of a given size, in header mode and positionally, reports
    /// exactly what a single read reports.
    /// </summary>
    /// <param name="chunkSize">The number of new bytes each block adds.</param>
    /// <param name="noHeader">Whether the document is read without a header row.</param>
    [TestMethod]
    [DataRow(1, false)]
    [DataRow(2, false)]
    [DataRow(3, false)]
    [DataRow(7, false)]
    [DataRow(64, false)]
    [DataRow(1, true)]
    [DataRow(5, true)]
    public void Read_WhenChunked_ShouldMatchTheSinglePassTranscript(int chunkSize, bool noHeader)
    {
        var options = new DelimitedReaderOptions { NoHeader = noHeader, AllowComments = true, FieldCountBehavior = DelimitedFieldCountBehavior.Ragged };
        int[] cuts = [.. Enumerable.Range(1, (s_tortureDocument.Length - 1) / chunkSize).Select(index => index * chunkSize)];

        CollectionAssert.AreEqual(
            DelimitedReaderTranscript.ReadWhole(s_tortureDocument, options),
            DelimitedReaderTranscript.ReadInBlocks(s_tortureDocument, options, cuts));
    }

    /// <summary>
    /// Verifies that reading the torture document with a new reader after every token, each resumed from the previous
    /// reader's state over the unconsumed bytes, reports exactly what a single read reports.
    /// </summary>
    /// <param name="noHeader">Whether the document is read without a header row.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Read_WhenResumedAfterEveryToken_ShouldMatchTheSinglePassTranscript(bool noHeader)
    {
        var options = new DelimitedReaderOptions { NoHeader = noHeader, AllowComments = true, FieldCountBehavior = DelimitedFieldCountBehavior.Ragged };

        CollectionAssert.AreEqual(
            DelimitedReaderTranscript.ReadWhole(s_tortureDocument, options),
            DelimitedReaderTranscript.ReadResumingAfterEveryToken(s_tortureDocument, options));
    }

    /// <summary>
    /// Verifies that each RFC 4180 corpus document, split in two at every offset and into chunks of every size, and
    /// read with a new reader after every token, reports exactly what a single read reports.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Rfc4180CorpusData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenRfc4180CorpusDocumentIsSplit_ShouldMatchTheSinglePassTranscript(ValidKat<string, string[][]> kat)
    {
        DelimitedReaderTranscript.AssertEverySplitMatches(Encoding.UTF8.GetBytes(kat.Input), new DelimitedReaderOptions { NoHeader = true }, kat.Name);
    }

    /// <summary>
    /// Verifies that each malformed document, split in two at every offset and into chunks of every size, and read with
    /// a new reader after every token, raises exactly the error a single read raises, message, line and offset
    /// included, after the same tokens.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="text">The document.</param>
    /// <param name="noHeader">Whether the document is read without a header row.</param>
    /// <param name="allowComments">Whether comment lines are allowed.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("unterminated quoted field", "a,b\n1,\"open\n", true, false)]
    [DataRow("unterminated quoted field after a comment", "# c\n\"x", true, true)]
    [DataRow("text after a closing quote", "a,b\n1,\"q\"x\n", true, false)]
    [DataRow("text after a quoted field that spans lines", "a,b\r\n1,\"q\r\nr\"x\r\n", true, false)]
    [DataRow("field count differs from the header", "a,b\n1,2\n1,2,3\n", false, false)]
    [DataRow("header repeats a name", "a,a\n1,2\n", false, false)]
    public void Read_WhenMalformedDocumentIsSplit_ShouldThrowAsTheSinglePassDoes(string testName, string text, bool noHeader, bool allowComments)
    {
        byte[] input = Encoding.UTF8.GetBytes(text);
        var options = new DelimitedReaderOptions { NoHeader = noHeader, AllowComments = allowComments };

        Assert.StartsWith(nameof(DelimitedFormatException) + "|", DelimitedReaderTranscript.ReadWhole(input, options)[^1], testName);

        DelimitedReaderTranscript.AssertEverySplitMatches(input, options, testName);
    }

    /// <summary>
    /// Verifies that each document whose malformed records are skipped, split in two at every offset and into chunks of
    /// every size, and read with a new reader after every token, skips exactly the records a single read skips.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="text">The document.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("text after a closing quote", "a,b\n1,\"q\"x,y\n2,3\n")]
    [DataRow("text after a quoted field that spans lines", "a,b\r\n1,\"q\r\nr\"x\r\n2,3\r\n")]
    [DataRow("field count differs from the header", "a,b\n1,2,3\n4,5\n6\n")]
    [DataRow("malformed last record without a line ending", "a,b\n1,2\n\"q\"x")]
    public void Read_WhenMalformedRecordsAreSkippedInBlocks_ShouldMatchTheSinglePassTranscript(string testName, string text)
    {
        byte[] input = Encoding.UTF8.GetBytes(text);
        var options = new DelimitedReaderOptions { MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord };

        Assert.StartsWith("end|", DelimitedReaderTranscript.ReadWhole(input, options)[^1], testName);

        DelimitedReaderTranscript.AssertEverySplitMatches(input, options, testName);
    }
}
