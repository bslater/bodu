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

        Utf8TomlReader reader = new(buffer.WrittenSpan);
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

        Utf8TomlReader reader = new(buffer.WrittenSpan);
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

        Utf8TomlReader reader = new(buffer.WrittenSpan);
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

        Utf8TomlReader reader = new(buffer.WrittenSpan);
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

        Utf8TomlReader reader = new(buffer.WrittenSpan);
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

        Utf8TomlReader reader = new(buffer.WrittenSpan);
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

        Utf8TomlReader reader = new(buffer.WrittenSpan);
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
}
