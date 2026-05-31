// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.DuplicateHeaders.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedTests
{
    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> throws
    /// <see cref="DelimitedFormatException" /> by default when the header row contains a duplicate column name.
    /// The default behaviour must reject the input rather than silently overwriting the first occurrence's index,
    /// which would otherwise hide the data of the first column from name-based lookup.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderHasDuplicateNames_ShouldThrowDelimitedFormatException()
    {
        DelimitedFormatException ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Parse("A,B,A\n1,2,3");
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.IsTrue(ex.Message.Contains("A", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDuplicateHeaderBehavior.FirstWins" /> retains the index of the first
    /// occurrence of a duplicated header name and exposes the first column's data via name-based lookup.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderHasDuplicateNames_AndFirstWins_ShouldMapNameToFirstIndex()
    {
        DelimitedParseOptions options = new()
        {
            DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.FirstWins,
        };

        DelimitedDocument doc = Delimited.Parse("A,B,A\n1,2,3", options);

        Assert.AreEqual("1", doc.Rows[0]["A"]);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDuplicateHeaderBehavior.LastWins" /> retains the index of the last
    /// occurrence of a duplicated header name and exposes the last column's data via name-based lookup.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderHasDuplicateNames_AndLastWins_ShouldMapNameToLastIndex()
    {
        DelimitedParseOptions options = new()
        {
            DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.LastWins,
        };

        DelimitedDocument doc = Delimited.Parse("A,B,A\n1,2,3", options);

        Assert.AreEqual("3", doc.Rows[0]["A"]);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDuplicateHeaderBehavior.Throw" /> is the explicit default policy.
    /// </summary>
    [TestMethod]
    public void DuplicateHeaderBehavior_WhenOptionsAreDefault_ShouldBeThrow()
    {
        DelimitedParseOptions options = DelimitedParseOptions.Default;

        Assert.AreEqual(DelimitedDuplicateHeaderBehavior.Throw, options.DuplicateHeaderBehavior);
    }
}
