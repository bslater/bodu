// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public partial class BigDecimalTests
{
    /// <summary>
    /// Verifies that the plain (invariant) string representation matches the canonical form.
    /// </summary>
    [TestMethod]
    [DataRow(0L, 0, "0")]
    [DataRow(100L, 0, "100")]
    [DataRow(5L, 1, "0.5")]
    [DataRow(-314L, 2, "-3.14")]
    [DataRow(1999L, 3, "1.999")]
    [DataRow(1L, 4, "0.0001")]
    public void ToString_WhenPlain_ShouldMatchCanonicalForm(long unscaled, int scale, string expected) =>
        Assert.AreEqual(expected, BD(unscaled, scale).ToString());

    /// <summary>
    /// Verifies that the fixed-point <c>F</c> specifier rounds and pads to the requested number of decimal places.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFixedFormat_ShouldRoundAndPad()
    {
        Assert.AreEqual("1.500", BD(15, 1).ToString("F3", CultureInfo.InvariantCulture));
        Assert.AreEqual("2.46", BD(2455, 3).ToString("F2", CultureInfo.InvariantCulture));   // 2.455 -> 2.46 (half to even, 5 odd)
        Assert.AreEqual("3", BD(314, 2).ToString("F0", CultureInfo.InvariantCulture));       // 3.14 -> 3
    }

    /// <summary>
    /// Verifies that a comma-decimal culture uses its own decimal separator.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCultureUsesCommaSeparator_ShouldUseComma()
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");

        Assert.AreEqual("3,14", BD(314, 2).ToString(null, culture));
    }

    /// <summary>
    /// Verifies that an unsupported format specifier throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsUnsupported_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = BD(5, 1).ToString("Z", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that the format-only <see cref="BigDecimal.ToString(string)" /> overload formats with the invariant
    /// culture, consistent with the parameterless <see cref="BigDecimal.ToString()" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenGivenFormatOnly_ShouldUseInvariantCulture()
    {
        Assert.AreEqual("3.14", BD(314, 2).ToString("G"));
        Assert.AreEqual("3.140", BD(314, 2).ToString("F3"));
    }

    /// <summary>
    /// Verifies that a fixed-point precision beyond the supported magnitude throws <see cref="FormatException" />
    /// instead of driving an unbounded <see cref="System.Numerics.BigInteger.Pow(System.Numerics.BigInteger, int)" />
    /// widening — the same denial-of-service shape the parse path already rejects for extreme exponents.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFixedPrecisionExceedsSupportedRange_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = BD(1, 0).ToString("F2000000", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that a value round-trips through its own text representation.
    /// </summary>
    [TestMethod]
    [DataRow(0L, 0)]
    [DataRow(-314L, 2)]
    [DataRow(1999L, 3)]
    [DataRow(1L, 10)]
    public void ToStringAndParse_WhenRoundTripped_ShouldPreserveValue(long unscaled, int scale)
    {
        BigDecimal original = BD(unscaled, scale);

        Assert.AreEqual(original, BigDecimal.Parse(original.ToString(null, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
    }
}
