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
}
