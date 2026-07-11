// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StochasticRoundingStrategyTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class StochasticRoundingStrategyTests
{
    /// <summary>
    /// Verifies that constructing with a <see langword="null" /> sampler throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSamplerIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new StochasticRoundingStrategy(null!);
        });
    }

    /// <summary>
    /// Verifies that the shared instance rounds a non-exact value to one of the two adjacent representable steps.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Smoke)]
    public void Shared_WhenRoundingMidpoint_ShouldReturnAdjacentStep()
    {
        decimal result = StochasticRoundingStrategy.Shared.Round(1.225m, 2);

        Assert.IsTrue(result == 1.22m || result == 1.23m, $"expected 1.22 or 1.23 but got {result}");
    }
}
