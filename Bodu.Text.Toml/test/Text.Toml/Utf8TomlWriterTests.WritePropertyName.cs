// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WritePropertyName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.WritePropertyName" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that a key matching the bare-key grammar is emitted without quoting, while a key containing illegal
    /// bare-key characters is basic-quoted with escaping.
    /// </summary>
    /// <param name="key">The property key to write.</param>
    /// <param name="expectedKey">The expected emitted key text, quoted where required.</param>
    [TestMethod]
    [DataRow("bareKey", "bareKey", DisplayName = "bare letters")]
    [DataRow("a-b_c", "a-b_c", DisplayName = "bare with hyphen and underscore")]
    [DataRow("1234", "1234", DisplayName = "bare digits")]
    [DataRow("needs quoting", "\"needs quoting\"", DisplayName = "space forces quoting")]
    [DataRow("a.b", "\"a.b\"", DisplayName = "dot forces quoting")]
    [DataRow("café", "\"café\"", DisplayName = "unicode forces quoting")]
    [DataRow("", "\"\"", DisplayName = "empty key is quoted")]
    [TestCategory("Regression")]
    public void WritePropertyName_WhenKey_ShouldQuoteOnlyWhenRequired(string key, string expectedKey)
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName(key);
            writer.WriteInteger(1);
            writer.WriteEndTable();
        });

        Assert.AreEqual($"{expectedKey} = 1\n", actual);
    }

    /// <summary>
    /// Verifies that a key requiring quoting escapes reserved characters within the emitted basic-quoted key.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenKeyContainsReservedCharacter_ShouldEscapeWithinQuotes()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("a\"b");
            writer.WriteInteger(1);
            writer.WriteEndTable();
        });

        Assert.AreEqual("\"a\\\"b\" = 1\n", actual);
    }

    /// <summary>
    /// Verifies that a sub-table header path quotes only the segments that require it, leaving bare segments unquoted,
    /// and emits a header for each intermediate table even when it carries no scalar members.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenSubTableHeaderHasMixedSegments_ShouldQuotePerSegment()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("server");
            writer.WriteStartTable();
            writer.WritePropertyName("data center");
            writer.WriteStartTable();
            writer.WritePropertyName("id");
            writer.WriteInteger(1);
            writer.WriteEndTable();
            writer.WriteEndTable();
            writer.WriteEndTable();
        });

        Assert.AreEqual("[server]\n\n[server.\"data center\"]\nid = 1\n", actual);
    }

    /// <summary>
    /// Verifies that writing a <see langword="null" /> property name throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);
            writer.WriteStartTable();
            writer.WritePropertyName((string)null!);
        }, "name");
    }
    /// <summary>
    /// Verifies that writing the same property name twice within one table throws
    /// <see cref="InvalidOperationException" />, because duplicate keys make a TOML document invalid and the writer
    /// must never emit output its own reader rejects.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenDuplicateInTable_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteInteger(1);
            writer.WritePropertyName("a");
            writer.WriteInteger(2);
            writer.WriteEndTable();
        });
    }

    /// <summary>
    /// Verifies that supplying a property name while the innermost open container is an array throws
    /// <see cref="InvalidOperationException" />, mirroring the structural validation contract of
    /// <c>Utf8JsonWriter</c>.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenCurrentContainerIsArray_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            writer.WritePropertyName("a");
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8TomlWriter.WritePropertyName" /> twice in a row throws
    /// <see cref="InvalidOperationException" /> rather than silently discarding the first pending name.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenCalledTwiceConsecutively_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WritePropertyName("b");
        });
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
    /// Verifies that a key that does not match the bare-key grammar is basic-quoted, while a bare key is not.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenKeyRequiresQuoting_ShouldEmitQuotedKey()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();
        writer.WritePropertyName("bare-key_1");
        writer.WriteInteger(1);
        writer.WritePropertyName("needs quoting");
        writer.WriteInteger(2);
        writer.WriteEndTable();

        Assert.AreEqual("bare-key_1 = 1\n\"needs quoting\" = 2\n", s_utf8.GetString(buffer.WrittenSpan));
    }

}
