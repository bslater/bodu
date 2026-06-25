// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.Options.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedTests
{

    // ------------------------------------------------ HasHeader -------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.HasHeader" /> set to <see langword="true" /> (the default)
    /// causes the first row to be treated as a header row and excluded from
    /// <see cref="DelimitedDocument.Rows" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHasHeaderEnabled_ShouldTreatFirstRowAsHeader()
    {
        DelimitedDocument doc = Delimited.Parse("col\nvalue");

        Assert.HasCount(1, doc.Headers);
        Assert.HasCount(1, doc.Rows);
        Assert.AreEqual("col", doc.Headers[0]);
        Assert.AreEqual("value", doc.Rows[0]["col"]);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.HasHeader" /> set to <see langword="false" /> treats all
    /// records as data rows.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHasHeaderDisabled_ShouldTreatAllRowsAsData()
    {
        DelimitedParseOptions options = new() { HasHeader = false };

        DelimitedDocument doc = Delimited.Parse("1,2\n3,4", options);

        Assert.IsEmpty(doc.Headers);
        Assert.HasCount(2, doc.Rows);
    }

    // ----------------------------------------------- Delimiter --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.Delimiter" /> set to <c>'\t'</c> parses tab-separated
    /// values correctly.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDelimiterIsTab_ShouldParseTabSeparatedFields()
    {
        DelimitedParseOptions options = new() { Delimiter = '\t' };

        DelimitedDocument doc = Delimited.Parse("a\tb\n1\t2", options);

        Assert.AreEqual("1", doc.Rows[0]["a"]);
        Assert.AreEqual("2", doc.Rows[0]["b"]);
    }

    /// <summary>
    /// Verifies that a comma is treated as a regular field character when the delimiter is set to <c>'\t'</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDelimiterIsTab_ShouldTreatCommaAsFieldContent()
    {
        DelimitedParseOptions options = new() { Delimiter = '\t' };

        DelimitedDocument doc = Delimited.Parse("val\nhas,comma", options);

        Assert.AreEqual("has,comma", doc.Rows[0]["val"]);
    }

    // ------------------------------------------------- Quote ----------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.Quote" /> set to <c>'\''</c> uses single quotes for field
    /// quoting.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuoteIsSingleQuote_ShouldUseSingleQuoteForQuoting()
    {
        DelimitedParseOptions options = new() { Quote = '\'' };

        DelimitedDocument doc = Delimited.Parse("val\n'hello, world'", options);

        Assert.AreEqual("hello, world", doc.Rows[0]["val"]);
    }

    // ----------------------------------------------- TrimFields -------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.TrimFields" /> set to <see langword="true" /> removes
    /// leading and trailing whitespace from unquoted field values.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTrimFieldsEnabled_ShouldTrimUnquotedFields()
    {
        DelimitedParseOptions options = new() { TrimFields = true };

        DelimitedDocument doc = Delimited.Parse("a\n  hello  ", options);

        Assert.AreEqual("hello", doc.Rows[0]["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.TrimFields" /> set to <see langword="false" /> (the default)
    /// preserves leading and trailing whitespace in unquoted fields.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTrimFieldsDisabled_ShouldPreserveWhitespaceInUnquotedFields()
    {
        DelimitedDocument doc = Delimited.Parse("a\n  hello  ");

        Assert.AreEqual("  hello  ", doc.Rows[0]["a"]);
    }

    // ---------------------------------------------- AllowComments -----------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.AllowComments" /> set to <see langword="true" /> causes
    /// lines starting with the <see cref="DelimitedParseOptions.CommentChar" /> to be skipped.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowCommentsEnabled_ShouldSkipCommentLines()
    {
        DelimitedParseOptions options = new() { AllowComments = true };

        DelimitedDocument doc = Delimited.Parse("col\n# this is a comment\nvalue", options);

        Assert.HasCount(1, doc.Rows);
        Assert.AreEqual("value", doc.Rows[0]["col"]);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.AllowComments" /> set to <see langword="false" /> (the
    /// default) treats lines starting with <c>#</c> as regular data records.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowCommentsDisabled_ShouldTreatHashLinesAsData()
    {
        DelimitedDocument doc = Delimited.Parse("col\n#notacomment", DelimitedParseOptions.Default);

        Assert.HasCount(1, doc.Rows);
        Assert.AreEqual("#notacomment", doc.Rows[0]["col"]);
    }

    /// <summary>
    /// Verifies that a custom <see cref="DelimitedParseOptions.CommentChar" /> is respected when
    /// <see cref="DelimitedParseOptions.AllowComments" /> is <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCustomCommentChar_ShouldSkipLinesStartingWithThatChar()
    {
        DelimitedParseOptions options = new() { AllowComments = true, CommentChar = ';' };

        DelimitedDocument doc = Delimited.Parse("col\n; skip this\nvalue", options);

        Assert.HasCount(1, doc.Rows);
        Assert.AreEqual("value", doc.Rows[0]["col"]);
    }

}
