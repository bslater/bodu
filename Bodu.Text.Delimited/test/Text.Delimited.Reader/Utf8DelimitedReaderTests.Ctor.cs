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
}
