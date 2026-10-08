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

    /// <summary>
    /// Verifies that a malformed line after two lines ended with CRLF is reported on its own line, 3, so that each CRLF
    /// counts as one line ending rather than two.
    /// </summary>
    [TestMethod]
    public void Read_WhenMalformedLineFollowsCrLfLines_ShouldReportItsLineNumber()
    {
        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Transcribe("a=1\r\nb=2\r\nbad\r\n");
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a malformed second line is reported with the one-based line number 2.
    /// </summary>
    [TestMethod]
    public void Read_WhenSecondLineIsMalformed_ShouldReportLineNumberTwo()
    {
        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Transcribe("\nbad\n");
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a section name containing <c>]</c> is read whole, up to the first <c>]</c> that only whitespace or
    /// a comment follows, rather than cut short at its first <c>]</c>.
    /// </summary>
    /// <param name="header">The section header line.</param>
    /// <param name="name">The section name the header holds.</param>
    [TestMethod]
    [DataRow("[This One Has A ] In It]", "This One Has A ] In It", DisplayName = "configparser bpo-38741")]
    [DataRow("[foo]bar]", "foo]bar", DisplayName = "go-ini #46")]
    [DataRow("[12345]]", "12345]", DisplayName = "iniparser PR #159, bracket at the end")]
    [DataRow("[123]45]", "123]45", DisplayName = "iniparser PR #159, bracket inside")]
    [DataRow("[a] b]", "a] b", DisplayName = "space after an inner bracket")]
    public void Read_WhenSectionNameContainsClosingBracket_ShouldReadTheWholeName(string header, string name)
    {
        List<string> tokens = Transcribe(header + "\nk=v\n");

        CollectionAssert.AreEqual(new List<string> { $"Section:{name}", "Name:k", "String:v" }, tokens);
    }

    /// <summary>
    /// Verifies that a section header followed by whitespace or a comment is read up to its <c>]</c>, and that the
    /// comment is skipped rather than reported as a comment token.
    /// </summary>
    /// <param name="header">The section header line.</param>
    /// <param name="name">The section name the header holds.</param>
    [TestMethod]
    [DataRow("[comments] ; note", "comments", DisplayName = "semicolon comment")]
    [DataRow("[s] # note", "s", DisplayName = "hash comment")]
    [DataRow("[s];note", "s", DisplayName = "comment straight after the bracket")]
    [DataRow("[a]; b]", "a", DisplayName = "comment holding a bracket")]
    [DataRow("[b] \t", "b", DisplayName = "trailing whitespace")]
    public void Read_WhenWhitespaceOrCommentFollowsSectionHeader_ShouldReadTheNameAndSkipTheComment(string header, string name)
    {
        List<string> tokens = Transcribe(header + "\nk=v\n");

        CollectionAssert.AreEqual(new List<string> { $"Section:{name}", "Name:k", "String:v" }, tokens);
    }

    /// <summary>
    /// Verifies that a section header followed by text that is neither whitespace nor a comment throws
    /// <see cref="IniFormatException" /> positioned at that text, rather than dropping the text.
    /// </summary>
    /// <param name="source">The INI text.</param>
    /// <param name="offset">The byte offset of the text after the header.</param>
    [TestMethod]
    [DataRow("[foo] bar\nk=v\n", 6, DisplayName = "text after the header")]
    [DataRow("[foo]bar\nk=v\n", 5, DisplayName = "text straight after the bracket")]
    [DataRow("[a]b]c\nk=v\n", 5, DisplayName = "text after the last bracket")]
    public void Read_WhenTextFollowsSectionHeader_ShouldThrowIniFormatException(string source, int offset)
    {
        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Transcribe(source);
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(offset, ex.Offset);
    }

    /// <summary>
    /// Verifies that when <c>#</c> comments are disallowed, a <c>#</c> after an inner <c>]</c> is part of the section
    /// name, because it does not start a comment.
    /// </summary>
    [TestMethod]
    public void Read_WhenHashCommentsAreDisallowedAndHashFollowsInnerBracket_ShouldReadItAsPartOfTheName()
    {
        List<string> tokens = Transcribe("[a]#b]\nk=v\n", new IniReaderOptions { DisallowHashComments = true });

        CollectionAssert.AreEqual(new List<string> { "Section:a]#b", "Name:k", "String:v" }, tokens);
    }

    /// <summary>
    /// Verifies that when <c>#</c> comments are disallowed, a <c>#</c> after a section header is text, which throws
    /// <see cref="IniFormatException" /> positioned at the <c>#</c>.
    /// </summary>
    [TestMethod]
    public void Read_WhenHashCommentsAreDisallowedAndHashFollowsSectionHeader_ShouldThrowIniFormatException()
    {
        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Transcribe("[s] # note\nk=v\n", new IniReaderOptions { DisallowHashComments = true });
        });

        Assert.AreEqual(4, ex.Offset);
    }
}
