// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.DotEnv.Reader;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DotEnvReader.Read" />, verifying the token stream produced
/// for representative DotEnv inputs.
/// </summary>
[TestClass]
public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// Reads every token from the supplied source and returns a compact <c>Kind:Value</c> transcript.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The token transcript.</returns>
    private static List<string> Transcribe(string source, DotEnvReaderOptions options = default)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8DotEnvReader(bytes, options);
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                DotEnvTokenType.PropertyName => $"Name:{reader.GetString()}",
                DotEnvTokenType.String => $"String:{reader.GetString()}",
                DotEnvTokenType.Comment => $"Comment:{reader.GetString()}",
                _ => reader.TokenType.ToString(),
            });
        }

        return tokens;
    }

    /// <summary>
    /// Verifies that a simple key/value assignment produces the framed property-name and string token stream.
    /// </summary>
    [TestMethod]
    public void Read_WhenSimpleAssignment_ShouldProduceFramedTokens()
    {
        List<string> tokens = Transcribe("HOST=localhost\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:HOST", "String:localhost", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that an <c>export</c> prefix is stripped and the key/value pair is still surfaced.
    /// </summary>
    [TestMethod]
    public void Read_WhenExportPrefix_ShouldStripPrefixAndReadEntry()
    {
        List<string> tokens = Transcribe("export PORT=5432\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:PORT", "String:5432", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a double-quoted value has its quotes stripped and its escape sequences resolved.
    /// </summary>
    [TestMethod]
    public void Read_WhenDoubleQuotedValueWithEscapes_ShouldResolveEscapes()
    {
        List<string> tokens = Transcribe("MSG=\"a\\tb\\nc\"\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:MSG", "String:a\tb\nc", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a single-quoted value is treated literally, without escape processing.
    /// </summary>
    [TestMethod]
    public void Read_WhenSingleQuotedValue_ShouldTreatContentLiterally()
    {
        List<string> tokens = Transcribe("PATH='a\\tb'\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:PATH", "String:a\\tb", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a comment line is surfaced as a <see cref="DotEnvTokenType.Comment" /> token by default.
    /// </summary>
    [TestMethod]
    public void Read_WhenCommentLine_ShouldSurfaceCommentToken()
    {
        List<string> tokens = Transcribe("# a note\nK=v\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Comment: a note", "Name:K", "String:v", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that an inline comment following whitespace terminates an unquoted value.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineComment_ShouldTerminateUnquotedValue()
    {
        List<string> tokens = Transcribe("K=value # trailing\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:K", "String:value", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that an empty document produces only the framing object tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyDocument_ShouldProduceOnlyFraming()
    {
        List<string> tokens = Transcribe(string.Empty);

        CollectionAssert.AreEqual(new List<string> { "StartObject", "EndObject" }, tokens);
    }

    /// <summary>
    /// Verifies that a key with no assignment operator throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenMissingAssignment_ShouldThrowDotEnvFormatException()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = Transcribe("KEY\n");
        });
    }

    /// <summary>
    /// Verifies that an unterminated double-quoted value throws <see cref="DotEnvFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnterminatedDoubleQuote_ShouldThrowDotEnvFormatException()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = Transcribe("K=\"open\n");
        });
    }
}
