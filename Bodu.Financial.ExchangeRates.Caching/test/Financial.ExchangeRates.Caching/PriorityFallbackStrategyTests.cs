// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityFallbackStrategyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the first-available behaviour of <see cref="PriorityFallbackStrategy" />.
/// </summary>
[TestClass]
public sealed class PriorityFallbackStrategyTests
{
    /// <summary>
    /// A fixed date used by the tests.
    /// </summary>
    private static readonly DateOnly D1 = new(2024, 1, 3);

    /// <summary>
    /// Verifies that the first candidate that resolves wins.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenFirstCandidateHasRate_ShouldReturnFirst()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[]
        {
            Named("First", ("USD", "AUD", D1, 1.5m)),
            Named("Second", ("USD", "AUD", D1, 1.6m)),
        };

        var ok = PriorityFallbackStrategy.Instance.TryAggregate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact, candidates, out ExchangeRateLookupResult result);

        Assert.IsTrue(ok);
        Assert.AreEqual("First", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a candidate that cannot resolve falls through to the next.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenFirstCandidateMisses_ShouldFallThrough()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[]
        {
            Named("First"),
            Named("Second", ("USD", "AUD", D1, 1.6m)),
        };

        var ok = PriorityFallbackStrategy.Instance.TryAggregate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact, candidates, out ExchangeRateLookupResult result);

        Assert.IsTrue(ok);
        Assert.AreEqual("Second", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that no resolving candidate yields <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenNoCandidateResolves_ShouldReturnFalse()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[] { Named("First") };

        Assert.IsFalse(PriorityFallbackStrategy.Instance.TryAggregate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact, candidates, out _));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> candidate list is rejected.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenCandidatesIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = PriorityFallbackStrategy.Instance.TryAggregate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact, null!, out _);
        });

        Assert.AreEqual("candidates", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the range overload returns the first candidate's non-empty result.
    /// </summary>
    [TestMethod]
    public async Task AggregateRangeAsync_WhenFirstCandidateEmpty_ShouldFallThrough()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[]
        {
            Named("First"),
            Named("Second", ("USD", "AUD", D1, 1.6m)),
        };

        IReadOnlyList<ExchangeRate> rates = await PriorityFallbackStrategy.Instance.AggregateRangeAsync("USD", "AUD", D1, D1, candidates, default);

        Assert.AreEqual(1, rates.Count);
        Assert.AreEqual("Second", rates[0].Provider);
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
