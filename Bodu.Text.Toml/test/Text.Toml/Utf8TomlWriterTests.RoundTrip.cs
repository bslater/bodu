// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that integer boundary values round-trip through a write/read cycle to an equal value.
    /// </summary>
    /// <param name="value">The integer value to round-trip.</param>
    [TestMethod]
    [DataRow(0L, DisplayName = "zero")]
    [DataRow(-1L, DisplayName = "negative one")]
    [DataRow(long.MaxValue, DisplayName = "Int64 max")]
    [DataRow(long.MinValue, DisplayName = "Int64 min")]
    public void WriteThenRead_WhenInteger_ShouldRoundTripValue(long value)
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);
        writer.WriteStartTable();
        writer.WritePropertyName("v");
        writer.WriteInteger(value);
        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "v");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(value, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that float values of varying magnitude round-trip through a write/read cycle to an equal value.
    /// </summary>
    /// <param name="value">The float value to round-trip.</param>
    [TestMethod]
    [DataRow(0.0, DisplayName = "zero")]
    [DataRow(1.5, DisplayName = "fraction")]
    [DataRow(-2.5, DisplayName = "negative fraction")]
    [DataRow(3.0, DisplayName = "whole")]
    [DataRow(1e10, DisplayName = "large without exponent text")]
    [DataRow(1e100, DisplayName = "very large")]
    [DataRow(6.626e-34, DisplayName = "very small")]
    [DataRow(0.3333333333333333, DisplayName = "repeating")]
    [TestCategory("Regression")]
    public void WriteThenRead_WhenFloat_ShouldRoundTripValue(double value)
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);
        writer.WriteStartTable();
        writer.WritePropertyName("v");
        writer.WriteFloat(value);
        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "v");
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.AreEqual(value, reader.GetDouble());
    }

    /// <summary>
    /// Verifies that strings carrying reserved and control characters round-trip through a write/read cycle to the same
    /// content.
    /// </summary>
    /// <param name="value">The string value to round-trip.</param>
    [TestMethod]
    [DataRow("", DisplayName = "empty")]
    [DataRow("simple", DisplayName = "simple")]
    [DataRow("with \"quotes\"", DisplayName = "quotes")]
    [DataRow("tab\tand\nnewline", DisplayName = "control characters")]
    [DataRow("back\\slash", DisplayName = "backslash")]
    [DataRow("café résumé", DisplayName = "unicode")]
    [TestCategory("Regression")]
    public void WriteThenRead_WhenString_ShouldRoundTripValue(string value)
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);
        writer.WriteStartTable();
        writer.WritePropertyName("v");
        writer.WriteString(value);
        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "v");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual(value, reader.GetString());
    }

    /// <summary>
    /// Verifies that an astral (non-BMP) Unicode string round-trips through a write/read cycle to the same content.
    /// </summary>
    [TestMethod]
    public void WriteThenRead_WhenStringHasAstralCharacter_ShouldRoundTripValue()
    {
        string value = "emoji " + char.ConvertFromUtf32(0x1F600);

        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);
        writer.WriteStartTable();
        writer.WritePropertyName("v");
        writer.WriteString(value);
        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "v");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual(value, reader.GetString());
    }

    /// <summary>
    /// Verifies that an offset date-time carrying fractional seconds and a non-zero offset round-trips to an equal
    /// instant.
    /// </summary>
    [TestMethod]
    public void WriteThenRead_WhenOffsetDateTimeWithFraction_ShouldRoundTripInstant()
    {
        DateTimeOffset value = new DateTimeOffset(2020, 6, 15, 8, 9, 10, TimeSpan.FromHours(-5)).AddTicks(1234567);

        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);
        writer.WriteStartTable();
        writer.WritePropertyName("v");
        writer.WriteOffsetDateTime(value);
        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "v");
        ExpectToken(ref reader, TomlTokenType.OffsetDateTime);
        Assert.AreEqual(value, reader.GetDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a quoted key requiring escaping round-trips through a write/read cycle to the same key text.
    /// </summary>
    [TestMethod]
    public void WriteThenRead_WhenKeyRequiresQuoting_ShouldRoundTripKey()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);
        writer.WriteStartTable();
        writer.WritePropertyName("a.b c");
        writer.WriteInteger(1);
        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "a.b c");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an empty array and an empty inline table round-trip through a write/read cycle, preserving their
    /// container kind.
    /// </summary>
    [TestMethod]
    public void WriteThenRead_WhenEmptyContainers_ShouldRoundTripKinds()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);
        writer.WriteStartTable();

        writer.WritePropertyName("arr");
        writer.WriteStartArray();
        writer.WriteEndArray();

        writer.WritePropertyName("inner");
        writer.WriteStartTable();
        writer.WriteEndTable();

        writer.WriteEndTable();

        TomlDocumentReader reader = new(buffer.WrittenSpan);
        ExpectToken(ref reader, TomlTokenType.StartTable);

        ExpectProperty(ref reader, "arr");
        ExpectToken(ref reader, TomlTokenType.StartArray);
        ExpectToken(ref reader, TomlTokenType.EndArray);

        ExpectProperty(ref reader, "inner");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.EndTable);
        Assert.IsFalse(reader.Read());
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

}
