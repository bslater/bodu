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

    /// <summary>
    /// Verifies that with <see cref="DelimitedReaderOptions.TrimFields" /> a field whose opening quote follows spaces
    /// is read as a quoted field, so <c>1, 2, "test"</c> reads <c>test</c> without its quotes.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrimFieldsAndAQuoteFollowsWhiteSpace_ShouldReadAQuotedField()
    {
        List<string> tokens = Transcribe("1, 2, \"test\"\n", new DelimitedReaderOptions { NoHeader = true, TrimFields = true });

        CollectionAssert.AreEqual(
            new List<string> { "StartArray", "StartArray", "String:1", "String:2", "String:test", "EndArray", "EndArray" },
            tokens);
    }

    /// <summary>
    /// Verifies that with <see cref="DelimitedReaderOptions.TrimFields" /> a quoted field after a tab keeps the
    /// delimiter it quotes, which only a quoted field can hold: <c>1</c>, a tab and <c>"a,b"</c> read <c>1</c> and
    /// <c>a,b</c>.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrimFieldsAndATabPrecedesAQuotedDelimiter_ShouldKeepTheDelimiterInTheField()
    {
        List<string> tokens = Transcribe("1,\t\"a,b\"\n", new DelimitedReaderOptions { NoHeader = true, TrimFields = true });

        CollectionAssert.AreEqual(
            new List<string> { "StartArray", "StartArray", "String:1", "String:a,b", "EndArray", "EndArray" },
            tokens);
    }

    /// <summary>
    /// Verifies that with <see cref="DelimitedReaderOptions.TrimFields" /> the spaces and tabs on both sides of a
    /// quoted field are trimmed while those inside its quotes are kept: <c>a, " b " ,c</c> and the same with tabs read
    /// <c>a</c>, <c> b </c> and <c>c</c>.
    /// </summary>
    /// <param name="useTabs">Whether tabs, rather than spaces, surround the quoted field.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Read_WhenTrimFieldsAndWhiteSpaceSurroundsAQuotedField_ShouldKeepOnlyTheQuotedContent(bool useTabs)
    {
        string outside = useTabs ? "\t" : " ";
        string source = "a," + outside + "\" b \"" + outside + ",c\n";

        List<string> tokens = Transcribe(source, new DelimitedReaderOptions { NoHeader = true, TrimFields = true });

        CollectionAssert.AreEqual(
            new List<string> { "StartArray", "StartArray", "String:a", "String: b ", "String:c", "EndArray", "EndArray" },
            tokens);
    }

    /// <summary>
    /// Verifies that with a tab delimiter and <see cref="DelimitedReaderOptions.TrimFields" />, skipping white space
    /// before an opening quote never skips a tab that delimits an empty field: <c>a</c>, two tabs, a space and
    /// <c>"b"</c> read <c>a</c>, an empty field and <c>b</c>.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrimFieldsAndTheDelimiterIsATab_ShouldNotSkipTheDelimiter()
    {
        var options = new DelimitedReaderOptions { NoHeader = true, TrimFields = true, Delimiter = '\t' };

        List<string> tokens = Transcribe("a\t\t \"b\"\n", options);

        CollectionAssert.AreEqual(
            new List<string> { "StartArray", "StartArray", "String:a", "String:", "String:b", "EndArray", "EndArray" },
            tokens);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReaderOptions.TrimFields" /> trims only spaces and tabs: a no-break space, an
    /// ideographic space and a vertical tab around a field stay in its value.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrimFieldsAndAFieldHoldsOtherWhiteSpace_ShouldKeepIt()
    {
        List<string> tokens = Transcribe(
            "\u00A0a\u00A0,\u3000b\u3000,\vc\v\n",
            new DelimitedReaderOptions { NoHeader = true, TrimFields = true });

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray", "StartArray", "String:\u00A0a\u00A0", "String:\u3000b\u3000", "String:\vc\v", "EndArray", "EndArray",
            },
            tokens);
    }
}
