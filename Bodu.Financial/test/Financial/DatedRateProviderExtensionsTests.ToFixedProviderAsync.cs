// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedRateProviderExtensionsTests.ToFixedProviderAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Extensions;

namespace Bodu.Financial;

public partial class DatedRateProviderExtensionsTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> provider throws <see cref="ArgumentNullException" /> with the parameter
    /// name <c>provider</c>.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await ((IDatedRateProvider)null!).ToFixedProviderAsync([AudUsd], RangeStart, RangeEnd);
        });

        Assert.AreEqual("provider", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> pairs sequence throws <see cref="ArgumentNullException" /> with the
    /// parameter name <c>pairs</c>.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenPairsIsNull_ShouldThrowArgumentNullException()
    {
        FixedDatedRateProvider source = new([]);

        ArgumentNullException ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await source.ToFixedProviderAsync(null!, RangeStart, RangeEnd);
        });

        Assert.AreEqual("pairs", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty pairs sequence throws <see cref="ArgumentException" /> with the parameter name
    /// <c>pairs</c> before any fetch is issued.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenPairsIsEmpty_ShouldThrowArgumentException()
    {
        FixedDatedRateProvider source = new([]);

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await source.ToFixedProviderAsync(Array.Empty<CurrencyPair>(), RangeStart, RangeEnd);
        });

        Assert.AreEqual("pairs", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a default (invalid) pair in the sequence throws <see cref="ArgumentException" /> with the
    /// parameter name <c>pairs</c>.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenPairIsInvalid_ShouldThrowArgumentException()
    {
        FixedDatedRateProvider source = new([]);

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await source.ToFixedProviderAsync([default], RangeStart, RangeEnd);
        });

        Assert.AreEqual("pairs", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an inverted window throws <see cref="ArgumentException" /> with the parameter name
    /// <c>endDate</c>, matching the range surfaces' contract.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenRangeInverted_ShouldThrowArgumentException()
    {
        FixedDatedRateProvider source = new([]);

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await source.ToFixedProviderAsync([AudUsd], RangeEnd, RangeStart);
        });

        Assert.AreEqual("endDate", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the requested window is materialized: rates inside the window resolve from the result and rates
    /// outside it are absent.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_ShouldMaterializeRequestedWindow()
    {
        FixedDatedRateProvider source = new(new[]
        {
            Rate("AUD", "USD", Known, 0.6828m),
            Rate("AUD", "USD", new DateOnly(2023, 2, 15), 0.70m),
        });

        FixedDatedRateProvider snapshot = await source.ToFixedProviderAsync([AudUsd], RangeStart, RangeEnd);

        Assert.IsTrue(snapshot.TryGetRate("AUD", "USD", Known, RateLookupOptions.Exact, out RateLookupResult result));
        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.IsFalse(snapshot.TryGetRate("AUD", "USD", new DateOnly(2023, 2, 15), RateLookupOptions.Exact, out _), "a rate outside the requested window is not materialized");
    }

    /// <summary>
    /// Verifies that a pair the source has no data for contributes no rows without failing the call, and that a
    /// materialization where every pair is missing yields an empty provider whose lookups miss.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenPairHasNoData_ShouldOmitItAndSucceed()
    {
        FixedDatedRateProvider source = new(new[] { Rate("AUD", "USD", Known, 0.6828m) });

        FixedDatedRateProvider partial = await source.ToFixedProviderAsync([AudUsd, AudEur], RangeStart, RangeEnd);
        Assert.IsTrue(partial.TryGetRate("AUD", "USD", Known, RateLookupOptions.Exact, out _));
        Assert.IsFalse(partial.TryGetRate("AUD", "EUR", Known, RateLookupOptions.Exact, out _));

        FixedDatedRateProvider empty = await source.ToFixedProviderAsync([AudEur], RangeStart, RangeEnd);
        Assert.AreEqual(0, empty.Book.Count);
        Assert.IsFalse(empty.TryGetRate("AUD", "EUR", Known, RateLookupOptions.Exact, out _));
    }

    /// <summary>
    /// Verifies that duplicate input pairs are fetched once.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenPairsContainDuplicates_ShouldFetchEachPairOnce()
    {
        CountingFixedSource source = new(new[] { Rate("AUD", "USD", Known, 0.6828m) });

        _ = await source.ToFixedProviderAsync([AudUsd, AudUsd, AudUsd], RangeStart, RangeEnd);

        Assert.AreEqual(1, source.RangeCallCount);
    }

    /// <summary>
    /// Verifies that the series-grain fetch provenance survives the web-to-fixed round trip.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_ShouldPreserveFetchedAtUtc()
    {
        DateTimeOffset fetched = new(2023, 1, 10, 0, 0, 0, TimeSpan.Zero);
        RateSeries sourceSeries = new(AudUsd, "Test", [(Known, 0.6828m)], fetched);
        FixedDatedRateProvider source = new(new RateBook([sourceSeries]));

        FixedDatedRateProvider snapshot = await source.ToFixedProviderAsync([AudUsd], RangeStart, RangeEnd);

        Assert.IsTrue(snapshot.Book.TryGetSeries(AudUsd, "Test", out RateSeries? series));
        Assert.AreEqual(fetched, series!.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that a source holding only the inverse pair still materializes a snapshot that resolves the requested
    /// direction, with the observation stored under its natively quoted pair.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenSourceHoldsInversePairOnly_ShouldResolveRequestedDirection()
    {
        FixedDatedRateProvider source = new(new[] { Rate("AUD", "USD", Known, 0.68m) });
        CurrencyPair usdAud = new(CurrencyCode.USD, CurrencyCode.AUD);

        FixedDatedRateProvider snapshot = await source.ToFixedProviderAsync([usdAud], RangeStart, RangeEnd);

        Assert.IsTrue(snapshot.Book.TryGetSeries(AudUsd, "Test", out RateSeries? series), "the observation is stored under its natively quoted pair");
        Assert.AreEqual(0.68m, series!.GetObservations().Single().Rate);
        Assert.IsTrue(snapshot.TryGetRate("USD", "AUD", Known, null, out RateLookupResult result));
        Assert.IsTrue(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that the cancellation token is forwarded to the source's range fetch, so a source that observes it
    /// propagates <see cref="OperationCanceledException" /> through the materialization.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenCancelled_ShouldPropagateCancellation()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();
        CountingFixedSource source = new(new[] { Rate("AUD", "USD", Known, 0.6828m) });

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            _ = await source.ToFixedProviderAsync([AudUsd], RangeStart, RangeEnd, cts.Token);
        });

        Assert.AreEqual(0, source.RangeCallCount, "the fetch observed the token before touching the inner source");
    }

    /// <summary>
    /// A fixed-book source that counts range fetches, so tests can assert deduplication of requested pairs.
    /// </summary>
    private sealed class CountingFixedSource
        : IDatedRateProvider
    {
        /// <summary>The fixed provider backing the counted lookups.</summary>
        private readonly FixedDatedRateProvider _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="CountingFixedSource" /> class.
        /// </summary>
        /// <param name="rates">The observations the source resolves from.</param>
        public CountingFixedSource(IEnumerable<ExchangeRate> rates) =>
            _inner = new FixedDatedRateProvider(rates);

        /// <summary>
        /// Gets the number of range fetches issued.
        /// </summary>
        /// <value>The invocation count.</value>
        public int RangeCallCount { get; private set; }

        /// <inheritdoc />
        public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null) =>
            _inner.GetRate(fromIsoCode, toIsoCode, options);

        /// <inheritdoc />
        public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null) =>
            _inner.GetRate(fromIsoCode, toIsoCode, date, options);

        /// <inheritdoc />
        public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result) =>
            _inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);

        /// <inheritdoc />
        public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
        {
            RangeCallCount++;
            return _inner.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
        }

        /// <inheritdoc />
        public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
            _inner.GetRateAsync(fromIsoCode, toIsoCode, options, cancellationToken);

        /// <inheritdoc />
        public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
            _inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken);

        /// <inheritdoc />
        public ValueTask<RateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RangeCallCount++;
            return _inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
        }
    }
}
