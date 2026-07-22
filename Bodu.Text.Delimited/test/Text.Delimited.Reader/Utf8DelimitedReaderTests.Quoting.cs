// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Quoting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains field-level quoting and whitespace robustness tests for <see cref="Utf8DelimitedReader" />, pinning the
/// RFC 4180 quoting decisions and the trailing-field / whitespace edge cases.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that a quoted-empty field and an unquoted-empty field both decode to the empty string.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyFields_ShouldDecodeToEmptyString()
    {
        List<string> tokens = Transcribe("\"\",\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string> { "StartArray", "StartArray", "String:", "String:", "EndArray", "EndArray" },
            tokens);
    }

    /// <summary>
    /// Verifies that a trailing delimiter produces a trailing empty field rather than dropping it.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrailingDelimiter_ShouldYieldTrailingEmptyField()
    {
        List<string> tokens = Transcribe("a,b,\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string> { "StartArray", "StartArray", "String:a", "String:b", "String:", "EndArray", "EndArray" },
            tokens);
    }

    /// <summary>
    /// Verifies the RFC 4180-strict decision that whitespace before an opening quote makes the field unquoted, so the
    /// quotes are treated as literal content.
    /// </summary>
    [TestMethod]
    public void Read_WhenWhitespaceBeforeQuote_ShouldTreatQuotesAsLiteral()
    {
        List<string> tokens = Transcribe("a, \"b\"\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string> { "StartArray", "StartArray", "String:a", "String: \"b\"", "EndArray", "EndArray" },
            tokens);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReaderOptions.TrimFields" /> trims surrounding whitespace from unquoted fields.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrimFields_ShouldTrimUnquotedWhitespace()
    {
        List<string> tokens = Transcribe(" a , b \n", new DelimitedReaderOptions { NoHeader = true, TrimFields = true });

        CollectionAssert.AreEqual(
            new List<string> { "StartArray", "StartArray", "String:a", "String:b", "EndArray", "EndArray" },
            tokens);
    }
}
