// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.DotEnv.Reader;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DotEnvReader.Read" />, verifying the token stream produced
/// for representative DotEnv inputs.
/// </summary>
[TestClass]
public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// Reads every token from the supplied source and returns a compact <c>Kind:Value</c> transcript.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The token transcript.</returns>
    private static List<string> Transcribe(string source, DotEnvReaderOptions options = default)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8DotEnvReader(bytes, options);
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                DotEnvTokenType.PropertyName => $"Name:{reader.GetString()}",
                DotEnvTokenType.String => $"String:{reader.GetString()}",
                DotEnvTokenType.Comment => $"Comment:{reader.GetString()}",
                _ => reader.TokenType.ToString(),
            });
        }

        return tokens;
    }

    /// <summary>
    /// Reads every entry from the supplied source as <c>KEY=value</c> text, prefixed with <c>export</c> and a space
    /// when the entry carried the prefix.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <returns>The entries, in source order.</returns>
    private static List<string> ReadEntryTexts(string source)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8DotEnvReader(bytes);
        var entries = new List<string>();
        string key = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == DotEnvTokenType.PropertyName)
                key = reader.CurrentIsExport ? "export " + reader.GetString() : reader.GetString();
            else if (reader.TokenType == DotEnvTokenType.String)
                entries.Add($"{key}={reader.GetString()}");
        }

        return entries;
    }

    /// <summary>
    /// Verifies that a simple key/value assignment produces the framed property-name and string token stream.
    /// </summary>
    [TestMethod]
    public void Read_WhenSimpleAssignment_ShouldProduceFramedTokens()
    {
        List<string> tokens = Transcribe("HOST=localhost\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:HOST", "String:localhost", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that an <c>export</c> prefix is stripped and the key/value pair is still surfaced.
    /// </summary>
    [TestMethod]
    public void Read_WhenExportPrefix_ShouldStripPrefixAndReadEntry()
    {
        List<string> tokens = Transcribe("export PORT=5432\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:PORT", "String:5432", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a double-quoted value has its quotes stripped and its escape sequences resolved.
    /// </summary>
    [TestMethod]
    public void Read_WhenDoubleQuotedValueWithEscapes_ShouldResolveEscapes()
    {
        List<string> tokens = Transcribe("MSG=\"a\\tb\\nc\"\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:MSG", "String:a\tb\nc", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a single-quoted value is treated literally, without escape processing.
    /// </summary>
    [TestMethod]
    public void Read_WhenSingleQuotedValue_ShouldTreatContentLiterally()
    {
        List<string> tokens = Transcribe("PATH='a\\tb'\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:PATH", "String:a\\tb", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a comment line is surfaced as a <see cref="DotEnvTokenType.Comment" /> token by default.
    /// </summary>
    [TestMethod]
    public void Read_WhenCommentLine_ShouldSurfaceCommentToken()
    {
        List<string> tokens = Transcribe("# a note\nK=v\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Comment: a note", "Name:K", "String:v", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that an inline comment following whitespace terminates an unquoted value.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineComment_ShouldTerminateUnquotedValue()
    {
        List<string> tokens = Transcribe("K=value # trailing\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:K", "String:value", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a <c>#</c> separated from the <c>=</c> by whitespace starts an inline comment, so the entry's value
    /// is empty rather than the comment text.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The DotEnv source text.</param>
    [TestMethod]
    [DataRow("one space", "KEY= # comment\n")]
    [DataRow("two spaces", "KEY=  # comment\n")]
    [DataRow("a tab", "KEY=\t# comment\n")]
    [DataRow("no final line feed", "KEY= # comment")]
    public void Read_WhenWhitespaceSeparatesTheAssignmentFromAHash_ShouldReadAnEmptyValue(string testName, string source)
    {
        _ = testName;

        List<string> tokens = Transcribe(source);

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:KEY", "String:", "EndObject" },
            tokens,
            string.Join(" | ", tokens));
    }

    /// <summary>
    /// Verifies that an entry whose value is only an inline comment does not swallow the entry on the next line.
    /// </summary>
    [TestMethod]
    public void Read_WhenAnEmptyValueIsFollowedByACommentAndAnotherEntry_ShouldReadBothEntries()
    {
        List<string> tokens = Transcribe("KEY= # comment\nOTHER=val\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:KEY", "String:", "Name:OTHER", "String:val", "EndObject" },
            tokens,
            string.Join(" | ", tokens));
    }

    /// <summary>
    /// Verifies that a <c>#</c> with no whitespace before it stays part of an unquoted value, whether it opens the
    /// value or follows other text.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="value">The expected value.</param>
    [TestMethod]
    [DataRow("a hash opening the value", "KEY=#value\n", "#value")]
    [DataRow("a hash inside a URL", "KEY=http://host/#anchor\n", "http://host/#anchor")]
    public void Read_WhenAHashHasNoWhitespaceBeforeIt_ShouldKeepItInTheValue(string testName, string source, string value)
    {
        _ = testName;

        List<string> tokens = Transcribe(source);

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:KEY", $"String:{value}", "EndObject" },
            tokens,
            string.Join(" | ", tokens));
    }

    /// <summary>
    /// Verifies that with inline comments disallowed, a <c>#</c> after the whitespace that follows <c>=</c> stays part
    /// of the value.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineCommentsAreDisallowed_ShouldKeepAHashAfterTheAssignmentInTheValue()
    {
        List<string> tokens = Transcribe("KEY= # comment\n", new DotEnvReaderOptions { DisallowInlineComments = true });

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:KEY", "String:# comment", "EndObject" },
            tokens,
            string.Join(" | ", tokens));
    }

    /// <summary>
    /// Verifies that a whitespace character other than space and tab is whitespace wherever the reader skips or trims
    /// whitespace: before a key, on a blank line, before a comment, after <c>export</c>, around <c>=</c>, after an
    /// unquoted value, and before an inline <c>#</c>.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="whitespace">The whitespace character, one to three UTF-8 bytes long.</param>
    [TestMethod]
    [DataRow("form feed", "\f")]
    [DataRow("vertical tab", "\v")]
    [DataRow("no-break space", " ")]
    [DataRow("ideographic space", "　")]
    public void Read_WhenWhitespaceIsNotSpaceOrTab_ShouldTreatItAsWhitespaceInEveryPosition(string testName, string whitespace)
    {
        _ = testName;
        string w = whitespace;
        (string Position, string Source, string Expected)[] cases =
        [
            ("before a key", $"{w}KEY=value\n", "KEY=value"),
            ("on a blank line", $"{w}\nKEY=value\n", "KEY=value"),
            ("before a comment", $"{w}# note\nKEY=value\n", "KEY=value"),
            ("after export", $"export{w}KEY=value\n", "export KEY=value"),
            ("before =", $"KEY{w}=value\n", "KEY=value"),
            ("after =", $"KEY={w}value\n", "KEY=value"),
            ("after an unquoted value", $"KEY=value{w}\n", "KEY=value"),
            ("before an inline #", $"KEY=value{w}# note\n", "KEY=value"),
            ("between = and an inline #", $"KEY={w}# note\n", "KEY="),
            ("in every position at once", $"{w}export{w}KEY{w}={w}value{w}#{w}note{w}\n", "export KEY=value"),
        ];
        var problems = new List<string>();

        foreach ((string position, string source, string expected) in cases)
        {
            try
            {
                List<string> entries = ReadEntryTexts(source);
                if (entries.Count != 1 || entries[0] != expected)
                    problems.Add($"{position}: read [{string.Join(", ", entries)}], expected [{expected}]");
            }
            catch (DotEnvFormatException ex)
            {
                problems.Add($"{position}: threw {ex.Message}");
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that an empty document produces only the framing object tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyDocument_ShouldProduceOnlyFraming()
    {
        List<string> tokens = Transcribe(string.Empty);

        CollectionAssert.AreEqual(new List<string> { "StartObject", "EndObject" }, tokens);
    }

    /// <summary>
    /// Verifies that a key with no assignment operator throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenMissingAssignment_ShouldThrowDotEnvFormatException()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = Transcribe("KEY\n");
        });
    }

    /// <summary>
    /// Verifies that an unterminated double-quoted value throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnterminatedDoubleQuote_ShouldThrowDotEnvFormatException()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = Transcribe("K=\"open\n");
        });
    }

    /// <summary>
    /// Verifies that a rejected entry is reported at the line, the 1-based column and the byte offset at which the
    /// reader detected the error, the column counted in bytes from the start of that byte's line.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="line">The expected 1-based line number.</param>
    /// <param name="column">The expected 1-based column number.</param>
    /// <param name="offset">The expected zero-based byte offset.</param>
    [TestMethod]
    [DataRow("invalid key at the start of the first line", "=abc\n", 1, 1, 0)]
    [DataRow("missing assignment on the first line", "API KEY=abc\n", 1, 5, 4)]
    [DataRow("invalid key after the export prefix", "export -KEY=v\n", 1, 8, 7)]
    [DataRow("indented invalid key on a later line", "A=1\n  1KEY=v\n", 2, 3, 6)]
    [DataRow("missing assignment on a later line", "A=1\nB=2\nKEY value\n", 3, 5, 12)]
    [DataRow("missing assignment after a CRLF line ending", "A=1\r\nKEY value\r\n", 2, 5, 9)]
    [DataRow("missing assignment after a lone CR line ending", "A=1\rKEY value\r", 2, 5, 8)]
    [DataRow("missing assignment after a byte-order mark", "﻿KEY value\n", 1, 5, 7)]
    [DataRow("unterminated double quote at the end of the input", "KEY=\"abc", 1, 9, 8)]
    [DataRow("unterminated single quote on a later line", "A=1\nKEY=  'abc\n", 2, 8, 11)]
    public void Read_WhenEntryIsMalformed_ShouldReportTheLineColumnAndOffsetOfTheError(string testName, string source, int line, int column, int offset)
    {
        _ = testName;

        DotEnvFormatException ex = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = Transcribe(source);
        });

        Assert.AreEqual(line, ex.LineNumber, "LineNumber");
        Assert.AreEqual(column, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(offset, ex.Offset, "Offset");
    }

    /// <summary>
    /// Verifies that a million consecutive comment lines are read without exhausting the stack, whether the reader
    /// reports or skips them, and that the one entry after them is read.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="skipComments">Whether the reader skips comment lines instead of reporting them.</param>
    [TestMethod]
    [DataRow("comment lines reported", false)]
    [DataRow("comment lines skipped", true)]
    public void Read_WhenManyConsecutiveCommentLines_ShouldNotOverflowTheStack(string testName, bool skipComments)
    {
        _ = testName;
        byte[] bytes = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("# comment\n", 1_000_000)) + "FOO=bar\n");
        var reader = new Utf8DotEnvReader(bytes, new DotEnvReaderOptions { SkipComments = skipComments });
        var entries = new List<string>();
        string? key = null;

        while (reader.Read())
        {
            if (reader.TokenType == DotEnvTokenType.PropertyName)
                key = reader.GetString();
            else if (reader.TokenType == DotEnvTokenType.String)
                entries.Add($"{key}={reader.GetString()}");
        }

        CollectionAssert.AreEqual(new List<string> { "FOO=bar" }, entries);
    }

    /// <summary>
    /// Verifies that a quoted value of two million characters, longer than any entry bound the library declares, is
    /// read whole rather than cut short or dropped.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="quote">The quote character that delimits the value.</param>
    [TestMethod]
    [DataRow("double-quoted", "\"")]
    [DataRow("single-quoted", "'")]
    public void Read_WhenQuotedValueIsVeryLong_ShouldReturnWholeValue(string testName, string quote)
    {
        _ = testName;
        const int Length = 2_000_000;
        byte[] bytes = Encoding.UTF8.GetBytes($"KEY={quote}{new string('a', Length)}{quote}\n");
        var reader = new Utf8DotEnvReader(bytes);
        string? value = null;

        while (reader.Read())
        {
            if (reader.TokenType == DotEnvTokenType.String)
                value = reader.GetString();
        }

        Assert.IsNotNull(value);
        Assert.AreEqual(Length, value.Length);
        Assert.IsFalse(value.AsSpan().ContainsAnyExcept('a'), "The value holds a character the input does not.");
    }
}
