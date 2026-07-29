// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public partial class BigDecimalTests
{
    /// <summary>
    /// Verifies that valid decimal, sign, dot-edge, and scientific forms parse to the expected canonical value.
    /// </summary>
    [TestMethod]
    [DataRow("5.", 5L, 0)]
    [DataRow(".5", 5L, 1)]
    [DataRow("+3", 3L, 0)]
    [DataRow("  12.5  ", 125L, 1)]
    [DataRow("1E3", 1000L, 0)]
    [DataRow("1.23E2", 123L, 0)]
    [DataRow("0.000", 0L, 0)]
    public void Parse_WhenValid_ShouldProduceExpectedValue(string text, long expectedUnscaled, int expectedScale)
    {
        BigDecimal value = BigDecimal.Parse(text, CultureInfo.InvariantCulture);

        Assert.AreEqual(BD(expectedUnscaled, expectedScale), value);
    }

    /// <summary>
    /// Verifies that <see cref="BigDecimal.TryParse(string, IFormatProvider, out BigDecimal)" /> returns false for
    /// malformed input.
    /// </summary>
    [TestMethod]
    [DataRow("abc")]
    [DataRow("")]
    [DataRow(".")]
    [DataRow("1.2.3")]
    [DataRow("1e")]
    [DataRow("1,000")]
    public void TryParse_WhenInvalid_ShouldReturnFalse(string text)
    {
        Assert.IsFalse(BigDecimal.TryParse(text, CultureInfo.InvariantCulture, out _));
    }

    /// <summary>
    /// Verifies that Parse rejects a null argument.
    /// </summary>
    [TestMethod]
    public void Parse_WhenArgumentIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BigDecimal.Parse((string)null!, CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that a very long digit string is parsed from a pooled buffer without overflowing the call stack,
    /// which the previous input-length-sized <c>stackalloc</c> would have done.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TryParse_WhenInputHasManyDigits_ShouldParseWithoutStackOverflow()
    {
        string manyDigits = new string('9', 2_000_000);

        Assert.IsTrue(BigDecimal.TryParse(manyDigits, CultureInfo.InvariantCulture, out BigDecimal value));
        Assert.AreNotEqual(BigDecimal.Zero, value);
    }

    /// <summary>
    /// Verifies that an extreme positive exponent, which would fold into a huge negative scale and drive an unbounded
    /// <see cref="System.Numerics.BigInteger.Pow(System.Numerics.BigInteger, int)" />, is rejected by
    /// <see cref="BigDecimal.TryParse(string, IFormatProvider, out BigDecimal)" /> rather than exhausting memory.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TryParse_WhenExponentDrivesUnboundedWidening_ShouldReturnFalse()
    {
        Assert.IsFalse(BigDecimal.TryParse("1e2000000000", CultureInfo.InvariantCulture, out _));
    }

    /// <summary>
    /// Verifies that the single-argument <see cref="BigDecimal.TryParse(string, out BigDecimal)" /> overload delegates
    /// to the provider-based parser, succeeding on valid text and failing (including <see langword="null" />) without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenGivenStringOnly_ShouldDelegateToProviderOverload()
    {
        Assert.IsTrue(BigDecimal.TryParse("3.14", out BigDecimal value));
        Assert.AreEqual(new BigDecimal(new System.Numerics.BigInteger(314), 2), value);

        Assert.IsFalse(BigDecimal.TryParse("not a number", out _));
        Assert.IsFalse(BigDecimal.TryParse(null, out _));
    }

    /// <summary>
    /// Verifies that the exponent honors the supplied culture's negative-sign symbol, consistent with the mantissa
    /// sign: a culture whose negative sign is U+2212 must be able to round-trip its own scientific notation.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenCultureUsesNonAsciiNegativeSign_ShouldParseExponentSign()
    {
        var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        format.NegativeSign = "−";

        Assert.IsTrue(BigDecimal.TryParse("1e−5", format, out BigDecimal value));
        Assert.AreEqual(new BigDecimal(System.Numerics.BigInteger.One, 5), value);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="BigDecimal" /> with a negative scale of excessive magnitude throws
    /// <see cref="ArgumentOutOfRangeException" /> rather than driving an unbounded widening.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Ctor_WhenNegativeScaleMagnitudeExcessive_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new BigDecimal(System.Numerics.BigInteger.One, int.MinValue);
        });

        Assert.AreEqual("scale", ex.ParamName);
    }
}
