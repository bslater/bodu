// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.WriteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter.WriteString" /> emits the expected output and enforces its contract.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>
    /// Verifies that a root string scalar resembling a document marker is emitted quoted, so parsing the output yields
    /// the original string rather than a document marker.
    /// </summary>
    /// <param name="value">The string scalar to write at the document root.</param>
    [TestMethod]
    [DataRow("...")]
    [DataRow("---")]
    [DataRow("... n")]
    [DataRow("--- n")]
    public void WriteString_WhenResemblesDocumentMarker_ShouldQuoteSoItRoundTrips(string value)
    {
        string yaml = Write((ref Utf8YamlWriter w) => w.WriteString(value));

        using var doc = YamlDocument.Parse(yaml);
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.ValueKind, yaml);
        Assert.AreEqual(value, doc.RootElement.GetString(), yaml);
    }

    /// <summary>Verifies that a string which would resolve to another type is quoted.</summary>
    [TestMethod]
    public void WriteString_WhenAmbiguous_ShouldQuote()
    {
        string yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("a");
            w.WriteString("123");
            w.WritePropertyName("b");
            w.WriteString("true");
            w.WritePropertyName("c");
            w.WriteString("no");
            w.WriteEndMapping();
        });

        Assert.AreEqual("a: \"123\"\nb: \"true\"\nc: \"no\"\n", yaml);
    }

    /// <summary>Verifies that a value containing special characters is double-quoted with escapes.</summary>
    [TestMethod]
    public void WriteString_WhenSpecialCharacters_ShouldEscape()
    {
        string yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("text");
            w.WriteString("line1\nline2\ttab");
            w.WriteEndMapping();
        });

        Assert.AreEqual("text: \"line1\\nline2\\ttab\"\n", yaml);
    }

    /// <summary>
    /// Verifies that writing a second root value throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenSecondRootValue_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8YamlWriter(buffer);
            writer.WriteString("first");
            writer.WriteString("second");
        });
    }

    /// <summary>
    /// Verifies that writing a string containing an unpaired surrogate throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenUnpairedSurrogate_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8YamlWriter(buffer);
            writer.WriteString("\uD800");
        });
    }

    /// <summary>
    /// Verifies that a non-printable control character is emitted as a hex escape within a double-quoted scalar.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenControlCharacter_ShouldEscapeAsHex()
    {
        string value = "a" + (char)0x01 + "b";
        string yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("c");
            w.WriteString(value);
            w.WriteEndMapping();
        });

        Assert.AreEqual("c: \"a\\x01b\"\n", yaml);
    }

}
