// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="MonetaryContext" /> — its defaults, scale resolution, rounding application, and validation.
/// </summary>
[TestClass]
public class MonetaryContextTests
{
    /// <summary>
    /// Verifies that <see cref="MonetaryContext.Default" /> exposes the documented default policy values.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Default_WhenInspected_ShouldExposeDocumentedDefaults()
    {
        MonetaryContext context = MonetaryContext.Default;

        Assert.AreEqual(ScalePolicy.CurrencyMinorUnits, context.ScalePolicy);
        Assert.AreEqual(CashRoundingPolicy.None, context.CashRounding);
        Assert.AreEqual(AllocationPolicy.LargestRemainder, context.Allocation);
        Assert.AreEqual(ConversionRoundingPolicy.RoundAtTarget, context.ConversionRounding);
        Assert.IsInstanceOfType<MidpointRoundingStrategy>(context.Rounding);
    }

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

    /// <summary>
    /// Verifies that the rounding strategy honours the configured midpoint mode.
    /// </summary>
    [TestMethod]
    public void Round_WhenAwayFromZeroStrategy_ShouldRoundMidpointAway()
    {
        MonetaryContext context = MonetaryContext.Default with { Rounding = MidpointRoundingStrategy.AwayFromZero };

        Assert.AreEqual(1.23m, context.Round(1.225m, 2));
        Assert.AreEqual(1.22m, MonetaryContext.Default.Round(1.225m, 2));
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContext.Validate" /> rejects an undefined scale policy.
    /// </summary>
    [TestMethod]
    public void Validate_WhenScalePolicyUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = (ScalePolicy)99 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => context.Validate());
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContext.Validate" /> rejects a custom scale outside the supported range.
    /// </summary>
    [TestMethod]
    public void Validate_WhenCustomScaleOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = 99 };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => context.Validate());
        Assert.AreEqual("CustomScale", ex.ParamName);
    }
}
