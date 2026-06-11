// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public partial class DelimitedTests
{
    /// <summary>
    /// Verifies that, under <see cref="DelimitedDuplicateHeaderBehavior.AllowDuplicates" />, a document with repeated
    /// header names parses without throwing.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateHeadersAllowed_ShouldParseWithoutThrowing()
    {
        var options = new DelimitedParseOptions { DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.AllowDuplicates };

        DelimitedDocument doc = Delimited.Parse("a,a,b\n1,2,3", options);

        Assert.AreEqual(3, doc.Headers.Count);
    }

    /// <summary>
    /// Verifies that a data row whose field count differs from the header throws
    /// <see cref="DelimitedFormatException" /> under the strict field-count policy.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRowFieldCountDiffersUnderStrict_ShouldThrow()
    {
        Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Parse("a,b\n1,2,3");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.CreateWriter(TextWriter, DelimitedParseOptions)" /> honors the supplied
    /// delimiter.
    /// </summary>
    [TestMethod]
    public void CreateWriter_WithOptions_ShouldEmitUsingDelimiter()
    {
        StringWriter sink = new();
        var options = new DelimitedParseOptions { Delimiter = '\t', HasHeader = false };

        using (DelimitedWriter writer = Delimited.CreateWriter(sink, options))
            writer.WriteRow(["a", "b"]);

        Assert.Contains("a\tb", sink.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.CreateReader(TextReader, DelimitedParseOptions)" /> streams rows using the
    /// supplied options.
    /// </summary>
    [TestMethod]
    public void CreateReader_WithOptions_ShouldStreamRows()
    {
        var options = new DelimitedParseOptions { HasHeader = false };

        using DelimitedReader reader = Delimited.CreateReader(new StringReader("a,b\n"), options);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("a", reader.Fields[0]);
    }
}
