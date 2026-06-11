// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.Integers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that decimal integers — bare, explicitly signed, and underscore-grouped — decode to their value.
    /// </summary>
    /// <param name="literal">The integer literal under test.</param>
    /// <param name="expected">The expected decoded value.</param>
    [TestMethod]
    [DataRow("0", 0L, DisplayName = "zero")]
    [DataRow("7", 7L, DisplayName = "single digit")]
    [DataRow("+99", 99L, DisplayName = "explicit plus")]
    [DataRow("-17", -17L, DisplayName = "negative")]
    [DataRow("-0", 0L, DisplayName = "negative zero")]
    [DataRow("+0", 0L, DisplayName = "positive zero")]
    [DataRow("1_000", 1000L, DisplayName = "underscore grouping")]
    [DataRow("1_2_3", 123L, DisplayName = "underscore every digit")]
    [DataRow("9223372036854775807", long.MaxValue, DisplayName = "Int64 max")]
    [DataRow("-9223372036854775808", long.MinValue, DisplayName = "Int64 min")]
    public void Read_WhenDecimalInteger_ShouldDecodeToValue(string literal, long expected)
    {
        TomlDocumentReader reader = Create($"v = {literal}\n");

        ExpectSingleValue(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(expected, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that prefixed hexadecimal, octal, and binary integers decode to their value, including mixed-case hex
    /// and underscore grouping.
    /// </summary>
    /// <param name="literal">The radix integer literal under test.</param>
    /// <param name="expected">The expected decoded value.</param>
    [TestMethod]
    [DataRow("0xFF", 255L, DisplayName = "hex upper")]
    [DataRow("0xff", 255L, DisplayName = "hex lower")]
    [DataRow("0xdead_BEEF", 3735928559L, DisplayName = "hex mixed case with underscore")]
    [DataRow("0x0", 0L, DisplayName = "hex zero")]
    [DataRow("0o17", 15L, DisplayName = "octal")]
    [DataRow("0o755", 493L, DisplayName = "octal mode")]
    [DataRow("0b1010", 10L, DisplayName = "binary")]
    [DataRow("0b1_0", 2L, DisplayName = "binary with underscore")]
    public void Read_WhenRadixInteger_ShouldDecodeToValue(string literal, long expected)
    {
        TomlDocumentReader reader = Create($"v = {literal}\n");

        ExpectSingleValue(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(expected, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that the maximum-magnitude hexadecimal literal decodes to <see cref="long.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenHexLiteralAtInt64Max_ShouldDecodeToMaxValue()
    {
        TomlDocumentReader reader = Create("v = 0x7FFFFFFFFFFFFFFF\n");

        ExpectSingleValue(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(long.MaxValue, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that an integer with a leading zero is rejected with <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenIntegerHasLeadingZero_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = 01\n");
        });
    }

    /// <summary>
    /// Verifies that a decimal integer exceeding the signed 64-bit range is rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenDecimalIntegerOverflowsInt64_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = 9223372036854775808\n");
        });
    }

    /// <summary>
    /// Verifies that a negative decimal integer below the signed 64-bit range is rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenNegativeIntegerUnderflowsInt64_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = -9223372036854775809\n");
        });
    }

    /// <summary>
    /// Verifies that a hexadecimal literal exceeding the signed 64-bit range is rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenHexLiteralOverflowsInt64_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = 0xFFFFFFFFFFFFFFFF\n");
        });
    }

    /// <summary>
    /// Verifies that malformed underscore placement and empty radix bodies are rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="literal">The malformed integer literal under test.</param>
    [TestMethod]
    [DataRow("_1", DisplayName = "leading underscore")]
    [DataRow("1_", DisplayName = "trailing underscore")]
    [DataRow("1__2", DisplayName = "double underscore")]
    [DataRow("0x_1", DisplayName = "underscore after radix prefix")]
    [DataRow("0x", DisplayName = "empty hex body")]
    [DataRow("0o", DisplayName = "empty octal body")]
    [DataRow("0b", DisplayName = "empty binary body")]
    [DataRow("0o9", DisplayName = "digit outside octal range")]
    [DataRow("0b2", DisplayName = "digit outside binary range")]
    [TestCategory("Regression")]
    public void Read_WhenIntegerUnderscoreOrRadixInvalid_ShouldThrowTomlFormatException(string literal)
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create($"v = {literal}\n");
        });
    }
}
