// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyMathTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Unit tests for the internal <see cref="MoneyMath" /> arithmetic kernel, focusing on the invariants its public
/// callers rely on so a future internal caller that forgets to validate gets a clear contract violation rather than
/// a raw arithmetic fault.
/// </summary>
[TestClass]
public sealed class MoneyMathTests
{
    /// <summary>
    /// Verifies that <see cref="MoneyMath.AllocateByRatios" /> throws <see cref="ArgumentException" /> — a clear
    /// domain error — rather than <see cref="DivideByZeroException" /> when every ratio is zero (a zero total weight).
    /// </summary>
    [TestMethod]
    public void AllocateByRatios_WhenAllRatiosAreZero_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = MoneyMath.AllocateByRatios(100m, 2, new decimal[] { 0m, 0m, 0m });
        });
    }

    /// <summary>
    /// Verifies that <see cref="MoneyMath.AllocateByRatios" /> throws <see cref="ArgumentException" /> when a ratio is
    /// negative, rather than producing a non-reconciling allocation.
    /// </summary>
    [TestMethod]
    public void AllocateByRatios_WhenRatioIsNegative_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = MoneyMath.AllocateByRatios(100m, 2, new decimal[] { 3m, -1m });
        });
    }

    /// <summary>
    /// Verifies that a valid ratio set produces shares that reconcile exactly to the input amount (no money is
    /// created or lost by the largest-remainder distribution).
    /// </summary>
    [TestMethod]
    public void AllocateByRatios_WhenRatiosAreValid_ShouldReconcileToInputAmount()
    {
        decimal[] shares = MoneyMath.AllocateByRatios(100m, 2, new decimal[] { 1m, 1m, 1m });

        Assert.AreEqual(100m, shares.Sum(), "Allocated shares must sum exactly to the input amount.");
        Assert.HasCount(3, shares);
    }
}
