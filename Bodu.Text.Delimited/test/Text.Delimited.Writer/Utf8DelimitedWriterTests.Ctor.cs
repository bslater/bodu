// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedWriterTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Delimited.Writer;

/// <summary>
/// Contains the constructor tests for <see cref="Utf8DelimitedWriter" />, verifying that both constructors that take
/// options refuse dialect characters the writer cannot emit as single bytes, or that a reader could not tell apart.
/// </summary>
/// <remarks>
/// The data rows give each character as its UTF-16 code unit, so that no control character reaches a test's display
/// name; zero leaves the option unset, which selects its default character.
/// </remarks>
public partial class Utf8DelimitedWriterTests
{
    /// <summary>
    /// Verifies that both writer constructors refuse, with an <see cref="ArgumentException" /> for <c>options</c> whose
    /// message names the offending option, a delimiter or quote character that is a line feed or a carriage return.
    /// </summary>
    /// <param name="delimiter">The delimiter's code unit, or zero for the default comma.</param>
    /// <param name="quote">The quote character's code unit, or zero for the default double quote.</param>
    /// <param name="option">The name of the option the exception names.</param>
    [TestMethod]
    [DataRow(0x0A, 0, "Delimiter")]
    [DataRow(0x0D, 0, "Delimiter")]
    [DataRow(0, 0x0A, "Quote")]
    [DataRow(0, 0x0D, "Quote")]
    public void Ctor_WhenTheDelimiterOrQuoteIsALineBreak_ShouldThrowArgumentException(int delimiter, int quote, string option)
    {
        var options = new DelimitedWriterOptions { Delimiter = (char)delimiter, Quote = (char)quote };
        using var stream = new MemoryStream();

        var bufferEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedWriter(new ArrayBufferWriter<byte>(), options);
        });

        var streamEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedWriter(stream, options);
        });

        Assert.AreEqual("options", bufferEx.ParamName);
        Assert.Contains(option, bufferEx.Message);
        Assert.AreEqual("options", streamEx.ParamName);
        Assert.Contains(option, streamEx.Message);
    }

    /// <summary>
    /// Verifies that both writer constructors refuse, with an <see cref="ArgumentException" /> for <c>options</c> whose
    /// message names the offending option, a delimiter or quote character outside ASCII, which the writer would
    /// otherwise compare with one byte of each field's UTF-8 encoding.
    /// </summary>
    /// <param name="delimiter">The delimiter's code unit, or zero for the default comma.</param>
    /// <param name="quote">The quote character's code unit, or zero for the default double quote.</param>
    /// <param name="option">The name of the option the exception names.</param>
    [TestMethod]
    [DataRow(0xA3, 0, "Delimiter")]
    [DataRow(0x3BB, 0, "Delimiter")]
    [DataRow(0, 0x201C, "Quote")]
    [DataRow(0, 0xFFFD, "Quote")]
    public void Ctor_WhenTheDelimiterOrQuoteIsNotAscii_ShouldThrowArgumentException(int delimiter, int quote, string option)
    {
        var options = new DelimitedWriterOptions { Delimiter = (char)delimiter, Quote = (char)quote };
        using var stream = new MemoryStream();

        var bufferEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedWriter(new ArrayBufferWriter<byte>(), options);
        });

        var streamEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedWriter(stream, options);
        });

        Assert.AreEqual("options", bufferEx.ParamName);
        Assert.Contains(option, bufferEx.Message);
        Assert.AreEqual("options", streamEx.ParamName);
        Assert.Contains(option, streamEx.Message);
    }

    /// <summary>
    /// Verifies that both writer constructors refuse, with an <see cref="ArgumentException" /> for <c>options</c> that
    /// names both options, a delimiter equal to the quote character, set explicitly or left at its default.
    /// </summary>
    /// <param name="delimiter">The delimiter's code unit, or zero for the default comma.</param>
    /// <param name="quote">The quote character's code unit, or zero for the default double quote.</param>
    [TestMethod]
    [DataRow(0x22, 0)]
    [DataRow(0x3B, 0x3B)]
    [DataRow(0, 0x2C)]
    public void Ctor_WhenTheDelimiterIsTheQuote_ShouldThrowArgumentException(int delimiter, int quote)
    {
        var options = new DelimitedWriterOptions { Delimiter = (char)delimiter, Quote = (char)quote };
        using var stream = new MemoryStream();

        var bufferEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedWriter(new ArrayBufferWriter<byte>(), options);
        });

        var streamEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Utf8DelimitedWriter(stream, options);
        });

        Assert.AreEqual("options", bufferEx.ParamName);
        Assert.Contains("Delimiter", bufferEx.Message);
        Assert.Contains("Quote", bufferEx.Message);
        Assert.AreEqual("options", streamEx.ParamName);
        Assert.Contains("Delimiter", streamEx.Message);
        Assert.Contains("Quote", streamEx.Message);
    }

    /// <summary>
    /// Verifies that distinct ASCII delimiter and quote characters other than CR and LF, control characters such as the
    /// tab and the unit separator included, are accepted and written with: a field holding the quote is quoted with it.
    /// </summary>
    /// <param name="delimiter">The delimiter's code unit.</param>
    /// <param name="quote">The quote character's code unit.</param>
    [TestMethod]
    [DataRow(0x09, 0x22)]
    [DataRow(0x7C, 0x27)]
    [DataRow(0x1F, 0x7F)]
    public void Ctor_WhenTheDelimiterAndQuoteAreDistinctAscii_ShouldWriteWithThem(int delimiter, int quote)
    {
        char separator = (char)delimiter;
        char quoteChar = (char)quote;
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer, new DelimitedWriterOptions { Delimiter = separator, Quote = quoteChar, NoHeader = true });

        writer.WriteStartArray();
        writer.WriteStartArray();
        writer.WriteString("a");
        writer.WriteString("b" + quoteChar + "c");
        writer.WriteEndArray();
        writer.WriteEndArray();
        writer.Flush();

        string expected = "a" + separator + quoteChar + "b" + quoteChar + quoteChar + "c" + quoteChar + "\r\n";
        Assert.AreEqual(expected, Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
