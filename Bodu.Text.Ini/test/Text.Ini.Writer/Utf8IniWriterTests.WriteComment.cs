// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriterTests.WriteComment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Contains the member tests for <see cref="Utf8IniWriter.WriteComment(string)" />.
/// </summary>
public partial class Utf8IniWriterTests
{
    /// <summary>
    /// Verifies that a comment is written with the configured prefix.
    /// </summary>
    [TestMethod]
    public void WriteComment_WhenCustomPrefix_ShouldUseIt()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer, new IniWriterOptions { CommentPrefix = '#' });

        writer.WriteComment(" a note");
        writer.Flush();

        Assert.AreEqual("# a note\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a comment holding a line break is written as one comment line per line of its text, each with the
    /// comment prefix, so that no line of it reads back as anything but a comment.
    /// </summary>
    [TestMethod]
    public void WriteComment_WhenTextContainsLineBreak_ShouldWriteEachLineAsAComment()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WriteComment("Multiline\nComment");
        writer.Flush();

        Assert.AreEqual(";Multiline\n;Comment\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a comment holding line breaks of any kind is written as one comment line per line, a CRLF counting
    /// as one line break and an empty line becoming an empty comment line.
    /// </summary>
    /// <param name="text">The comment text.</param>
    /// <param name="expected">The text written.</param>
    [TestMethod]
    [DataRow("a\rb", ";a\n;b\n", DisplayName = "CR")]
    [DataRow("a\r\nb", ";a\n;b\n", DisplayName = "CRLF")]
    [DataRow("a\n\nb", ";a\n;\n;b\n", DisplayName = "empty line")]
    [DataRow("a\n", ";a\n;\n", DisplayName = "trailing line break")]
    public void WriteComment_WhenTextHasSeveralLines_ShouldWriteOneCommentLinePerLine(string text, string expected)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WriteComment(text);
        writer.Flush();

        Assert.AreEqual(expected, Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that each line of a comment holding a line break is written with the configured prefix.
    /// </summary>
    [TestMethod]
    public void WriteComment_WhenCustomPrefixAndTextContainsLineBreak_ShouldPrefixEveryLine()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer, new IniWriterOptions { CommentPrefix = '#' });

        writer.WriteComment("Multiline\nComment");
        writer.Flush();

        Assert.AreEqual("#Multiline\n#Comment\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a comment holding line breaks reads back as comment lines only, one per line of its text.
    /// </summary>
    [TestMethod]
    public void WriteComment_WhenTextContainsLineBreaks_ShouldReadBackAsCommentsOnly()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WriteComment("a\r\nb=c\r[d]\ne");
        writer.Flush();

        var reader = new Utf8IniReader(buffer.WrittenSpan);
        var tokens = new List<string>();
        while (reader.Read())
            tokens.Add($"{reader.TokenType}:{reader.GetString()}");

        CollectionAssert.AreEqual(new List<string> { "Comment:a", "Comment:b=c", "Comment:[d]", "Comment:e" }, tokens);
    }
}
