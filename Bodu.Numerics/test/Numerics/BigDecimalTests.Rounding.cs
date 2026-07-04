// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.Rounding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class BigDecimalTests
{
    /// <summary>
    /// Verifies that rounding to a scale applies the requested midpoint rounding mode.
    /// </summary>
    [TestMethod]
    public void Round_WhenGivenScaleAndMode_ShouldRoundAccordingly()
    {
        Assert.AreEqual("2.4", BigDecimal.Round(BD(2449, 3), 1, MidpointRounding.ToEven).ToString());  // 2.449 -> 2.4 (remainder < half)
        Assert.AreEqual("2.4", BigDecimal.Round(BD(244, 2), 1, MidpointRounding.ToEven).ToString());   // 2.44 -> 2.4
        Assert.AreEqual("2.4", BigDecimal.Round(BD(245, 2), 1, MidpointRounding.ToEven).ToString());   // 2.45 -> 2.4 (half to even, 4 is even)
        Assert.AreEqual("2.6", BigDecimal.Round(BD(255, 2), 1, MidpointRounding.ToEven).ToString());   // 2.55 -> 2.6 (half to even, 5 is odd)
    }

    /// <summary>
    /// Verifies that rounding leaves a value with fewer decimal places unchanged.
    /// </summary>
    [TestMethod]
    public void Round_WhenValueHasFewerPlaces_ShouldReturnUnchanged()
    {
        Assert.AreEqual(BD(5, 1), BigDecimal.Round(BD(5, 1), 4, MidpointRounding.ToEven));
    }

    /// <summary>
    /// Verifies that Floor, Ceiling, and Truncate produce the expected integer values.
    /// </summary>
    [TestMethod]
    public void FloorCeilingTruncate_WhenComputed_ShouldMatchExpected()
    {
        Assert.AreEqual("2", BigDecimal.Floor(BD(29, 1)).ToString());      // 2.9 -> 2
        Assert.AreEqual("-3", BigDecimal.Floor(BD(-21, 1)).ToString());    // -2.1 -> -3
        Assert.AreEqual("3", BigDecimal.Ceiling(BD(21, 1)).ToString());    // 2.1 -> 3
        Assert.AreEqual("-2", BigDecimal.Ceiling(BD(-29, 1)).ToString());  // -2.9 -> -2
        Assert.AreEqual("2", BigDecimal.Truncate(BD(29, 1)).ToString());   // 2.9 -> 2
        Assert.AreEqual("-2", BigDecimal.Truncate(BD(-29, 1)).ToString()); // -2.9 -> -2
    }

    /// <summary>
    /// Verifies that a negative rounding scale throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Round_WhenScaleIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = BigDecimal.Round(BD(5, 1), -1, MidpointRounding.ToEven);
        });
    }
}
