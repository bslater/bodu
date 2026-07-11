// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StochasticRoundingStrategyTests.Round.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class StochasticRoundingStrategyTests
{
    /// <summary>
    /// Verifies that a draw below the discarded fraction rounds the value up (toward positive infinity).
    /// </summary>
    [TestMethod]
    public void Round_WhenDrawBelowFraction_ShouldRoundUp()
    {
        StochasticRoundingStrategy strategy = new(() => 0.4);

        Assert.AreEqual(1.23m, strategy.Round(1.225m, 2));
    }

    /// <summary>
    /// Verifies that a draw at or above the discarded fraction rounds the value down (toward negative infinity).
    /// </summary>
    [TestMethod]
    public void Round_WhenDrawAtOrAboveFraction_ShouldRoundDown()
    {
        StochasticRoundingStrategy strategy = new(() => 0.6);

        Assert.AreEqual(1.22m, strategy.Round(1.225m, 2));
    }

    /// <summary>
    /// Verifies that a value already exact at the target scale is returned unchanged, without consulting the sampler.
    /// </summary>
    [TestMethod]
    public void Round_WhenValueExactAtScale_ShouldReturnUnchanged()
    {
        bool sampled = false;
        StochasticRoundingStrategy strategy = new(() =>
        {
            sampled = true;
            return 0.0;
        });

        decimal result = strategy.Round(1.23m, 2);

        Assert.AreEqual(1.23m, result);
        Assert.IsFalse(sampled, "an exact value should not draw from the sampler");
    }

    /// <summary>
    /// Verifies that a negative value rounds up toward positive infinity when the draw falls below the fraction, so the
    /// convention is unbiased on the whole number line.
    /// </summary>
    [TestMethod]
    public void Round_WhenNegativeValueAndDrawBelowFraction_ShouldRoundTowardZero()
    {
        StochasticRoundingStrategy strategy = new(() => 0.4);

        Assert.AreEqual(-1.22m, strategy.Round(-1.225m, 2));
    }

    /// <summary>
    /// Verifies that a negative value rounds down toward negative infinity when the draw is at or above the fraction.
    /// </summary>
    [TestMethod]
    public void Round_WhenNegativeValueAndDrawAboveFraction_ShouldRoundAwayFromZero()
    {
        StochasticRoundingStrategy strategy = new(() => 0.6);

        Assert.AreEqual(-1.23m, strategy.Round(-1.225m, 2));
    }

    /// <summary>
    /// Verifies that a scale of zero rounds to an integer.
    /// </summary>
    [TestMethod]
    public void Round_WhenScaleZeroAndDrawBelowFraction_ShouldRoundToInteger()
    {
        StochasticRoundingStrategy strategy = new(() => 0.4);

        Assert.AreEqual(2m, strategy.Round(1.5m, 0));
    }

    /// <summary>
    /// Verifies that a negative scale throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Round_WhenScaleNegative_ShouldThrowArgumentOutOfRangeException()
    {
        StochasticRoundingStrategy strategy = new(() => 0.0);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = strategy.Round(1.5m, -1);
        });
    }

    /// <summary>
    /// Verifies that a scale beyond the <see cref="decimal" /> range throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Round_WhenScaleExceedsMaximum_ShouldThrowArgumentOutOfRangeException()
    {
        StochasticRoundingStrategy strategy = new(() => 0.0);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = strategy.Round(1.5m, 29);
        });
    }
}
