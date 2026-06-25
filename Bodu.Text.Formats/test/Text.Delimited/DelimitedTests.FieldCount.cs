// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.FieldCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedTests
{
    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> rejects rows that have fewer fields than
    /// the header row under the default <see cref="DelimitedFieldCountBehavior.Strict" /> policy applied when a
    /// header is present.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRowHasFewerFieldsThanHeader_ShouldThrowDelimitedFormatException()
    {
        DelimitedFormatException ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Parse("A,B,C\n1,2");
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> rejects rows that have more fields than
    /// the header row under the default <see cref="DelimitedFieldCountBehavior.Strict" /> policy applied when a
    /// header is present.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRowHasMoreFieldsThanHeader_ShouldThrowDelimitedFormatException()
    {
        DelimitedFormatException ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Parse("A,B\n1,2,3");
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedFieldCountBehavior.Ragged" /> retains the prior lenient behaviour where
    /// rows of any size are accepted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRowsAreRagged_AndRaggedBehavior_ShouldAcceptInput()
    {
        DelimitedParseOptions options = new()
        {
            FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,
        };

        DelimitedDocument doc = Delimited.Parse("A,B,C\n1,2\n3,4,5,6", options);

        Assert.HasCount(2, doc.Rows);
        Assert.AreEqual(2, doc.Rows[0].Count);
        Assert.AreEqual(4, doc.Rows[1].Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedFieldCountBehavior" /> defaults to <see cref="DelimitedFieldCountBehavior.Strict" />
    /// when the dialect has a header row.
    /// </summary>
    [TestMethod]
    public void FieldCountBehavior_WhenOptionsAreDefault_ShouldBeStrict()
    {
        DelimitedParseOptions options = DelimitedParseOptions.Default;

        Assert.AreEqual(DelimitedFieldCountBehavior.Strict, options.FieldCountBehavior);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedFieldCountBehavior.Strict" /> does not apply when there is no header row,
    /// since there is no reference field count to enforce. Headerless ragged input is accepted unchanged.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNoHeaderAndRaggedRows_ShouldAcceptInput()
    {
        DelimitedParseOptions options = new() { HasHeader = false };

        DelimitedDocument doc = Delimited.Parse("1,2\n3,4,5", options);

        Assert.HasCount(2, doc.Rows);
        Assert.AreEqual(2, doc.Rows[0].Count);
        Assert.AreEqual(3, doc.Rows[1].Count);
    }
}
