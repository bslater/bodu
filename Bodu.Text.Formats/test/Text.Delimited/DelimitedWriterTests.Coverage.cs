// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedWriterTests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public partial class DelimitedWriterTests
{
    /// <summary>
    /// Verifies that a field containing the quote character is wrapped in quotes with the embedded quote doubled.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenFieldContainsQuote_ShouldQuoteAndDoubleIt()
    {
        StringWriter sink = new();

        using (DelimitedWriter writer = new(sink, new DelimitedParseOptions { HasHeader = false }))
            writer.WriteRow(new[] { "a\"b" });

        Assert.Contains("\"a\"\"b\"", sink.ToString());
    }

    /// <summary>
    /// Verifies that disposing a <see cref="DelimitedWriter" /> twice is a no-op on the second call.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        DelimitedWriter writer = new(new StringWriter());

        writer.Dispose();
        writer.Dispose();
    }
}
