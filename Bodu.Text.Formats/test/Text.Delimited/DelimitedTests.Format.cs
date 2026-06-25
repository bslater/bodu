// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.Format.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedTests
{

    /// <summary>
    /// Verifies that <see cref="Delimited.Format(DelimitedDocument)" /> throws <see cref="ArgumentNullException" />
    /// when the document is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Delimited.Format(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Format(DelimitedDocument)" /> produces an empty string for an empty
    /// document.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocumentIsEmpty_ShouldReturnEmptyString()
    {
        DelimitedDocument doc = Delimited.Parse(string.Empty);

        string formatted = Delimited.Format(doc);

        Assert.AreEqual(string.Empty, formatted);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Format(DelimitedDocument)" /> writes the header row when headers are
    /// present.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocumentHasHeaders_ShouldWriteHeaderRow()
    {
        DelimitedDocument doc = Delimited.Parse("name,age\nAlice,30");

        string formatted = Delimited.Format(doc);

        StringAssert.StartsWith(formatted, "name,age");
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Format(DelimitedDocument)" /> does not write a header line when the
    /// document has no headers.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocumentHasNoHeaders_ShouldNotWriteHeaderRow()
    {
        DelimitedParseOptions options = new() { HasHeader = false };
        DelimitedDocument doc = Delimited.Parse("1,2,3", options);

        string formatted = Delimited.Format(doc);

        StringAssert.StartsWith(formatted, "1,2,3");
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Format(DelimitedDocument)" /> double-quotes a field that contains the
    /// delimiter character.
    /// </summary>
    [TestMethod]
    public void Format_WhenFieldContainsDelimiter_ShouldQuoteField()
    {
        DelimitedDocument doc = Delimited.Parse("val\n\"a,b\"");

        string formatted = Delimited.Format(doc);

        StringAssert.Contains(formatted, "\"a,b\"");
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Format(DelimitedDocument)" /> doubles a quote character inside a quoted
    /// field.
    /// </summary>
    [TestMethod]
    public void Format_WhenFieldContainsQuoteChar_ShouldDoubleIt()
    {
        DelimitedDocument doc = Delimited.Parse("q\n\"say \"\"hi\"\"\"");

        string formatted = Delimited.Format(doc);

        StringAssert.Contains(formatted, "\"\"");
    }

    /// <summary>
    /// Verifies that a round-trip through <see cref="Delimited.Parse(ReadOnlySpan{char})" /> and
    /// <see cref="Delimited.Format(DelimitedDocument)" /> preserves all headers and field values.
    /// </summary>
    [TestMethod]
    public void Format_WhenRoundTripped_ShouldPreserveDocumentContent()
    {
        const string source = "name,age,city\nAlice,30,Paris\nBob,25,London";

        DelimitedDocument original = Delimited.Parse(source);
        string formatted = Delimited.Format(original);
        DelimitedDocument roundTripped = Delimited.Parse(formatted);

        Assert.HasCount(2, roundTripped.Rows);
        Assert.AreEqual("Alice", roundTripped.Rows[0]["name"]);
        Assert.AreEqual("30", roundTripped.Rows[0]["age"]);
        Assert.AreEqual("London", roundTripped.Rows[1]["city"]);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Format(DelimitedDocument, DelimitedParseOptions)" /> uses the delimiter
    /// character specified in the options.
    /// </summary>
    [TestMethod]
    public void Format_WhenTabDelimiterSpecified_ShouldUseTabInOutput()
    {
        DelimitedParseOptions options = new() { Delimiter = '\t' };
        DelimitedDocument doc = Delimited.Parse("a\tb\n1\t2", options);

        string formatted = Delimited.Format(doc, options);

        StringAssert.Contains(formatted, "a\tb");
    }

}
