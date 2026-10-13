// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Encoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.Kat;
using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains encoding and line-handling robustness tests for <see cref="Utf8DelimitedReader" /> - BOM stripping,
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

    /// <summary>
    /// Gets the invalid UTF-8 sequences that end a quoted field's content, just before a quote: a truncated two-,
    /// three- and four-byte sequence, a lone continuation byte, a byte UTF-8 never uses, and a truncated sequence after
    /// a valid character.
    /// </summary>
    /// <value>
    /// The rows, each named for its sequence and holding the field content and the text it decodes to, one U+FFFD for
    /// each maximal invalid subsequence.
    /// </value>
    public static IEnumerable<object[]> InvalidUtf8BeforeAQuote =>
    [
        [new ValidKat<byte[], string>("truncated two-byte sequence", [0xC3], "\uFFFD")],
        [new ValidKat<byte[], string>("truncated three-byte sequence", [0xE2, 0x82], "\uFFFD")],
        [new ValidKat<byte[], string>("truncated four-byte sequence", [0xF0, 0x9F, 0x98], "\uFFFD")],
        [new ValidKat<byte[], string>("lone continuation byte", [0x80], "\uFFFD")],
        [new ValidKat<byte[], string>("invalid byte 0xFF", [0xFF], "\uFFFD")],
        [new ValidKat<byte[], string>("truncated sequence after a letter", [(byte)'a', 0xC3], "a\uFFFD")],
    ];

    /// <summary>
    /// Verifies the reproduction of the defect: a truncated UTF-8 sequence before the closing quote of a field reads as
    /// U+FFFD, and the closing quote still ends the field, so the next field and record read as written.
    /// </summary>
    [TestMethod]
    public void Read_WhenATruncatedSequencePrecedesTheClosingQuote_ShouldReadAReplacementCharacterAndEndTheField()
    {
        byte[] bytes = [(byte)'"', 0xC3, (byte)'"', (byte)',', (byte)'z', (byte)'\r', (byte)'\n', (byte)'1', (byte)',', (byte)'2', (byte)'\r', (byte)'\n'];

        List<string> tokens = Transcribe(bytes, new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartArray", "String:�", "String:z", "EndArray",
                "StartArray", "String:1", "String:2", "EndArray",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that invalid UTF-8 just before the closing quote of a field decodes as the same bytes decode in an
    /// unquoted field, each invalid sequence to U+FFFD, and that the closing quote still ends the field.
    /// </summary>
    /// <param name="kat">The row holding the invalid content and the text it decodes to.</param>
    [TestMethod]
    [DynamicData(
        nameof(InvalidUtf8BeforeAQuote),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenInvalidUtf8PrecedesTheClosingQuote_ShouldDecodeItAsAnUnquotedFieldDoes(ValidKat<byte[], string> kat)
    {
        byte[] quoted = [(byte)'"', .. kat.Input, (byte)'"', (byte)',', (byte)'z', (byte)'\r', (byte)'\n'];
        byte[] unquoted = [.. kat.Input, (byte)',', (byte)'z', (byte)'\r', (byte)'\n'];

        List<string> fromQuoted = Transcribe(quoted, new DelimitedReaderOptions { NoHeader = true });
        List<string> fromUnquoted = Transcribe(unquoted, new DelimitedReaderOptions { NoHeader = true });

        Assert.AreEqual(kat.Expected, Encoding.UTF8.GetString(kat.Input));
        Assert.AreEqual($"String:{kat.Expected}", fromQuoted[2]);
        CollectionAssert.AreEqual(fromUnquoted, fromQuoted);
    }

    /// <summary>
    /// Verifies that invalid UTF-8 just before an escaped (doubled) quote inside a quoted field decodes to U+FFFD and
    /// leaves the escaped quote a literal quote.
    /// </summary>
    /// <param name="kat">The row holding the invalid content and the text it decodes to.</param>
    [TestMethod]
    [DynamicData(
        nameof(InvalidUtf8BeforeAQuote),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenInvalidUtf8PrecedesAnEscapedQuote_ShouldKeepTheEscapedQuote(ValidKat<byte[], string> kat)
    {
        byte[] bytes = [(byte)'"', .. kat.Input, (byte)'"', (byte)'"', (byte)'b', (byte)'"', (byte)',', (byte)'z', (byte)'\n'];

        List<string> tokens = Transcribe(bytes, new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartArray", $"String:{kat.Expected}\"b", "String:z", "EndArray",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Reads the supplied UTF-8 bytes to completion and renders each token, as <c>Transcribe(string, …)</c> does for
    /// text.
    /// </summary>
    /// <param name="bytes">The delimited source bytes.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The rendered tokens.</returns>
    private static List<string> Transcribe(byte[] bytes, DelimitedReaderOptions options)
    {
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
}
