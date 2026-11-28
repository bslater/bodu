// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.Resumable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.Kat;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Contains the tests of the resumable reading contract of <see cref="Utf8DotEnvReader" />: an input read in blocks,
/// each block's reader resuming from the state of the one before, reports exactly what a single read of the whole input
/// reports.
/// </summary>
public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// A document that exercises what the resumable reader must survive being split inside: a byte-order mark, CRLF, LF
    /// and lone CR line endings, comment lines, <c>export</c> prefixes and a key that begins as one does, whitespace
    /// characters of one, two and three bytes, inline comments, escapes, single-quoted and multi-line double-quoted
    /// values, a line continuation over a CRLF, multi-byte UTF-8, and a last entry without a line ending.
    /// </summary>
    private static readonly byte[] s_tortureDocument =
    [
        0xEF, 0xBB, 0xBF,
        .. Encoding.UTF8.GetBytes(
            "# leading comment\r\n" +
            "export HOST=localhost\n" +
            "  PORT = 5432 # inline\r\n" +
            " QUOTED=\"a \\\"b\\\"\\n c\" # after\n" +
            "SINGLE='x # y'\r" +
            "MULTI=\"line one\r\nline two\"\n" +
            "LC=\"one\\\r\ntwo\"\n" +
            "\n" +
            "　export　SPACED　=　café ☃　\n" +
            "exportFOO=1\n" +
            "\t# indented comment\n" +
            "EMPTY=\n" +
            "LAST=no-newline"),
    ];

    /// <summary>
    /// Verifies that a block that ends inside an entry makes <see cref="Utf8DotEnvReader.Read" /> return
    /// <see langword="false" /> with the reader left as it was: the previous token is still current, and
    /// <see cref="Utf8DotEnvReader.BytesConsumed" /> ends at the start of the entry.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockEndsInsideAnEntry_ShouldReturnFalseAndLeaveTheEntryUnread()
    {
        var reader = new Utf8DotEnvReader("A=1\nB=\"x"u8, isFinalBlock: false, new DotEnvReaderState());

        while (reader.TokenType != DotEnvTokenType.String)
            Assert.IsTrue(reader.Read());

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(DotEnvTokenType.String, reader.TokenType);
        Assert.AreEqual("1", reader.GetString());
        Assert.AreEqual(4, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that a carriage return that ends a block that is not the last is left for the next block, which may
    /// begin with the line feed of a CRLF, so the pair counts as one line ending.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockEndsWithACarriageReturn_ShouldWaitForTheByteAfterIt()
    {
        byte[] input = "A=1\r\nB=2\r\n"u8.ToArray();
        var first = new Utf8DotEnvReader(input.AsSpan(0, 4), isFinalBlock: false, new DotEnvReaderState());

        Assert.IsTrue(first.Read());
        Assert.IsFalse(first.Read());
        Assert.AreEqual(0, first.BytesConsumed);

        CollectionAssert.AreEqual(DotEnvReaderTranscript.ReadWhole(input, default), DotEnvReaderTranscript.ReadInBlocks(input, default(DotEnvReaderOptions), [4]));
    }

    /// <summary>
    /// Verifies that a multi-byte whitespace character that a block cuts short is read once the block that completes it
    /// arrives.
    /// </summary>
    /// <param name="cut">The offset at which the first block ends, inside the character.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void Read_WhenBlockEndsInsideAMultiByteWhitespaceCharacter_ShouldWaitForTheWholeCharacter(int cut)
    {
        byte[] input = Encoding.UTF8.GetBytes("　A=1\n");
        var first = new Utf8DotEnvReader(input.AsSpan(0, cut), isFinalBlock: false, new DotEnvReaderState());

        Assert.IsTrue(first.Read());
        Assert.IsFalse(first.Read());
        Assert.AreEqual(0, first.BytesConsumed);

        CollectionAssert.AreEqual(DotEnvReaderTranscript.ReadWhole(input, default), DotEnvReaderTranscript.ReadInBlocks(input, default(DotEnvReaderOptions), [cut]));
    }

    /// <summary>
    /// Verifies that a block that ends within a word that may yet be the <c>export</c> prefix, or a key that begins as
    /// it does, leaves the word for the next block.
    /// </summary>
    /// <param name="text">The document.</param>
    /// <param name="cut">The offset at which the first block ends.</param>
    [TestMethod]
    [DataRow("export A=1\n", 3)]
    [DataRow("export A=1\n", 6)]
    [DataRow("exportA=1\n", 6)]
    [DataRow("export　A=1\n", 7)]
    public void Read_WhenBlockEndsWithinAnExportPrefix_ShouldWaitForTheByteAfterIt(string text, int cut)
    {
        byte[] input = Encoding.UTF8.GetBytes(text);
        var first = new Utf8DotEnvReader(input.AsSpan(0, cut), isFinalBlock: false, new DotEnvReaderState());

        Assert.IsTrue(first.Read());
        Assert.IsFalse(first.Read());
        Assert.AreEqual(0, first.BytesConsumed);

        CollectionAssert.AreEqual(DotEnvReaderTranscript.ReadWhole(input, default), DotEnvReaderTranscript.ReadInBlocks(input, default(DotEnvReaderOptions), [cut]));
    }

    /// <summary>
    /// Verifies that a CRLF that continues a double-quoted value after a backslash, split between its two bytes, reads
    /// as a single read reads it.
    /// </summary>
    [TestMethod]
    public void Read_WhenALineContinuationCrLfIsSplit_ShouldMatchTheSinglePassTranscript()
    {
        byte[] input = "LC=\"one\\\r\ntwo\"\nB=2\n"u8.ToArray();
        int cut = Array.IndexOf(input, (byte)'\r') + 1;

        CollectionAssert.AreEqual(DotEnvReaderTranscript.ReadWhole(input, default), DotEnvReaderTranscript.ReadInBlocks(input, default(DotEnvReaderOptions), [cut]));
    }

    /// <summary>
    /// Verifies that a byte-order mark split across blocks is skipped once the block that completes it arrives, the
    /// start of the document waiting for it.
    /// </summary>
    [TestMethod]
    public void Read_WhenByteOrderMarkSplitAcrossBlocks_ShouldSkipIt()
    {
        var first = new Utf8DotEnvReader([0xEF, 0xBB], isFinalBlock: false, new DotEnvReaderState());

        Assert.IsFalse(first.Read());
        Assert.AreEqual(0, first.BytesConsumed);

        var next = new Utf8DotEnvReader([0xEF, 0xBB, 0xBF, .. "A=1\n"u8.ToArray()], isFinalBlock: true, first.CurrentState);
        Assert.IsTrue(next.Read());
        Assert.AreEqual(DotEnvTokenType.StartObject, next.TokenType);
        Assert.AreEqual(3, next.BytesConsumed);
        Assert.IsTrue(next.Read());
        Assert.AreEqual("A", next.GetString());
    }

    /// <summary>
    /// Verifies that an empty block that is not the last, which may yet begin with a byte-order mark, reports no token
    /// and consumes nothing.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockIsEmptyAndNotTheLast_ShouldReturnFalse()
    {
        var reader = new Utf8DotEnvReader(ReadOnlySpan<byte>.Empty, isFinalBlock: false, new DotEnvReaderState());

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(DotEnvTokenType.None, reader.TokenType);
        Assert.AreEqual(0, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that a final block that ends inside a double-quoted value raises <see cref="DotEnvFormatException" />,
    /// as a single read of the same bytes does.
    /// </summary>
    [TestMethod]
    public void Read_WhenFinalBlockEndsInsideAQuotedValue_ShouldThrowDotEnvFormatException()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            var reader = new Utf8DotEnvReader("A=\"x"u8, isFinalBlock: true, new DotEnvReaderState());
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
    /// <param name="disallowExportPrefix">Whether the <c>export</c> prefix is read as part of the key.</param>
    /// <param name="disallowInlineComments">Whether a <c>#</c> after an unquoted value is part of the value.</param>
    /// <param name="skipComments">Whether comment lines are skipped rather than reported.</param>
    [TestMethod]
    [DataRow(1, false, false, false)]
    [DataRow(2, false, false, false)]
    [DataRow(3, false, false, false)]
    [DataRow(7, false, false, false)]
    [DataRow(64, false, false, false)]
    [DataRow(1, true, false, false)]
    [DataRow(1, false, true, false)]
    [DataRow(1, false, false, true)]
    public void Read_WhenChunked_ShouldMatchTheSinglePassTranscript(int chunkSize, bool disallowExportPrefix, bool disallowInlineComments, bool skipComments)
    {
        var options = new DotEnvReaderOptions
        {
            DisallowExportPrefix = disallowExportPrefix,
            DisallowInlineComments = disallowInlineComments,
            SkipComments = skipComments,
        };
        int[] cuts = [.. Enumerable.Range(1, (s_tortureDocument.Length - 1) / chunkSize).Select(index => index * chunkSize)];

        CollectionAssert.AreEqual(
            DotEnvReaderTranscript.ReadWhole(s_tortureDocument, options),
            DotEnvReaderTranscript.ReadInBlocks(s_tortureDocument, options, cuts));
    }

    /// <summary>
    /// Verifies that reading the torture document with a new reader after every token, each resumed from the previous
    /// reader's state over the unconsumed bytes, reports exactly what a single read reports.
    /// </summary>
    [TestMethod]
    public void Read_WhenResumedAfterEveryToken_ShouldMatchTheSinglePassTranscript()
    {
        CollectionAssert.AreEqual(
            DotEnvReaderTranscript.ReadWhole(s_tortureDocument, default),
            DotEnvReaderTranscript.ReadResumingAfterEveryToken(s_tortureDocument, default));
    }

    /// <summary>
    /// Verifies that each corpus document, split in two at every offset and into chunks of every size, and read with a
    /// new reader after every token, reports exactly what a single read reports.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(DotEnvCorpusData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenDotEnvCorpusDocumentIsSplit_ShouldMatchTheSinglePassTranscript(ValidKat<string, string[][]> kat)
    {
        DotEnvReaderTranscript.AssertEverySplitMatches(Encoding.UTF8.GetBytes(kat.Input), default, kat.Name);
    }

    /// <summary>
    /// Verifies that each malformed corpus document, split in two at every offset and into chunks of every size, and
    /// read with a new reader after every token, raises exactly the error a single read raises, message, line, column
    /// and offset included, after the same tokens.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(DotEnvCorpusInvalidData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenMalformedDotEnvCorpusDocumentIsSplit_ShouldThrowAsTheSinglePassDoes(InvalidKat<string> kat)
    {
        byte[] input = Encoding.UTF8.GetBytes(kat.Input);

        Assert.StartsWith(nameof(DotEnvFormatException) + "|", DotEnvReaderTranscript.ReadWhole(input, default)[^1], kat.Name);

        DotEnvReaderTranscript.AssertEverySplitMatches(input, default, kat.Name);
    }

    /// <summary>
    /// Verifies that each malformed document whose error lies part way through a line, after multi-byte text or on a
    /// line that a quoted value runs onto, split in two at every offset and into chunks of every size, and read with a
    /// new reader after every token, raises exactly the error a single read raises, message, line, column and offset
    /// included.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="text">The document.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("missing assignment after indentation", "A=1\n    KEY value\n")]
    [DataRow("invalid key after a multi-byte indentation", "A=1\n　 -KEY=v\n")]
    [DataRow("invalid key that is a multi-byte character", "A=1\néKEY=v\n")]
    [DataRow("invalid key after the export prefix", "export -KEY=v\n")]
    [DataRow("bare export prefix", "A=1\nexport\n")]
    [DataRow("text after a closing quote after whitespace", "A=1\nKEY=\"a\"　oops\n")]
    [DataRow("text after a value that spans lines", "KEY=\"a\r\nb\"junk\r\n")]
    [DataRow("text after a single quote on a later line", "A=1\rB='x'y\r")]
    [DataRow("unterminated double quote over the lines after it", "A=1\nexport  KEY = \"abc\ndef\nB=2\n")]
    [DataRow("unterminated single quote at a lone CR", "A=1\nKEY='abc\rB=2\r")]
    [DataRow("escape at the end of the input", "A=1\nKEY=\"abc\\")]
    [DataRow("missing assignment after a byte-order mark", "﻿KEY value\n")]
    public void Read_WhenMalformedDocumentIsSplit_ShouldThrowAsTheSinglePassDoes(string testName, string text)
    {
        byte[] input = Encoding.UTF8.GetBytes(text);

        Assert.StartsWith(nameof(DotEnvFormatException) + "|", DotEnvReaderTranscript.ReadWhole(input, default)[^1], testName);

        DotEnvReaderTranscript.AssertEverySplitMatches(input, default, testName);
    }
}
