// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Scalars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Test.Assertions;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that integer values, including the signed 64-bit boundaries and zero, are emitted in decimal form.
    /// </summary>
    /// <param name="value">The integer value to write.</param>
    /// <param name="expected">The expected emitted value text.</param>
    [TestMethod]
    [DataRow(0L, "0", DisplayName = "zero")]
    [DataRow(42L, "42", DisplayName = "positive")]
    [DataRow(-42L, "-42", DisplayName = "negative")]
    [DataRow(long.MaxValue, "9223372036854775807", DisplayName = "Int64 max")]
    [DataRow(long.MinValue, "-9223372036854775808", DisplayName = "Int64 min")]
    public void Write_WhenInteger_ShouldEmitDecimal(long value, string expected)
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteInteger(value);
            writer.WriteEndTable();
        });

        Assert.AreEqual($"v = {expected}\n", actual);
    }

    /// <summary>
    /// Verifies that boolean values are emitted as the lowercase keywords <c>true</c> and <c>false</c>.
    /// </summary>
    /// <param name="value">The boolean value to write.</param>
    /// <param name="expected">The expected emitted keyword.</param>
    [TestMethod]
    [DataRow(true, "true", DisplayName = "true")]
    [DataRow(false, "false", DisplayName = "false")]
    public void Write_WhenBoolean_ShouldEmitKeyword(bool value, string expected)
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteBoolean(value);
            writer.WriteEndTable();
        });

        Assert.AreEqual($"v = {expected}\n", actual);
    }

    /// <summary>
    /// Verifies that floating-point values render their shortest round-trippable spelling, always carrying a fractional
    /// point or exponent so that they read back as floats.
    /// </summary>
    /// <param name="value">The floating-point value to write.</param>
    /// <param name="expected">The expected emitted value text.</param>
    /// <remarks>
    /// The canonical spelling is the .NET round-trip (<c>"R"</c>) format with a <c>.0</c> suffix appended when the
    /// result has neither a decimal point nor an exponent. Large and small magnitudes therefore surface an uppercase
    /// <c>E</c> exponent, which TOML accepts.
    /// </remarks>
    [TestMethod]
    [DataRow(0.0, "0.0", DisplayName = "zero")]
    [DataRow(1.5, "1.5", DisplayName = "fraction")]
    [DataRow(3.0, "3.0", DisplayName = "whole number gains point")]
    [DataRow(100.0, "100.0", DisplayName = "round whole")]
    [DataRow(-2.5, "-2.5", DisplayName = "negative fraction")]
    [DataRow(1e10, "10000000000.0", DisplayName = "exponent expands without E")]
    [DataRow(1e100, "1E+100", DisplayName = "large magnitude uses E")]
    [DataRow(6.626e-34, "6.626E-34", DisplayName = "small magnitude uses E")]
    [TestCategory("Regression")]
    public void Write_WhenFloat_ShouldEmitShortestRoundTrippableSpelling(double value, string expected)
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteFloat(value);
            writer.WriteEndTable();
        });

        Assert.AreEqual($"v = {expected}\n", actual);
    }

    /// <summary>
    /// Verifies that the float sentinels are emitted as the TOML keywords <c>inf</c>, <c>-inf</c>, and <c>nan</c>.
    /// </summary>
    /// <param name="value">The floating-point sentinel to write.</param>
    /// <param name="expected">The expected emitted keyword.</param>
    [TestMethod]
    [DataRow(double.PositiveInfinity, "inf", DisplayName = "positive infinity")]
    [DataRow(double.NegativeInfinity, "-inf", DisplayName = "negative infinity")]
    [DataRow(double.NaN, "nan", DisplayName = "not a number")]
    public void Write_WhenFloatSentinel_ShouldEmitKeyword(double value, string expected)
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteFloat(value);
            writer.WriteEndTable();
        });

        Assert.AreEqual($"v = {expected}\n", actual);
    }

    /// <summary>
    /// Verifies that a negative-zero float retains its sign in the emitted text.
    /// </summary>
    [TestMethod]
    public void Write_WhenFloatIsNegativeZero_ShouldEmitSignedZero()
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteFloat(-0.0);
            writer.WriteEndTable();
        });

        Assert.AreEqual("v = -0.0\n", actual);
    }

    /// <summary>
    /// Verifies that an empty string value is emitted as an empty basic string.
    /// </summary>
    [TestMethod]
    public void Write_WhenStringIsEmpty_ShouldEmitEmptyQuotes()
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
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
    public void Write_WhenStringContainsReservedCharacter_ShouldEscape(string value, string expectedInner)
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
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
    public void Write_WhenStringContainsBareControlCharacters_ShouldEmitUnicodeEscapes()
    {
        var value = "a" + (char)0x00 + (char)0x01 + (char)0x7F + "b";

        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteString(value);
            writer.WriteEndTable();
        });

        Assert.AreEqual("v = \"a\\u0000\\u0001\\u007Fb\"\n", actual);
    }

    /// <summary>
    /// Verifies that the four date-time kinds, including fractional seconds, are emitted in their RFC 3339 canonical
    /// form.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Write_WhenDateTimeKindsWithFractions_ShouldEmitRfc3339()
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();

            writer.WritePropertyName("odt");
            writer.WriteOffsetDateTime(new DateTimeOffset(2020, 1, 1, 12, 30, 45, 500, TimeSpan.FromHours(2)));

            writer.WritePropertyName("odtz");
            writer.WriteOffsetDateTime(new DateTimeOffset(2020, 1, 1, 12, 30, 45, TimeSpan.Zero));

            writer.WritePropertyName("ldt");
            writer.WriteLocalDateTime(new DateTime(2020, 1, 1, 12, 30, 45).AddTicks(1234567));

            writer.WritePropertyName("ld");
            writer.WriteLocalDate(new DateOnly(2020, 1, 1));

            writer.WritePropertyName("lt");
            writer.WriteLocalTime(new TimeOnly(0, 0, 0).Add(TimeSpan.FromTicks(1000000)));

            writer.WriteEndTable();
        });

        var expected =
            "odt = 2020-01-01T12:30:45.5+02:00\n" +
            "odtz = 2020-01-01T12:30:45Z\n" +
            "ldt = 2020-01-01T12:30:45.1234567\n" +
            "ld = 2020-01-01\n" +
            "lt = 00:00:00.1\n";

        Assert.AreEqual(expected, actual);
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
}
