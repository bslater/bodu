// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.Strings.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that a single-line literal string preserves its bytes verbatim, applying no escape processing.
    /// </summary>
    [TestMethod]
    public void Read_WhenLiteralString_ShouldPreserveContentVerbatim()
    {
        Utf8TomlReader reader = Create("v = 'C:\\Users\\nobody'\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("C:\\Users\\nobody", reader.GetString());
    }

    /// <summary>
    /// Verifies that a multi-line literal string preserves embedded newlines and backslashes without escape processing.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultilineLiteralString_ShouldPreserveContentVerbatim()
    {
        Utf8TomlReader reader = Create("v = '''line1\nC:\\x'''\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("line1\nC:\\x", reader.GetString());
    }

    /// <summary>
    /// Verifies that a leading newline immediately after the opening delimiter of a multi-line literal string is
    /// trimmed.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultilineLiteralStringWithLeadingNewline_ShouldTrimFirstNewline()
    {
        Utf8TomlReader reader = Create("v = '''\nfoo'''\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("foo", reader.GetString());
    }

    /// <summary>
    /// Verifies that a leading newline immediately after the opening delimiter of a multi-line basic string is trimmed.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultilineBasicStringWithLeadingNewline_ShouldTrimFirstNewline()
    {
        Utf8TomlReader reader = Create("v = \"\"\"\nfoo\"\"\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("foo", reader.GetString());
    }

    /// <summary>
    /// Verifies that a line-ending backslash in a multi-line basic string trims the newline and all leading whitespace
    /// of the following line.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultilineBasicStringLineEndingBackslash_ShouldTrimNewlineAndIndent()
    {
        Utf8TomlReader reader = Create("v = \"\"\"a \\\n    b\"\"\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("a b", reader.GetString());
    }

    /// <summary>
    /// Verifies that a multi-line basic string preserves an unescaped embedded CRLF newline verbatim, matching the
    /// <c>mlb-content =/ newline</c> grammar production where the newline itself is content.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultilineBasicStringWithEmbeddedCrlf_ShouldPreserveCrlf()
    {
        Utf8TomlReader reader = Create("v = \"\"\"a\r\nb\"\"\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("a\r\nb", reader.GetString());
    }

    /// <summary>
    /// Verifies that a multi-line basic string preserves an unescaped embedded LF newline as a line feed.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultilineBasicStringWithEmbeddedLf_ShouldPreserveLineFeed()
    {
        Utf8TomlReader reader = Create("v = \"\"\"a\nb\"\"\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("a\nb", reader.GetString());
    }

    /// <summary>
    /// Verifies that up to two trailing double-quotes are kept as content directly before the closing delimiter of a
    /// multi-line basic string.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultilineBasicStringEndsWithQuotes_ShouldKeepLeadingQuotesAsContent()
    {
        Utf8TomlReader reader = Create("v = \"\"\"a\"\"\"\"\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("a\"\"", reader.GetString());
    }

    /// <summary>
    /// Verifies that each supported single-character escape in a basic string decodes to its control character.
    /// </summary>
    /// <param name="literal">The escape sequence as it appears between the quotes.</param>
    /// <param name="expected">The single character the escape decodes to.</param>
    [TestMethod]
    [DataRow("\\b", "\b", DisplayName = "backspace")]
    [DataRow("\\t", "\t", DisplayName = "tab")]
    [DataRow("\\n", "\n", DisplayName = "line feed")]
    [DataRow("\\f", "\f", DisplayName = "form feed")]
    [DataRow("\\r", "\r", DisplayName = "carriage return")]
    [DataRow("\\\"", "\"", DisplayName = "quote")]
    [DataRow("\\\\", "\\", DisplayName = "backslash")]
    public void Read_WhenBasicStringContainsSimpleEscape_ShouldDecodeToControlCharacter(string literal, string expected)
    {
        Utf8TomlReader reader = Create($"v = \"{literal}\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual(expected, reader.GetString());
    }

    /// <summary>
    /// Verifies that a four-digit <c>\uXXXX</c> escape decodes to its Unicode scalar value.
    /// </summary>
    [TestMethod]
    public void Read_WhenBasicStringContainsShortUnicodeEscape_ShouldDecodeScalar()
    {
        Utf8TomlReader reader = Create("v = \"\\u00E9\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("é", reader.GetString());
    }

    /// <summary>
    /// Verifies that an eight-digit <c>\UXXXXXXXX</c> escape decodes a non-BMP Unicode scalar value to a surrogate
    /// pair.
    /// </summary>
    [TestMethod]
    public void Read_WhenBasicStringContainsLongUnicodeEscape_ShouldDecodeAstralScalar()
    {
        Utf8TomlReader reader = Create("v = \"\\U0001F600\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual(char.ConvertFromUtf32(0x1F600), reader.GetString());
    }

    /// <summary>
    /// Verifies that a basic string containing several escapes interleaved with literal text decodes correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenBasicStringMixesEscapesAndText_ShouldDecodeWholeString()
    {
        Utf8TomlReader reader = Create("v = \"a\\tb\\nc\\\"d\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("a\tb\nc\"d", reader.GetString());
    }

    /// <summary>
    /// Verifies that a basic string holding a tab (the only literal control character TOML permits) is preserved.
    /// </summary>
    [TestMethod]
    public void Read_WhenBasicStringContainsLiteralTab_ShouldPreserveTab()
    {
        Utf8TomlReader reader = Create("v = \"a\tb\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual("a\tb", reader.GetString());
    }

    /// <summary>
    /// Verifies that an empty single-line basic string decodes to an empty string.
    /// </summary>
    [TestMethod]
    public void Read_WhenBasicStringIsEmpty_ShouldDecodeToEmptyString()
    {
        Utf8TomlReader reader = Create("v = \"\"\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual(string.Empty, reader.GetString());
    }

    /// <summary>
    /// Verifies that an empty single-line literal string decodes to an empty string.
    /// </summary>
    [TestMethod]
    public void Read_WhenLiteralStringIsEmpty_ShouldDecodeToEmptyString()
    {
        Utf8TomlReader reader = Create("v = ''\n");

        ExpectSingleValue(ref reader, TomlTokenType.String);
        Assert.AreEqual(string.Empty, reader.GetString());
    }
}
