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

    /// <summary>
    /// Verifies that <see cref="MonetaryContext.ResolveScale(int)" /> rejects a custom scale outside 0 to 28 with
    /// <see cref="ArgumentOutOfRangeException" /> naming <see cref="MonetaryContext.CustomScale" />, as
    /// <see cref="MonetaryContext.Validate" /> does.
    /// </summary>
    /// <param name="customScale">The out-of-range custom scale.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(29)]
    public void ResolveScale_WhenCustomScaleOutOfRange_ShouldThrowArgumentOutOfRangeException(int customScale)
    {
        MonetaryContext context =
            MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = customScale };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = context.ResolveScale(2);
        });

        Assert.AreEqual(nameof(MonetaryContext.CustomScale), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContext.ResolveScale(int)" /> rejects a currency minor-unit count outside 0 to
    /// 28 with <see cref="ArgumentOutOfRangeException" /> naming the argument, whatever the scale policy, rather than
    /// returning it as the resolved scale.
    /// </summary>
    /// <param name="policy">The scale policy in effect.</param>
    /// <param name="currencyMinorUnits">The out-of-range minor-unit count.</param>
    [TestMethod]
    [DataRow(ScalePolicy.CurrencyMinorUnits, -1)]
    [DataRow(ScalePolicy.CurrencyMinorUnits, int.MinValue)]
    [DataRow(ScalePolicy.CurrencyMinorUnits, 29)]
    [DataRow(ScalePolicy.Unrounded, -1)]
    [DataRow(ScalePolicy.Unrounded, int.MinValue)]
    [DataRow(ScalePolicy.Unrounded, 29)]
    [DataRow(ScalePolicy.Custom, -1)]
    [DataRow(ScalePolicy.Custom, int.MinValue)]
    [DataRow(ScalePolicy.Custom, 29)]
    public void ResolveScale_WhenCurrencyMinorUnitsOutOfRange_ShouldThrowArgumentOutOfRangeException(
        ScalePolicy policy,
        int currencyMinorUnits)
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = policy, CustomScale = 2 };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = context.ResolveScale(currencyMinorUnits);
        });

        Assert.AreEqual(nameof(currencyMinorUnits), ex.ParamName);
    }
}
