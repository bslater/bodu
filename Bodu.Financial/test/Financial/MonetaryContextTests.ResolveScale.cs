// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextTests.ResolveScale.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MonetaryContextTests
{

    /// <summary>
    /// Verifies that <see cref="MonetaryContext.ResolveScale(int)" /> returns the currency's minor units under the
    /// default scale policy.
    /// </summary>
    [TestMethod]
    public void ResolveScale_WhenCurrencyMinorUnits_ShouldReturnCurrencyScale()
    {
        Assert.AreEqual(2, MonetaryContext.Default.ResolveScale(2));
        Assert.AreEqual(0, MonetaryContext.Default.ResolveScale(0));
    }

    /// <summary>
    /// Verifies that <see cref="ScalePolicy.Unrounded" /> resolves to the deferred-rounding sentinel and leaves the
    /// amount unchanged when rounding is applied.
    /// </summary>
    [TestMethod]
    public void ResolveScale_WhenUnrounded_ShouldReturnNegativeOneAndNotRound()
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = ScalePolicy.Unrounded };

        Assert.AreEqual(-1, context.ResolveScale(2));
        Assert.AreEqual(1.23456m, context.Round(1.23456m, 2));
    }

    /// <summary>
    /// Verifies that <see cref="ScalePolicy.Custom" /> rounds to the supplied custom scale.
    /// </summary>
    [TestMethod]
    public void ResolveScale_WhenCustom_ShouldUseCustomScale()
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = 4 };

        Assert.AreEqual(4, context.ResolveScale(2));
        Assert.AreEqual(1.2346m, context.Round(1.234567m, 2));
    }

    /// <summary>
    /// Verifies that resolving a custom scale without a value throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ResolveScale_WhenCustomWithoutValue_ShouldThrowArgumentException()
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom };

        Assert.ThrowsExactly<ArgumentException>(() => _ = context.ResolveScale(2));
    }
}
