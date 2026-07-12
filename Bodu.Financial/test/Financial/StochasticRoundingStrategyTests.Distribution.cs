// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StochasticRoundingStrategyTests.Distribution.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class StochasticRoundingStrategyTests
{
    /// <summary>
    /// Verifies that rounding a fixed midpoint many times yields a mean close to the raw value, confirming the
    /// convention is statistically unbiased.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Round_WhenSampledManyTimes_ShouldBeUnbiased()
    {
        // A seeded generator keeps the statistical assertion deterministic across runs.
        Random random = new(20240101);
        StochasticRoundingStrategy strategy = new(random.NextDouble);

        const int iterations = 200_000;
        decimal sum = 0m;
        for (int i = 0; i < iterations; i++)
            sum += strategy.Round(1.5m, 0);

        decimal mean = sum / iterations;

        Assert.IsTrue(Math.Abs(mean - 1.5m) < 0.01m, $"expected a mean near 1.5 but got {mean}");
    }

    /// <summary>
    /// Verifies that rounding a non-midpoint value up occurs at roughly the frequency of its fractional part, so the
    /// per-outcome probability tracks the discarded fraction rather than a fixed midpoint rule.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Round_WhenFractionSmall_ShouldRoundUpProportionally()
    {
        Random random = new(76543);
        StochasticRoundingStrategy strategy = new(random.NextDouble);

        const int iterations = 200_000;
        int roundedUp = 0;
        for (int i = 0; i < iterations; i++)
        {
            // 1.10 at scale 0 discards a fraction of 0.10, so ~10% of draws should round up to 2.
            if (strategy.Round(1.10m, 0) == 2m)
                roundedUp++;
        }

        double upFraction = (double)roundedUp / iterations;

        Assert.IsTrue(Math.Abs(upFraction - 0.10) < 0.01, $"expected ~0.10 round-up frequency but got {upFraction}");
    }
}
