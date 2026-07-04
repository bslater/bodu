// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Test;

namespace Bodu.Numerics;

/// <summary>
/// Contains the unit tests for <see cref="BigDecimal" />.
/// </summary>
[TestClass]
public partial class BigDecimalTests
{
    /// <summary>
    /// Builds a <see cref="BigDecimal" /> from an unscaled value and scale.
    /// </summary>
    /// <param name="unscaled">The unscaled value.</param>
    /// <param name="scale">The scale.</param>
    /// <returns>The constructed value.</returns>
    private static BigDecimal BD(long unscaled, int scale) =>
        new(new BigInteger(unscaled), scale);

    /// <summary>
    /// Verifies that a representative decimal computation produces the exact expected value (smoke test).
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BigDecimal_WhenComputingPriceWithTax_ShouldProduceExactTotal()
    {
        BigDecimal price = BD(1999, 2);   // 19.99
        BigDecimal tax = price * BD(1, 1); // × 0.1 = 1.999
        BigDecimal total = price + tax;    // 21.989

        Assert.AreEqual("1.999", tax.ToString());
        Assert.AreEqual("21.989", total.ToString());
    }
}
