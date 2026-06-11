// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.Floats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that fractional, exponential, signed, and underscore-grouped float literals decode to their value.
    /// </summary>
    /// <param name="literal">The float literal under test.</param>
    /// <param name="expected">The expected decoded value.</param>
    [TestMethod]
    [DataRow("1.5", 1.5, DisplayName = "fraction")]
    [DataRow("0.0", 0.0, DisplayName = "zero fraction")]
    [DataRow("+1.0", 1.0, DisplayName = "explicit plus")]
    [DataRow("-2.5", -2.5, DisplayName = "negative fraction")]
    [DataRow("3.1415", 3.1415, DisplayName = "pi")]
    [DataRow("5e22", 5e22, DisplayName = "lowercase exponent")]
    [DataRow("1E6", 1e6, DisplayName = "uppercase exponent")]
    [DataRow("5e+22", 5e22, DisplayName = "exponent explicit plus")]
    [DataRow("6.626e-34", 6.626e-34, DisplayName = "fraction and negative exponent")]
    [DataRow("9_224_617.445", 9224617.445, DisplayName = "underscore grouping")]
    [DataRow("1e1_0", 1e10, DisplayName = "underscore in exponent")]
    public void Read_WhenFloat_ShouldDecodeToValue(string literal, double expected)
    {
        TomlDocumentReader reader = Create($"v = {literal}\n");

        ExpectSingleValue(ref reader, TomlTokenType.Float);
        Assert.AreEqual(expected, reader.GetDouble());
    }

    /// <summary>
    /// Verifies that the positive infinity sentinels, with and without an explicit sign, decode to
    /// <see cref="double.PositiveInfinity" />.
    /// </summary>
    /// <param name="literal">The infinity literal under test.</param>
    [TestMethod]
    [DataRow("inf", DisplayName = "bare")]
    [DataRow("+inf", DisplayName = "explicit plus")]
    public void Read_WhenPositiveInfinity_ShouldDecodeToInfinity(string literal)
    {
        TomlDocumentReader reader = Create($"v = {literal}\n");

        ExpectSingleValue(ref reader, TomlTokenType.Float);
        Assert.AreEqual(double.PositiveInfinity, reader.GetDouble());
    }

    /// <summary>
    /// Verifies that <c>-inf</c> decodes to <see cref="double.NegativeInfinity" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenNegativeInfinity_ShouldDecodeToNegativeInfinity()
    {
        TomlDocumentReader reader = Create("v = -inf\n");

        ExpectSingleValue(ref reader, TomlTokenType.Float);
        Assert.AreEqual(double.NegativeInfinity, reader.GetDouble());
    }

    /// <summary>
    /// Verifies that the not-a-number sentinels decode to a NaN value regardless of sign.
    /// </summary>
    /// <param name="literal">The NaN literal under test.</param>
    [TestMethod]
    [DataRow("nan", DisplayName = "bare")]
    [DataRow("+nan", DisplayName = "explicit plus")]
    [DataRow("-nan", DisplayName = "explicit minus")]
    public void Read_WhenNotANumber_ShouldDecodeToNaN(string literal)
    {
        TomlDocumentReader reader = Create($"v = {literal}\n");

        ExpectSingleValue(ref reader, TomlTokenType.Float);
        Assert.IsTrue(double.IsNaN(reader.GetDouble()));
    }

    /// <summary>
    /// Verifies that malformed float literals are rejected with <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="literal">The malformed float literal under test.</param>
    [TestMethod]
    [DataRow(".5", DisplayName = "missing integer part")]
    [DataRow("5.", DisplayName = "missing fraction digits")]
    [DataRow("1.e3", DisplayName = "missing fraction before exponent")]
    [DataRow("1e", DisplayName = "missing exponent digits")]
    [DataRow("1.0.0", DisplayName = "two decimal points")]
    [DataRow("01.5", DisplayName = "leading zero integer part")]
    [DataRow("1_.0", DisplayName = "underscore before decimal point")]
    [DataRow("1.0_", DisplayName = "trailing underscore")]
    [TestCategory("Regression")]
    public void Read_WhenFloatLiteralMalformed_ShouldThrowTomlFormatException(string literal)
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create($"v = {literal}\n");
        });
    }
}
