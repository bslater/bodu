// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniReaderTests.Resumable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Contains the tests of the resumable reading contract of <see cref="Utf8IniReader" />: an input read in blocks, each
/// block's reader resuming from the state of the one before, reports exactly what a single read of the whole input
/// reports.
/// </summary>
public partial class Utf8IniReaderTests
{
    /// <summary>
    /// A document that exercises what the resumable reader must survive being split inside: a byte-order mark, CRLF, LF
    /// and lone CR line endings, global keys, comment lines of both kinds, a section name holding <c>]</c>, a comment
    /// after a header, indented keys and spaces around the assignment, a value holding <c>=</c> and an inline marker,
    /// an empty value, multi-byte UTF-8, and a last entry without a line ending.
    /// </summary>
    private static readonly byte[] s_tortureDocument =
    [
        0xEF, 0xBB, 0xBF,
        .. Encoding.UTF8.GetBytes(
            "; leading comment\r\n" +
            "global=1\n" +
            "\n" +
            "[server]\r\n" +
            "  host = localhost\n" +
            "\tport\t=\t5432\r" +
            "# hash comment\n" +
            "[This One Has A ] In It] ; note\n" +
            "conn=a=b:c ; kept\n" +
            "empty=\n" +
            "[café]\n" +
            "drink=☕ ünïcode\r\n" +
            "last=no-newline"),
    ];

    /// <summary>
    /// Verifies that a block that ends inside a line makes <see cref="Utf8IniReader.Read" /> return
    /// <see langword="false" /> with the reader left as it was: the previous token is still current, and
    /// <see cref="Utf8IniReader.BytesConsumed" /> ends at the start of the line.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockEndsInsideALine_ShouldReturnFalseAndLeaveTheLineUnread()
    {
        var reader = new Utf8IniReader("k=v\n[sec"u8, isFinalBlock: false, new IniReaderState());

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.Read());
        Assert.AreEqual(IniTokenType.String, reader.TokenType);
        Assert.AreEqual("v", reader.GetString());
        Assert.AreEqual(4, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that a carriage return that ends a block that is not the last is left for the next block, which may
    /// begin with the line feed of a CRLF, so the pair counts as one line ending.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockEndsWithACarriageReturn_ShouldWaitForTheByteAfterIt()
    {
        byte[] input = "k=v\r\nj=w\r\n"u8.ToArray();
        var first = new Utf8IniReader(input.AsSpan(0, 4), isFinalBlock: false, new IniReaderState());

        Assert.IsFalse(first.Read());
        Assert.AreEqual(0, first.BytesConsumed);

        CollectionAssert.AreEqual(IniReaderTranscript.ReadWhole(input, default), IniReaderTranscript.ReadInBlocks(input, default(IniReaderOptions), [4]));
    }

    /// <summary>
    /// Verifies that a byte-order mark split across blocks is skipped once the block that completes it arrives.
    /// </summary>
    [TestMethod]
    public void Read_WhenByteOrderMarkSplitAcrossBlocks_ShouldSkipIt()
    {
        var first = new Utf8IniReader([0xEF, 0xBB], isFinalBlock: false, new IniReaderState());

        Assert.IsFalse(first.Read());
        Assert.AreEqual(0, first.BytesConsumed);

        var next = new Utf8IniReader([0xEF, 0xBB, 0xBF, .. "[s]\n"u8.ToArray()], isFinalBlock: true, first.CurrentState);
        Assert.IsTrue(next.Read());
        Assert.AreEqual("s", next.GetString());
        Assert.AreEqual(3, next.TokenOffset);
    }

    /// <summary>
    /// Verifies that a first block that holds the start of a byte-order mark followed by other bytes is read as text,
    /// as a single read of the same bytes reads it, however the input is split.
    /// </summary>
    [TestMethod]
    public void Read_WhenInputStartsWithPartOfAByteOrderMarkAndIsSplit_ShouldMatchTheSinglePassTranscript()
    {
        byte[] input = [0xEF, 0xBB, .. "a=1\n"u8.ToArray()];

        IniReaderTranscript.AssertEverySplitMatches(input, default, "part of a byte-order mark");
    }

    /// <summary>
    /// Verifies that an empty block that is not the last reports no token and consumes nothing.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockIsEmptyAndNotTheLast_ShouldReturnFalse()
    {
        var reader = new Utf8IniReader(ReadOnlySpan<byte>.Empty, isFinalBlock: false, new IniReaderState());

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(IniTokenType.None, reader.TokenType);
        Assert.AreEqual(0, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that a final block that ends inside a section header raises <see cref="IniFormatException" />, as a
    /// single read of the same bytes does.
    /// </summary>
    [TestMethod]
    public void Read_WhenFinalBlockEndsInsideASectionHeader_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            var reader = new Utf8IniReader("k=v\n[sec"u8, isFinalBlock: true, new IniReaderState());
            while (reader.Read())
            {
            }
        });
    }

