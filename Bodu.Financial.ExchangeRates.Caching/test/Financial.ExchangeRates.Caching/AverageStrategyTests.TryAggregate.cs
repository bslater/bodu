// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AverageStrategyTests.TryAggregate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class AverageStrategyTests
{
    /// <summary>
    /// Verifies that two contributors are averaged and tagged with the default label, not inverted.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void TryAggregate_WhenTwoContributors_ShouldReturnMean()
    {
        IReadOnlyList<NamedDatedRateProvider> candidates = new[]
        {
            Named("A", ("AUD", "USD", D1, 0.5000m)),
            Named("B", ("AUD", "USD", D1, 0.5100m)),
        };

        bool ok = new AverageStrategy().TryAggregate("AUD", "USD", D1, RateLookupOptions.Exact, candidates, out RateLookupResult result);

        Assert.IsTrue(ok);
        Assert.AreEqual(0.5050m, result.Rate.Rate);
        Assert.AreEqual(AverageStrategy.DefaultProviderLabel, result.Rate.Provider);
        Assert.IsFalse(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that three contributors produce the exact decimal mean without pre-rounding.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TryAggregate_WhenThreeContributors_ShouldReturnExactDecimalMean()
    {
        IReadOnlyList<NamedDatedRateProvider> candidates = new[]
        {
            Named("A", ("AUD", "USD", D1, 0.5m)),
            Named("B", ("AUD", "USD", D1, 0.5m)),
            Named("C", ("AUD", "USD", D1, 0.6m)),
        };

        new AverageStrategy().TryAggregate("AUD", "USD", D1, RateLookupOptions.Exact, candidates, out RateLookupResult result);

        Assert.AreEqual((0.5m + 0.5m + 0.6m) / 3, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a single contributor yields its own rate.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenSingleContributor_ShouldReturnThatRate()
    {
        IReadOnlyList<NamedDatedRateProvider> candidates = new[] { Named("A", ("AUD", "USD", D1, 0.5m)) };

        new AverageStrategy().TryAggregate("AUD", "USD", D1, RateLookupOptions.Exact, candidates, out RateLookupResult result);

        Assert.AreEqual(0.5m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that when no contributor resolves, the strategy returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenNoContributorResolves_ShouldReturnFalse()
    {
        IReadOnlyList<NamedDatedRateProvider> candidates = new[] { Named("A") };

        Assert.IsFalse(new AverageStrategy().TryAggregate("AUD", "USD", D1, RateLookupOptions.Exact, candidates, out _));
    }

    /// <summary>
    /// Verifies that a custom provider label tags the synthesized rate.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenCustomLabel_ShouldTagResult()
    {
        IReadOnlyList<NamedDatedRateProvider> candidates = new[] { Named("A", ("AUD", "USD", D1, 0.5m)) };

        new AverageStrategy("Mid").TryAggregate("AUD", "USD", D1, RateLookupOptions.Exact, candidates, out RateLookupResult result);

        Assert.AreEqual("Mid", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> candidate list is rejected.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenCandidatesIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new AverageStrategy().TryAggregate("AUD", "USD", D1, RateLookupOptions.Exact, null!, out _);
        });

        Assert.AreEqual("candidates", ex.ParamName);
    }
}
