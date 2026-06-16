// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AverageStrategyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the mean-of-contributors behaviour of <see cref="AverageStrategy" />.
/// </summary>
[TestClass]
public sealed class AverageStrategyTests
{
    /// <summary>
    /// A fixed date used by the tests.
    /// </summary>
    private static readonly DateOnly D1 = new(2024, 1, 3);

    /// <summary>
    /// Verifies that the constructor rejects a blank provider label.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenLabelIsBlank_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AverageStrategy("  ");
        });

        Assert.AreEqual("providerLabel", ex.ParamName);
    }

    /// <summary>
    /// Verifies that two contributors are averaged and tagged with the default label, not inverted.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void TryAggregate_WhenTwoContributors_ShouldReturnMean()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[]
        {
            Named("A", ("AUD", "USD", D1, 0.5000m)),
            Named("B", ("AUD", "USD", D1, 0.5100m)),
        };

        bool ok = new AverageStrategy().TryAggregate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, candidates, out ExchangeRateLookupResult result);

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
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[]
        {
            Named("A", ("AUD", "USD", D1, 0.5m)),
            Named("B", ("AUD", "USD", D1, 0.5m)),
            Named("C", ("AUD", "USD", D1, 0.6m)),
        };

        new AverageStrategy().TryAggregate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, candidates, out ExchangeRateLookupResult result);

        Assert.AreEqual((0.5m + 0.5m + 0.6m) / 3, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a single contributor yields its own rate.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenSingleContributor_ShouldReturnThatRate()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[] { Named("A", ("AUD", "USD", D1, 0.5m)) };

        new AverageStrategy().TryAggregate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, candidates, out ExchangeRateLookupResult result);

        Assert.AreEqual(0.5m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that when no contributor resolves, the strategy returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenNoContributorResolves_ShouldReturnFalse()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[] { Named("A") };

        Assert.IsFalse(new AverageStrategy().TryAggregate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, candidates, out _));
    }

    /// <summary>
    /// Verifies that a custom provider label tags the synthesized rate.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenCustomLabel_ShouldTagResult()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[] { Named("A", ("AUD", "USD", D1, 0.5m)) };

        new AverageStrategy("Mid").TryAggregate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, candidates, out ExchangeRateLookupResult result);

        Assert.AreEqual("Mid", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> candidate list is rejected.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenCandidatesIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new AverageStrategy().TryAggregate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, null!, out _);
        });

        Assert.AreEqual("candidates", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the range overload averages only the dates present in every contributing candidate.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task AggregateRangeAsync_WhenDatesMisaligned_ShouldAverageOnlySharedDates()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[]
        {
            Named("A", ("AUD", "USD", new DateOnly(2024, 1, 1), 0.50m), ("AUD", "USD", new DateOnly(2024, 1, 2), 0.52m)),
            Named("B", ("AUD", "USD", new DateOnly(2024, 1, 2), 0.54m), ("AUD", "USD", new DateOnly(2024, 1, 3), 0.56m)),
        };

        IReadOnlyList<ExchangeRate> rates =
            await new AverageStrategy().AggregateRangeAsync("AUD", "USD", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 3), candidates, default);

        Assert.AreEqual(1, rates.Count);
        Assert.AreEqual(new DateOnly(2024, 1, 2), rates[0].Date);
        Assert.AreEqual(0.53m, rates[0].Rate);
    }

    /// <summary>
    /// Builds a named candidate from observation rows.
    /// </summary>
    /// <param name="name">The candidate name.</param>
    /// <param name="rows">The observation rows.</param>
    /// <returns>The named candidate.</returns>
    private static NamedDatedExchangeRateProvider Named(string name, params (string From, string To, DateOnly Date, decimal Rate)[] rows) =>
        new(name, new FixedDatedExchangeRateProvider(rows.Select(r => new ExchangeRate(r.From, r.To, r.Date, r.Rate, name))));
}
