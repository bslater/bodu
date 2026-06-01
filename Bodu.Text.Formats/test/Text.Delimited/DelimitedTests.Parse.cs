// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedTests
{

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> returns an empty document for an empty
    /// source string.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSourceIsEmpty_ShouldReturnEmptyDocument()
    {
        DelimitedDocument doc = Delimited.Parse(string.Empty);

        Assert.AreEqual(0, doc.Headers.Count);
        Assert.AreEqual(0, doc.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> parses a simple single-row document with
    /// a header and one data row.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSimpleCsv_ShouldParseHeaderAndDataRow()
    {
        DelimitedDocument doc = Delimited.Parse("name,age\nAlice,30");

        Assert.AreEqual(2, doc.Headers.Count);
        Assert.AreEqual(1, doc.Rows.Count);
        Assert.AreEqual("Alice", doc.Rows[0]["name"]);
        Assert.AreEqual("30", doc.Rows[0]["age"]);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> handles an empty field (two consecutive
    /// delimiters) as an empty string.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFieldIsEmpty_ShouldReturnEmptyString()
    {
        DelimitedDocument doc = Delimited.Parse("a,b,c\n1,,3");

        Assert.AreEqual(string.Empty, doc.Rows[0]["b"]);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> strips surrounding double quotes from a
    /// quoted field.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFieldIsQuoted_ShouldStripQuotes()
    {
        DelimitedDocument doc = Delimited.Parse("phrase\n\"hello world\"");

        Assert.AreEqual("hello world", doc.Rows[0]["phrase"]);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> resolves two consecutive quote characters
    /// inside a quoted field as a single literal quote.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuotedFieldContainsDoubledQuote_ShouldProduceLiteralQuote()
    {
        DelimitedDocument doc = Delimited.Parse("q\n\"say \"\"hi\"\"\"");

        Assert.AreEqual("say \"hi\"", doc.Rows[0]["q"]);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> preserves literal newlines inside a
    /// quoted field.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuotedFieldSpansMultipleLines_ShouldPreserveLiteralNewlines()
    {
        DelimitedDocument doc = Delimited.Parse("note\n\"line one\nline two\"");

        Assert.AreEqual("line one\nline two", doc.Rows[0]["note"]);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> handles CRLF line endings correctly.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSourceHasCrlfLineEndings_ShouldParseCorrectly()
    {
        DelimitedDocument doc = Delimited.Parse("a,b\r\n1,2\r\n3,4");

        Assert.AreEqual(2, doc.Rows.Count);
        Assert.AreEqual("1", doc.Rows[0]["a"]);
        Assert.AreEqual("3", doc.Rows[1]["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> skips blank lines.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSourceContainsBlankLines_ShouldSkipThem()
    {
        DelimitedDocument doc = Delimited.Parse("name\n\nAlice\n\nBob");

        Assert.AreEqual(2, doc.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> parses multiple data rows correctly.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultipleDataRows_ShouldParseAll()
    {
        DelimitedDocument doc = Delimited.Parse("x\n1\n2\n3");

        Assert.AreEqual(3, doc.Rows.Count);
        Assert.AreEqual("1", doc.Rows[0]["x"]);
        Assert.AreEqual("2", doc.Rows[1]["x"]);
        Assert.AreEqual("3", doc.Rows[2]["x"]);
    }

    /// <summary>
    /// Verifies that a quoted field containing the delimiter character is parsed correctly.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuotedFieldContainsDelimiter_ShouldTreatItAsPartOfField()
    {
        DelimitedDocument doc = Delimited.Parse("val\n\"a,b,c\"");

        Assert.AreEqual("a,b,c", doc.Rows[0]["val"]);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> correctly parses a source with no
    /// trailing newline.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSourceHasNoTrailingNewline_ShouldParseLastRow()
    {
        DelimitedDocument doc = Delimited.Parse("a\n1");

        Assert.AreEqual(1, doc.Rows.Count);
        Assert.AreEqual("1", doc.Rows[0]["a"]);
    }

}
