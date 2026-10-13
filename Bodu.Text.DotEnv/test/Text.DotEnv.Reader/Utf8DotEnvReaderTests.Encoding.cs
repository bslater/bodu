// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.Encoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.DotEnv.Reader;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Contains encoding and line-handling robustness tests for <see cref="Utf8DotEnvReader" /> - BOM stripping, line-ending
/// variants, and UTF-8 multibyte values - mirroring the classic interop defect classes.
/// </summary>
public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// Verifies that a leading UTF-8 byte-order mark is stripped so the first key is not corrupted.
    /// </summary>
    [TestMethod]
    public void Read_WhenLeadingByteOrderMark_ShouldStripIt()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. "HOST=localhost\n"u8];
        var reader = new Utf8DotEnvReader(bytes);
        var names = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == DotEnvTokenType.PropertyName)
                names.Add(reader.GetString());
        }

        CollectionAssert.AreEqual(new List<string> { "HOST" }, names);
    }

    /// <summary>
    /// Verifies that CRLF line endings are handled equivalently to LF.
    /// </summary>
    [TestMethod]
    public void Read_WhenCrlfLineEndings_ShouldReadEntries()
    {
        List<string> tokens = Transcribe("A=1\r\nB=2\r\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:A", "String:1", "Name:B", "String:2", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that lone carriage-return (classic Mac) line endings are handled.
    /// </summary>
    [TestMethod]
    public void Read_WhenCarriageReturnOnlyLineEndings_ShouldReadEntries()
    {
        List<string> tokens = Transcribe("A=1\rB=2\r");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:A", "String:1", "Name:B", "String:2", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a final entry without a trailing newline is read.
    /// </summary>
    [TestMethod]
    public void Read_WhenNoTrailingNewline_ShouldReadFinalEntry()
    {
        List<string> tokens = Transcribe("A=1\nB=2");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:A", "String:1", "Name:B", "String:2", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a UTF-8 multibyte value is decoded correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnicodeValue_ShouldDecodeMultibyte()
    {
        List<string> tokens = Transcribe("GREETING=\"café ☕\"\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:GREETING", "String:café ☕", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a backslash before a character outside ASCII is kept with that character, whatever its UTF-8
    /// length and however the value is quoted, since the pair is not an escape, and that the entry after it is read.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="quote">The quote around the value, or the empty string for an unquoted value.</param>
    /// <param name="value">The text of the value, which is also the text it should read as.</param>
    [TestMethod]
    [DataRow("double-quoted two-byte character", "\"", "\\é")]
    [DataRow("double-quoted three-byte character between letters", "\"", "a\\€b")]
    [DataRow("double-quoted four-byte character", "\"", "\\\U0001F600")]
    [DataRow("double-quoted two in a row", "\"", "\\é\\ü")]
    [DataRow("single-quoted two-byte character", "'", "\\é")]
    [DataRow("single-quoted three-byte character between letters", "'", "a\\€b")]
    [DataRow("unquoted two-byte character", "", "\\é")]
    [DataRow("unquoted four-byte character", "", "\\\U0001F600")]
    public void Read_WhenABackslashPrecedesACharacterOutsideAscii_ShouldKeepTheBackslashAndTheCharacter(string testName, string quote, string value)
    {
        _ = testName;

        List<string> tokens = Transcribe($"KEY={quote}{value}{quote}\nNEXT=1\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:KEY", $"String:{value}", "Name:NEXT", "String:1", "EndObject" },
            tokens,
            string.Join(" | ", tokens));
    }

    /// <summary>
    /// Verifies that bytes that are not valid UTF-8 read as U+FFFD, one for each sequence the decoder rejects, however
    /// the value is quoted, and that they never swallow the closing quote, so the value ends where it should and the
    /// entry after it is read.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="quote">The quote around the value, or the empty string for an unquoted value.</param>
    /// <param name="invalid">The bytes written after <c>a</c> in the value, in hexadecimal.</param>
    /// <param name="expected">The text the value should read as.</param>
    [TestMethod]
    [DataRow("double-quoted continuation byte", "\"", "80", "a�")]
    [DataRow("double-quoted byte that never starts a sequence", "\"", "FF", "a�")]
    [DataRow("double-quoted lead byte without its continuation", "\"", "C3", "a�")]
    [DataRow("double-quoted three-byte sequence cut short", "\"", "E282", "a�")]
    [DataRow("double-quoted four-byte sequence cut short", "\"", "F09F98", "a�")]
    [DataRow("double-quoted two continuation bytes", "\"", "8080", "a��")]
    [DataRow("double-quoted encoded surrogate", "\"", "EDA080", "a���")]
    [DataRow("double-quoted backslash before a continuation byte", "\"", "5C80", "a\\�")]
    [DataRow("single-quoted continuation byte", "'", "80", "a�")]
    [DataRow("single-quoted three-byte sequence cut short", "'", "E282", "a�")]
    [DataRow("unquoted continuation byte", "", "80", "a�")]
    [DataRow("unquoted three-byte sequence cut short", "", "E282", "a�")]
    public void Read_WhenAValueHoldsBytesThatAreNotUtf8_ShouldReadReplacementCharactersAndTheNextEntry(string testName, string quote, string invalid, string expected)
    {
        _ = testName;
        byte[] bytes = [.. Encoding.UTF8.GetBytes($"K={quote}a"), .. Convert.FromHexString(invalid), .. Encoding.UTF8.GetBytes($"{quote}\nNEXT=1\n")];

        List<string> tokens = Transcribe(bytes);

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:K", $"String:{expected}", "Name:NEXT", "String:1", "EndObject" },
            tokens,
            string.Join(" | ", tokens));
    }

    /// <summary>
    /// Verifies that a line break inside a double-quoted value counts as a line whether it is a LF, a CR LF or a lone
    /// CR, as it does between entries, so an error on a later line reports that line.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The DotEnv source text, with a malformed entry after the value.</param>
    /// <param name="line">The 1-based line of the malformed entry.</param>
    [TestMethod]
    [DataRow("LF", "A=\"x\ny\"\nB C\n", 3)]
    [DataRow("CR LF", "A=\"x\r\ny\"\r\nB C\r\n", 3)]
    [DataRow("lone CR", "A=\"x\ry\"\rB C\r", 3)]
    [DataRow("two lone CRs", "A=\"x\r\ry\"\rB C\r", 4)]
    [DataRow("lone CR before the closing quote", "A=\"x\r\"\rB C\r", 3)]
    [DataRow("LF then a lone CR", "A=\"x\n\ry\"\nB C\n", 4)]
    [DataRow("lone CR after a backslash", "A=\"x\\\ry\"\rB C\r", 3)]
    public void Read_WhenADoubleQuotedValueHoldsALineBreak_ShouldCountItAsALine(string testName, string source, int line)
    {
        _ = testName;

        DotEnvFormatException ex = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = Transcribe(source);
        });

        Assert.AreEqual(line, ex.LineNumber, ex.Message);
    }

    /// <summary>
    /// Verifies that a lone CR inside a double-quoted value stays part of the value, though it counts as a line.
    /// </summary>
    [TestMethod]
    public void Read_WhenADoubleQuotedValueHoldsALoneCarriageReturn_ShouldKeepItInTheValue()
    {
        List<string> tokens = Transcribe("A=\"x\ry\"\rB=2\r");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:A", "String:x\ry", "Name:B", "String:2", "EndObject" },
            tokens,
            string.Join(" | ", tokens));
    }

    /// <summary>
    /// Verifies that a comment ends at a LF, a CR LF or a lone CR, so the entry on the next line is read.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="lineEnding">The line ending after each line.</param>
    [TestMethod]
    [DataRow("LF", "\n")]
    [DataRow("CR LF", "\r\n")]
    [DataRow("lone CR", "\r")]
    public void Read_WhenACommentEndsAtALineEnding_ShouldReadTheEntryOnTheNextLine(string testName, string lineEnding)
    {
        _ = testName;

        List<string> tokens = Transcribe($"# note{lineEnding}A=1{lineEnding}");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Comment: note", "Name:A", "String:1", "EndObject" },
            tokens,
            string.Join(" | ", tokens));
    }

    /// <summary>
    /// Verifies that a single-quoted value that meets a LF, a CR LF or a lone CR before its closing quote is reported
    /// unterminated at its opening quote.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="lineEnding">The line ending after each line.</param>
    [TestMethod]
    [DataRow("LF", "\n")]
    [DataRow("CR LF", "\r\n")]
    [DataRow("lone CR", "\r")]
    public void Read_WhenASingleQuotedValueMeetsALineEnding_ShouldReportItUnterminatedAtItsOpeningQuote(string testName, string lineEnding)
    {
        _ = testName;

        DotEnvFormatException ex = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = Transcribe($"A=1{lineEnding}KEY='abc{lineEnding}def'{lineEnding}");
        });

        Assert.AreEqual(2, ex.LineNumber, "LineNumber");
        Assert.AreEqual(5, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(7 + lineEnding.Length, ex.Offset, "Offset");
    }

    /// <summary>
    /// Verifies that only a whole byte-order mark is skipped: input that starts with its first two bytes keeps them, so
    /// they are rejected as the start of a key at the first column and offset zero.
    /// </summary>
    [TestMethod]
    public void Read_WhenTheInputStartsWithPartOfAByteOrderMark_ShouldNotSkipIt()
    {
        byte[] bytes = [0xEF, 0xBB, .. "A=1\n"u8];

        DotEnvFormatException ex = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = Transcribe(bytes);
        });

        Assert.AreEqual(1, ex.LineNumber, "LineNumber");
        Assert.AreEqual(1, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(0, ex.Offset, "Offset");
    }

    /// <summary>
    /// Verifies that bytes that are not valid UTF-8 directly before an escape sequence or a line break in a
    /// double-quoted value read as U+FFFD, and leave the escape or the line break that follows them as it is.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="invalid">The bytes written after <c>a</c> in the value, in hexadecimal.</param>
    /// <param name="following">The text written after the invalid bytes.</param>
    /// <param name="expected">The text the value should read as.</param>
    [TestMethod]
    [DataRow("before an escaped quote", "E282", "\\\"", "a\uFFFD\"")]
    [DataRow("before an escaped backslash", "C3", "\\\\", "a\uFFFD\\")]
    [DataRow("before a newline escape", "F09F98", "\\n", "a\uFFFD\n")]
    [DataRow("before a line continuation", "80", "\\\nb", "a\uFFFDb")]
    [DataRow("before a LF", "E282", "\n", "a\uFFFD\n")]
    [DataRow("before a lone CR", "80", "\r", "a\uFFFD\r")]
    [DataRow("before a CR LF", "FF", "\r\n", "a\uFFFD\r\n")]
    public void Read_WhenBytesThatAreNotUtf8PrecedeAnEscapeOrALineBreakInADoubleQuotedValue_ShouldReadAReplacementCharacterBeforeIt(string testName, string invalid, string following, string expected)
    {
        _ = testName;
        byte[] bytes = [.. "K=\"a"u8, .. Convert.FromHexString(invalid), .. Encoding.UTF8.GetBytes(following), .. "\"\nNEXT=1\n"u8];

        List<string> tokens = Transcribe(bytes);

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:K", $"String:{expected}", "Name:NEXT", "String:1", "EndObject" },
            tokens,
            string.Join(" | ", tokens));
    }

    /// <summary>
    /// Verifies that a double-quoted value of thousands of bytes, with characters outside ASCII throughout and with or
    /// without escape sequences, reads back whole.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="unit">The text repeated to form the value, as written between the quotes.</param>
    /// <param name="decodedUnit">The text <paramref name="unit" /> reads as.</param>
    [TestMethod]
    [DataRow("with escapes", "\u00E9\\\"\u20AC\\n\U0001F600x", "\u00E9\"\u20AC\n\U0001F600x")]
    [DataRow("without escapes", "\u00E9 \u20AC\n\U0001F600x", "\u00E9 \u20AC\n\U0001F600x")]
    public void Read_WhenADoubleQuotedValueIsLong_ShouldDecodeItWhole(string testName, string unit, string decodedUnit)
    {
        _ = testName;
        string source = "K=\"" + string.Concat(Enumerable.Repeat(unit, 300)) + "\"\nNEXT=1\n";
        string expected = string.Concat(Enumerable.Repeat(decodedUnit, 300));

        List<string> tokens = Transcribe(source);

        Assert.HasCount(6, tokens);
        Assert.AreEqual($"String:{expected}", tokens[2]);
        Assert.AreEqual("Name:NEXT", tokens[3]);
    }
}
