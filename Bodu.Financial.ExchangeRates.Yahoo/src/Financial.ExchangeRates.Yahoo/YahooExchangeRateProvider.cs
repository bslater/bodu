// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Serves Yahoo Finance exchange rates as <see cref="ExchangeRate" /> values, implementing the Bodu.Financial provider
/// contracts over the Yahoo Finance <c>v8/finance/chart</c> JSON REST service.
/// </summary>
/// <remarks>
/// <para>
/// The provider implements the dated <see cref="IDatedExchangeRateProvider" /> contract and the simple
/// <see cref="IExchangeRateProvider" /> contract (returning the most recent available rate). Unlike a single-issuer
/// feed, Yahoo serves arbitrary pairs through the <c>{FROM}{TO}=X</c> ticker convention, so any pair of ISO codes can
/// be requested directly. Use <see cref="LoadPairAsync" /> to warm a pair's in-memory store, or
/// <see cref="GetRatesAsync" /> to read a whole date range at once.
/// </para>
/// <para>
/// Because fetches are asynchronous while the lookup contracts are synchronous, a synchronous lookup that misses an
/// un-fetched pair will, when <see cref="YahooExchangeRateOptions.AllowSynchronousNetworkAccess" /> is enabled, block
/// to fetch a window around the requested date and retry. Fetched observations are accumulated into an immutable
/// <see cref="ExchangeRateBook" /> snapshot that backs the synchronous lookups, so inverse pairs, same-currency
/// identity, and date-resolution policies are inherited from <see cref="FixedDatedExchangeRateProvider" />.
/// </para>
/// <para>
/// <strong>Logging.</strong> When an <see cref="ILogger" /> is supplied (directly or through the dependency-injection
/// package) the provider records: the start of a pair/chart download (<see cref="LogLevel.Debug" />), a completed
/// download with its observation count (<see cref="LogLevel.Information" />), each ingested observation (
/// <see cref="LogLevel.Trace" />), a failed download (<see cref="LogLevel.Warning" />, then re-thrown), and a
/// synchronous on-demand network fetch (<see cref="LogLevel.Warning" />). Every level is configurable through the
/// corresponding <c>*LogLevel</c> property on <see cref="YahooExchangeRateOptions" />; omitting the logger selects
/// <see cref="NullLogger.Instance" />, so logging is opt-in and free when unused.
/// </para>
/// </remarks>
public sealed partial class YahooExchangeRateProvider
    : IDatedExchangeRateProvider, IExchangeRateProvider
{
    /// <summary>
    /// The provider identifier stamped on every rate this provider produces.
    /// </summary>
    public const string ProviderName = "Yahoo";

    /// <summary>
    /// The tolerance, in days, used to resolve the most recent rate for the undated
    /// <see cref="IExchangeRateProvider" /> surface; large enough to reach any rate fetched into the store.
    /// </summary>
    private const int LatestRateToleranceDays = 100_000;

    /// <summary>
    /// The source that fetches and parses charts.
    /// </summary>
    private readonly IYahooExchangeRateChartSource _source;

    /// <summary>
    /// The provider options.
    /// </summary>
    private readonly YahooExchangeRateOptions _options;

    /// <summary>
    /// Guards mutation of the accumulator, loaded-range index, series index, and snapshot fields.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// The accumulator into which each fetched chart's observations are upserted.
    /// </summary>
    private readonly ExchangeRateTableBuilder _builder = new();

    /// <summary>
    /// The set of inclusive date ranges fetched so far for each pair. A gap-respecting coverage set is used rather than
    /// a single <c>(min, max)</c> envelope so a request that straddles an unfetched interior gap is correctly treated
    /// as uncovered and re-fetched.
    /// </summary>
    private readonly Dictionary<ExchangeRatePair, DateRangeCoverage> _coverage = new();

    /// <summary>
    /// The discovered currency series, keyed by pair.
    /// </summary>
    private readonly Dictionary<ExchangeRatePair, YahooSeriesInfo> _series = new();

    /// <summary>
    /// The current immutable book backing range queries; replaced under <see cref="_gate" /> after each fetch.
    /// </summary>
    private volatile ExchangeRateBook _book;

    /// <summary>
    /// The current immutable lookup provider; replaced under <see cref="_gate" /> after each fetch.
    /// </summary>
    private volatile FixedDatedExchangeRateProvider _snapshot;

    /// <summary>
    /// The logger that records chart downloads and on-demand network fetches.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The time source used to resolve the current instant for the undated lookup surface.
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Coalesces concurrent fetches of the same pair-and-window so a cache miss triggers at most one in-flight chart
    /// request, with other callers awaiting the shared fetch.
    /// </summary>
    private readonly SingleFlightCoordinator<PairWindow> _loadCoordinator = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateProvider" /> class backed by the Yahoo Finance
    /// chart endpoint, queried with the supplied HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue chart requests.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records chart downloads and on-demand network fetches. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for the undated lookup surface. <see langword="null" />
    /// selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public YahooExchangeRateProvider(HttpClient httpClient, YahooExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateProvider" /> class backed by an explicit chart
    /// source, used for testing.
    /// </summary>
    /// <param name="source">The chart source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records chart downloads and on-demand network fetches. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for the undated lookup surface. <see langword="null" />
    /// selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    internal YahooExchangeRateProvider(IYahooExchangeRateChartSource source, YahooExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _source = source;
        _options = options;
        _logger = logger ?? NullLogger.Instance;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _book = _builder.ToBook();
        _snapshot = new FixedDatedExchangeRateProvider(_book);
    }

    /// <summary>
    /// Resolves the rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" /> on
    /// <paramref name="date" />, throwing when none is available.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">
    /// The lookup rules; <see langword="null" /> is treated as <see cref="ExchangeRateLookupOptions.Exact" />.
    /// </param>
    /// <returns>The resolved <see cref="ExchangeRateLookupResult" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an ISO code is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when an ISO code or the options are invalid.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no rate is available for the request.</exception>
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null) =>
        TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, YahooResourceStrings.IO_KeyNotFound_YahooRate, fromIsoCode, toIsoCode, date));

    /// <summary>
    /// Attempts to resolve the rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" /> on
    /// <paramref name="date" />, fetching the pair on demand when permitted.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">
    /// The lookup rules; <see langword="null" /> is treated as <see cref="ExchangeRateLookupOptions.Exact" />.
    /// </param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the resolved result; otherwise <see langword="default" />.
    /// </param>
    /// <returns><see langword="true" /> when a rate was resolved; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an ISO code is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when an ISO code or the options are invalid.</exception>
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result)
    {
        if (_snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
            return true;

        if (_options.AllowSynchronousNetworkAccess && TryLoadPairForDate(fromIsoCode, toIsoCode, date))
        {
            Log.SynchronousNetworkFetch(_logger, _options.SynchronousNetworkFetchLogLevel, date);
            return _snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Gets the most recent available rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" />.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <returns>The multiplier converting one unit of the source currency to the destination currency.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an ISO code is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when an ISO code is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no rate is available for the pair.</exception>
    public decimal GetRate(string fromIsoCode, string toIsoCode)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRate(fromIsoCode, toIsoCode, today, ExchangeRateLookupOptions.PreviousWithin(LatestRateToleranceDays)).Rate.Rate;
    }

    /// <summary>
    /// Fetches and loads a pair's rates for the inclusive date range, unless that window is already covered.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the fetch.</param>
    /// <returns>A task that completes when the pair's window has been loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an ISO code is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when an ISO code is invalid.</exception>
    /// <exception cref="YahooExchangeRateDateRangeException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    public Task LoadPairAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(fromIsoCode);
        ThrowHelper.ThrowIfNull(toIsoCode);
        ThrowIfRangeInverted(startDate, endDate);

        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);

        lock (_gate)
        {
            if (IsRangeCovered(pair, startDate, endDate))
                return Task.CompletedTask;
        }

        // Coalesce concurrent fetches of the same pair-and-window so only one chart request is in flight. The shared
        // fetch runs under a token decoupled from any caller, so one caller's cancellation cannot fault the others;
        // cancellationToken only abandons this caller's wait.
        return _loadCoordinator.RunAsync(
            new PairWindow(pair, startDate, endDate),
            ct => LoadPairCoreAsync(pair, fromIsoCode, toIsoCode, startDate, endDate, ct),
            cancellationToken);
    }

    /// <summary>
    /// Fetches and stores a pair's chart for the inclusive window, re-checking coverage inside the single-flight
    /// section so a joiner that arrives after a prior fetch completes does no redundant work.
    /// </summary>
    /// <param name="pair">The currency pair to fetch.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the fetch.</param>
    /// <returns>A task that completes when the pair's window has been loaded.</returns>
    private async Task LoadPairCoreAsync(
        ExchangeRatePair pair,
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (IsRangeCovered(pair, startDate, endDate))
                return;
        }

        var symbol = _options.BuildSymbol(fromIsoCode, toIsoCode);
        YahooChartRequest request = new(pair, symbol, startDate, endDate);

        Log.PairLoadStarting(_logger, _options.DownloadStartingLogLevel, symbol);

        YahooExchangeRateChart chart;
        try
        {
            chart = await _source.GetChartAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.PairLoadFailed(_logger, _options.DownloadFailedLogLevel, symbol, ex);
            throw;
        }

        lock (_gate)
        {
            var count = Accumulate(chart);
            ExtendCoveredRange(pair, startDate, endDate);
            RebuildSnapshot();
            Log.PairLoaded(_logger, _options.DownloadCompletedLogLevel, symbol, count);
        }
    }

    /// <summary>
    /// Reads every available rate for a pair within the inclusive date range, fetching it first.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the fetch.</param>
    /// <returns>A task that yields the rates in the range, ordered by date.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an ISO code is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when an ISO code is invalid.</exception>
    /// <exception cref="YahooExchangeRateDateRangeException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    public async ValueTask<IReadOnlyList<ExchangeRate>> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(fromIsoCode);
        ThrowHelper.ThrowIfNull(toIsoCode);
        ThrowIfRangeInverted(startDate, endDate);

        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);

        await LoadPairAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken).ConfigureAwait(false);

        return CollectRates(pair, startDate, endDate);
    }

    /// <summary>
    /// Gets the currency pairs fetched so far.
    /// </summary>
    /// <returns>A snapshot of the discovered series, one per currency pair.</returns>
    public IReadOnlyCollection<YahooSeriesInfo> GetAvailablePairs()
    {
        lock (_gate)
        {
            return _series.Values.ToArray();
        }
    }

    /// <summary>
    /// Builds the default chart source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue chart requests.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>A new chart source.</returns>
    private static YahooChartExchangeRateSource CreateSource(HttpClient httpClient, YahooExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return new YahooChartExchangeRateSource(httpClient, options);
    }

    /// <summary>
    /// Throws when an inclusive date range is inverted.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <exception cref="YahooExchangeRateDateRangeException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    private static void ThrowIfRangeInverted(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new YahooExchangeRateDateRangeException(
                string.Format(CultureInfo.CurrentCulture, YahooResourceStrings.Arg_OutOfRange_YahooDateRange, startDate, endDate));
        }
    }

    /// <summary>
    /// Upserts a fetched chart's observations and series metadata into the accumulator.
    /// </summary>
    /// <param name="chart">The parsed chart.</param>
    /// <returns>The number of rate observations upserted.</returns>
    private int Accumulate(YahooExchangeRateChart chart)
    {
        YahooSeriesInfo info = chart.GetSeriesInfo();
        _series[info.Pair] = info;

        var count = 0;
        foreach (ExchangeRate rate in chart.EnumerateRates())
        {
            _builder.Upsert(new ExchangeRatePair(rate.FromIsoCode, rate.ToIsoCode), ProviderName, rate.Date, rate.Rate);
            Log.ObservationIngested(_logger, _options.ObservationIngestedLogLevel, rate.FromIsoCode, rate.ToIsoCode, rate.Date, rate.Rate);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Rebuilds the immutable book and lookup snapshot from the accumulator.
    /// </summary>
    private void RebuildSnapshot()
    {
        _book = _builder.ToBook();
        _snapshot = new FixedDatedExchangeRateProvider(_book);
    }

    /// <summary>
    /// Reports whether every day in the inclusive range has already been fetched for a pair.
    /// </summary>
    /// <param name="pair">The pair to check.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns><see langword="true" /> when the range is fully covered; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// Coverage is tracked as a gap-respecting set of intervals, so a window that straddles an unfetched interior gap
    /// reports as not covered even when earlier and later sub-ranges have been fetched.
    /// </remarks>
    private bool IsRangeCovered(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate) =>
        _coverage.TryGetValue(pair, out DateRangeCoverage? coverage) && coverage.Contains(startDate, endDate);

    /// <summary>
    /// Records the inclusive range as fetched for a pair, merging it into the pair's coverage set.
    /// </summary>
    /// <param name="pair">The pair to extend.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    private void ExtendCoveredRange(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate)
    {
        if (!_coverage.TryGetValue(pair, out DateRangeCoverage? coverage))
        {
            coverage = new DateRangeCoverage();
            _coverage[pair] = coverage;
        }

        coverage.Add(startDate, endDate);
    }

    /// <summary>
    /// Synchronously fetches a window around a date for the on-demand lookup path, unless the pair is already covered
    /// or is a same-currency request.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The date around which to fetch.</param>
    /// <returns><see langword="true" /> when a fetch was attempted; otherwise <see langword="false" />.</returns>
    private bool TryLoadPairForDate(string fromIsoCode, string toIsoCode, DateOnly date)
    {
        if (string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
            return false;

        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);
        var startDate = date.AddDays(-(int)_options.DefaultLookback.TotalDays);

        lock (_gate)
        {
            if (IsRangeCovered(pair, startDate, date))
                return false;
        }

        // Intentional opt-in synchronous fetch on the lookup path, gated by AllowSynchronousNetworkAccess.
#pragma warning disable VSTHRD002
        LoadPairAsync(fromIsoCode, toIsoCode, startDate, date).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        return true;
    }

    /// <summary>
    /// Collects the rates for a pair within a date range from the current book, inverting the reverse series when only
    /// it is available.
    /// </summary>
    /// <param name="pair">The requested pair.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns>The rates in the range, ordered by date.</returns>
    private List<ExchangeRate> CollectRates(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate)
    {
        ExchangeRateBook book = _book;
        List<ExchangeRate> result = new();

        if (book.TryGetSeries(pair, ProviderName, out ExchangeRateSeries? series) && series is not null)
        {
            foreach (ExchangeRateObservation observation in series.GetObservations())
            {
                if (observation.Date >= startDate && observation.Date <= endDate)
                    result.Add(new ExchangeRate(pair.FromIsoCode, pair.ToIsoCode, observation.Date, observation.Rate, ProviderName));
            }
        }
        else if (book.TryGetSeries(pair.Inverse(), ProviderName, out ExchangeRateSeries? inverse) && inverse is not null)
        {
            foreach (ExchangeRateObservation observation in inverse.GetObservations())
            {
                if (observation.Date >= startDate && observation.Date <= endDate)
                    result.Add(new ExchangeRate(pair.FromIsoCode, pair.ToIsoCode, observation.Date, 1m / observation.Rate, ProviderName, isInverted: true));
            }
        }

        result.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return result;
    }
}
