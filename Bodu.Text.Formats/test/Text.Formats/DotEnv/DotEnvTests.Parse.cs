// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTests.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.DotEnv;

public sealed partial class DotEnvTests
{

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> returns an empty document for an empty
    /// source string.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSourceIsEmpty_ShouldReturnEmptyDocument()
    {
        DotEnvDocument doc = DotEnv.Parse(string.Empty);

        Assert.AreEqual(0, doc.Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> ignores lines whose first non-whitespace
    /// character is <c>#</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLineIsComment_ShouldIgnoreLine()
    {
        DotEnvDocument doc = DotEnv.Parse("# this is a comment\nKEY=value");

        Assert.AreEqual(1, doc.Entries.Count);
        Assert.AreEqual("value", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> ignores blank lines.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLineIsBlank_ShouldIgnoreLine()
    {
        DotEnvDocument doc = DotEnv.Parse("\n\nKEY=value\n\n");

        Assert.AreEqual(1, doc.Entries.Count);
        Assert.AreEqual("value", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> parses an unquoted value correctly.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueIsUnquoted_ShouldReturnTrimmedValue()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=  hello  ");

        Assert.AreEqual("hello", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> returns an empty string for a key with no
    /// value after the <c>=</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueIsEmpty_ShouldReturnEmptyString()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=");

        Assert.AreEqual(string.Empty, doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> strips surrounding double quotes and returns
    /// the inner content.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueIsDoubleQuoted_ShouldStripQuotesAndReturnContent()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=\"hello world\"");

        Assert.AreEqual("hello world", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> strips surrounding single quotes and returns
    /// the inner content literally.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueIsSingleQuoted_ShouldStripQuotesAndReturnContentLiteral()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY='hello $VAR'");

        Assert.AreEqual("hello $VAR", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> resolves <c>\"</c> as a literal double-quote
    /// character within a double-quoted value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedValueContainsEscapedQuote_ShouldResolveEscape()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=\"say \\\"hi\\\"\"");

        Assert.AreEqual("say \"hi\"", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> resolves <c>\\</c> as a single backslash
    /// within a double-quoted value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedValueContainsEscapedBackslash_ShouldResolveEscape()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=\"a\\\\b\"");

        Assert.AreEqual("a\\b", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> resolves <c>\n</c>, <c>\t</c>, and <c>\r</c>
    /// escape sequences within a double-quoted value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedValueContainsEscapeSequences_ShouldResolveAll()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=\"a\\nb\\tc\\r\"");

        Assert.AreEqual("a\nb\tc\r", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> resolves <c>\$</c> as a literal dollar sign
    /// within a double-quoted value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedValueContainsEscapedDollar_ShouldResolveEscape()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=\"\\$VAR\"");

        Assert.AreEqual("$VAR", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> preserves literal newlines inside a
    /// double-quoted value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedValueSpansMultipleLines_ShouldPreserveLiteralNewlines()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=\"line one\nline two\"");

        Assert.AreEqual("line one\nline two", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that a <c>\</c> followed by a newline inside a double-quoted value is treated as a line
    /// continuation and produces no characters.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedValueHasLineContinuation_ShouldDiscardBackslashAndNewline()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=\"hello \\\nworld\"");

        Assert.AreEqual("hello world", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> correctly handles the <c>export </c> prefix
    /// when <see cref="DotEnvParseOptions.AllowExportPrefix" /> is <see langword="true" /> (the default).
    /// </summary>
    [TestMethod]
    public void Parse_WhenExportPrefixPresent_ShouldStripPrefixAndParseEntry()
    {
        DotEnvDocument doc = DotEnv.Parse("export KEY=value");

        Assert.AreEqual("value", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> strips an inline comment from an unquoted
    /// value when <see cref="DotEnvParseOptions.AllowInlineComments" /> is <see langword="true" /> (the default).
    /// </summary>
    [TestMethod]
    public void Parse_WhenUnquotedValueHasInlineComment_ShouldTruncateAtHashPrecededBySpace()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=value # this is a comment");

        Assert.AreEqual("value", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that a <c>#</c> not preceded by whitespace in an unquoted value is treated as part of the value
    /// and not as a comment.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHashInUnquotedValueNotPrecededBySpace_ShouldBePartOfValue()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=value#notacomment");

        Assert.AreEqual("value#notacomment", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> handles a source with CRLF line endings.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSourceHasCrlfLineEndings_ShouldParseCorrectly()
    {
        DotEnvDocument doc = DotEnv.Parse("A=1\r\nB=2\r\n");

        Assert.AreEqual("1", doc["A"]);
        Assert.AreEqual("2", doc["B"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> handles multiple entries correctly and
    /// exposes them all.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultipleEntries_ShouldParseAll()
    {
        DotEnvDocument doc = DotEnv.Parse("HOST=localhost\nPORT=8080\nDEBUG=True");

        Assert.AreEqual(3, doc.Entries.Count);
        Assert.AreEqual("localhost", doc["HOST"]);
        Assert.AreEqual("8080", doc["PORT"]);
        Assert.AreEqual("True", doc["DEBUG"]);
    }

}
