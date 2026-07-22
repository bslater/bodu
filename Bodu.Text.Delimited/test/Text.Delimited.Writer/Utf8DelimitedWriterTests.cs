// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

using Bodu.Text.Delimited.Writer;

namespace Bodu.Text.Delimited.Writer;

/// <summary>
/// Contains tests for <see cref="Utf8DelimitedWriter" />, verifying the emitted CSV bytes and RFC 4180 quoting.
/// </summary>
[TestClass]
public class Utf8DelimitedWriterTests
{
    /// <summary>
    /// Verifies that object records emit a single header row followed by one value row per record.
    /// </summary>
    [TestMethod]
    public void WriteEndObject_WhenObjectRecords_ShouldEmitHeaderThenRows()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer);

        writer.WriteStartArray();
        WriteRecord(ref writer, ("name", "Ada"), ("age", "36"));
        WriteRecord(ref writer, ("name", "Grace"), ("age", "45"));
        writer.WriteEndArray();
        writer.Flush();

        Assert.AreEqual("name,age\r\nAda,36\r\nGrace,45\r\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a field containing the delimiter, a quote, or a newline is quoted with internal quotes doubled.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenFieldNeedsQuoting_ShouldQuoteAndEscape()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer, new DelimitedWriterOptions { NoHeader = true });

        writer.WriteStartArray();
        writer.WriteStartArray();
        writer.WriteString("x,y");
        writer.WriteString("a\"b");
        writer.WriteString("c\nd");
        writer.WriteEndArray();
        writer.WriteEndArray();
        writer.Flush();

        Assert.AreEqual("\"x,y\",\"a\"\"b\",\"c\nd\"\r\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a document written by <see cref="Utf8DelimitedWriter" /> reads back to the same records.
    /// </summary>
    [TestMethod]
    public void Write_WhenRoundTripped_ShouldPreserveRecords()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer);

        writer.WriteStartArray();
        WriteRecord(ref writer, ("a", "1"), ("b", "two, 2"));
        writer.WriteEndArray();
        writer.Flush();

        var reader = new Bodu.Text.Delimited.Reader.Utf8DelimitedReader(buffer.WrittenSpan);
        var values = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == DelimitedTokenType.String)
                values.Add(reader.GetString());
        }

        CollectionAssert.AreEqual(new List<string> { "1", "two, 2" }, values);
    }

    /// <summary>
    /// Writes a single object record of name/value field pairs.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="fields">The name/value field pairs.</param>
    private static void WriteRecord(ref Utf8DelimitedWriter writer, params (string Name, string Value)[] fields)
    {
        writer.WriteStartObject();
        foreach ((string name, string value) in fields)
        {
            writer.WritePropertyName(name);
            writer.WriteString(value);
        }

        writer.WriteEndObject();
    }
}
