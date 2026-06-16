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
/// The provider derives from <see cref="WebExchangeRateProvider" />, which supplies the in-memory accumulator, the
/// immutable snapshot, the full synchronous and asynchronous lookup matrix, and ownership of the
/// <see cref="HttpClient" /> when this provider creates one. Loading is feed-based: each ECB feed runs from its earliest
/// date to the most recent business day, so the feed covering a requested date also covers the remainder of the range.
/// Use <see cref="PreloadAsync" />, <see cref="LoadFeedAsync" />, or <see cref="LoadRangeAsync" /> to warm the store.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured from <see cref="EcbEndpointOptions.UserAgent" /> and
/// <see cref="EcbEndpointOptions.HttpTimeout" />, disposing it with the provider. The constructor that takes an
/// <see cref="HttpClient" /> uses the caller-supplied client as-is; this is the path the dependency-injection package
/// uses.
/// </para>
/// <para>
/// <strong>Logging.</strong> When an <see cref="ILogger" /> is supplied (directly or through the dependency-injection
/// package) the provider records: the start of a feed download (<see cref="LogLevel.Debug" />), a completed download
/// with its observation count (<see cref="LogLevel.Information" />), each ingested observation (
/// <see cref="LogLevel.Trace" />), a failed download (<see cref="LogLevel.Warning" />, then re-thrown), and a
/// synchronous on-demand network fetch (<see cref="LogLevel.Warning" />). Every level is configurable through the
/// corresponding <c>*LogLevel</c> property on <see cref="EcbExchangeRateOptions" />; omitting the logger selects
/// <see cref="NullLogger.Instance" />, so logging is opt-in and free when unused.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var ecb = new EcbExchangeRateProvider(new EcbExchangeRateOptions());
/// await ecb.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));
///
/// ExchangeRateLookupResult usd = ecb.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
/// ExchangeRateLookupResult eur = ecb.GetRate("USD", "EUR", new DateOnly(2023, 1, 3)); // inverted
///]]>
/// </code>
/// </example>
public sealed class EcbExchangeRateProvider
    : WebExchangeRateProvider
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
    /// The source that downloads and parses feed files.
    /// </summary>
    private readonly IEcbExchangeRateTableSource _source;

    /// <summary>
    /// The provider options.
    /// </summary>
    private readonly EcbExchangeRateOptions _options;

    /// <summary>
    /// The logger that records feed downloads and on-demand network fetches.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The names of feeds whose data has been loaded.
    /// </summary>
    private readonly HashSet<string> _loadedFeeds = new(StringComparer.Ordinal);

    /// <summary>
    /// The discovered currency series, keyed by pair.
    /// </summary>
    private readonly Dictionary<ExchangeRatePair, EcbSeriesInfo> _series = new();

    /// <summary>
    /// Coalesces concurrent downloads of the same feed so a cache miss triggers at most one in-flight fetch per feed.
    /// </summary>
    private readonly SingleFlightCoordinator<string> _loadCoordinator = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateProvider" /> class backed by an
    /// <see cref="HttpClient" /> the provider creates and owns, configured from the supplied options.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public EcbExchangeRateProvider(EcbExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateProvider" /> class backed by the ECB
    /// <c>eurofxref</c> feeds, downloaded with the caller-supplied HTTP client. The caller owns the client's
    /// configuration and lifetime.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download feed files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public EcbExchangeRateProvider(HttpClient httpClient, EcbExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateProvider" /> class backed by an explicit table
    /// source, used for testing.
    /// </summary>
    /// <param name="source">The table source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    internal EcbExchangeRateProvider(IEcbExchangeRateTableSource source, EcbExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateProvider" /> class from an owned client, building the
    /// table source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private EcbExchangeRateProvider(EcbExchangeRateOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(CreateSource(ownedHttpClient, options), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateProvider" /> class, the shared core all public and
    /// internal constructors funnel through.
    /// </summary>
    /// <param name="source">The table source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private EcbExchangeRateProvider(
        IEcbExchangeRateTableSource source,
        EcbExchangeRateOptions options,
        HttpClient? ownedHttpClient,
        ILogger? logger,
        TimeProvider? timeProvider)
        : base(ownedHttpClient, timeProvider)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _source = source;
        _options = options;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    protected override string ProviderId => ProviderName;

    /// <inheritdoc />
    protected override bool AllowSynchronousNetworkAccess => _options.AllowSynchronousNetworkAccess;

    /// <inheritdoc />
    protected override TimeSpan DefaultLookback => TimeSpan.Zero;

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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="feed" /> is <see langword="null" />.</exception>
    public Task LoadFeedAsync(EcbExchangeRateFeed feed, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(feed);

        lock (SyncRoot)
        {
            if (_loadedFeeds.Contains(feed.Name))
                return Task.CompletedTask;
        }

        // Coalesce concurrent loads of the same feed so only one download is in flight; joiners await that shared task.
        // The shared fetch runs under a token decoupled from any caller, so one caller's cancellation cannot fault the
        // others; cancellationToken only abandons this caller's wait.
        return _loadCoordinator.RunAsync(feed.Name, ct => LoadFeedCoreAsync(feed, ct), cancellationToken);
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
    public Task LoadRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);

        return LoadFeedAsync(SelectFeedForRange(startDate), cancellationToken);
    }

    /// <summary>
    /// Gets the currency pairs discovered across the feeds loaded so far.
    /// </summary>
    /// <returns>A snapshot of the discovered series, one per currency pair.</returns>
    public IReadOnlyCollection<EcbSeriesInfo> GetAvailablePairs()
    {
        lock (SyncRoot)
        {
            return _series.Values.ToArray();
        }
    }

    /// <inheritdoc />
    protected override ValueTask EnsureLoadedAsync(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        new(LoadFeedAsync(SelectFeedForRange(startDate), cancellationToken));

    /// <inheritdoc />
    protected override bool IsLoaded(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate)
    {
        EcbExchangeRateFeed feed = SelectFeedForRange(startDate);

        lock (SyncRoot)
        {
            return _loadedFeeds.Contains(feed.Name);
        }
    }

    /// <inheritdoc />
    protected override void ValidateRangeRequest(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        if (!string.Equals(fromIsoCode, BaseCurrencyIsoCode, StringComparison.Ordinal) &&
            !string.Equals(toIsoCode, BaseCurrencyIsoCode, StringComparison.Ordinal))
        {
            throw new EcbExchangeRateSeriesNotFoundException(
                string.Format(CultureInfo.CurrentCulture, EcbResourceStrings.IO_KeyNotFound_EcbSeries, fromIsoCode, toIsoCode));
        }
    }

    /// <inheritdoc />
    protected override void OnObservationIngested(ExchangeRate rate) =>
        Log.ObservationIngested(_logger, _options.ObservationIngestedLogLevel, rate.FromIsoCode, rate.ToIsoCode, rate.Date, rate.Rate);

    /// <inheritdoc />
    protected override void OnSynchronousNetworkFetch(DateOnly date) =>
        Log.SynchronousNetworkFetch(_logger, _options.SynchronousNetworkFetchLogLevel, date);

    /// <inheritdoc />
    protected override Exception CreateRangeInvertedException(DateOnly startDate, DateOnly endDate) =>
        new EcbExchangeRateDateRangeException(
            string.Format(CultureInfo.CurrentCulture, EcbResourceStrings.Arg_OutOfRange_EcbDateRange, startDate, endDate));

    /// <inheritdoc />
    protected override string FormatRateNotFound(string fromIsoCode, string toIsoCode, DateOnly date) =>
        string.Format(CultureInfo.CurrentCulture, EcbResourceStrings.IO_KeyNotFound_EcbRate, fromIsoCode, toIsoCode, date);

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
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the endpoint's user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(EcbExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return ExchangeRateHttpClientFactory.Create(options.Endpoint.UserAgent, options.Endpoint.HttpTimeout);
    }

    /// <summary>
    /// Downloads and stores a single feed, re-checking the loaded set inside the single-flight section so a joiner that
    /// arrives after a prior load completes does no redundant work.
    /// </summary>
    /// <param name="feed">The feed to load.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the feed has been loaded.</returns>
    private async Task LoadFeedCoreAsync(EcbExchangeRateFeed feed, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
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

        // Capture the load instant immediately after the download completes so it stamps every rate this feed produces.
        var fetchedAt = TimeProvider.GetUtcNow();

        lock (SyncRoot)
        {
            if (!_loadedFeeds.Add(feed.Name))
                return;

            foreach (EcbSeriesInfo info in table.GetSeriesInfo())
                _series[info.Pair] = info;

            var count = AddObservations(table.EnumerateRates(), fetchedAt);
            RebuildSnapshot();

            Log.FeedLoaded(_logger, _options.DownloadCompletedLogLevel, feed.Name, count);
        }
    }

    /// <summary>
    /// Selects the feed that covers the start of a requested range, falling back to the widest feed when none reaches
    /// that far back.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <returns>The covering feed.</returns>
    private EcbExchangeRateFeed SelectFeedForRange(DateOnly startDate)
    {
        var today = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);
        return EcbExchangeRateFeed.ForDate(startDate, _options.Feeds, today) ?? SelectWidestFeed();
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
}
