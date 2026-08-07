// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

using Bodu.Text.DotEnv.Reader;
using Bodu.Text.DotEnv.Writer;

namespace Bodu.Text.DotEnv.Writer;

/// <summary>
/// Contains tests for <see cref="Utf8DotEnvWriter" />, verifying the emitted bytes and round-trip fidelity against
/// <see cref="Utf8DotEnvReader" />.
/// </summary>
[TestClass]
public partial class Utf8DotEnvWriterTests
{
    /// <summary>
    /// Writes the supplied entries to a buffer and returns the emitted text.
    /// </summary>
    /// <param name="entries">The key/value entries to write.</param>
    /// <returns>The emitted DotEnv text.</returns>
    private static string Write(params (string Key, string Value)[] entries)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DotEnvWriter(buffer);

        writer.WriteStartObject();
        foreach ((string key, string value) in entries)
        {
            writer.WritePropertyName(key);
            writer.WriteString(value);
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Verifies that a simple entry is written unquoted as a <c>KEY=VALUE</c> line.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenSimpleValue_ShouldWriteUnquotedLine()
    {
        string text = Write(("HOST", "localhost"));

        Assert.AreEqual("HOST=localhost\n", text);
    }

    /// <summary>
    /// Verifies that a value containing whitespace and a hash is double-quoted and escaped.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenValueNeedsQuoting_ShouldDoubleQuoteAndEscape()
    {
        string text = Write(("MSG", "a\tb\nc"));

        Assert.AreEqual("MSG=\"a\\tb\\nc\"\n", text);
    }

    /// <summary>
    /// Verifies that an empty value is written as a bare assignment.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenEmptyValue_ShouldWriteBareAssignment()
    {
        string text = Write(("EMPTY", string.Empty));

        Assert.AreEqual("EMPTY=\n", text);
    }

    /// <summary>
    /// Verifies that a document written by <see cref="Utf8DotEnvWriter" /> reads back to the same key/value pairs
    /// through <see cref="Utf8DotEnvReader" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenRoundTripped_ShouldPreserveKeyValuePairs()
    {
        (string Key, string Value)[] entries =
        [
            ("HOST", "localhost"),
            ("MSG", "hello world # not a comment"),
            ("MULTILINE", "line1\nline2"),
            ("EMPTY", string.Empty),
        ];

        string text = Write(entries);
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        var reader = new Utf8DotEnvReader(bytes);

        var readBack = new List<(string Key, string Value)>();
        string? pendingKey = null;

        while (reader.Read())
        {
            if (reader.TokenType == DotEnvTokenType.PropertyName)
                pendingKey = reader.GetString();
            else if (reader.TokenType == DotEnvTokenType.String)
                readBack.Add((pendingKey!, reader.GetString()));
        }

        CollectionAssert.AreEqual(entries, readBack);
    }
}
