// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedWriterTests.WriteRow.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedWriterTests
{
    /// <summary>
    /// Verifies that <see cref="DelimitedWriter.WriteRow" /> with a <see langword="null" /> argument throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenFieldsIsNull_ShouldThrowArgumentNullException()
    {
        using var sw = new StringWriter();
        using DelimitedWriter writer = new(sw);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            writer.WriteRow(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedWriter.WriteRow" /> throws <see cref="ObjectDisposedException" />
    /// after the writer has been disposed.
    /// </summary>
    [TestMethod]
    public void WriteRow_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var sw = new StringWriter();
        DelimitedWriter writer = new(sw);
        writer.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            writer.WriteRow(["a"]);
        });
    }

    /// <summary>
    /// Verifies that writing a single unquoted field produces the field value followed by a line feed.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenSingleUnquotedField_ShouldWriteFieldAndNewline()
    {
        var sw = new StringWriter();
        using (DelimitedWriter writer = new(sw))
            writer.WriteRow(["hello"]);

        Assert.AreEqual("hello\n", sw.ToString());
    }

    /// <summary>
    /// Verifies that writing multiple fields separates them with the configured delimiter.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenMultipleFields_ShouldSeparateWithDelimiter()
    {
        var sw = new StringWriter();
        using (DelimitedWriter writer = new(sw))
            writer.WriteRow(["a", "b", "c"]);

        Assert.AreEqual("a,b,c\n", sw.ToString());
    }

    /// <summary>
    /// Verifies that a field containing the delimiter character is enclosed in quotes.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenFieldContainsDelimiter_ShouldQuoteField()
    {
        var sw = new StringWriter();
        using (DelimitedWriter writer = new(sw))
            writer.WriteRow(["New York, NY"]);

        Assert.AreEqual("\"New York, NY\"\n", sw.ToString());
    }

    /// <summary>
    /// Verifies that a quote character embedded in a quoted field is doubled per RFC 4180 §2, rule 7.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenFieldContainsQuote_ShouldDoubleQuoteInOutput()
    {
        var sw = new StringWriter();
        using (DelimitedWriter writer = new(sw))
            writer.WriteRow(["say \"hi\""]);

        Assert.AreEqual("\"say \"\"hi\"\"\"\n", sw.ToString());
    }

    /// <summary>
    /// Verifies that an empty field is enclosed in quotes so it survives a round-trip through the parser.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenFieldIsEmpty_ShouldWriteEmptyQuotedField()
    {
        var sw = new StringWriter();
        using (DelimitedWriter writer = new(sw))
            writer.WriteRow([""]);

        Assert.AreEqual("\"\"\n", sw.ToString());
    }

    /// <summary>
    /// Verifies that a field containing a newline character is enclosed in quotes with the newline
    /// preserved literally (RFC 4180 §2, rule 6).
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenFieldContainsNewline_ShouldQuoteAndPreserveNewline()
    {
        var sw = new StringWriter();
        using (DelimitedWriter writer = new(sw))
            writer.WriteRow(["line1\nline2"]);

        Assert.AreEqual("\"line1\nline2\"\n", sw.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedWriter.WriteHeader" /> writes the header fields followed by a
    /// line feed and does not increment <see cref="DelimitedWriter.RowsWritten" />.
    /// </summary>
    [TestMethod]
    public void WriteHeader_WhenCalled_ShouldWriteHeaderRowAndNotIncrementRowsWritten()
    {
        var sw = new StringWriter();
        using DelimitedWriter writer = new(sw);

        writer.WriteHeader(["name", "age"]);

        Assert.AreEqual("name,age\n", sw.ToString());
        Assert.AreEqual(0, writer.RowsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedWriter.WriteHeader" /> throws <see cref="ArgumentNullException" />
    /// when passed a <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void WriteHeader_WhenHeadersIsNull_ShouldThrowArgumentNullException()
    {
        using var sw = new StringWriter();
        using DelimitedWriter writer = new(sw);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            writer.WriteHeader(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedWriter.RowsWritten" /> increments once for each
    /// <see cref="DelimitedWriter.WriteRow" /> call and ignores header rows.
    /// </summary>
    [TestMethod]
    public void RowsWritten_AfterMultipleWriteRowCalls_ShouldTrackDataRowsOnly()
    {
        var sw = new StringWriter();
        using DelimitedWriter writer = new(sw);

        writer.WriteHeader(["h"]);
        Assert.AreEqual(0, writer.RowsWritten);

        writer.WriteRow(["r1"]);
        Assert.AreEqual(1, writer.RowsWritten);

        writer.WriteRow(["r2"]);
        Assert.AreEqual(2, writer.RowsWritten);
    }

    /// <summary>
    /// Verifies that a <see cref="DelimitedParseOptions.Delimiter" /> other than the default comma is
    /// used as the field separator.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenCustomDelimiter_ShouldUseThatDelimiter()
    {
        var sw = new StringWriter();
        var options = new DelimitedParseOptions { Delimiter = '\t' };
        using (DelimitedWriter writer = new(sw, options))
            writer.WriteRow(["a", "b", "c"]);

        Assert.AreEqual("a\tb\tc\n", sw.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.CreateWriter(TextWriter)" /> returns a functional
    /// <see cref="DelimitedWriter" /> that produces correct output.
    /// </summary>
    [TestMethod]
    public void CreateWriter_WhenCalledOnDelimitedClass_ShouldReturnWorkingWriter()
    {
        var sw = new StringWriter();
        using DelimitedWriter writer = Delimited.CreateWriter(sw);

        writer.WriteHeader(["x", "y"]);
        writer.WriteRow(["1", "2"]);

        Assert.AreEqual("x,y\n1,2\n", sw.ToString());
    }

    /// <summary>
    /// Verifies that output written by <see cref="DelimitedWriter" /> can be read back by
    /// <see cref="DelimitedReader" /> with identical headers and field values.
    /// </summary>
    [TestMethod]
    public void WriteRow_RoundTripWithDelimitedReader_ShouldPreserveAllFields()
    {
        string[][] data =
        [
            ["Alice", "30", "New York, NY"],
            ["Bob", "25", "say \"hi\""],
            ["Carol", "35", ""],
            ["Dave", "40", "line1\nline2"],
        ];

        // Write
        var sw = new StringWriter();
        using (DelimitedWriter writer = new(sw))
        {
            writer.WriteHeader(["name", "age", "note"]);
            foreach (string[] row in data)
                writer.WriteRow(row);
        }

        // Read back — Headers is populated only after the first Read() call.
        using DelimitedReader reader = new(new StringReader(sw.ToString()));

        int rowIndex = 0;
        while (reader.Read())
        {
            Assert.AreEqual(data[rowIndex][0], reader.Fields[0], $"Row {rowIndex} field 0");
            Assert.AreEqual(data[rowIndex][1], reader.Fields[1], $"Row {rowIndex} field 1");
            Assert.AreEqual(data[rowIndex][2], reader.Fields[2], $"Row {rowIndex} field 2");
            rowIndex++;
        }

        Assert.AreEqual(data.Length, rowIndex, "Row count mismatch.");
        CollectionAssert.AreEqual(new[] { "name", "age", "note" }, reader.Headers.ToArray());
    }
}
