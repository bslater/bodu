// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedExchangeRateProviderExtensionsTests.ToFixedProviderAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.Extensions;

namespace Bodu.Financial;

public partial class DatedExchangeRateProviderExtensionsTests
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
            _ = await ((IDatedExchangeRateProvider)null!).ToFixedProviderAsync([AudUsd], RangeStart, RangeEnd);
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
        FixedDatedExchangeRateProvider source = new([]);

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
        FixedDatedExchangeRateProvider source = new([]);

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await source.ToFixedProviderAsync(Array.Empty<ExchangeRatePair>(), RangeStart, RangeEnd);
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
        FixedDatedExchangeRateProvider source = new([]);

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
        FixedDatedExchangeRateProvider source = new([]);

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
        FixedDatedExchangeRateProvider source = new(new[]
        {
            Rate("AUD", "USD", Known, 0.6828m),
            Rate("AUD", "USD", new DateOnly(2023, 2, 15), 0.70m),
        });

        FixedDatedExchangeRateProvider snapshot = await source.ToFixedProviderAsync([AudUsd], RangeStart, RangeEnd);

        Assert.IsTrue(snapshot.TryGetRate("AUD", "USD", Known, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result));
        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.IsFalse(snapshot.TryGetRate("AUD", "USD", new DateOnly(2023, 2, 15), ExchangeRateLookupOptions.Exact, out _), "a rate outside the requested window is not materialized");
    }

    /// <summary>
    /// Verifies that materializing from a web provider triggers its fetches and the result is isolated from data the
    /// source accumulates afterwards.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenSourceIsWebProvider_ShouldFetchAndStayIsolated()
    {
        using TestBulkWebExchangeRateProvider source = new(Rate("AUD", "USD", Known, 0.6828m, "TESTBULK"));

        FixedDatedExchangeRateProvider snapshot = await source.ToFixedProviderAsync([AudUsd], RangeStart, RangeEnd);

        Assert.AreEqual(1, source.LoadCount, "materialization triggered the catalogue fetch");
        Assert.IsTrue(snapshot.TryGetRate("AUD", "USD", Known, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result));
        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreNotSame(source.GetLoadedSnapshot(), snapshot, "the materialized snapshot is a decoupled instance");
    }

    /// <summary>
    /// Verifies that a pair the source has no data for contributes no rows without failing the call, and that a
    /// materialization where every pair is missing yields an empty provider whose lookups miss.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenPairHasNoData_ShouldOmitItAndSucceed()
    {
        FixedDatedExchangeRateProvider source = new(new[] { Rate("AUD", "USD", Known, 0.6828m) });

        FixedDatedExchangeRateProvider partial = await source.ToFixedProviderAsync([AudUsd, AudEur], RangeStart, RangeEnd);
        Assert.IsTrue(partial.TryGetRate("AUD", "USD", Known, ExchangeRateLookupOptions.Exact, out _));
        Assert.IsFalse(partial.TryGetRate("AUD", "EUR", Known, ExchangeRateLookupOptions.Exact, out _));

        FixedDatedExchangeRateProvider empty = await source.ToFixedProviderAsync([AudEur], RangeStart, RangeEnd);
        Assert.AreEqual(0, empty.Book.Count);
        Assert.IsFalse(empty.TryGetRate("AUD", "EUR", Known, ExchangeRateLookupOptions.Exact, out _));
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
        ExchangeRateSeries sourceSeries = new(AudUsd, "Test", [(Known, 0.6828m)], fetched);
        FixedDatedExchangeRateProvider source = new(new ExchangeRateBook([sourceSeries]));

        FixedDatedExchangeRateProvider snapshot = await source.ToFixedProviderAsync([AudUsd], RangeStart, RangeEnd);

        Assert.IsTrue(snapshot.Book.TryGetSeries(AudUsd, "Test", out ExchangeRateSeries? series));
        Assert.AreEqual(fetched, series!.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that a source holding only the inverse pair still materializes a snapshot that resolves the requested
    /// direction, with the observation stored under its natively quoted pair.
    /// </summary>
    [TestMethod]
    public async Task ToFixedProviderAsync_WhenSourceHoldsInversePairOnly_ShouldResolveRequestedDirection()
    {
        FixedDatedExchangeRateProvider source = new(new[] { Rate("AUD", "USD", Known, 0.68m) });
        ExchangeRatePair usdAud = new(CurrencyCode.USD, CurrencyCode.AUD);

        FixedDatedExchangeRateProvider snapshot = await source.ToFixedProviderAsync([usdAud], RangeStart, RangeEnd);

        Assert.IsTrue(snapshot.Book.TryGetSeries(AudUsd, "Test", out ExchangeRateSeries? series), "the observation is stored under its natively quoted pair");
        Assert.AreEqual(0.68m, series!.GetObservations().Single().Rate);
        Assert.IsTrue(snapshot.TryGetRate("USD", "AUD", Known, null, out ExchangeRateLookupResult result));
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
        : IDatedExchangeRateProvider
    {
        /// <summary>The fixed provider backing the counted lookups.</summary>
        private readonly FixedDatedExchangeRateProvider _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="CountingFixedSource" /> class.
        /// </summary>
        /// <param name="rates">The observations the source resolves from.</param>
        public CountingFixedSource(IEnumerable<ExchangeRate> rates) =>
            _inner = new FixedDatedExchangeRateProvider(rates);

        /// <summary>
        /// Gets the number of range fetches issued.
        /// </summary>
        /// <value>The invocation count.</value>
        public int RangeCallCount { get; private set; }

        /// <inheritdoc />
        public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, ExchangeRateLookupOptions? options = null) =>
            _inner.GetRate(fromIsoCode, toIsoCode, options);

        /// <inheritdoc />
        public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null) =>
            _inner.GetRate(fromIsoCode, toIsoCode, date, options);

        /// <inheritdoc />
        public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result) =>
            _inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);

        /// <inheritdoc />
        public ExchangeRateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
        {
            RangeCallCount++;
            return _inner.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
        }

        /// <inheritdoc />
        public ValueTask<ExchangeRateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, ExchangeRateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
            _inner.GetRateAsync(fromIsoCode, toIsoCode, options, cancellationToken);

        /// <inheritdoc />
        public ValueTask<ExchangeRateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
            _inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken);

        /// <inheritdoc />
        public ValueTask<ExchangeRateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RangeCallCount++;
            return _inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
        }
    }
}
