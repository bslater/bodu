// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingExchangeRateProviderTests.HistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public partial class AggregatingExchangeRateProviderTests
{
    /// <summary>
    /// The fixed instant the composed-availability tests evaluate rolling windows against.
    /// </summary>
    private static readonly DateTimeOffset AvailabilityNow = new(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a group whose only child advertises a bounded availability advertises that same availability.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenSingleBoundedChild_ShouldReturnChildValue()
    {
        ExchangeRateHistoryAvailability declared = ExchangeRateHistoryAvailability.Since(new DateOnly(2020, 1, 1));
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(NamedAware("A", declared));

        Assert.AreEqual(declared, agg.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that the group advertises the most generous availability across bounded children — the one whose
    /// earliest available date reaches furthest back — because a date any single child can serve is a date the group
    /// can serve.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenAllChildrenBounded_ShouldReturnMostGenerous()
    {
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            NamedAware("Shallow", ExchangeRateHistoryAvailability.RollingDays(30)),
            NamedAware("Deep", ExchangeRateHistoryAvailability.Since(new DateOnly(2015, 1, 1))),
            NamedAware("Mid", ExchangeRateHistoryAvailability.Since(new DateOnly(2022, 1, 1))));

        Assert.AreEqual(ExchangeRateHistoryAvailability.Since(new DateOnly(2015, 1, 1)), agg.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that a single <see cref="ExchangeRateHistoryAvailability.Unbounded" /> child makes the whole group
    /// unbounded, regardless of how shallow the other children are.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenAnyChildUnbounded_ShouldReturnUnbounded()
    {
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            NamedAware("Shallow", ExchangeRateHistoryAvailability.RollingDays(30)),
            NamedAware("Deep", ExchangeRateHistoryAvailability.Unbounded));

        Assert.AreEqual(ExchangeRateHistoryAvailability.Unbounded, agg.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that a child that does not implement <see cref="IHistoryAwareExchangeRateProvider" /> makes the whole
    /// group unbounded: a non-aware child declares no floor, so the group cannot declare one either.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenAnyChildNotHistoryAware_ShouldReturnUnbounded()
    {
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            NamedAware("Shallow", ExchangeRateHistoryAvailability.RollingDays(30)),
            new NamedDatedExchangeRateProvider("Legacy", new CountingDatedExchangeRateProvider([])));

        Assert.AreEqual(ExchangeRateHistoryAvailability.Unbounded, agg.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that rolling and since declarations are compared through their earliest available date as of the
    /// injected clock, so a deep rolling window beats a shallow fixed inception.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenRollingReachesFurtherThanSince_ShouldReturnRolling()
    {
        // As of 2024-06-01 a 3650-day rolling window reaches back to 2014, further than the 2023 inception.
        ExchangeRateHistoryAvailability rolling = ExchangeRateHistoryAvailability.RollingDays(3650);
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            NamedAware("Rolling", rolling),
            NamedAware("Since", ExchangeRateHistoryAvailability.Since(new DateOnly(2023, 1, 1))));

        Assert.AreEqual(rolling, agg.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that the priority fallback never consults a child whose advertised history excludes the requested date:
    /// the lookup resolves from the next candidate and the excluded child records zero calls, even though it holds a
    /// rate for that date.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenHigherPriorityChildOutsideAdvertisedHistory_ShouldResolveFromNextWithoutCallingIt()
    {
        DateOnly requested = new(2024, 4, 15);
        HistoryAwareCountingProvider shallow = AwareWith("Shallow", ExchangeRateHistoryAvailability.Since(new DateOnly(2024, 5, 1)), ("USD", "AUD", requested, 1.50m));
        HistoryAwareCountingProvider deep = AwareWith("Deep", ExchangeRateHistoryAvailability.Unbounded, ("USD", "AUD", requested, 1.60m));
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            new NamedDatedExchangeRateProvider("Shallow", shallow),
            new NamedDatedExchangeRateProvider("Deep", deep));

        bool found = agg.TryGetRate("USD", "AUD", requested, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.60m, result.Rate.Rate, "the rate comes from the first candidate that advertises the date");
        Assert.AreEqual(0, shallow.SingleDateCallCount, "the declared-unavailable child is never consulted");
        Assert.AreEqual(1, deep.SingleDateCallCount);
    }

    /// <summary>
    /// Verifies that a lookup every candidate has declared unavailable reports the same miss it reports when every
    /// candidate fails, without any child being consulted.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenEveryChildOutsideAdvertisedHistory_ShouldThrowKeyNotFoundExceptionWithoutInnerCalls()
    {
        DateOnly requested = new(2024, 4, 15);
        HistoryAwareCountingProvider shallow = AwareWith("Shallow", ExchangeRateHistoryAvailability.Since(new DateOnly(2024, 5, 1)), ("USD", "AUD", requested, 1.50m));
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(new NamedDatedExchangeRateProvider("Shallow", shallow));

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = agg.GetRate("USD", "AUD", requested, ExchangeRateLookupOptions.Exact);
        });

        Assert.AreEqual(0, shallow.SingleDateCallCount);
    }

    /// <summary>
    /// Verifies that a forward-resolving lookup whose tolerance reaches from an unavailable requested date into a
    /// child's advertised history keeps that child, so the filter never hides a rate the child could resolve.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenForwardToleranceReachesChildHistory_ShouldKeepChild()
    {
        DateOnly floor = new(2024, 5, 1);
        HistoryAwareCountingProvider shallow = AwareWith("Shallow", ExchangeRateHistoryAvailability.Since(floor), ("USD", "AUD", new DateOnly(2024, 5, 2), 1.55m));
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(new NamedDatedExchangeRateProvider("Shallow", shallow));

        bool found = agg.TryGetRate("USD", "AUD", new DateOnly(2024, 4, 29), ExchangeRateLookupOptions.NextWithin(5), out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(new DateOnly(2024, 5, 2), result.Rate.Date);
        Assert.AreEqual(1, shallow.SingleDateCallCount);
    }

    /// <summary>
    /// Verifies that a range request drops a child whose advertised history begins after the window ends, resolving the
    /// range from the remaining candidates without calling the dropped child.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenChildHistoryBeginsAfterWindowEnd_ShouldDropChild()
    {
        DateOnly inWindow = new(2024, 3, 15);
        HistoryAwareCountingProvider shallow = AwareWith("Shallow", ExchangeRateHistoryAvailability.Since(new DateOnly(2024, 5, 1)), ("USD", "AUD", inWindow, 1.50m));
        HistoryAwareCountingProvider deep = AwareWith("Deep", ExchangeRateHistoryAvailability.Unbounded, ("USD", "AUD", inWindow, 1.60m));
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            new NamedDatedExchangeRateProvider("Shallow", shallow),
            new NamedDatedExchangeRateProvider("Deep", deep));

        ExchangeRateRangeResult result = agg.GetRates("USD", "AUD", new DateOnly(2024, 3, 1), new DateOnly(2024, 4, 1));

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1.60m, result[0].Rate);
        Assert.AreEqual(0, shallow.RangeCallCount, "a child that can serve no part of the window is dropped");
        Assert.AreEqual(1, deep.RangeCallCount);
    }

    /// <summary>
    /// Verifies that a child whose advertised history clips only the start of the requested window is kept, since
    /// strategies already tolerate partial data.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenChildHistoryOverlapsWindow_ShouldKeepChild()
    {
        HistoryAwareCountingProvider shallow = AwareWith("Shallow", ExchangeRateHistoryAvailability.Since(new DateOnly(2024, 3, 20)), ("USD", "AUD", new DateOnly(2024, 3, 25), 1.50m));
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(new NamedDatedExchangeRateProvider("Shallow", shallow));

        ExchangeRateRangeResult result = agg.GetRates("USD", "AUD", new DateOnly(2024, 3, 1), new DateOnly(2024, 4, 1));

        Assert.AreEqual(1, shallow.RangeCallCount, "a partially overlapping child stays in the candidate set");
        Assert.AreEqual(1, result.Count);
    }

    /// <summary>
    /// Verifies that disabling <see cref="ExchangeRateAggregationOptions.RespectHistoryAvailability" /> offers a
    /// declared-unavailable child to the strategy unchanged, restoring the unfiltered behaviour.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenRespectHistoryAvailabilityDisabled_ShouldConsultDeclaredUnavailableChild()
    {
        DateOnly requested = new(2024, 4, 15);
        HistoryAwareCountingProvider shallow = AwareWith("Shallow", ExchangeRateHistoryAvailability.Since(new DateOnly(2024, 5, 1)), ("USD", "AUD", requested, 1.50m));
        AggregatingExchangeRateProvider agg = new(
            new[] { new NamedDatedExchangeRateProvider("Shallow", shallow) },
            new ExchangeRateAggregationOptions { RespectHistoryAvailability = false },
            new MutableTimeProvider(AvailabilityNow));

        bool found = agg.TryGetRate("USD", "AUD", requested, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.50m, result.Rate.Rate);
        Assert.AreEqual(1, shallow.SingleDateCallCount);
    }

    /// <summary>
    /// Builds an aggregator over the supplied children with the clock pinned to <see cref="AvailabilityNow" />, so
    /// rolling-window comparisons are deterministic.
    /// </summary>
    /// <param name="children">The named children to group.</param>
    /// <returns>The aggregator under test.</returns>
    private static AggregatingExchangeRateProvider CreateAvailabilityGroup(params NamedDatedExchangeRateProvider[] children) =>
        new(children, options: null, timeProvider: new MutableTimeProvider(AvailabilityNow));

    /// <summary>
    /// Builds a history-aware counting child from the supplied observation rows, tagged with the child name.
    /// </summary>
    /// <param name="name">The provider name stamped on the child's rates.</param>
    /// <param name="availability">The history depth the child advertises.</param>
    /// <param name="rows">The observation rows.</param>
    /// <returns>The counting child.</returns>
    private static HistoryAwareCountingProvider AwareWith(string name, ExchangeRateHistoryAvailability availability, params (string From, string To, DateOnly Date, decimal Rate)[] rows) =>
        new(availability, rows.Select(r => new ExchangeRate(CurrencyInfo.ParseCurrencyCode(r.From), CurrencyInfo.ParseCurrencyCode(r.To), r.Date, r.Rate, name)));

    /// <summary>
    /// Builds a named history-aware child with an empty book that advertises the supplied availability.
    /// </summary>
    /// <param name="name">The child name.</param>
    /// <param name="availability">The history depth the child advertises.</param>
    /// <returns>The named child.</returns>
    private static NamedDatedExchangeRateProvider NamedAware(string name, ExchangeRateHistoryAvailability availability) =>
        new(name, new HistoryAwareCountingProvider(availability, []));
}
