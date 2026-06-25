// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedTests
{

    /// <summary>
    /// Verifies that <see cref="Delimited.TryParse(ReadOnlySpan{char}, out DelimitedDocument)" /> returns
    /// <see langword="true" /> and a non-null document for valid input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsValid_ShouldReturnTrueAndDocument()
    {
        bool result = Delimited.TryParse("a,b\n1,2", out DelimitedDocument? document);

        Assert.IsTrue(result);
        Assert.IsNotNull(document);
        Assert.HasCount(1, document.Rows);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.TryParse(ReadOnlySpan{char}, out DelimitedDocument)" /> returns
    /// <see langword="true" /> and an empty document for empty input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsEmpty_ShouldReturnTrueWithEmptyDocument()
    {
        bool result = Delimited.TryParse(string.Empty, out DelimitedDocument? document);

        Assert.IsTrue(result);
        Assert.IsNotNull(document);
        Assert.IsEmpty(document.Rows);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.TryParse(ReadOnlySpan{char}, out DelimitedDocument)" /> returns
    /// <see langword="false" /> and a <see langword="null" /> document for an unterminated quoted field.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenQuotedFieldIsUnterminated_ShouldReturnFalseWithNull()
    {
        bool result = Delimited.TryParse("a\n\"unclosed", out DelimitedDocument? document);

        Assert.IsFalse(result);
        Assert.IsNull(document);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.TryParse(ReadOnlySpan{char}, DelimitedParseOptions, out DelimitedDocument)" />
    /// respects the supplied options.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTabDelimiterUsed_ShouldParseTabSeparatedFields()
    {
        DelimitedParseOptions options = new() { Delimiter = '\t' };

        bool result = Delimited.TryParse("a\tb\n1\t2", options, out DelimitedDocument? document);

        Assert.IsTrue(result);
        Assert.IsNotNull(document);
        Assert.AreEqual("1", document.Rows[0]["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.TryParse(ReadOnlySpan{char}, out DelimitedDocument)" /> does not throw
    /// even when the input is malformed.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsMalformed_ShouldNotThrow()
    {
        bool result = false;
        DelimitedDocument? document = null;

        Exception? caughtException = null;

        try
        {
            result = Delimited.TryParse("col\n\"unterminated", out document);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        Assert.IsNull(caughtException, $"TryParse threw {caughtException?.GetType().Name}.");
        Assert.IsFalse(result);
        Assert.IsNull(document);
    }

}
