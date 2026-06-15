// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Serves Bank of England daily spot rates as <see cref="ExchangeRate" /> values, implementing the Bodu.Financial
/// provider contracts over data queried from the Bank's Interactive Statistical Database (IADB) CSV endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The provider implements the dated <see cref="IDatedExchangeRateProvider" /> contract (the natural fit for historical
/// data) and the simple <see cref="IExchangeRateProvider" /> contract (returning the most recent available rate). It
/// also exposes the asynchronous <see cref="LoadRangeAsync" /> method to warm its in-memory store, and
/// <see cref="GetRatesAsync" /> to read a whole date range at once.
/// </para>
/// <para>
/// Unlike a file-partitioned source, the IADB is queried by date range, so loading is range-based: each load fetches
/// the requested inclusive range and accumulates it into an immutable <see cref="ExchangeRateBook" /> snapshot that
/// backs the synchronous lookups. A synchronous lookup that misses an unloaded date will, when
/// <see cref="BoeExchangeRateOptions.AllowSynchronousNetworkAccess" /> is enabled, block to download a bounded window
/// around that date and retry. Inverse pairs, same-currency identity, and date-resolution policies are inherited from
/// <see cref="FixedDatedExchangeRateProvider" />.
/// </para>
/// <para>
/// <strong>Logging.</strong> When an <see cref="ILogger" /> is supplied (directly or through the dependency-injection
/// package) the provider records: the start of a range download (<see cref="LogLevel.Debug" />), a completed download
/// with its observation count (<see cref="LogLevel.Information" />), each ingested observation (
/// <see cref="LogLevel.Trace" />), and a failed download (<see cref="LogLevel.Warning" />, then re-thrown). Every level
/// is configurable through the corresponding <c>*LogLevel</c> property on <see cref="BoeExchangeRateOptions" />;
/// omitting the logger selects <see cref="NullLogger.Instance" />, so logging is opt-in and free when unused.
/// </para>
/// </remarks>
public sealed class BoeExchangeRateProvider
    : IDatedExchangeRateProvider, IExchangeRateProvider
{
    /// <summary>
    /// The provider identifier stamped on every rate this provider produces.
    /// </summary>
    public const string ProviderName = "BoE";

    /// <summary>
    /// The base currency the Bank of England quotes against.
    /// </summary>
    public const string BaseCurrencyIsoCode = "GBP";

    /// <summary>
    /// The tolerance, in days, used to resolve the most recent rate for the undated
    /// <see cref="IExchangeRateProvider" /> surface; large enough to reach the start of the loaded history from the
    /// current date.
    /// </summary>
    private const int LatestRateToleranceDays = 100_000;

    /// <summary>
    /// The source that downloads and parses range responses.
    /// </summary>
    private readonly IBoeExchangeRateTableSource _source;

    /// <summary>
    /// The provider options.
    /// </summary>
    private readonly BoeExchangeRateOptions _options;

    /// <summary>
    /// Guards mutation of the accumulator, loaded-range list, series index, and snapshot fields.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// The accumulator into which each loaded range's observations are upserted.
    /// </summary>
    private readonly ExchangeRateTableBuilder _builder = new();

    /// <summary>
    /// The inclusive ranges whose data has been loaded.
    /// </summary>
    private readonly List<(DateOnly From, DateOnly To)> _loadedRanges = new();

    /// <summary>
    /// The discovered currency series, keyed by pair.
    /// </summary>
    private readonly Dictionary<ExchangeRatePair, BoeSeriesInfo> _series = new();

    /// <summary>
    /// The current immutable book backing range queries; replaced under <see cref="_gate" /> after each load.
    /// </summary>
    private volatile ExchangeRateBook _book;

    /// <summary>
    /// The current immutable lookup provider; replaced under <see cref="_gate" /> after each load.
    /// </summary>
    private volatile FixedDatedExchangeRateProvider _snapshot;

    /// <summary>
    /// The logger that records range downloads and on-demand network fetches.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The time source used to resolve the current instant for on-demand windowing and the undated lookup surface.
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Coalesces concurrent downloads of the same series window so a cache miss triggers at most one in-flight fetch
    /// per requested range.
    /// </summary>
    private readonly SingleFlightCoordinator<(DateOnly From, DateOnly To)> _loadCoordinator = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateProvider" /> class backed by the IADB CSV endpoint,
    /// queried with the supplied HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download range responses.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records range downloads and on-demand network fetches. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for on-demand windowing and the undated lookup surface.
    /// <see langword="null" /> selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public BoeExchangeRateProvider(HttpClient httpClient, BoeExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateProvider" /> class backed by an explicit table
    /// source, used for testing.
    /// </summary>
    /// <param name="source">The table source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records range downloads and on-demand network fetches. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for on-demand windowing and the undated lookup surface.
    /// <see langword="null" /> selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    internal BoeExchangeRateProvider(IBoeExchangeRateTableSource source, BoeExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
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
                string.Format(CultureInfo.CurrentCulture, BoeResourceStrings.IO_KeyNotFound_BoeRate, fromIsoCode, toIsoCode, date));

    /// <summary>
    /// Attempts to resolve the rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" /> on
    /// <paramref name="date" />, loading a bounded window around the date on demand when permitted.
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

        if (_options.AllowSynchronousNetworkAccess && TryLoadWindowForDate(date))
            return _snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);

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
    /// Downloads and loads the inclusive date range, unless it is already covered by a previous load.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the range has been loaded.</returns>
    /// <exception cref="BoeExchangeRateDateRangeException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    public Task LoadRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        ThrowIfRangeInverted(startDate, endDate);

        lock (_gate)
        {
            if (IsRangeCovered(startDate, endDate))
                return Task.CompletedTask;
        }

        // Coalesce concurrent loads of the same window so only one download is in flight; joiners await that task. The
        // shared fetch runs under a token decoupled from any caller, so one caller's cancellation cannot fault the
        // others; cancellationToken only abandons this caller's wait.
        return _loadCoordinator.RunAsync((startDate, endDate), ct => LoadRangeCoreAsync(startDate, endDate, ct), cancellationToken);
    }

    /// <summary>
    /// Downloads and stores a single range, re-checking coverage inside the single-flight section so a joiner that
    /// arrives after a prior load completes does no redundant work.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the range has been loaded.</returns>
    private async Task LoadRangeCoreAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (IsRangeCovered(startDate, endDate))
                return;
        }

        Log.FeedLoadStarting(_logger, _options.DownloadStartingLogLevel, startDate, endDate);

        BoeExchangeRateTable table;
        try
        {
            table = await _source.GetTableAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.FeedLoadFailed(_logger, _options.DownloadFailedLogLevel, startDate, endDate, ex);
            throw;
        }

        // Capture the load instant immediately after the download completes so it stamps every rate this range produces.
        var fetchedAt = _timeProvider.GetUtcNow();

        lock (_gate)
        {
            var count = Accumulate(table, fetchedAt);
            _loadedRanges.Add((startDate, endDate));
            RebuildSnapshot();
            Log.FeedLoaded(_logger, _options.DownloadCompletedLogLevel, startDate, endDate, count);
        }
    }

    /// <summary>
    /// Reads every available rate for a pair within the inclusive date range, loading the range first.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code; one side of the pair must be <c>GBP</c>.</param>
    /// <param name="toIsoCode">The destination-currency ISO code; one side of the pair must be <c>GBP</c>.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that yields the rates in the range, ordered by date.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an ISO code is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when an ISO code is invalid.</exception>
    /// <exception cref="BoeExchangeRateDateRangeException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    /// <exception cref="BoeExchangeRateSeriesNotFoundException">
    /// Thrown when neither side of the pair is <c>GBP</c>.
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
        if (!string.Equals(fromIsoCode, BaseCurrencyIsoCode, StringComparison.Ordinal) &&
            !string.Equals(toIsoCode, BaseCurrencyIsoCode, StringComparison.Ordinal))
        {
            throw new BoeExchangeRateSeriesNotFoundException(
                string.Format(CultureInfo.CurrentCulture, BoeResourceStrings.IO_KeyNotFound_BoeSeries, fromIsoCode, toIsoCode));
        }

        await LoadRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);

        return CollectRates(pair, startDate, endDate);
    }

    /// <summary>
    /// Gets the currency pairs discovered across the ranges loaded so far.
    /// </summary>
    /// <returns>A snapshot of the discovered series, one per currency pair.</returns>
    public IReadOnlyCollection<BoeSeriesInfo> GetAvailablePairs()
    {
        lock (_gate)
        {
            return _series.Values.ToArray();
        }
    }

    /// <summary>
    /// Builds the default IADB CSV table source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download range responses.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>A new table source.</returns>
    private static BoeCsvExchangeRateTableSource CreateSource(HttpClient httpClient, BoeExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        IBoeResponseCache cache = options.EnableDiskCache
            ? new FileSystemBoeResponseCache(options.CacheDirectory)
            : NullBoeResponseCache.Instance;

        return new BoeCsvExchangeRateTableSource(httpClient, options, cache);
    }

    /// <summary>
    /// Throws when an inclusive date range is inverted.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <exception cref="BoeExchangeRateDateRangeException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    private static void ThrowIfRangeInverted(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new BoeExchangeRateDateRangeException(
                string.Format(CultureInfo.CurrentCulture, BoeResourceStrings.Arg_OutOfRange_BoeDateRange, startDate, endDate));
        }
    }

    /// <summary>
    /// Determines whether an inclusive range is already contained within a single previously loaded range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns>
    /// <see langword="true" /> when a loaded range fully contains the requested range; otherwise
    /// <see langword="false" />.
    /// </returns>
    private bool IsRangeCovered(DateOnly startDate, DateOnly endDate)
    {
        foreach ((DateOnly from, DateOnly to) in _loadedRanges)
        {
            if (from <= startDate && to >= endDate)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Upserts a parsed table's observations and series metadata into the accumulator, stamping each contributing
    /// series with the load instant.
    /// </summary>
    /// <param name="table">The parsed table.</param>
    /// <param name="fetchedAt">The UTC instant at which the table was downloaded.</param>
    /// <returns>The number of rate observations upserted.</returns>
    private int Accumulate(BoeExchangeRateTable table, DateTimeOffset fetchedAt)
    {
        foreach (BoeSeriesInfo info in table.GetSeriesInfo())
            _series[info.Pair] = info;

        var count = 0;
        foreach (ExchangeRate rate in table.EnumerateRates())
        {
            _builder.Upsert(new ExchangeRatePair(rate.FromIsoCode, rate.ToIsoCode), ProviderName, rate.Date, rate.Rate, fetchedAt);
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
    /// Synchronously loads a bounded window around a date when it is not already covered, for the on-demand lookup
    /// path.
    /// </summary>
    /// <param name="date">The date whose surrounding window should be loaded.</param>
    /// <returns>
    /// <see langword="true" /> when a load was attempted; <see langword="false" /> when the window is empty or already
    /// covered.
    /// </returns>
    private bool TryLoadWindowForDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var window = _options.OnDemandWindowDays;

        var from = date.AddDays(-window);
        var to = date.AddDays(window);
        if (to > today)
            to = today;

        if (to < from)
            return false;

        lock (_gate)
        {
            if (IsRangeCovered(from, to))
                return false;
        }

        // Intentional opt-in synchronous fetch on the lookup path, gated by AllowSynchronousNetworkAccess.
#pragma warning disable VSTHRD002
        LoadRangeAsync(from, to).GetAwaiter().GetResult();
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
                    result.Add(new ExchangeRate(pair.FromIsoCode, pair.ToIsoCode, observation.Date, observation.Rate, ProviderName, isInverted: false, series.FetchedAtUtc));
            }
        }
        else if (book.TryGetSeries(pair.Inverse(), ProviderName, out ExchangeRateSeries? inverse) && inverse is not null)
        {
            foreach (ExchangeRateObservation observation in inverse.GetObservations())
            {
                if (observation.Date >= startDate && observation.Date <= endDate)
                    result.Add(new ExchangeRate(pair.FromIsoCode, pair.ToIsoCode, observation.Date, 1m / observation.Rate, ProviderName, isInverted: true, inverse.FetchedAtUtc));
            }
        }

        result.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return result;
    }
}
