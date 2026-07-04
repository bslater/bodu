// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.Comparison.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Test;

namespace Bodu.Numerics;

public partial class BigDecimalTests
{
    /// <summary>
    /// Verifies that comparison orders values by their numeric magnitude regardless of scale.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenScalesDiffer_ShouldOrderByValue()
    {
        Assert.IsTrue(BD(1, 1) < BD(2, 1));          // 0.1 < 0.2
        Assert.IsTrue(BD(100, 2) < BD(2, 0));        // 1.00 < 2
        Assert.IsTrue(BD(-5, 1) < BigDecimal.Zero);  // -0.5 < 0
        Assert.AreEqual(0, BD(10, 1).CompareTo(BD(100, 2)));  // 1.0 == 1.00
        Assert.IsTrue(BD(3, 0) >= BD(30, 1));        // 3 >= 3.0
    }

    /// <summary>
    /// Verifies that values with differing signs order by sign alone, including every combination involving zero.
    /// These rows pin the sign short-circuit of <see cref="BigDecimal.CompareTo(BigDecimal)" /> and pass both before
    /// and after the comparison fast paths were introduced.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenSignsDiffer_ShouldOrderBySign()
    {
        Assert.IsTrue(BD(-1, 5).CompareTo(BD(1, 5)) < 0);              // negative < positive
        Assert.IsTrue(BD(1, 5).CompareTo(BD(-1, 5)) > 0);              // positive > negative
        Assert.IsTrue(BigDecimal.Zero.CompareTo(BD(1, 30)) < 0);       // 0 < tiny positive
        Assert.IsTrue(BigDecimal.Zero.CompareTo(BD(-1, 30)) > 0);      // 0 > tiny negative
        Assert.AreEqual(0, BigDecimal.Zero.CompareTo(BigDecimal.Zero));
    }

    /// <summary>
    /// Verifies that values sharing the same canonical scale order by their unscaled values for both signs. These rows
    /// pin the equal-scale fast path of <see cref="BigDecimal.CompareTo(BigDecimal)" />.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenScalesEqual_ShouldOrderByUnscaledValue()
    {
        Assert.IsTrue(BD(123, 2).CompareTo(BD(124, 2)) < 0);           // 1.23 < 1.24
        Assert.IsTrue(BD(124, 2).CompareTo(BD(123, 2)) > 0);
        Assert.AreEqual(0, BD(123, 2).CompareTo(BD(123, 2)));
        Assert.IsTrue(BD(-124, 2).CompareTo(BD(-123, 2)) < 0);         // -1.24 < -1.23
        Assert.IsTrue(BD(-123, 2).CompareTo(BD(-124, 2)) > 0);
    }

    /// <summary>
    /// Verifies that values whose integer magnitudes clearly differ order correctly across dissimilar scales, for both
    /// signs. These rows exercise the magnitude short-circuit of <see cref="BigDecimal.CompareTo(BigDecimal)" />.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenIntegerMagnitudesDiffer_ShouldOrderByValue()
    {
        Assert.IsTrue(BD(5, 1).CompareTo(BD(500, 0)) < 0);             // 0.5 < 500
        Assert.IsTrue(BD(500, 0).CompareTo(BD(5, 1)) > 0);
        Assert.IsTrue(BD(1, 10).CompareTo(BD(2, 0)) < 0);              // 1e-10 < 2
        Assert.IsTrue(BD(-500, 0).CompareTo(BD(-5, 1)) < 0);           // -500 < -0.5
        Assert.IsTrue(BD(-5, 1).CompareTo(BD(-500, 0)) > 0);
        Assert.IsTrue(BD(10_000_000_000L, 0).CompareTo(BD(2, 0)) > 0); // 1e10 > 2
    }

    /// <summary>
    /// Verifies that values of the same sign and the same integer magnitude — where only an exact digit-level
    /// comparison can decide — order correctly across differing scales. These rows exercise the full-rescale fallback
    /// of <see cref="BigDecimal.CompareTo(BigDecimal)" />.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenMagnitudesAreClose_ShouldFallBackToExactComparison()
    {
        Assert.IsTrue(BD(1_0499999, 7).CompareTo(BD(105, 2)) < 0);     // 1.0499999 < 1.05
        Assert.IsTrue(BD(105, 2).CompareTo(BD(1_0499999, 7)) > 0);
        Assert.IsTrue(BD(1_0500001, 7).CompareTo(BD(105, 2)) > 0);     // 1.0500001 > 1.05
        Assert.IsTrue(BD(1234561, 4).CompareTo(BD(123456, 3)) > 0);    // 123.4561 > 123.456
        Assert.IsTrue(BD(-1_0499999, 7).CompareTo(BD(-105, 2)) > 0);   // -1.0499999 > -1.05
        Assert.AreEqual(0, BD(105, 2).CompareTo(BD(105, 2)));
    }

    /// <summary>
    /// Verifies that comparing values separated by a pathological scale gap orders correctly by magnitude. Before the
    /// magnitude short-circuit this comparison rescaled the small-scale operand to the large scale, materializing a
    /// transient <see cref="BigInteger" /> with ~100,000 extra digits purely to compare; the result is identical either
    /// way, so this row asserts correctness while the short-circuit bounds the cost.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void CompareTo_WhenScaleGapIsLarge_ShouldOrderByMagnitude()
    {
        BigDecimal tiny = BD(3, 100_000);          // 3 × 10^-100000
        BigDecimal small = BD(2, 0);               // 2

        Assert.IsTrue(tiny.CompareTo(small) < 0);
        Assert.IsTrue(small.CompareTo(tiny) > 0);
        Assert.IsTrue((-small).CompareTo(-tiny) < 0);
        Assert.IsTrue((-tiny).CompareTo(-small) > 0);
        Assert.AreEqual(0, tiny.CompareTo(BD(3, 100_000)));
    }

    /// <summary>
    /// Verifies that <see cref="BigDecimal.Min" /> and <see cref="BigDecimal.Max" /> select the correct value.
    /// </summary>
    [TestMethod]
    public void MinAndMax_WhenComputed_ShouldSelectExpectedValue()
    {
        Assert.AreEqual(BD(1, 1), BigDecimal.Min(BD(1, 1), BD(2, 1)));
        Assert.AreEqual(BD(2, 1), BigDecimal.Max(BD(1, 1), BD(2, 1)));
    }

    /// <summary>
    /// Verifies that comparing against a non-<see cref="BigDecimal" /> object throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenObjectIsWrongType_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = BigDecimal.One.CompareTo("not a decimal");
        });
    }
}
