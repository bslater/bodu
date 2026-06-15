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
/// The provider derives from <see cref="WebExchangeRateProvider" />, which supplies the in-memory accumulator, the
/// immutable snapshot, the full synchronous and asynchronous lookup matrix, and ownership of the
/// <see cref="HttpClient" /> when this provider creates one. The IADB is queried by date range, so loading is
/// range-based: each load fetches the requested inclusive range and accumulates it. Use <see cref="LoadRangeAsync" /> to
/// warm a range.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured from <see cref="BoeEndpointOptions.UserAgent" /> and
/// <see cref="BoeEndpointOptions.HttpTimeout" />, disposing it with the provider. The constructor that takes an
/// <see cref="HttpClient" /> uses the caller-supplied client as-is; this is the path the dependency-injection package
/// uses.
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
    : WebExchangeRateProvider
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
    /// The source that downloads and parses range responses.
    /// </summary>
    private readonly IBoeExchangeRateTableSource _source;

    /// <summary>
    /// The provider options.
    /// </summary>
    private readonly BoeExchangeRateOptions _options;

    /// <summary>
    /// The logger that records range downloads and on-demand network fetches.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The inclusive ranges whose data has been loaded.
    /// </summary>
    private readonly List<(DateOnly From, DateOnly To)> _loadedRanges = new();

    /// <summary>
    /// The discovered currency series, keyed by pair.
    /// </summary>
    private readonly Dictionary<ExchangeRatePair, BoeSeriesInfo> _series = new();

    /// <summary>
    /// Coalesces concurrent downloads of the same series window so a cache miss triggers at most one in-flight fetch
    /// per requested range.
    /// </summary>
    private readonly SingleFlightCoordinator<(DateOnly From, DateOnly To)> _loadCoordinator = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateProvider" /> class backed by an
    /// <see cref="HttpClient" /> the provider creates and owns, configured from the supplied options.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public BoeExchangeRateProvider(BoeExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateProvider" /> class backed by the IADB CSV endpoint,
    /// queried with the caller-supplied HTTP client. The caller owns the client's configuration and lifetime.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download range responses.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public BoeExchangeRateProvider(HttpClient httpClient, BoeExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateProvider" /> class backed by an explicit table
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
    internal BoeExchangeRateProvider(IBoeExchangeRateTableSource source, BoeExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateProvider" /> class from an owned client, building the
    /// table source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private BoeExchangeRateProvider(BoeExchangeRateOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(CreateSource(ownedHttpClient, options), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateProvider" /> class, the shared core all public and
    /// internal constructors funnel through.
    /// </summary>
    /// <param name="source">The table source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private BoeExchangeRateProvider(
        IBoeExchangeRateTableSource source,
        BoeExchangeRateOptions options,
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
    protected override TimeSpan DefaultLookback => TimeSpan.FromDays(_options.OnDemandWindowDays);

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
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);

        return LoadRangeInternalAsync(startDate, endDate, cancellationToken);
    }

    /// <summary>
    /// Gets the currency pairs discovered across the ranges loaded so far.
    /// </summary>
    /// <returns>A snapshot of the discovered series, one per currency pair.</returns>
    public IReadOnlyCollection<BoeSeriesInfo> GetAvailablePairs()
    {
        lock (SyncRoot)
        {
            return _series.Values.ToArray();
        }
    }

    /// <inheritdoc />
    protected override ValueTask EnsureLoadedAsync(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        new(LoadRangeInternalAsync(startDate, endDate, cancellationToken));

    /// <inheritdoc />
    protected override bool IsLoaded(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate)
    {
        lock (SyncRoot)
        {
            return IsRangeCovered(startDate, endDate);
        }
    }

    /// <inheritdoc />
    protected override void ValidateRangeRequest(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        if (!string.Equals(fromIsoCode, BaseCurrencyIsoCode, StringComparison.Ordinal) &&
            !string.Equals(toIsoCode, BaseCurrencyIsoCode, StringComparison.Ordinal))
        {
            throw new BoeExchangeRateSeriesNotFoundException(
                string.Format(CultureInfo.CurrentCulture, BoeResourceStrings.IO_KeyNotFound_BoeSeries, fromIsoCode, toIsoCode));
        }
    }

    /// <inheritdoc />
    protected override void OnObservationIngested(ExchangeRate rate) =>
        Log.ObservationIngested(_logger, _options.ObservationIngestedLogLevel, rate.FromIsoCode, rate.ToIsoCode, rate.Date, rate.Rate);

    /// <inheritdoc />
    protected override Exception CreateRangeInvertedException(DateOnly startDate, DateOnly endDate) =>
        new BoeExchangeRateDateRangeException(
            string.Format(CultureInfo.CurrentCulture, BoeResourceStrings.Arg_OutOfRange_BoeDateRange, startDate, endDate));

    /// <inheritdoc />
    protected override string FormatRateNotFound(string fromIsoCode, string toIsoCode, DateOnly date) =>
        string.Format(CultureInfo.CurrentCulture, BoeResourceStrings.IO_KeyNotFound_BoeRate, fromIsoCode, toIsoCode, date);

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
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the endpoint's user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(BoeExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return ExchangeRateHttpClientFactory.Create(options.Endpoint.UserAgent, options.Endpoint.HttpTimeout);
    }

    /// <summary>
    /// Loads the inclusive date range, coalescing concurrent loads of the same window and skipping a covered range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the range has been loaded.</returns>
    private Task LoadRangeInternalAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            if (IsRangeCovered(startDate, endDate))
                return Task.CompletedTask;
        }

        // Coalesce concurrent loads of the same window so only one download is in flight; joiners await that task.
        return _loadCoordinator.RunAsync((startDate, endDate), () => LoadRangeCoreAsync(startDate, endDate, cancellationToken));
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
        lock (SyncRoot)
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

        lock (SyncRoot)
        {
            foreach (BoeSeriesInfo info in table.GetSeriesInfo())
                _series[info.Pair] = info;

            var count = AddObservations(table.EnumerateRates());
            _loadedRanges.Add((startDate, endDate));
            RebuildSnapshot();

            Log.FeedLoaded(_logger, _options.DownloadCompletedLogLevel, startDate, endDate, count);
        }
    }

    /// <summary>
    /// Determines whether an inclusive range is already contained within a single previously loaded range. The caller
    /// must hold <see cref="WebExchangeRateProvider.SyncRoot" />.
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
}
