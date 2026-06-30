// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReaderTests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public partial class DelimitedReaderTests
{
    /// <summary>
    /// Streams every data row from <paramref name="source" /> at the supplied buffer size.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="options">The parse options, or <see langword="null" /> for the defaults.</param>
    /// <param name="bufferSize">The reader buffer size in characters.</param>
    /// <returns>The data rows, each as an array of field values.</returns>
    private static List<string[]> StreamRows(string source, DelimitedParseOptions? options = null, int bufferSize = 1)
    {
        using DelimitedReader reader = new(new StringReader(source), options ?? DelimitedParseOptions.Default, bufferSize);
        List<string[]> rows = new();
        while (reader.Read())
            rows.Add(reader.Fields.ToArray());

        return rows;
    }

    /// <summary>
    /// Verifies that a leading UTF-8 byte-order mark is stripped before the first field is parsed.
    /// </summary>
    [TestMethod]
    public void Read_WhenSourceStartsWithByteOrderMark_ShouldStripIt()
    {
        List<string[]> rows = StreamRows("﻿a,b\nAda,42\n");

        Assert.AreEqual("Ada", rows[0][0]);
    }

    /// <summary>
    /// Verifies that disposing a <see cref="DelimitedReader" /> twice is a no-op on the second call.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        DelimitedReader reader = new(new StringReader("a,b\n"));

        reader.Dispose();
        reader.Dispose();
    }

    /// <summary>
    /// Verifies that leading blank lines are skipped before the header and first data row are parsed.
    /// </summary>
    [TestMethod]
    public void Read_WhenLeadingBlankLines_ShouldSkipThem()
    {
        List<string[]> rows = StreamRows("\n\nh\nv\n");

        CollectionAssert.AreEqual(new[] { "v" }, rows[0]);
    }

    /// <summary>
    /// Verifies that an input consisting only of blank lines yields no data rows.
    /// </summary>
    [TestMethod]
    public void Read_WhenOnlyBlankLines_ShouldReturnNoRows()
    {
        List<string[]> rows = StreamRows("\n\n", new DelimitedParseOptions { HasHeader = false });

        Assert.IsEmpty(rows);
    }

    /// <summary>
    /// Verifies that characters trailing a closing quote are skipped up to the line terminator.
    /// </summary>
    [TestMethod]
    public void Read_WhenContentAfterClosingQuote_ShouldSkipToLineEnd()
    {
        List<string[]> rows = StreamRows("\"a\"junk\nb\n", new DelimitedParseOptions { HasHeader = false });

        CollectionAssert.AreEqual(new[] { "a" }, rows[0]);
        CollectionAssert.AreEqual(new[] { "b" }, rows[1]);
    }

    /// <summary>
    /// Verifies that a lone carriage return embedded in a quoted field is preserved.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldContainsCarriageReturn_ShouldPreserveIt()
    {
        List<string[]> rows = StreamRows("\"a\rb\"\n", new DelimitedParseOptions { HasHeader = false });

        Assert.AreEqual("a\rb", rows[0][0]);
    }

    /// <summary>
    /// Verifies that a CRLF embedded in a quoted field is preserved verbatim.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldContainsCrlf_ShouldPreserveIt()
    {
        List<string[]> rows = StreamRows("\"a\r\nb\"\n", new DelimitedParseOptions { HasHeader = false });

        Assert.AreEqual("a\r\nb", rows[0][0]);
    }

    /// <summary>
    /// Verifies that a comment line at end of input without a trailing newline is skipped.
    /// </summary>
    [TestMethod]
    public void Read_WhenCommentLineAtEofWithoutNewline_ShouldSkipIt()
    {
        List<string[]> rows = StreamRows("a\n# trailing comment", new DelimitedParseOptions { HasHeader = false, AllowComments = true });

        CollectionAssert.AreEqual(new[] { "a" }, rows[0]);
        Assert.HasCount(1, rows);
    }
}
