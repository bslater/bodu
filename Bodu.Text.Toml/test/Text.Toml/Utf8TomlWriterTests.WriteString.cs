// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WriteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.WriteString" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that an empty string value is emitted as an empty basic string.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenEmpty_ShouldEmitEmptyQuotes()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteString(string.Empty);
            writer.WriteEndTable();
        });

        Assert.AreEqual("v = \"\"\n", actual);
    }

    /// <summary>
    /// Verifies that string values escape the reserved characters — quote, backslash, and the named control codes — in
    /// their basic-string form.
    /// </summary>
    /// <param name="value">The string value to write.</param>
    /// <param name="expectedInner">The expected emitted text between the surrounding quotes.</param>
    [TestMethod]
    [DataRow("a\tb", "a\\tb", DisplayName = "tab")]
    [DataRow("a\nb", "a\\nb", DisplayName = "line feed")]
    [DataRow("a\rb", "a\\rb", DisplayName = "carriage return")]
    [DataRow("a\bb", "a\\bb", DisplayName = "backspace")]
    [DataRow("a\fb", "a\\fb", DisplayName = "form feed")]
    [DataRow("a\"b", "a\\\"b", DisplayName = "quote")]
    [DataRow("a\\b", "a\\\\b", DisplayName = "backslash")]
    [TestCategory("Regression")]
    public void WriteString_WhenContainsReservedCharacter_ShouldEscape(string value, string expectedInner)
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteString(value);
            writer.WriteEndTable();
        });

        Assert.AreEqual($"v = \"{expectedInner}\"\n", actual);
    }

    /// <summary>
    /// Verifies that low control characters without a named escape are emitted as the <c>\uXXXX</c> form, including
    /// the delete character U+007F.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenContainsBareControlCharacters_ShouldEmitUnicodeEscapes()
    {
        string value = "a" + (char)0x00 + (char)0x01 + (char)0x7F + "b";

        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteString(value);
            writer.WriteEndTable();
        });

        Assert.AreEqual("v = \"a\\u0000\\u0001\\u007Fb\"\n", actual);
    }

    /// <summary>
    /// Verifies that writing a <see langword="null" /> string value throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteString((string)null!);
        }, "value");
    }

    /// <summary>
    /// Verifies that completing a root-level scalar value throws <see cref="InvalidOperationException" />, because the
    /// root of a TOML document must be a table; the writer must not complete silently without emitting output.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenRootValueIsScalar_ShouldThrowInvalidOperationException()
    {
        ArrayBufferWriter<byte> buffer = new();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Utf8TomlWriter writer = new(buffer);
            writer.WriteString("x");
        });

        Assert.AreEqual(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that a string value containing an unpaired surrogate throws <see cref="ArgumentException" /> at the
    /// <see cref="Utf8TomlWriter.WriteString" /> call that supplied it, rather than deferring the failure to the
    /// root-table close.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenValueContainsLoneSurrogate_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("s");
            writer.WriteString("a\uD800b");
        });
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

}
