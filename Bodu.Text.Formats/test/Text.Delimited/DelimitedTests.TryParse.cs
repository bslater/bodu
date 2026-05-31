// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.TryParse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        var result = Delimited.TryParse("a,b\n1,2", out DelimitedDocument? document);

        Assert.IsTrue(result);
        Assert.IsNotNull(document);
        Assert.AreEqual(1, document.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.TryParse(ReadOnlySpan{char}, out DelimitedDocument)" /> returns
    /// <see langword="true" /> and an empty document for empty input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsEmpty_ShouldReturnTrueWithEmptyDocument()
    {
        var result = Delimited.TryParse(string.Empty, out DelimitedDocument? document);

        Assert.IsTrue(result);
        Assert.IsNotNull(document);
        Assert.AreEqual(0, document.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Delimited.TryParse(ReadOnlySpan{char}, out DelimitedDocument)" /> returns
    /// <see langword="false" /> and a <see langword="null" /> document for an unterminated quoted field.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenQuotedFieldIsUnterminated_ShouldReturnFalseWithNull()
    {
        var result = Delimited.TryParse("a\n\"unclosed", out DelimitedDocument? document);

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

        var result = Delimited.TryParse("a\tb\n1\t2", options, out DelimitedDocument? document);

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
        var result = false;
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
