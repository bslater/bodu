// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WriteFloat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.WriteFloat" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
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
    public void WriteFloat_WhenValue_ShouldEmitShortestRoundTrippableSpelling(double value, string expected)
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
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
    public void WriteFloat_WhenSentinel_ShouldEmitKeyword(double value, string expected)
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
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
    public void WriteFloat_WhenNegativeZero_ShouldEmitSignedZero()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteFloat(-0.0);
            writer.WriteEndTable();
        });

        Assert.AreEqual("v = -0.0\n", actual);
    }

}