    /// <summary>
    /// Verifies that reading the torture document in chunks of a given size, under each option, reports exactly what a
    /// single read reports.
    /// </summary>
    /// <param name="chunkSize">The number of new bytes each block adds.</param>
    /// <param name="disallowHashComments">Whether a <c>#</c> is text rather than the start of a comment.</param>
    /// <param name="skipComments">Whether comment lines are skipped rather than reported.</param>
    [TestMethod]
    [DataRow(1, false, false)]
    [DataRow(2, false, false)]
    [DataRow(3, false, false)]
    [DataRow(7, false, false)]
    [DataRow(64, false, false)]
    [DataRow(1, false, true)]
    [DataRow(5, false, true)]
    public void Read_WhenChunked_ShouldMatchTheSinglePassTranscript(int chunkSize, bool disallowHashComments, bool skipComments)
    {
        var options = new IniReaderOptions { DisallowHashComments = disallowHashComments, SkipComments = skipComments };
        int[] cuts = [.. Enumerable.Range(1, (s_tortureDocument.Length - 1) / chunkSize).Select(index => index * chunkSize)];

        CollectionAssert.AreEqual(
            IniReaderTranscript.ReadWhole(s_tortureDocument, options),
            IniReaderTranscript.ReadInBlocks(s_tortureDocument, options, cuts));
    }

    /// <summary>
    /// Verifies that reading the torture document with a new reader after every token, each resumed from the previous
    /// reader's state over the unconsumed bytes, reports exactly what a single read reports.
    /// </summary>
    /// <param name="skipComments">Whether comment lines are skipped rather than reported.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Read_WhenResumedAfterEveryToken_ShouldMatchTheSinglePassTranscript(bool skipComments)
    {
        var options = new IniReaderOptions { SkipComments = skipComments };

        CollectionAssert.AreEqual(
            IniReaderTranscript.ReadWhole(s_tortureDocument, options),
            IniReaderTranscript.ReadResumingAfterEveryToken(s_tortureDocument, options));
    }

    /// <summary>
    /// Verifies that each well-formed document, split in two at every offset and into chunks of every size, and read
    /// with a new reader after every token, reports exactly what a single read reports.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="text">The document.</param>
    /// <param name="disallowHashComments">Whether a <c>#</c> is text rather than the start of a comment.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("global key then a section", "key0=a\n[db]\nhost=x\nport=5\n", false)]
    [DataRow("mixed line endings", "a=1\r\nb=2\rc=3\n", false)]
    [DataRow("lone CR line endings without a final one", "[s]\ra=1\r;c\rb=2", false)]
    [DataRow("section names holding a bracket", "[foo]bar]\nk=v\n[12345]]\n[a] b]\nk=v\n", false)]
    [DataRow("comments and whitespace after headers", "[comments] ; note\n[s] # note\n[t];note\n[a]; b]\n[b] \t\nk=v\n", false)]
    [DataRow("hash after an inner bracket without hash comments", "[a]#b]\nk=v\n", true)]
    [DataRow("hash key without hash comments", "#k=v\n", true)]
    [DataRow("blank and whitespace-only lines", "\n \t\n\r\n[s]\n\n  k = v  \n\t\n", false)]
    [DataRow("values kept literally", "conn=a=b:c\nk=v ; not a comment\nq=\"a b\"\np=${HOME}/%PATH%\n", false)]
    [DataRow("multi-byte names and values", "[café]\ndrink=☕\nkéy=välue ✓\n", false)]
    [DataRow("an empty source", "", false)]
    public void Read_WhenDocumentIsSplit_ShouldMatchTheSinglePassTranscript(string testName, string text, bool disallowHashComments)
    {
        IniReaderTranscript.AssertEverySplitMatches(Encoding.UTF8.GetBytes(text), new IniReaderOptions { DisallowHashComments = disallowHashComments }, testName);
    }

    /// <summary>
    /// Verifies that each malformed document, split in two at every offset and into chunks of every size, and read with
    /// a new reader after every token, raises exactly the error a single read raises, message, line and offset
    /// included, after the same tokens.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="text">The document.</param>
    /// <param name="disallowHashComments">Whether a <c>#</c> is text rather than the start of a comment.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("missing assignment", "[s]\nkey\n", false)]
    [DataRow("missing assignment after CRLF lines", "a=1\r\nb=2\r\nnoequals\r\n", false)]
    [DataRow("missing assignment at the end of the input", "a=1\nkey", false)]
    [DataRow("colon delimiter", "host: localhost\n", false)]
    [DataRow("unterminated section", "k=v\n[db\nhost=x\n", false)]
    [DataRow("empty section name", "[]\nhost=x\n", false)]
    [DataRow("empty key", "a=1\n  =value\n", false)]
    [DataRow("text after the header", "k=v\n[foo] bar\nk=v\n", false)]
    [DataRow("text straight after the bracket", "[foo]bar\r\nk=v\r\n", false)]
    [DataRow("text after the last bracket", "[a]b]c\nk=v\n", false)]
    [DataRow("hash after a header without hash comments", "[s] # note\nk=v\n", true)]
    public void Read_WhenMalformedDocumentIsSplit_ShouldThrowAsTheSinglePassDoes(string testName, string text, bool disallowHashComments)
    {
        byte[] input = Encoding.UTF8.GetBytes(text);
        var options = new IniReaderOptions { DisallowHashComments = disallowHashComments };

        Assert.StartsWith(nameof(IniFormatException) + "|", IniReaderTranscript.ReadWhole(input, options)[^1], testName);

        IniReaderTranscript.AssertEverySplitMatches(input, options, testName);
    }
}
