// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the constructor tests for <see cref="Utf8DelimitedReader" />, verifying that it refuses dialect characters
/// that cannot be told apart from each other or from a line break.
/// </summary>
/// <remarks>
/// The data rows give each character as its UTF-16 code unit, so that no control character reaches a test's display
/// name; zero leaves the option unset, which selects its default character.
/// </remarks>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that the reader refuses, with <see cref="ArgumentException" />, a delimiter or quote that is a line feed
    /// or a carriage return, and a delimiter equal to the quote.
    /// </summary>
    /// <param name="delimiter">The delimiter's code unit, or zero for the default comma.</param>
    /// <param name="quote">The quote character's code unit, or zero for the default double quote.</param>
    [TestMethod]
    [DataRow(0x0A, 0)]
    [DataRow(0x0D, 0)]
    [DataRow(0, 0x0A)]
    [DataRow(0, 0x0D)]
    [DataRow(0x3B, 0x3B)]
    public void Ctor_WhenTheDelimiterIsALineBreakOrTheQuote_ShouldThrowArgumentException(int delimiter, int quote)
    {
        var options = new DelimitedReaderOptions { Delimiter = (char)delimiter, Quote = (char)quote };
        byte[] source = "a,b\nc,d\n"u8.ToArray();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedReader(source, options);
        });
    }

    /// <summary>
    /// Verifies that the reader refuses, with <see cref="ArgumentException" />, a double quote as the delimiter, the
    /// character the default quote already uses for quoting.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTheDelimiterIsTheQuote_ShouldThrowArgumentException()
    {
        var options = new DelimitedReaderOptions { Delimiter = '"' };
        byte[] source = "a\"b\"c"u8.ToArray();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedReader(source, options);
        });
    }

    /// <summary>
    /// Verifies that, with comments allowed, the reader refuses, with <see cref="ArgumentException" />, a delimiter or
    /// comment character that is a line feed, a carriage return or U+FFFD, and a delimiter equal to the comment
    /// character.
    /// </summary>
    /// <param name="delimiter">The delimiter's code unit, or zero for the default comma.</param>
    /// <param name="commentChar">The comment character's code unit, or zero for the default <c>#</c>.</param>
    [TestMethod]
    [DataRow(0x0A, 0)]
    [DataRow(0x0D, 0)]
    [DataRow(0xFFFD, 0)]
    [DataRow(0, 0x0A)]
    [DataRow(0, 0x0D)]
    [DataRow(0, 0xFFFD)]
    [DataRow(0x3B, 0x3B)]
    public void Ctor_WhenTheDelimiterOrCommentCharIsALineBreakOrTheSame_ShouldThrowArgumentException(int delimiter, int commentChar)
    {
        var options = new DelimitedReaderOptions { Delimiter = (char)delimiter, CommentChar = (char)commentChar, AllowComments = true };
        byte[] source = "a,b\n# comment\nc,d\n"u8.ToArray();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedReader(source, options);
        });
    }

    /// <summary>
    /// Verifies that the reader refuses, with an <see cref="ArgumentException" /> for <c>options</c> whose message names
    /// the offending option, a delimiter or comment character outside ASCII, which the reader would otherwise narrow to
    /// a single byte and match inside other characters.
    /// </summary>
    /// <param name="delimiter">The delimiter's code unit, or zero for the default comma.</param>
    /// <param name="commentChar">The comment character's code unit, or zero for the default <c>#</c>.</param>
    /// <param name="option">The name of the option the exception names.</param>
    [TestMethod]
    [DataRow(0xA3, 0x20AC, "Delimiter")]
    [DataRow(0x3BB, 0x20AC, "Delimiter")]
    [DataRow(0x80, 0, "Delimiter")]
    [DataRow(0, 0x20AC, "CommentChar")]
    [DataRow(0, 0xA0, "CommentChar")]
    public void Ctor_WhenTheDelimiterOrCommentCharIsNotAscii_ShouldThrowArgumentException(int delimiter, int commentChar, string option)
    {
        var options = new DelimitedReaderOptions { Delimiter = (char)delimiter, CommentChar = (char)commentChar };
        byte[] source = "a\u00A3b,c\n"u8.ToArray();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedReader(source, options);
        });

        Assert.AreEqual("options", ex.ParamName);
        Assert.Contains(option, ex.Message);
    }

    /// <summary>
    /// Verifies that the reader refuses, with an <see cref="ArgumentException" /> for <c>options</c> that names the
    /// <c>Quote</c> option, a quote character outside ASCII.
    /// </summary>
    /// <param name="quote">The quote character's code unit.</param>
    [TestMethod]
    [DataRow(0xAB)]
    [DataRow(0x201C)]
    public void Ctor_WhenTheQuoteIsNotAscii_ShouldThrowArgumentException(int quote)
    {
        var options = new DelimitedReaderOptions { Quote = (char)quote };
        byte[] source = "a,b\n"u8.ToArray();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedReader(source, options);
        });

        Assert.AreEqual("options", ex.ParamName);
        Assert.Contains("Quote", ex.Message);
    }

    /// <summary>
    /// Verifies that the reader refuses, with an <see cref="ArgumentException" /> for <c>options</c> that names both
    /// options, a comment character equal to the quote character, set explicitly or left at its default.
    /// </summary>
    /// <param name="quote">The quote character's code unit, or zero for the default double quote.</param>
    /// <param name="commentChar">The comment character's code unit, or zero for the default <c>#</c>.</param>
    [TestMethod]
    [DataRow(0, 0x22)]
    [DataRow(0x27, 0x27)]
    [DataRow(0x23, 0)]
    public void Ctor_WhenTheCommentCharIsTheQuote_ShouldThrowArgumentException(int quote, int commentChar)
    {
        var options = new DelimitedReaderOptions { Quote = (char)quote, CommentChar = (char)commentChar, AllowComments = true };
        byte[] source = "a,b\n"u8.ToArray();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedReader(source, options);
        });

        Assert.AreEqual("options", ex.ParamName);
        Assert.Contains("CommentChar", ex.Message);
        Assert.Contains("Quote", ex.Message);
    }

    /// <summary>
    /// Verifies that the reader refuses a comment character equal to the delimiter even with comments off, so the options
    /// stay coherent whether or not comments are later turned on: a <c>#</c> delimiter clashes with the default comment
    /// character.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTheCommentCharIsTheDelimiterAndCommentsAreOff_ShouldThrowArgumentException()
    {
        var options = new DelimitedReaderOptions { Delimiter = '#', AllowComments = false };
        byte[] source = "a#b\n"u8.ToArray();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedReader(source, options);
        });

        Assert.AreEqual("options", ex.ParamName);
        Assert.Contains("CommentChar", ex.Message);
        Assert.Contains("Delimiter", ex.Message);
    }

    /// <summary>
    /// Verifies that distinct ASCII dialect characters other than CR and LF, control characters such as the tab and the
    /// unit separator included, are accepted and read with.
    /// </summary>
    /// <param name="delimiter">The delimiter's code unit.</param>
    /// <param name="quote">The quote character's code unit.</param>
    /// <param name="commentChar">The comment character's code unit.</param>
    [TestMethod]
    [DataRow(0x09, 0x22, 0x23)]
    [DataRow(0x20, 0x27, 0x3B)]
    [DataRow(0x1F, 0x22, 0x23)]
    [DataRow(0x23, 0x22, 0x3B)]
    [DataRow(0x7C, 0x7F, 0x21)]
    public void Ctor_WhenTheDialectCharactersAreDistinctAscii_ShouldReadWithThem(int delimiter, int quote, int commentChar)
    {
        char separator = (char)delimiter;
        var options = new DelimitedReaderOptions
        {
            Delimiter = separator,
            Quote = (char)quote,
            CommentChar = (char)commentChar,
            NoHeader = true,
        };

        List<string[]> records = ReadRecords("a" + separator + "b\n", options);

        Assert.AreEqual(1, records.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, records[0]);
    }
}
