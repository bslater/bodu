// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReaderTests.Read.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReaderTests
{
    /// <summary>
    /// Verifies that <see cref="DelimitedReader.Read" /> returns <see langword="false" /> immediately on
    /// empty input and leaves <see cref="DelimitedReader.Headers" /> empty.
    /// </summary>
    [TestMethod]
    public void Read_WhenInputIsEmpty_ShouldReturnFalse()
    {
        using DelimitedReader reader = new(new StringReader(string.Empty));

        var result = reader.Read();

        Assert.IsFalse(result);
        Assert.AreEqual(0, reader.Headers.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReader.Read" /> returns <see langword="false" /> when the source
    /// contains only a header row, and that <see cref="DelimitedReader.Headers" /> is populated correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenHeaderRowOnly_ShouldReturnFalseAndPopulateHeaders()
    {
        using DelimitedReader reader = new(new StringReader("name,age"));

        var result = reader.Read();

        Assert.IsFalse(result);
        CollectionAssert.AreEqual(new[] { "name", "age" }, reader.Headers.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReader.Read" /> returns <see langword="true" /> for the first
    /// data row and <see langword="false" /> on the subsequent call when the source has exactly one data row.
    /// </summary>
    [TestMethod]
    public void Read_WhenSingleDataRow_ShouldReturnTrueForFirstCallAndFalseForSecond()
    {
        using DelimitedReader reader = new(new StringReader("name,age\r\nAlice,30"));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("Alice", reader.Fields[0]);
        Assert.AreEqual("30", reader.Fields[1]);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReader.Read" /> advances through all data rows, returning their
    /// field values in source order.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultipleDataRows_ShouldReadAllRows()
    {
        var input = "a,b\r\n1,2\r\n3,4\r\n5,6";
        using DelimitedReader reader = new(new StringReader(input));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("1", reader.Fields[0]);
        Assert.AreEqual("2", reader.Fields[1]);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("3", reader.Fields[0]);
        Assert.AreEqual("4", reader.Fields[1]);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("5", reader.Fields[0]);
        Assert.AreEqual("6", reader.Fields[1]);

        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that when <see cref="DelimitedParseOptions.HasHeader" /> is <see langword="false" />, all
    /// rows are treated as data rows and <see cref="DelimitedReader.Headers" /> remains empty.
    /// </summary>
    [TestMethod]
    public void Read_WhenHasHeaderIsFalse_ShouldTreatFirstRowAsData()
    {
        var input = "1,2\r\n3,4";
        using DelimitedReader reader = new(new StringReader(input), new DelimitedParseOptions { HasHeader = false });

        Assert.AreEqual(0, reader.Headers.Count);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("1", reader.Fields[0]);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("3", reader.Fields[0]);

        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a quoted field containing the delimiter character is returned as a single field with
    /// the delimiter preserved as part of the value.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldContainsDelimiter_ShouldPreserveValue()
    {
        using DelimitedReader reader = new(new StringReader("city\r\n\"New York, NY\""));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.Fields.Count);
        Assert.AreEqual("New York, NY", reader.Fields[0]);
    }

    /// <summary>
    /// Verifies that an empty quoted field (<c>""</c>) is returned as an empty string rather than
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyQuotedField_ShouldReturnEmptyString()
    {
        using DelimitedReader reader = new(new StringReader("a\r\n\"\""));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(string.Empty, reader.Fields[0]);
    }

    /// <summary>
    /// Verifies that a quoted field containing an embedded newline is returned as a single multi-line field
    /// value.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldHasEmbeddedNewline_ShouldPreserveNewline()
    {
        using DelimitedReader reader = new(new StringReader("note\r\n\"line1\nline2\""));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("line1\nline2", reader.Fields[0]);
    }

    /// <summary>
    /// Verifies that two consecutive quote characters inside a quoted field are resolved to a single
    /// literal quote character (RFC 4180 §2, rule 7).
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldHasDoubledQuote_ShouldResolveSingleQuote()
    {
        using DelimitedReader reader = new(new StringReader("q\r\n\"say \"\"hi\"\"\""));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("say \"hi\"", reader.Fields[0]);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.TrimFields" /> trims leading and trailing whitespace
    /// from unquoted field values.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrimFieldsIsTrue_ShouldTrimUnquotedFields()
    {
        var options = new DelimitedParseOptions { TrimFields = true };
        using DelimitedReader reader = new(new StringReader("a,b\r\n  hello  ,  world  "), options);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("hello", reader.Fields[0]);
        Assert.AreEqual("world", reader.Fields[1]);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedParseOptions.TrimFields" /> does not alter the content of quoted
    /// fields — whitespace inside quotes is always preserved.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrimFieldsIsTrue_ShouldPreserveWhitespaceInQuotedFields()
    {
        var options = new DelimitedParseOptions { TrimFields = true };
        using DelimitedReader reader = new(new StringReader("a\r\n\"  hello  \""), options);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("  hello  ", reader.Fields[0]);
    }

    /// <summary>
    /// Verifies that lines whose first character is the comment character are skipped when
    /// <see cref="DelimitedParseOptions.AllowComments" /> is <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenAllowCommentsIsTrue_ShouldSkipCommentLines()
    {
        var input = "a,b\r\n# this is a comment\r\n1,2";
        var options = new DelimitedParseOptions { AllowComments = true };
        using DelimitedReader reader = new(new StringReader(input), options);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("1", reader.Fields[0]);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that blank lines between data rows are silently skipped.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlankLinesBetweenRows_ShouldSkipThem()
    {
        var input = "a,b\r\n1,2\r\n\r\n3,4";
        using DelimitedReader reader = new(new StringReader(input));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("1", reader.Fields[0]);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("3", reader.Fields[0]);

        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReader.Read" /> throws <see cref="ObjectDisposedException" />
    /// after <see cref="DelimitedReader.Dispose" /> has been called.
    /// </summary>
    [TestMethod]
    public void Read_AfterDispose_ShouldThrowExactly()
    {
        DelimitedReader reader = new(new StringReader("a,b\r\n1,2"));
        reader.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReader.RowNumber" /> increments by one for each data row
    /// successfully returned by <see cref="DelimitedReader.Read" />.
    /// </summary>
    [TestMethod]
    public void RowNumber_AfterSuccessfulReads_ShouldIncrementPerDataRow()
    {
        using DelimitedReader reader = new(new StringReader("h\r\nrow1\r\nrow2\r\nrow3"));

        Assert.AreEqual(0, reader.RowNumber);
        reader.Read(); Assert.AreEqual(1, reader.RowNumber);
        reader.Read(); Assert.AreEqual(2, reader.RowNumber);
        reader.Read(); Assert.AreEqual(3, reader.RowNumber);
        reader.Read(); Assert.AreEqual(3, reader.RowNumber); // exhausted, no increment
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReader.LineNumber" /> accurately reflects the line being
    /// processed, accounting for the header row and CRLF line endings.
    /// </summary>
    [TestMethod]
    public void LineNumber_WhenReadingWithCrlfEndings_ShouldTrackLineNumbers()
    {
        using DelimitedReader reader = new(new StringReader("h\r\nrow1\r\nrow2"));

        Assert.AreEqual(1, reader.LineNumber); // before first Read()

        reader.Read(); // consumes header (line 1) then row1 (line 2) — positioned at line 3
        Assert.AreEqual(3, reader.LineNumber);

        reader.Read(); // consumes row2 (line 3, no trailing newline)
        Assert.AreEqual(3, reader.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReader" /> reads a large input correctly when forced to refill
    /// the internal buffer frequently by using a buffer size of 1.
    /// </summary>
    [TestMethod]
    public void Read_WhenBufferSizeIsOne_ShouldReadAllFieldsCorrectly()
    {
        var input = "name,value\r\nAlice,100\r\nBob,200\r\nCarol,300";
        using DelimitedReader reader = new(new StringReader(input), DelimitedParseOptions.Default, bufferSize: 1);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("Alice", reader.Fields[0]);
        Assert.AreEqual("100", reader.Fields[1]);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("Bob", reader.Fields[0]);
        Assert.AreEqual("200", reader.Fields[1]);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("Carol", reader.Fields[0]);
        Assert.AreEqual("300", reader.Fields[1]);

        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a quoted field spanning multiple buffer fills is reconstructed correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldSpansBufferBoundary_ShouldReconstructCorrectly()
    {
        var input = "note\r\n\"hello, world\"";
        using DelimitedReader reader = new(new StringReader(input), DelimitedParseOptions.Default, bufferSize: 4);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("hello, world", reader.Fields[0]);
    }

    /// <summary>
    /// Verifies that an unterminated quoted field throws <see cref="DelimitedFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldIsUnterminated_ShouldThrowExactly()
    {
        using DelimitedReader reader = new(new StringReader("a\r\n\"no closing quote"));

        _ = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.CreateReader(TextReader)" /> returns a functional
    /// <see cref="DelimitedReader" /> that reads the source correctly.
    /// </summary>
    [TestMethod]
    public void CreateReader_WhenCalledOnDelimitedClass_ShouldReturnWorkingReader()
    {
        using DelimitedReader reader = Delimited.CreateReader(new StringReader("x,y\r\n1,2"));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("1", reader.Fields[0]);
        Assert.AreEqual("2", reader.Fields[1]);
    }
}
