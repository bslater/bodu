// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniReaderTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8IniReader.Read" />, verifying the source-order token stream.
/// </summary>
[TestClass]
public partial class Utf8IniReaderTests
{
    /// <summary>
    /// Reads every token and returns a compact <c>Kind:Text</c> transcript.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The token transcript.</returns>
    private static List<string> Transcribe(string source, IniReaderOptions options = default)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8IniReader(bytes, options);
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                IniTokenType.SectionHeader => $"Section:{reader.GetString()}",
                IniTokenType.PropertyName => $"Name:{reader.GetString()}",
                IniTokenType.String => $"String:{reader.GetString()}",
                IniTokenType.Comment => $"Comment:{reader.GetString()}",
                _ => reader.TokenType.ToString(),
            });
        }

        return tokens;
    }

    /// <summary>
    /// Verifies that a document with a global key and a section produces the source-order token stream.
    /// </summary>
    [TestMethod]
    public void Read_WhenGlobalKeyThenSection_ShouldProduceSourceOrderTokens()
    {
        List<string> tokens = Transcribe("key0=a\n[db]\nhost=x\nport=5\n");

        CollectionAssert.AreEqual(
            new List<string>
            {
                "Name:key0", "String:a",
                "Section:db",
                "Name:host", "String:x",
                "Name:port", "String:5",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that key and value whitespace around the assignment is trimmed.
    /// </summary>
    [TestMethod]
    public void Read_WhenWhitespaceAroundAssignment_ShouldTrimKeyAndValue()
    {
        List<string> tokens = Transcribe("  host  =  localhost  \n");

        CollectionAssert.AreEqual(new List<string> { "Name:host", "String:localhost" }, tokens);
    }

    /// <summary>
    /// Verifies that both <c>;</c> and <c>#</c> comment lines are surfaced by default.
    /// </summary>
    [TestMethod]
    public void Read_WhenSemicolonAndHashComments_ShouldSurfaceBoth()
    {
        List<string> tokens = Transcribe("; one\n# two\nk=v\n");

        CollectionAssert.AreEqual(
            new List<string> { "Comment: one", "Comment: two", "Name:k", "String:v" },
            tokens);
    }

    /// <summary>
    /// Verifies that an empty document produces no tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyDocument_ShouldProduceNoTokens()
    {
        List<string> tokens = Transcribe(string.Empty);

        Assert.AreEqual(0, tokens.Count);
    }

    /// <summary>
    /// Verifies that an entry with no assignment operator throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenMissingAssignment_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Transcribe("[s]\nkeywithoutvalue\n");
        });
    }
}
