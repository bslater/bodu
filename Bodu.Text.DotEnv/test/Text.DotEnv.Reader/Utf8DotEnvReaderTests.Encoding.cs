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
}
