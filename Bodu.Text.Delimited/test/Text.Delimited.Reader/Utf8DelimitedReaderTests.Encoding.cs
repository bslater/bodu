// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Encoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains encoding and line-handling robustness tests for <see cref="Utf8DelimitedReader" /> — BOM stripping,
/// line-ending variants, and UTF-8 multibyte fields.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that a leading UTF-8 byte-order mark is stripped so the first header column is not corrupted.
    /// </summary>
    [TestMethod]
    public void Read_WhenLeadingByteOrderMark_ShouldStripIt()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. "name,age\nAda,36\n"u8];
        var reader = new Utf8DelimitedReader(bytes);

        _ = reader.Read(); // StartArray populates headers
        CollectionAssert.AreEqual(new List<string> { "name", "age" }, reader.Headers.ToList());
    }

    /// <summary>
    /// Verifies that lone carriage-return (classic Mac) line endings terminate records.
    /// </summary>
    [TestMethod]
    public void Read_WhenCarriageReturnOnlyLineEndings_ShouldReadRecords()
    {
        List<string> tokens = Transcribe("a,b\r1,2\r3,4\r");

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartObject", "Name:a", "String:1", "Name:b", "String:2", "EndObject",
                "StartObject", "Name:a", "String:3", "Name:b", "String:4", "EndObject",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that a UTF-8 multibyte field value is decoded correctly, both quoted and unquoted.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnicodeFields_ShouldDecodeMultibyte()
    {
        List<string> tokens = Transcribe("café,\"☕ time\"\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string> { "StartArray", "StartArray", "String:café", "String:☕ time", "EndArray", "EndArray" },
            tokens);
    }
}
