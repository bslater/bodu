// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.HistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public partial class CachingRateProviderTests
{
    /// <summary>
    /// Verifies that the decorator forwards a history-aware inner provider's advertised availability unchanged: the
    /// cache can only hold what the inner once served, so the decorator is exactly as deep as its source.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenInnerIsHistoryAware_ShouldForwardInnerValue()
    {
        RateHistoryAvailability declared = RateHistoryAvailability.RollingDays(180);
        HistoryAwareCountingProvider inner = new(declared, []);
        CachingRateProvider decorator = CreateDecorator(inner);

        Assert.AreEqual(declared, decorator.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that the decorator reflects a change to the inner provider's advertised availability rather than
    /// snapshotting it at construction.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenInnerDeclarationChanges_ShouldReflectCurrentInnerValue()
    {
        HistoryAwareCountingProvider inner = new(RateHistoryAvailability.Unbounded, []);
        CachingRateProvider decorator = CreateDecorator(inner);

        inner.HistoryAvailability = RateHistoryAvailability.Since(new DateOnly(2020, 1, 1));

        Assert.AreEqual(RateHistoryAvailability.Since(new DateOnly(2020, 1, 1)), decorator.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that wrapping an inner provider that does not implement
    /// <see cref="IHistoricalRateProvider" /> yields
    /// <see cref="RateHistoryAvailability.Unbounded" />: a non-aware source declares no floor.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenInnerIsNotHistoryAware_ShouldReturnUnbounded()
    {
        CachingRateProvider decorator = CreateDecorator(InnerWith());

        Assert.AreEqual(RateHistoryAvailability.Unbounded, decorator.HistoryAvailability);
    }

    /// <summary>
    /// The advertised earliest date used by the clamp tests: five days before the fixture's <see cref="Now" />.
    /// </summary>
    private static readonly DateOnly Floor = new(2023, 1, 5);

    /// <summary>
    /// A date before <see cref="Floor" />, outside the advertised history.
    /// </summary>
    private static readonly DateOnly BeforeFloor = new(2023, 1, 3);

    /// <summary>
    /// A date on or after <see cref="Floor" />, inside the advertised history.
    /// </summary>
    private static readonly DateOnly AfterFloor = new(2023, 1, 6);

    /// <summary>
    /// Verifies that a single-date miss for a date outside the inner provider's advertised history is skipped: the
    /// lookup reports a miss without the inner provider ever being called.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDateOutsideAdvertisedHistory_ShouldReturnFalseWithoutInnerCall()
    {
        HistoryAwareCountingProvider inner = AwareInnerWith(RateHistoryAvailability.Since(Floor), ("USD", "AUD", BeforeFloor, 1.50m));
        CachingRateProvider decorator = CreateDecorator(inner);

        bool found = decorator.TryGetRate("USD", "AUD", BeforeFloor, RateLookupOptions.Exact, out _);

        Assert.IsFalse(found);
        Assert.AreEqual(0, inner.SingleDateCallCount, "the inner provider is never consulted for a declared-unavailable date");
    }

    /// <summary>
    /// Verifies that the throwing single-date surface raises the same <see cref="KeyNotFoundException" /> an ordinary
    /// miss produces when the requested date is outside the advertised history, without calling the inner provider.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenDateOutsideAdvertisedHistory_ShouldThrowKeyNotFoundExceptionWithoutInnerCall()
    {
        HistoryAwareCountingProvider inner = AwareInnerWith(RateHistoryAvailability.Since(Floor), ("USD", "AUD", BeforeFloor, 1.50m));
        CachingRateProvider decorator = CreateDecorator(inner);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = decorator.GetRate("USD", "AUD", BeforeFloor, RateLookupOptions.Exact);
        });

        Assert.AreEqual(0, inner.SingleDateCallCount);
    }

    /// <summary>
    /// Verifies that the asynchronous single-date surface raises the same <see cref="KeyNotFoundException" /> an
    /// ordinary miss produces when the requested date is outside the advertised history, without calling the inner
    /// provider.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenDateOutsideAdvertisedHistory_ShouldThrowKeyNotFoundExceptionWithoutInnerCall()
    {
        HistoryAwareCountingProvider inner = AwareInnerWith(RateHistoryAvailability.Since(Floor), ("USD", "AUD", BeforeFloor, 1.50m));
        CachingRateProvider decorator = CreateDecorator(inner);

        _ = await Assert.ThrowsExactlyAsync<KeyNotFoundException>(async () =>
        {
            _ = await decorator.GetRateAsync("USD", "AUD", BeforeFloor, RateLookupOptions.Exact);
        });

        Assert.AreEqual(0, inner.SingleDateCallCount);
    }

    /// <summary>
    /// Verifies that a forward-resolving lookup whose tolerance reaches from an unavailable requested date into the
    /// advertised history is still delegated, so the clamp never hides a rate the source could resolve.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenForwardToleranceReachesAdvertisedHistory_ShouldDelegateToInner()
    {
        HistoryAwareCountingProvider inner = AwareInnerWith(RateHistoryAvailability.Since(Floor), ("USD", "AUD", AfterFloor, 1.52m));
        CachingRateProvider decorator = CreateDecorator(inner);

        bool found = decorator.TryGetRate("USD", "AUD", BeforeFloor, RateLookupOptions.NextWithin(5), out RateLookupResult result);

        Assert.IsTrue(found, "NextOnOrAfter can resolve to a date inside the advertised history");
        Assert.AreEqual(AfterFloor, result.Rate.Date);
        Assert.AreEqual(1, inner.SingleDateCallCount);
    }

    /// <summary>
    /// Verifies that disabling <see cref="CachingRateOptions.RespectHistoryAvailability" /> forwards a
    /// declared-unavailable date to the inner provider unchanged, restoring the unclamped behaviour.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenRespectHistoryAvailabilityDisabled_ShouldDelegateToInner()
    {
        _options.RespectHistoryAvailability = false;
        HistoryAwareCountingProvider inner = AwareInnerWith(RateHistoryAvailability.Since(Floor), ("USD", "AUD", BeforeFloor, 1.50m));
        CachingRateProvider decorator = CreateDecorator(inner);

        bool found = decorator.TryGetRate("USD", "AUD", BeforeFloor, RateLookupOptions.Exact, out RateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.50m, result.Rate.Rate);
        Assert.AreEqual(1, inner.SingleDateCallCount);
    }

    /// <summary>
    /// Verifies that a range window lying entirely before the advertised earliest date is never fetched: the result is
    /// empty, the inner provider is not called, and the whole window is recorded as covered so an immediate repeat is
    /// served from the cache.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenWholeWindowOutsideAdvertisedHistory_ShouldReturnEmptyWithoutInnerCall()
    {
        HistoryAwareCountingProvider inner = AwareInnerWith(RateHistoryAvailability.Since(Floor), ("USD", "AUD", BeforeFloor, 1.50m));
        CachingRateProvider decorator = CreateDecorator(inner);

        RateRangeResult first = decorator.GetRates("USD", "AUD", new DateOnly(2023, 1, 1), BeforeFloor);
        RateRangeResult repeat = decorator.GetRates("USD", "AUD", new DateOnly(2023, 1, 1), BeforeFloor);

        Assert.IsTrue(first.IsEmpty);
        Assert.IsTrue(repeat.IsEmpty);
        Assert.AreEqual(0, inner.RangeCallCount, "neither the skipped fetch nor the covered repeat reaches the inner provider");
    }

    /// <summary>
    /// Verifies that the asynchronous range surface skips a window lying entirely before the advertised earliest date,
    /// identically to the synchronous surface.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenWholeWindowOutsideAdvertisedHistory_ShouldReturnEmptyWithoutInnerCall()
    {
        HistoryAwareCountingProvider inner = AwareInnerWith(RateHistoryAvailability.Since(Floor), ("USD", "AUD", BeforeFloor, 1.50m));
        CachingRateProvider decorator = CreateDecorator(inner);

        RateRangeResult result = await decorator.GetRatesAsync("USD", "AUD", new DateOnly(2023, 1, 1), BeforeFloor);

        Assert.IsTrue(result.IsEmpty);
        Assert.AreEqual(0, inner.RangeCallCount);
    }

    /// <summary>
    /// Verifies that a range window straddling the advertised earliest date is fetched from that date rather than the
    /// requested start, while the whole requested window is recorded as covered so an immediate repeat is served from
    /// the cache without another fetch.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenWindowStartsBeforeAdvertisedHistory_ShouldClampFetchStartAndCoverWholeWindow()
    {
        HistoryAwareCountingProvider inner = AwareInnerWith(
            RateHistoryAvailability.Since(Floor),
            ("USD", "AUD", BeforeFloor, 1.50m),
            ("USD", "AUD", AfterFloor, 1.52m));
        CachingRateProvider decorator = CreateDecorator(inner);
        DateOnly requestedStart = new(2023, 1, 2);
        DateOnly requestedEnd = new(2023, 1, 8);

        RateRangeResult first = decorator.GetRates("USD", "AUD", requestedStart, requestedEnd);
        RateRangeResult repeat = decorator.GetRates("USD", "AUD", requestedStart, requestedEnd);

        Assert.AreEqual(Floor, inner.LastRangeStart, "the fetch starts at the advertised earliest date");
        Assert.AreEqual(requestedEnd, inner.LastRangeEnd);
        Assert.AreEqual(1, first.Count, "only the in-history observation is fetched");
        Assert.AreEqual(AfterFloor, first[0].Date);
        Assert.AreEqual(1, inner.RangeCallCount, "the covered repeat is served from the cache");
        Assert.AreEqual(1, repeat.Count);
    }

    /// <summary>
    /// Verifies that the asynchronous range surface clamps a straddling window to the advertised earliest date,
    /// identically to the synchronous surface.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenWindowStartsBeforeAdvertisedHistory_ShouldClampFetchStart()
    {
        HistoryAwareCountingProvider inner = AwareInnerWith(
            RateHistoryAvailability.Since(Floor),
            ("USD", "AUD", BeforeFloor, 1.50m),
            ("USD", "AUD", AfterFloor, 1.52m));
        CachingRateProvider decorator = CreateDecorator(inner);

        RateRangeResult result = await decorator.GetRatesAsync("USD", "AUD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 8));

        Assert.AreEqual(Floor, inner.LastRangeStart);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(AfterFloor, result[0].Date);
    }

    /// <summary>
    /// Verifies that disabling <see cref="CachingRateOptions.RespectHistoryAvailability" /> fetches the full
    /// requested window from the inner provider even when it starts before the advertised earliest date.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenRespectHistoryAvailabilityDisabled_ShouldFetchFullWindow()
    {
        _options.RespectHistoryAvailability = false;
        HistoryAwareCountingProvider inner = AwareInnerWith(
            RateHistoryAvailability.Since(Floor),
            ("USD", "AUD", BeforeFloor, 1.50m),
            ("USD", "AUD", AfterFloor, 1.52m));
        CachingRateProvider decorator = CreateDecorator(inner);
        DateOnly requestedStart = new(2023, 1, 2);

        RateRangeResult result = decorator.GetRates("USD", "AUD", requestedStart, new DateOnly(2023, 1, 8));

        Assert.AreEqual(requestedStart, inner.LastRangeStart, "the requested window is forwarded unchanged");
        Assert.AreEqual(2, result.Count, "the declared-unavailable observation is fetched too");
    }

    /// <summary>
    /// Verifies that a non-aware inner provider is never clamped: the full requested window is fetched even though it
    /// lies in the distant past.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenInnerIsNotHistoryAware_ShouldFetchFullWindow()
    {
        CountingDatedRateProvider inner = InnerWith(("USD", "AUD", BeforeFloor, 1.50m));
        CachingRateProvider decorator = CreateDecorator(inner);

        RateRangeResult result = decorator.GetRates("USD", "AUD", new DateOnly(2023, 1, 1), BeforeFloor);

        Assert.AreEqual(1, inner.GetRatesAsyncCallCount, "a non-aware inner is treated as unbounded and always fetched");
        Assert.AreEqual(1, result.Count);
    }

    /// <summary>
    /// Builds a history-aware counting inner source from the supplied observation rows.
    /// </summary>
    /// <param name="availability">The history depth the inner source advertises.</param>
    /// <param name="rows">The observation rows, each a from/to/date/rate tuple.</param>
    /// <returns>A new history-aware counting inner source.</returns>
    private static HistoryAwareCountingProvider AwareInnerWith(RateHistoryAvailability availability, params (string From, string To, DateOnly Date, decimal Rate)[] rows) =>
        new(availability, rows.Select(static r => new ExchangeRate(CurrencyInfo.ParseCurrencyCode(r.From), CurrencyInfo.ParseCurrencyCode(r.To), r.Date, r.Rate, "Test")));
}
