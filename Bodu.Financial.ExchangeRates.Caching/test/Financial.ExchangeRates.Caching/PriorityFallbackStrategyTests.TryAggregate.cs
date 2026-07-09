// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityFallbackStrategyTests.TryAggregate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class PriorityFallbackStrategyTests
{
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

        bool ok = PriorityFallbackStrategy.Instance.TryAggregate("USD", "AUD", D1, RateLookupOptions.Exact, candidates, out RateLookupResult result);

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

        bool ok = PriorityFallbackStrategy.Instance.TryAggregate("USD", "AUD", D1, RateLookupOptions.Exact, candidates, out RateLookupResult result);

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

        Assert.IsFalse(PriorityFallbackStrategy.Instance.TryAggregate("USD", "AUD", D1, RateLookupOptions.Exact, candidates, out _));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> candidate list is rejected.
    /// </summary>
    [TestMethod]
    public void TryAggregate_WhenCandidatesIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = PriorityFallbackStrategy.Instance.TryAggregate("USD", "AUD", D1, RateLookupOptions.Exact, null!, out _);
        });

        Assert.AreEqual("candidates", ex.ParamName);
    }
}
