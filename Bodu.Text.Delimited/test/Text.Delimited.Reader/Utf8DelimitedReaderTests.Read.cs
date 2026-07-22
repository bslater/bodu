// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedReader.Read" />, verifying the token stream produced
/// for representative CSV inputs.
/// </summary>
[TestClass]
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Reads every token and returns a compact transcript.
    /// </summary>
    /// <param name="source">The CSV source text.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The token transcript.</returns>
    private static List<string> Transcribe(string source, DelimitedReaderOptions options = default)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8DelimitedReader(bytes, options);
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                DelimitedTokenType.PropertyName => $"Name:{reader.GetString()}",
                DelimitedTokenType.String => $"String:{reader.GetString()}",
                _ => reader.TokenType.ToString(),
            });
        }

        return tokens;
    }

    /// <summary>
    /// Verifies that a header-mode CSV produces the object-framed token stream keyed by header name.
    /// </summary>
    [TestMethod]
    public void Read_WhenHeaderMode_ShouldFrameRecordsAsObjects()
    {
        List<string> tokens = Transcribe("name,age\nAda,36\n");

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartObject", "Name:name", "String:Ada", "Name:age", "String:36", "EndObject",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that headerless mode frames each record as a positional array.
    /// </summary>
    [TestMethod]
    public void Read_WhenHeaderless_ShouldFrameRecordsAsArrays()
    {
        List<string> tokens = Transcribe("Ada,36\nGrace,45\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartArray", "String:Ada", "String:36", "EndArray",
                "StartArray", "String:Grace", "String:45", "EndArray",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that a quoted field with an embedded delimiter, newline, and doubled quote decodes correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldWithSpecials_ShouldDecode()
    {
        List<string> tokens = Transcribe("a\n\"x,y\r\nz\"\"q\"\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartArray", "String:a", "EndArray",
                "StartArray", "String:x,y\r\nz\"q", "EndArray",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that a record whose field count differs from the header throws under the strict default.
    /// </summary>
    [TestMethod]
    public void Read_WhenStrictFieldCountMismatch_ShouldThrowDelimitedFormatException()
    {
        Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a,b\n1,2,3\n");
        });
    }

    /// <summary>
    /// Verifies that an unterminated quoted field throws <see cref="DelimitedFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnterminatedQuote_ShouldThrowDelimitedFormatException()
    {
        Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a\n\"open\n", new DelimitedReaderOptions { NoHeader = true });
        });
    }

    /// <summary>
    /// Verifies that blank lines are skipped between records.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlankLines_ShouldSkipThem()
    {
        List<string> tokens = Transcribe("a\n\n1\n\n2\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartArray", "String:a", "EndArray",
                "StartArray", "String:1", "EndArray",
                "StartArray", "String:2", "EndArray",
                "EndArray",
            },
            tokens);
    }
}
