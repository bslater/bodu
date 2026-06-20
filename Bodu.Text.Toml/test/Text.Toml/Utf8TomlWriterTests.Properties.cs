// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that the combined property/value methods produce the same document as separate
    /// <c>WritePropertyName</c> and value calls.
    /// </summary>
    [TestMethod]
    public void WriteProperties_WhenEveryPairedOverload_ShouldMatchSeparateCalls()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);

        writer.WriteStartTable();
        writer.WriteString("s", "text");
        writer.WriteInteger("i", 42);
        writer.WriteFloat("f", 1.5);
        writer.WriteBoolean("b", true);
        writer.WriteOffsetDateTime("odt", new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero));
        writer.WriteLocalDateTime("ldt", new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Unspecified));
        writer.WriteLocalDate("ld", new DateOnly(1979, 5, 27));
        writer.WriteLocalTime("lt", new TimeOnly(7, 32, 0));
        writer.WriteEndTable();

        Assert.AreEqual(
            "s = \"text\"\ni = 42\nf = 1.5\nb = true\nodt = 1979-05-27T07:32:00Z\nldt = 1979-05-27T07:32:00\nld = 1979-05-27\nlt = 07:32:00\n",
            Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlWriter.WriteStartTable(string)" /> and
    /// <see cref="Utf8TomlWriter.WriteStartArray(string)" /> open a named container in one call.
    /// </summary>
    [TestMethod]
    public void WriteStartTable_WhenNamed_ShouldOpenNamedContainers()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);

        writer.WriteStartTable();
        writer.WriteStartArray("ports");
        writer.WriteInteger(1);
        writer.WriteInteger(2);
        writer.WriteEndArray();
        writer.WriteStartTable("server");
        writer.WriteString("host", "a");
        writer.WriteEndTable();
        writer.WriteEndTable();

        Assert.AreEqual(
            "ports = [1, 2]\n\n[server]\nhost = \"a\"\n",
            Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that the char-span and UTF-8 overloads of <c>WritePropertyName</c> and <c>WriteString</c> write the
    /// same content as the string overloads.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenSpanAndUtf8Overloads_ShouldMatchStringOverload()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);

        writer.WriteStartTable();
        writer.WritePropertyName("a".AsSpan());
        writer.WriteString("é-value".AsSpan());
        writer.WritePropertyName("b"u8);
        writer.WriteString("héllo"u8);
        writer.WriteEndTable();

        Assert.AreEqual(
            "a = \"é-value\"\nb = \"héllo\"\n",
            Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a UTF-8 overload rejects invalid UTF-8 with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenInvalidUtf8_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8TomlWriter(buffer);
            writer.WriteStartTable();
            writer.WritePropertyName("a");

            ReadOnlySpan<byte> invalid = [0x61, 0xFF, 0x62];
            writer.WriteString(invalid);
        });

        Assert.AreEqual("utf8Value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the paired overloads enforce the same state rules as the separate calls — a duplicate key is
    /// rejected.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenNamedPairRepeatsKey_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8TomlWriter(buffer);
            writer.WriteStartTable();
            writer.WriteInteger("a", 1);
            writer.WriteInteger("a", 2);
        });
    }
}
