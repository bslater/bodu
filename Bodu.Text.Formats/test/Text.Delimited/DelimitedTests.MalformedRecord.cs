// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.MalformedRecord.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedTests
{
    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> rejects an unescaped character following a
    /// closing quote by default. Silently skipping to the end of the record would discard every field after the
    /// malformed one and is treated as a parser error.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCharacterFollowsClosingQuote_ShouldThrowDelimitedFormatException()
    {
        DelimitedFormatException ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Parse("name\n\"a\"x,b");
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedMalformedRecordBehavior.SkipRecord" /> restores the legacy lenient
    /// behaviour: any characters following a closing quote are discarded along with the rest of the record.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCharacterFollowsClosingQuote_AndSkipRecord_ShouldKeepClosedFieldOnly()
    {
        DelimitedParseOptions options = new()
        {
            HasHeader = false,
            MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord,
        };

        DelimitedDocument doc = Delimited.Parse("\"a\"x,b", options);

        Assert.AreEqual(1, doc.Rows.Count);
        Assert.AreEqual(1, doc.Rows[0].Count);
        Assert.AreEqual("a", doc.Rows[0][0]);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedMalformedRecordBehavior.Throw" /> is the explicit default policy.
    /// </summary>
    [TestMethod]
    public void MalformedRecordBehavior_WhenOptionsAreDefault_ShouldBeThrow()
    {
        DelimitedParseOptions options = DelimitedParseOptions.Default;

        Assert.AreEqual(DelimitedMalformedRecordBehavior.Throw, options.MalformedRecordBehavior);
    }
}
