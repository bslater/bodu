// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Serves European Central Bank euro reference rates as <see cref="ExchangeRate" /> values, implementing the
/// Bodu.Financial provider contracts over data downloaded from the ECB's published <c>eurofxref</c> XML feeds.
/// </summary>
/// <remarks>
/// <para>
/// The provider implements the dated <see cref="IDatedExchangeRateProvider" /> contract (the natural fit for historical
/// data) and the simple <see cref="IExchangeRateProvider" /> contract (returning the most recent available rate). It
/// also exposes asynchronous <see cref="PreloadAsync" />, <see cref="LoadFeedAsync" />, and
/// <see cref="LoadRangeAsync" /> methods to warm its in-memory store, and <see cref="GetRatesAsync" /> to read a whole
/// date range at once.
/// </para>
/// <para>
/// Because downloads are asynchronous while the lookup contracts are synchronous, a synchronous lookup that misses a
/// date whose covering feed has not yet been loaded will, when
/// <see cref="EcbExchangeRateOptions.AllowSynchronousNetworkAccess" /> is enabled, block to download the narrowest feed
/// that covers that date and retry. Loaded feeds are accumulated into an immutable <see cref="ExchangeRateBook" />
/// snapshot that backs the synchronous lookups, so inverse pairs, same-currency identity, and date-resolution policies
/// are inherited from <see cref="FixedDatedExchangeRateProvider" />.
/// </para>
/// <para>
/// <strong>Logging.</strong> When an <see cref="ILogger" /> is supplied (directly or through the dependency-injection
/// package) the provider records: the start of a feed download (<see cref="LogLevel.Debug" />), a completed download
/// with its observation count (<see cref="LogLevel.Information" />), each ingested observation
/// (<see cref="LogLevel.Trace" />), a failed download (<see cref="LogLevel.Warning" />, then re-thrown), and a
/// synchronous on-demand network fetch (<see cref="LogLevel.Warning" />). Every level is configurable through the
/// corresponding <c>*LogLevel</c> property on <see cref="EcbExchangeRateOptions" />; omitting the logger selects
/// <see cref="NullLogger.Instance" />, so logging is opt-in and free when unused.
/// </para>
/// </remarks>
public sealed class EcbExchangeRateProvider
    : IDatedExchangeRateProvider, IExchangeRateProvider
{
    /// <summary>
    /// The provider identifier stamped on every rate this provider produces.
    /// </summary>
    public const string ProviderName = "ECB";

    /// <summary>
    /// The base currency the ECB quotes against.
    /// </summary>
    public const string BaseCurrencyIsoCode = "EUR";

    /// <summary>
    /// The tolerance, in days, used to resolve the most recent rate for the undated
    /// <see cref="IExchangeRateProvider" /> surface; large enough to reach the start of the ECB history from the
    /// current date.
    /// </summary>
    private const int LatestRateToleranceDays = 100_000;

    /// <summary>
    /// The source that downloads and parses feed files.
    /// </summary>
    private readonly IEcbExchangeRateTableSource _source;

    /// <summary>
    /// The provider options.
    /// </summary>
    private readonly EcbExchangeRateOptions _options;

    /// <summary>
    /// Guards mutation of the accumulator, loaded-feed set, series index, and snapshot fields.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// The accumulator into which each loaded feed's observations are upserted.
    /// </summary>
    private readonly ExchangeRateTableBuilder _builder = new();

    /// <summary>
    /// The names of feeds whose data has been loaded.
    /// </summary>
    private readonly HashSet<string> _loadedFeeds = new(StringComparer.Ordinal);

    /// <summary>
    /// The discovered currency series, keyed by pair.
    /// </summary>
    private readonly Dictionary<ExchangeRatePair, EcbSeriesInfo> _series = new();

    /// <summary>
    /// The current immutable book backing range queries; replaced under <see cref="_gate" /> after each load.
    /// </summary>
    private volatile ExchangeRateBook _book;

    /// <summary>
    /// The current immutable lookup provider; replaced under <see cref="_gate" /> after each load.
    /// </summary>
    private volatile FixedDatedExchangeRateProvider _snapshot;

    /// <summary>
    /// The logger that records feed downloads and on-demand network fetches.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateProvider" /> class backed by the ECB
    /// <c>eurofxref</c> feeds, downloaded with the supplied HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download feed files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records feed downloads and on-demand network fetches. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public EcbExchangeRateProvider(HttpClient httpClient, EcbExchangeRateOptions options, ILogger? logger = null)
        : this(CreateSource(httpClient, options), options, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateProvider" /> class backed by an explicit table
    /// source, used for testing.
    /// </summary>
    /// <param name="source">The table source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records feed downloads and on-demand network fetches. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    internal EcbExchangeRateProvider(IEcbExchangeRateTableSource source, EcbExchangeRateOptions options, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _source = source;
        _options = options;
        _logger = logger ?? NullLogger.Instance;
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
                string.Format(CultureInfo.CurrentCulture, EcbResourceStrings.IO_KeyNotFound_EcbRate, fromIsoCode, toIsoCode, date));

    /// <summary>
    /// Attempts to resolve the rate from <paramref name="fromIsoCode" /> to <paramref name="toIsoCode" /> on
    /// <paramref name="date" />, loading the covering feed on demand when permitted.
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

        if (_options.AllowSynchronousNetworkAccess && TryLoadFeedForDate(date))
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return GetRate(fromIsoCode, toIsoCode, today, ExchangeRateLookupOptions.PreviousWithin(LatestRateToleranceDays)).Rate.Rate;
    }

    /// <summary>
    /// Downloads and loads the full-history feed, warming the store with every published day.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the full history has been loaded.</returns>
    /// <remarks>
    /// Because each ECB feed extends from its earliest date to the most recent business day, the widest feed in the
    /// catalogue subsumes the narrower ones; preloading therefore loads that single feed rather than every overlapping
    /// feed.
    /// </remarks>
    public Task PreloadAsync(CancellationToken cancellationToken = default) =>
        LoadFeedAsync(SelectWidestFeed(), cancellationToken);

    /// <summary>
    /// Downloads and loads a single feed, if it has not already been loaded.
    /// </summary>
    /// <param name="feed">The feed to load.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the feed has been loaded.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="feed" /> is <see langword="null" />.
    /// </exception>
    public async Task LoadFeedAsync(EcbExchangeRateFeed feed, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(feed);

        lock (_gate)
        {
            if (_loadedFeeds.Contains(feed.Name))
                return;
        }

        Log.FeedLoadStarting(_logger, _options.DownloadStartingLogLevel, feed.Name);

        EcbExchangeRateTable table;
        try
        {
            table = await _source.GetTableAsync(feed, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.FeedLoadFailed(_logger, _options.DownloadFailedLogLevel, feed.Name, ex);
            throw;
        }

        lock (_gate)
        {
            if (!_loadedFeeds.Add(feed.Name))
                return;

            var count = Accumulate(table);
            RebuildSnapshot();
            Log.FeedLoaded(_logger, _options.DownloadCompletedLogLevel, feed.Name, count);
        }
    }

    /// <summary>
    /// Downloads and loads the narrowest feed whose coverage reaches the start of the inclusive date range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the covering feed has been loaded.</returns>
    /// <exception cref="EcbExchangeRateDateRangeException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    /// <remarks>
    /// A single ECB feed runs from its earliest date to the most recent business day, so the feed that covers
    /// <paramref name="startDate" /> also covers the remainder of the range; when no feed reaches that far back, the
    /// widest feed is loaded.
    /// </remarks>
    public Task LoadRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        ThrowIfRangeInverted(startDate, endDate);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        EcbExchangeRateFeed feed = EcbExchangeRateFeed.ForDate(startDate, _options.Feeds, today) ?? SelectWidestFeed();

        return LoadFeedAsync(feed, cancellationToken);
    }

    /// <summary>
    /// Reads every available rate for a pair within the inclusive date range, loading any covering feed first.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code; one side of the pair must be <c>EUR</c>.</param>
    /// <param name="toIsoCode">The destination-currency ISO code; one side of the pair must be <c>EUR</c>.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that yields the rates in the range, ordered by date.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an ISO code is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when an ISO code is invalid.</exception>
    /// <exception cref="EcbExchangeRateDateRangeException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    /// <exception cref="EcbExchangeRateSeriesNotFoundException">
    /// Thrown when neither side of the pair is <c>EUR</c>.
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
            throw new EcbExchangeRateSeriesNotFoundException(
                string.Format(CultureInfo.CurrentCulture, EcbResourceStrings.IO_KeyNotFound_EcbSeries, fromIsoCode, toIsoCode));
        }

        await LoadRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);

        return CollectRates(pair, startDate, endDate);
    }

    /// <summary>
    /// Gets the currency pairs discovered across the feeds loaded so far.
    /// </summary>
    /// <returns>A snapshot of the discovered series, one per currency pair.</returns>
    public IReadOnlyCollection<EcbSeriesInfo> GetAvailablePairs()
    {
        lock (_gate)
        {
            return _series.Values.ToArray();
        }
    }

    /// <summary>
    /// Builds the default <c>eurofxref</c> table source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download feed files.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>A new table source.</returns>
    private static EcbXmlExchangeRateTableSource CreateSource(HttpClient httpClient, EcbExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        IEcbFeedCache cache = options.EnableDiskCache
            ? new FileSystemEcbFeedCache(options.CacheDirectory)
            : NullEcbFeedCache.Instance;

        return new EcbXmlExchangeRateTableSource(httpClient, options, cache);
    }

    /// <summary>
    /// Throws when an inclusive date range is inverted.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <exception cref="EcbExchangeRateDateRangeException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    private static void ThrowIfRangeInverted(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new EcbExchangeRateDateRangeException(
                string.Format(CultureInfo.CurrentCulture, EcbResourceStrings.Arg_OutOfRange_EcbDateRange, startDate, endDate));
        }
    }

    /// <summary>
    /// Selects the widest feed in the catalogue — the full-history feed when present, otherwise the last configured
    /// feed.
    /// </summary>
    /// <returns>The widest feed.</returns>
    private EcbExchangeRateFeed SelectWidestFeed()
    {
        IReadOnlyList<EcbExchangeRateFeed> feeds = _options.Feeds;
        for (var i = 0; i < feeds.Count; i++)
        {
            if (feeds[i].IsFullHistory)
                return feeds[i];
        }

        return feeds[^1];
    }

    /// <summary>
    /// Upserts a parsed table's observations and series metadata into the accumulator.
    /// </summary>
    /// <param name="table">The parsed table.</param>
    /// <returns>The number of rate observations upserted.</returns>
    private int Accumulate(EcbExchangeRateTable table)
    {
        foreach (EcbSeriesInfo info in table.GetSeriesInfo())
            _series[info.Pair] = info;

        var count = 0;
        foreach (ExchangeRate rate in table.EnumerateRates())
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
    /// Synchronously loads the feed covering a date when it has not been loaded, for the on-demand lookup path.
    /// </summary>
    /// <param name="date">The date whose covering feed should be loaded.</param>
    /// <returns>
    /// <see langword="true" /> when a load was attempted; <see langword="false" /> when no feed covers the date or it
    /// was already loaded.
    /// </returns>
    private bool TryLoadFeedForDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        EcbExchangeRateFeed? feed = EcbExchangeRateFeed.ForDate(date, _options.Feeds, today);
        if (feed is null)
            return false;

        lock (_gate)
        {
            if (_loadedFeeds.Contains(feed.Name))
                return false;
        }

        // Intentional opt-in synchronous fetch on the lookup path, gated by AllowSynchronousNetworkAccess.
#pragma warning disable VSTHRD002
        LoadFeedAsync(feed).GetAwaiter().GetResult();
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
