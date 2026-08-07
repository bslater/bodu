// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

using Bodu.Text.Ini.Reader;
using Bodu.Text.Ini.Writer;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Contains tests for <see cref="Utf8IniWriter" />, verifying the emitted bytes and round-trip fidelity against
/// <see cref="Utf8IniReader" />.
/// </summary>
[TestClass]
public partial class Utf8IniWriterTests
{
    /// <summary>
    /// Verifies that a global key, a section header, and section entries are emitted in order.
    /// </summary>
    [TestMethod]
    public void Write_WhenGlobalKeyAndSection_ShouldEmitExpectedText()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WritePropertyName("key0");
        writer.WriteString("a");
        writer.WriteSectionHeader("db");
        writer.WritePropertyName("host");
        writer.WriteString("localhost");
        writer.Flush();

        Assert.AreEqual("key0=a\n[db]\nhost=localhost\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

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
    /// Verifies that a document written by <see cref="Utf8IniWriter" /> reads back to the same section/key/value tokens.
    /// </summary>
    [TestMethod]
    public void Write_WhenRoundTripped_ShouldPreserveTokens()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WriteSectionHeader("server");
        writer.WritePropertyName("host");
        writer.WriteString("example.com");
        writer.WritePropertyName("port");
        writer.WriteString("8080");
        writer.Flush();

        var reader = new Utf8IniReader(buffer.WrittenSpan);
        var tokens = new List<string>();
        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                IniTokenType.SectionHeader => $"Section:{reader.GetString()}",
                IniTokenType.PropertyName => $"Name:{reader.GetString()}",
                IniTokenType.String => $"String:{reader.GetString()}",
                _ => reader.TokenType.ToString(),
            });
        }

        CollectionAssert.AreEqual(
            new List<string> { "Section:server", "Name:host", "String:example.com", "Name:port", "String:8080" },
            tokens);
    }
}
