// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.cs" company="Bodu Pty. Ltd.">
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
/// Verifies the behaviour of <see cref="Utf8TomlWriter" />, the forward-only canonical TOML writer.
/// </summary>
[TestClass]
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// The UTF-8 encoding used to decode the writer output for text assertions; it omits a byte-order mark.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Builds a TOML document by invoking <paramref name="build" /> against a fresh writer, passed by reference so the
    /// caller can drive the <see langword="ref struct" /> writer.
    /// </summary>
    /// <param name="writer">The writer to drive.</param>
    private delegate void WriterBuild(ref Utf8TomlWriter writer);

    /// <summary>
    /// Writes a document with the supplied build callback and returns the emitted canonical UTF-8 text.
    /// </summary>
    /// <param name="build">The callback that drives the writer.</param>
    /// <returns>The decoded canonical TOML text.</returns>
    private static string WriteDocument(WriterBuild build)
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);
        build(ref writer);
        return s_utf8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Verifies that a small document containing a root scalar, a sub-table, an inline array, and an array of tables
    /// is emitted as the expected canonical UTF-8 text.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Write_WhenSmallDocument_ShouldEmitExpectedCanonicalText()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();

        writer.WritePropertyName("name");
        writer.WriteString("x");

        writer.WritePropertyName("nums");
        writer.WriteStartArray();
        writer.WriteInteger(1);
        writer.WriteInteger(2);
        writer.WriteInteger(3);
        writer.WriteEndArray();

        writer.WritePropertyName("table");
        writer.WriteStartTable();
        writer.WritePropertyName("a");
        writer.WriteInteger(1);
        writer.WritePropertyName("b");
        writer.WriteString("y");
        writer.WriteEndTable();

        writer.WritePropertyName("products");
        writer.WriteStartArray();
        writer.WriteStartTable();
        writer.WritePropertyName("name");
        writer.WriteString("A");
        writer.WriteEndTable();
        writer.WriteStartTable();
        writer.WritePropertyName("name");
        writer.WriteString("B");
        writer.WriteEndTable();
        writer.WriteEndArray();

        writer.WriteEndTable();

        var expected =
            "name = \"x\"\n" +
            "nums = [1, 2, 3]\n" +
            "\n" +
            "[table]\n" +
            "a = 1\n" +
            "b = \"y\"\n" +
            "\n" +
            "[[products]]\n" +
            "name = \"A\"\n" +
            "\n" +
            "[[products]]\n" +
            "name = \"B\"\n";

        Assert.AreEqual(expected, s_utf8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a table reached inside a nested array — where the enclosing member is a plain array rather than an
    /// array of tables — is emitted in inline <c>{ … }</c> form, and the nested array stays inline.
    /// </summary>
    /// <remarks>
    /// A direct array-of-tables member surfaces as <c>[[header]]</c> blocks; only a table that appears within a value
    /// formatted as an inline array renders inline. Wrapping the table array in an outer array forces that inline path.
    /// </remarks>
    [TestMethod]
    public void Write_WhenTableInsideNestedArray_ShouldEmitInlineTable()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();
        writer.WritePropertyName("points");
        writer.WriteStartArray();
        writer.WriteStartArray();

        writer.WriteStartTable();
        writer.WritePropertyName("x");
        writer.WriteInteger(1);
        writer.WritePropertyName("y");
        writer.WriteInteger(2);
        writer.WriteEndTable();

        writer.WriteEndArray();
        writer.WriteEndArray();
        writer.WriteEndTable();

        Assert.AreEqual("points = [[{ x = 1, y = 2 }]]\n", s_utf8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a top-level array whose every element is a table is emitted as a run of <c>[[header]]</c>
    /// array-of-tables blocks rather than as an inline array.
    /// </summary>
    [TestMethod]
    public void Write_WhenArrayOfTables_ShouldEmitDoubleBracketHeaders()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();
        writer.WritePropertyName("points");
        writer.WriteStartArray();

        writer.WriteStartTable();
        writer.WritePropertyName("x");
        writer.WriteInteger(1);
        writer.WriteEndTable();

        writer.WriteStartTable();
        writer.WritePropertyName("x");
        writer.WriteInteger(2);
        writer.WriteEndTable();

        writer.WriteEndArray();
        writer.WriteEndTable();

        Assert.AreEqual(
            "[[points]]\nx = 1\n\n[[points]]\nx = 2\n",
            s_utf8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that each scalar value kind, including every date-time kind, is emitted in its canonical TOML form.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarKinds_ShouldEmitCanonicalForms()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();

        writer.WritePropertyName("s");
        writer.WriteString("hi\tthere");

        writer.WritePropertyName("i");
        writer.WriteInteger(-42);

        writer.WritePropertyName("f");
        writer.WriteFloat(1.5);

        writer.WritePropertyName("whole");
        writer.WriteFloat(3.0);

        writer.WritePropertyName("inf");
        writer.WriteFloat(double.PositiveInfinity);

        writer.WritePropertyName("ninf");
        writer.WriteFloat(double.NegativeInfinity);

        writer.WritePropertyName("nan");
        writer.WriteFloat(double.NaN);

        writer.WritePropertyName("b");
        writer.WriteBoolean(true);

        writer.WritePropertyName("odt");
        writer.WriteOffsetDateTime(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero));

        writer.WritePropertyName("ldt");
        writer.WriteLocalDateTime(new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Unspecified));

        writer.WritePropertyName("ld");
        writer.WriteLocalDate(new DateOnly(1979, 5, 27));

        writer.WritePropertyName("lt");
        writer.WriteLocalTime(new TimeOnly(7, 32, 10));

        writer.WriteEndTable();

        var expected =
            "s = \"hi\\tthere\"\n" +
            "i = -42\n" +
            "f = 1.5\n" +
            "whole = 3.0\n" +
            "inf = inf\n" +
            "ninf = -inf\n" +
            "nan = nan\n" +
            "b = true\n" +
            "odt = 1979-05-27T07:32:00Z\n" +
            "ldt = 1979-05-27T07:32:00\n" +
            "ld = 1979-05-27\n" +
            "lt = 07:32:10\n";

        Assert.AreEqual(expected, s_utf8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a key that does not match the bare-key grammar is basic-quoted, while a bare key is not.
    /// </summary>
    [TestMethod]
    public void Write_WhenKeyRequiresQuoting_ShouldEmitQuotedKey()
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

    /// <summary>
    /// Verifies that a document round-trips: writing it and reading the bytes back yields the same token stream and
    /// decoded scalar values, including the nested-table, inline-array, and array-of-tables shapes.
    /// </summary>
    [TestMethod]
    public void WriteThenRead_WhenMixedDocument_ShouldRoundTripTokenStream()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();

        writer.WritePropertyName("title");
        writer.WriteString("demo");

        writer.WritePropertyName("nums");
        writer.WriteStartArray();
        writer.WriteInteger(1);
        writer.WriteInteger(2);
        writer.WriteEndArray();

        writer.WritePropertyName("owner");
        writer.WriteStartTable();
        writer.WritePropertyName("name");
        writer.WriteString("Tom");
        writer.WritePropertyName("age");
        writer.WriteInteger(45);
        writer.WriteEndTable();

        writer.WritePropertyName("products");
        writer.WriteStartArray();
        writer.WriteStartTable();
        writer.WritePropertyName("sku");
        writer.WriteInteger(738594937);
        writer.WriteEndTable();
        writer.WriteStartTable();
        writer.WritePropertyName("sku");
        writer.WriteInteger(284758393);
        writer.WriteEndTable();
        writer.WriteEndArray();

        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);

        ExpectToken(ref reader, TomlTokenType.StartTable);

        ExpectProperty(ref reader, "title");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("demo", reader.GetString());

        ExpectProperty(ref reader, "nums");
        ExpectToken(ref reader, TomlTokenType.StartArray);
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndArray);

        ExpectProperty(ref reader, "owner");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "name");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("Tom", reader.GetString());
        ExpectProperty(ref reader, "age");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(45L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectProperty(ref reader, "products");
        ExpectToken(ref reader, TomlTokenType.StartArray);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "sku");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(738594937L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "sku");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(284758393L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.EndArray);

        ExpectToken(ref reader, TomlTokenType.EndTable);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that every scalar kind round-trips through a write/read cycle to an equal decoded value.
    /// </summary>
    [TestMethod]
    public void WriteThenRead_WhenScalarKinds_ShouldRoundTripValues()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        DateTimeOffset offset = new(1979, 5, 27, 0, 32, 0, TimeSpan.FromHours(-7));
        DateTime local = new(1979, 5, 27, 7, 32, 0, DateTimeKind.Unspecified);
        DateOnly date = new(1979, 5, 27);
        TimeOnly time = new(7, 32, 10);

        writer.WriteStartTable();

        writer.WritePropertyName("s");
        writer.WriteString("hello \"world\"\nnext");

        writer.WritePropertyName("i");
        writer.WriteInteger(long.MinValue);

        writer.WritePropertyName("f");
        writer.WriteFloat(3.14159);

        writer.WritePropertyName("b");
        writer.WriteBoolean(false);

        writer.WritePropertyName("odt");
        writer.WriteOffsetDateTime(offset);

        writer.WritePropertyName("ldt");
        writer.WriteLocalDateTime(local);

        writer.WritePropertyName("ld");
        writer.WriteLocalDate(date);

        writer.WritePropertyName("lt");
        writer.WriteLocalTime(time);

        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);

        ExpectToken(ref reader, TomlTokenType.StartTable);

        ExpectProperty(ref reader, "s");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("hello \"world\"\nnext", reader.GetString());

        ExpectProperty(ref reader, "i");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(long.MinValue, reader.GetInt64());

        ExpectProperty(ref reader, "f");
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.AreEqual(3.14159, reader.GetDouble());

        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.Boolean);
        Assert.IsFalse(reader.GetBoolean());

        ExpectProperty(ref reader, "odt");
        ExpectToken(ref reader, TomlTokenType.OffsetDateTime);
        Assert.AreEqual(offset, reader.GetDateTimeOffset());

        ExpectProperty(ref reader, "ldt");
        ExpectToken(ref reader, TomlTokenType.LocalDateTime);
        Assert.AreEqual(local, reader.GetDateTime());

        ExpectProperty(ref reader, "ld");
        ExpectToken(ref reader, TomlTokenType.LocalDate);
        Assert.AreEqual(date, reader.GetDateOnly());

        ExpectProperty(ref reader, "lt");
        ExpectToken(ref reader, TomlTokenType.LocalTime);
        Assert.AreEqual(time, reader.GetTimeOnly());

        ExpectToken(ref reader, TomlTokenType.EndTable);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that the floating-point sentinels round-trip through a write/read cycle to their IEEE 754 values.
    /// </summary>
    [TestMethod]
    public void WriteThenRead_WhenFloatSentinels_ShouldRoundTripInfinityAndNaN()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();
        writer.WritePropertyName("p");
        writer.WriteFloat(double.PositiveInfinity);
        writer.WritePropertyName("n");
        writer.WriteFloat(double.NegativeInfinity);
        writer.WritePropertyName("x");
        writer.WriteFloat(double.NaN);
        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);

        ExpectToken(ref reader, TomlTokenType.StartTable);

        ExpectProperty(ref reader, "p");
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.AreEqual(double.PositiveInfinity, reader.GetDouble());

        ExpectProperty(ref reader, "n");
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.AreEqual(double.NegativeInfinity, reader.GetDouble());

        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.IsTrue(double.IsNaN(reader.GetDouble()));

        ExpectToken(ref reader, TomlTokenType.EndTable);
    }

    /// <summary>
    /// Verifies that a deeply nested dotted table structure round-trips through a write/read cycle.
    /// </summary>
    [TestMethod]
    public void WriteThenRead_WhenNestedTables_ShouldRoundTripStructure()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();
        writer.WritePropertyName("a");
        writer.WriteStartTable();
        writer.WritePropertyName("b");
        writer.WriteStartTable();
        writer.WritePropertyName("c");
        writer.WriteInteger(1);
        writer.WriteEndTable();
        writer.WriteEndTable();
        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "c");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that opening a container beyond the configured maximum depth throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Write_WhenNestingExceedsMaxDepth_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            TomlWriterOptions options = new() { MaxDepth = 1 };
            Utf8TomlWriter writer = new(buffer, options);

            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteStartTable();
        });
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> buffer writer to the constructor throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>output</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOutputIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new Utf8TomlWriter((System.Buffers.IBufferWriter<byte>)null!);
        }, "output");
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> buffer writer to the options constructor overload throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>output</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOutputIsNull_ForOptionsOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new Utf8TomlWriter((System.Buffers.IBufferWriter<byte>)null!, new TomlWriterOptions { MaxDepth = 4 });
        }, "output");
    }

    /// <summary>
    /// Advances the reader and asserts that it landed on a <see cref="TomlTokenType.PropertyName" /> carrying the
    /// expected key.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <param name="expected">The expected property name.</param>
    private static void ExpectProperty(ref TomlDocumentReader reader, string expected)
    {
        ExpectToken(ref reader, TomlTokenType.PropertyName);
        Assert.AreEqual(expected, reader.GetString());
    }

    /// <summary>
    /// Advances the reader and asserts that it landed on the expected token type.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <param name="expected">The expected token type.</param>
    private static void ExpectToken(ref TomlDocumentReader reader, TomlTokenType expected)
    {
        Assert.IsTrue(reader.Read(), $"Expected {expected} but the reader reported end of document.");
        Assert.AreEqual(expected, reader.TokenType);
    }
}
