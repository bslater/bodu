// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Headers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedReader.Headers" />.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that, with no header row, reading <see cref="Utf8DelimitedReader.Headers" /> after the document's start
    /// token gives an empty list and leaves the only record, <c>a,b</c>, still to be read.
    /// </summary>
    [TestMethod]
    public void Headers_WhenReadBeforeTheFirstRecordWithNoHeader_ShouldNotConsumeIt()
    {
        var reader = new Utf8DelimitedReader("a,b"u8, new DelimitedReaderOptions { NoHeader = true });

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(DelimitedTokenType.StartArray, reader.TokenType);
        Assert.AreEqual(0, reader.Headers.Count);

        var fields = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == DelimitedTokenType.String)
                fields.Add(reader.GetString());
        }

        CollectionAssert.AreEqual(new List<string> { "a", "b" }, fields);
    }
}
