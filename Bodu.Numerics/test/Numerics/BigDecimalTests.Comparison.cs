// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.Comparison.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
