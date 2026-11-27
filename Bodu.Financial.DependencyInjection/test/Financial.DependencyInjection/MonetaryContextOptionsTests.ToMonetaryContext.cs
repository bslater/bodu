// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextOptionsTests.ToMonetaryContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.DependencyInjection;

public sealed partial class MonetaryContextOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="MonetaryContextOptions.ToMonetaryContext" /> builds a context equal to
    /// <see cref="MonetaryContext.Default" /> when no setting is changed.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void ToMonetaryContext_WhenSettingsAreDefault_ShouldEqualMonetaryContextDefault() =>
        Assert.AreEqual(MonetaryContext.Default, new MonetaryContextOptions().ToMonetaryContext());

    /// <summary>
    /// Verifies that <see cref="MonetaryContextOptions.ToMonetaryContext" /> gives each midpoint rounding mode a
    /// <see cref="MidpointRoundingStrategy" /> of the <see cref="MidpointRounding" /> of the same name.
    /// </summary>
    /// <param name="mode">The rounding mode under test.</param>
    [TestMethod]
    [DataRow(MonetaryRoundingMode.ToEven)]
    [DataRow(MonetaryRoundingMode.AwayFromZero)]
    [DataRow(MonetaryRoundingMode.ToZero)]
    [DataRow(MonetaryRoundingMode.ToNegativeInfinity)]
    [DataRow(MonetaryRoundingMode.ToPositiveInfinity)]
    public void ToMonetaryContext_WhenRoundingIsAMidpointMode_ShouldAssignTheMatchingStrategy(MonetaryRoundingMode mode)
    {
        MonetaryContext context = new MonetaryContextOptions { Rounding = mode }.ToMonetaryContext();

        var strategy = (MidpointRoundingStrategy)context.Rounding;
        Assert.AreEqual(Enum.Parse<MidpointRounding>(mode.ToString()), strategy.Mode);
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContextOptions.ToMonetaryContext" /> gives
    /// <see cref="MonetaryRoundingMode.Stochastic" /> the shared <see cref="StochasticRoundingStrategy" />.
    /// </summary>
    [TestMethod]
    public void ToMonetaryContext_WhenRoundingIsStochastic_ShouldAssignTheSharedStochasticStrategy()
    {
        MonetaryContext context = new MonetaryContextOptions { Rounding = MonetaryRoundingMode.Stochastic }.ToMonetaryContext();

        Assert.AreSame(StochasticRoundingStrategy.Shared, context.Rounding);
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContextOptions.ToMonetaryContext" /> builds equal contexts from two options
    /// instances with the same settings, because the rounding strategies are shared.
    /// </summary>
    [TestMethod]
    public void ToMonetaryContext_WhenSettingsAreEqual_ShouldBuildEqualContexts()
    {
        MonetaryContext first = new MonetaryContextOptions { Rounding = MonetaryRoundingMode.ToZero }.ToMonetaryContext();
        MonetaryContext second = new MonetaryContextOptions { Rounding = MonetaryRoundingMode.ToZero }.ToMonetaryContext();

        Assert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContextOptions.ToMonetaryContext" /> carries every policy setting into the
    /// context.
    /// </summary>
    [TestMethod]
    public void ToMonetaryContext_WhenSettingsAreChanged_ShouldCarryThem()
    {
        var options = new MonetaryContextOptions
        {
            ScalePolicy = ScalePolicy.Custom,
            CustomScale = 4,
            CashRounding = CashRoundingPolicy.CurrencyCashIncrement,
            ConversionRounding = ConversionRoundingPolicy.Defer,
        };

        MonetaryContext context = options.ToMonetaryContext();

        Assert.AreEqual(ScalePolicy.Custom, context.ScalePolicy);
        Assert.AreEqual(4, context.CustomScale);
        Assert.AreEqual(CashRoundingPolicy.CurrencyCashIncrement, context.CashRounding);
        Assert.AreEqual(AllocationPolicy.LargestRemainder, context.Allocation);
        Assert.AreEqual(ConversionRoundingPolicy.Defer, context.ConversionRounding);
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContextOptions.ToMonetaryContext" /> rejects an undefined rounding mode with
    /// <see cref="ArgumentOutOfRangeException" /> naming <see cref="MonetaryContextOptions.Rounding" />.
    /// </summary>
    [TestMethod]
    public void ToMonetaryContext_WhenRoundingUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new MonetaryContextOptions { Rounding = (MonetaryRoundingMode)42 };

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = options.ToMonetaryContext();
        });

        Assert.AreEqual(nameof(MonetaryContextOptions.Rounding), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContextOptions.ToMonetaryContext" /> rejects an undefined scale policy with
    /// <see cref="ArgumentOutOfRangeException" />, as <see cref="MonetaryContext.Validate" /> does.
    /// </summary>
    [TestMethod]
    public void ToMonetaryContext_WhenScalePolicyUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new MonetaryContextOptions { ScalePolicy = (ScalePolicy)7 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = options.ToMonetaryContext();
        });
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContextOptions.ToMonetaryContext" /> rejects a custom scale policy without a
    /// scale with <see cref="ArgumentException" /> naming <see cref="MonetaryContextOptions.CustomScale" />.
    /// </summary>
    [TestMethod]
    public void ToMonetaryContext_WhenCustomScaleMissing_ShouldThrowArgumentException()
    {
        var options = new MonetaryContextOptions { ScalePolicy = ScalePolicy.Custom };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = options.ToMonetaryContext();
        });

        Assert.AreEqual(nameof(MonetaryContextOptions.CustomScale), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContextOptions.ToMonetaryContext" /> rejects a custom scale outside the range 0
    /// to 28 with <see cref="ArgumentOutOfRangeException" /> naming <see cref="MonetaryContextOptions.CustomScale" />.
    /// </summary>
    [TestMethod]
    public void ToMonetaryContext_WhenCustomScaleOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new MonetaryContextOptions { ScalePolicy = ScalePolicy.Custom, CustomScale = 29 };

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = options.ToMonetaryContext();
        });

        Assert.AreEqual(nameof(MonetaryContextOptions.CustomScale), ex.ParamName);
    }
}
