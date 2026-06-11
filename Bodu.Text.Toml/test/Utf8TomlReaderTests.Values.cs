// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.Values.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that an escape-free basic string exposes its raw content bytes and reports no escapes, so decoding is
    /// a direct transcode.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenEscapeFreeBasicString_ShouldExposeRawContentWithoutEscapes()
    {
        Utf8TomlReader lexer = Create("s = \"plain\"\n");

        Advance(ref lexer, 2);
        Assert.AreEqual(TomlTokenType.String, lexer.TokenType);
        Assert.IsFalse(lexer.HasEscapes);
        Assert.IsTrue(lexer.ValueSpan.SequenceEqual("plain"u8));
        Assert.AreEqual("plain", lexer.GetString());
    }

    /// <summary>
    /// Verifies that a basic string with escapes exposes the raw escaped bytes while <see cref="Utf8TomlReader.GetString" />
    /// resolves them.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenEscapedBasicString_ShouldExposeRawBytesAndDecodeOnDemand()
    {
        Utf8TomlReader lexer = Create("s = \"a\\nb\"\n");

        Advance(ref lexer, 2);
        Assert.IsTrue(lexer.HasEscapes);
        Assert.IsTrue(lexer.ValueSpan.SequenceEqual("a\\nb"u8));
        Assert.AreEqual("a\nb", lexer.GetString());
    }

    /// <summary>
    /// Verifies that a literal string exposes its content verbatim with no escape processing.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenLiteralString_ShouldExposeContentVerbatim()
    {
        Utf8TomlReader lexer = Create(@"s = 'C:\Users\node'" + "\n");

        Advance(ref lexer, 2);
        Assert.IsFalse(lexer.HasEscapes);
        Assert.AreEqual(@"C:\Users\node", lexer.GetString());
    }

    /// <summary>
    /// Verifies that a multi-line basic string trims its leading newline, resolves escapes and line-ending
    /// backslashes, and preserves interior newlines as written.
    /// </summary>
    [TestMethod]
    public void GetString_WhenMultilineBasicString_ShouldTrimLeadingNewlineAndResolveEscapes()
    {
        Utf8TomlReader lexer = Create("s = \"\"\"\nline1\nline2 \\\n   joined\"\"\"\n");

        Advance(ref lexer, 2);
        Assert.AreEqual("line1\nline2 joined", lexer.GetString());
    }

    /// <summary>
    /// Verifies that a multi-line literal string trims its leading newline and preserves all other content verbatim.
    /// </summary>
    [TestMethod]
    public void GetString_WhenMultilineLiteralString_ShouldTrimLeadingNewlineOnly()
    {
        Utf8TomlReader lexer = Create("s = '''\nraw \\n text'''\n");

        Advance(ref lexer, 2);
        Assert.IsFalse(lexer.HasEscapes);
        Assert.AreEqual("raw \\n text", lexer.GetString());
    }

    /// <summary>
    /// Verifies that quotes belonging to a multi-line basic string's content adjacent to the closing delimiter are
    /// preserved.
    /// </summary>
    [TestMethod]
    public void GetString_WhenMultilineStringEndsWithQuotes_ShouldKeepContentQuotes()
    {
        Utf8TomlReader lexer = Create("s = \"\"\"two quotes:\"\"\"\"\"\n");

        Advance(ref lexer, 2);
        Assert.AreEqual("two quotes:\"\"", lexer.GetString());
    }

    /// <summary>
    /// Verifies that a number token exposes its raw text — including underscores — while the typed accessor returns
    /// the decoded value.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenUnderscoredInteger_ShouldExposeRawTextAndDecodeValue()
    {
        Utf8TomlReader lexer = Create("i = 1_000_000\n");

        Advance(ref lexer, 2);
        Assert.IsTrue(lexer.ValueSpan.SequenceEqual("1_000_000"u8));
        Assert.AreEqual(1_000_000L, lexer.GetInt64());
    }

    /// <summary>
    /// Verifies that radix integers (hexadecimal, octal, binary) decode to the expected values.
    /// </summary>
    [TestMethod]
    [DataRow("0xDEADBEEF", 3735928559L, DisplayName = "hex")]
    [DataRow("0o755", 493L, DisplayName = "octal")]
    [DataRow("0b1101", 13L, DisplayName = "binary")]
    [DataRow("0xF_F", 255L, DisplayName = "hex with underscore")]
    public void GetInt64_WhenRadixInteger_ShouldDecodeValue(string literal, long expected)
    {
        Utf8TomlReader lexer = Create($"i = {literal}\n");

        Advance(ref lexer, 2);
        Assert.AreEqual(expected, lexer.GetInt64());
    }

    /// <summary>
    /// Verifies that the integer range boundaries decode exactly.
    /// </summary>
    [TestMethod]
    [DataRow("9223372036854775807", long.MaxValue, DisplayName = "long.MaxValue")]
    [DataRow("-9223372036854775808", long.MinValue, DisplayName = "long.MinValue")]
    public void GetInt64_WhenAtRangeBoundary_ShouldDecodeValue(string literal, long expected)
    {
        Utf8TomlReader lexer = Create($"i = {literal}\n");

        Advance(ref lexer, 2);
        Assert.AreEqual(expected, lexer.GetInt64());
    }

    /// <summary>
    /// Verifies that special float values decode to the expected IEEE 754 values.
    /// </summary>
    [TestMethod]
    [DataRow("inf", double.PositiveInfinity, DisplayName = "inf")]
    [DataRow("+inf", double.PositiveInfinity, DisplayName = "+inf")]
    [DataRow("-inf", double.NegativeInfinity, DisplayName = "-inf")]
    [DataRow("6.626e-34", 6.626e-34, DisplayName = "exponent")]
    public void GetDouble_WhenSpecialOrExponentFloat_ShouldDecodeValue(string literal, double expected)
    {
        Utf8TomlReader lexer = Create($"f = {literal}\n");

        Advance(ref lexer, 2);
        Assert.AreEqual(expected, lexer.GetDouble());
    }

    /// <summary>
    /// Verifies that <c>nan</c> decodes to <see cref="double.NaN" />.
    /// </summary>
    [TestMethod]
    public void GetDouble_WhenNan_ShouldDecodeToNaN()
    {
        Utf8TomlReader lexer = Create("f = nan\n");

        Advance(ref lexer, 2);
        Assert.IsTrue(double.IsNaN(lexer.GetDouble()));
    }

    /// <summary>
    /// Verifies that a typed accessor mismatching the current token type throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenCurrentTokenIsString_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Utf8TomlReader lexer = Create("s = \"x\"\n");
            Advance(ref lexer, 2);
            _ = lexer.GetInt64();
        });
    }

    /// <summary>
    /// Advances the lexer <paramref name="count" /> tokens, asserting each read succeeds.
    /// </summary>
    /// <param name="lexer">The lexer to advance.</param>
    /// <param name="count">The number of tokens to read.</param>
    private static void Advance(ref Utf8TomlReader lexer, int count)
    {
        for (var i = 0; i < count; i++)
            Assert.IsTrue(lexer.Read(), $"Expected a token at position {i + 1} but the lexer reported end of document.");
    }
}
