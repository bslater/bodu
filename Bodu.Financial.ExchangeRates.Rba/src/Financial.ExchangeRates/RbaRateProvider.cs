// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Serves Reserve Bank of Australia historical exchange rates as <see cref="ExchangeRate" /> values, implementing the
/// Bodu.Financial provider contracts over data downloaded from the RBA's published <c>.xls</c> files.
/// </summary>
/// <remarks>
/// <para>
/// The provider derives from <see cref="WebRateProvider" />, which supplies the in-memory accumulator, the immutable
/// snapshot, the full synchronous and asynchronous lookup matrix, and ownership of the <see cref="HttpClient" /> when
/// this provider creates one. Loading is era-based: each era is a published <c>.xls</c> file covering a span of dates,
/// and a range load fetches every era overlapping the requested window. Use <see cref="PreloadAsync" />,
/// <see cref="LoadEraAsync" />, or <see cref="LoadRangeAsync" /> to warm the store.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured from <see cref="WebRateProviderOptions.UserAgent" />,
/// <see cref="WebRateProviderOptions.HttpTimeout" /> and
/// <see cref="WebRateProviderOptions.MaxResponseContentBufferSize" />, disposing it with the provider. The constructor
/// that takes an <see cref="HttpClient" /> uses the caller-supplied client as-is; this is the path the
/// dependency-injection package uses.
/// </para>
/// <para>
/// <strong>Logging.</strong> When an <see cref="ILogger" /> is supplied (directly or through the dependency-injection
/// package) the provider records: the start of an era download (<see cref="LogLevel.Debug" />), a completed download
/// with its observation count (<see cref="LogLevel.Information" />), each ingested observation (
/// <see cref="LogLevel.Trace" />), and a failed download (<see cref="LogLevel.Warning" />, then re-thrown). Every level
/// is configurable through the corresponding <c>*LogLevel</c> property on <see cref="RbaRateProviderOptions" />;
/// omitting the logger selects <see cref="NullLogger.Instance" />, so logging is opt-in and free when unused.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var rba = new RbaRateProvider(new RbaRateProviderOptions());
/// await rba.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));
///
/// RateLookupResult aud = rba.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
/// // aud.Rate.Provider == RbaRateProvider.ProviderName; the reverse direction (USD->AUD) is inverted.
///]]>
/// </code>
/// </example>
public sealed class RbaRateProvider
    : WebRateProvider
{
    /// <summary>The provider identifier stamped on every rate this provider produces.</summary>
    public const string ProviderName = "RBA";

    /// <summary>The base currency the RBA quotes against.</summary>
    public const CurrencyCode BaseCurrency = CurrencyCode.AUD;

    /// <summary>The source that downloads and parses era files.</summary>
    private readonly IRbaRateTableSource _source;

    /// <summary>The provider options.</summary>
    private readonly RbaRateProviderOptions _options;

    /// <summary>The logger that records era downloads and on-demand network fetches.</summary>
    private readonly ILogger _logger;

    /// <summary>The eras whose data has been loaded, keyed by label, each with the days it can no longer revise and when its open days expire.</summary>
    private readonly Dictionary<string, LoadedUnit> _loadedEras = new(StringComparer.Ordinal);

    /// <summary>The discovered currency series, keyed by pair.</summary>
    private readonly Dictionary<CurrencyPair, RbaSeriesInfo> _series = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaRateProvider" /> class backed by an <see cref="HttpClient" />
    /// the provider creates and owns, configured from the supplied options.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">
    /// The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public RbaRateProvider(RbaRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaRateProvider" /> class backed by the RBA <c>.xls</c> files,
    /// downloaded with the caller-supplied HTTP client. The caller owns the client's configuration and lifetime.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download era files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">
    /// The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public RbaRateProvider(HttpClient httpClient, RbaRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options, logger), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaRateProvider" /> class backed by an explicit table source, used
    /// for testing.
    /// </summary>
    /// <param name="source">The table source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">
    /// The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    internal RbaRateProvider(IRbaRateTableSource source, RbaRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaRateProvider" /> class from an owned client, building the table
    /// source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private RbaRateProvider(RbaRateProviderOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(CreateSource(ownedHttpClient, options, logger), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaRateProvider" /> class, the shared core all public and internal
    /// constructors funnel through.
    /// </summary>
    /// <param name="source">The table source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private RbaRateProvider(
        IRbaRateTableSource source,
        RbaRateProviderOptions options,
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
    /// <remarks>
    /// The options' <see cref="WebRateProviderOptions.HistoryAvailability" /> when it is set to anything other than
    /// <see cref="RateHistoryAvailability.Unbounded" />, its default. Otherwise the depth is computed from the
    /// configured <see cref="RbaRateProviderOptions.Eras" />: the earliest era start bounds how far back the historical
    /// workbook catalog reaches - 1 January 1983 for the default <see cref="RbaEraWorkbook.Default" /> catalog.
    /// </remarks>
    public override RateHistoryAvailability HistoryAvailability
    {
        get
        {
            // No RBA era catalogue reaches back without bound, so the base default, Unbounded, stands for "not set" and
            // the depth of the era catalogue is advertised in its place.
            if (_options.HistoryAvailability.Kind != RateHistoryAvailabilityKind.Unbounded)
                return _options.HistoryAvailability;

            DateOnly earliest = _options.Eras[0].Start;
            foreach (RbaEraWorkbook era in _options.Eras)
            {
                if (era.Start < earliest)
                    earliest = era.Start;
            }

            return RateHistoryAvailability.Since(earliest);
        }
    }

    /// <inheritdoc />
    protected override TimeSpan DefaultLookback => _options.DefaultLookback;

    /// <summary>
    /// Downloads and loads every era in the configured catalog.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while awaiting the loads.</param>
    /// <returns>A task that completes when every era has been loaded.</returns>
    /// <remarks>
    /// Up to four eras are downloaded at once, and each is published only after every era before it in the catalog.
    /// Once a load fails no further era is started; the loads already started finish, and the first failure is then
    /// thrown.
    /// </remarks>
    public Task PreloadAsync(CancellationToken cancellationToken = default) =>
        LoadErasInOrderAsync(
            _options.Eras.Select(era => (era, era.End ?? DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime))),
            cancellationToken);

    /// <summary>
    /// Downloads and loads a single era, unless it has already been loaded and is still current.
    /// </summary>
    /// <param name="era">The era to load.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the era has been loaded.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="era" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// A closed era never changes and is loaded once. The open-ended current era, loaded before the RBA published the
    /// latest day's rates, is downloaded again once <see cref="WebRateProviderOptions.RefreshInterval" /> has passed
    /// since it was loaded.
    /// </remarks>
    public Task LoadEraAsync(RbaEraWorkbook era, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(era);

        return LoadEraThroughAsync(era, era.End ?? DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime), Task.CompletedTask, cancellationToken);
    }

    /// <summary>
    /// Downloads and loads every era whose coverage overlaps the inclusive date range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the loads.</param>
    /// <returns>A task that completes when the overlapping eras have been loaded.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    public Task LoadRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);

        return LoadRangeInternalAsync(startDate, endDate, cancellationToken);
    }

    /// <summary>
    /// Gets the currency pairs discovered across the eras loaded so far.
    /// </summary>
    /// <returns>A snapshot of the discovered series, one per currency pair.</returns>
    public IReadOnlyCollection<RbaSeriesInfo> GetAvailablePairs()
    {
        lock (SyncRoot)
        {
            return _series.Values.ToArray();
        }
    }

    /// <inheritdoc />
    protected override ValueTask EnsureLoadedAsync(CurrencyPair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        new(LoadRangeInternalAsync(startDate, endDate, cancellationToken));

    /// <inheritdoc />
    protected override bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate)
    {
        lock (SyncRoot)
        {
            foreach (RbaEraWorkbook era in _options.Eras)
            {
                if (era.Start <= endDate && (era.End is null || era.End.Value >= startDate) && !IsEraCurrent(era, ThroughWithin(era, endDate)))
                    return false;
            }

            return true;
        }
    }

    /// <inheritdoc />
    protected override void ValidateRangeRequest(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        string baseIso = BaseCurrency.ToString();
        if (!string.Equals(fromIsoCode, baseIso, StringComparison.Ordinal) &&
            !string.Equals(toIsoCode, baseIso, StringComparison.Ordinal))
        {
            throw new RateSeriesNotFoundException(
                string.Format(CultureInfo.CurrentCulture, RbaResourceStrings.IO_KeyNotFound_RbaSeries, fromIsoCode, toIsoCode));
        }
    }

    /// <inheritdoc />
    protected override void OnObservationIngested(ExchangeRate rate)
    {
        if (_logger.IsEnabled(_options.ObservationIngestedLogLevel))
        {
            string from = rate.From.ToString();
            string to = rate.To.ToString();
            Log.ObservationIngested(_logger, _options.ObservationIngestedLogLevel, from, to, rate.Date, rate.Rate);
        }
    }

    /// <inheritdoc />
    protected override void OnSynchronousNetworkFetch(DateOnly date) =>
        Log.SynchronousNetworkFetch(_logger, _options.SynchronousNetworkFetchLogLevel, date);

    /// <inheritdoc />
    protected override string FormatRateNotFound(string fromIsoCode, string toIsoCode, DateOnly date) =>
        string.Format(CultureInfo.CurrentCulture, RbaResourceStrings.IO_KeyNotFound_RbaRate, fromIsoCode, toIsoCode, date);

    /// <summary>
    /// Builds the default <c>.xls</c> table source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download era files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The provider's logger, forwarded to the on-disk response cache for degradation warnings.
    /// </param>
    /// <returns>A new table source.</returns>
    private static RbaXlsRateTableSource CreateSource(HttpClient httpClient, RbaRateProviderOptions options, ILogger? logger)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        IByteCache<RbaEraWorkbook> cache = options.EnableDiskCache
            ? new FileSystemRbaWorkbookCache(options.CacheDirectory, logger)
            : NullByteCache<RbaEraWorkbook>.Instance;

        return new RbaXlsRateTableSource(httpClient, options, cache);
    }

    /// <summary>
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the options' user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(RbaRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return RateProviderHttpClientFactory.Create(options.UserAgent, options.HttpTimeout, options.MaxResponseContentBufferSize);
    }

    /// <summary>
    /// Loads every era overlapping the inclusive date range, skipping eras already loaded.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the loads.</param>
    /// <returns>A task that completes when the overlapping eras have been loaded.</returns>
    private Task LoadRangeInternalAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        LoadErasInOrderAsync(
            _options.Eras
                .Where(era => era.Start <= endDate && (era.End is null || era.End.Value >= startDate))
                .Select(era => (era, ThroughWithin(era, endDate))),
            cancellationToken);

    /// <summary>
    /// Loads eras, each through a given day, downloading up to four at once but publishing each only after the one
    /// before it, so a later era's rates never land ahead of an earlier era's in a series.
    /// </summary>
    /// <param name="eras">
    /// The eras to load, in the order to publish them, each with the last day of it the request needs.
    /// </param>
    /// <param name="cancellationToken">A token to observe while awaiting the loads.</param>
    /// <returns>A task that completes when every era started has been loaded.</returns>
    private Task LoadErasInOrderAsync(IEnumerable<(RbaEraWorkbook Era, DateOnly Through)> eras, CancellationToken cancellationToken)
    {
        // ForEachAsync starts one load at a time, in order, so each load is handed the load started before it.
        Task previous = Task.CompletedTask;
        return BoundedConcurrency.ForEachAsync(
            eras,
            BoundedConcurrency.DefaultLimit,
            unit =>
            {
                previous = LoadEraThroughAsync(unit.Era, unit.Through, previous, cancellationToken);
                return previous;
            });
    }

    /// <summary>
    /// Returns the last day of an era a request that ends on a given day needs.
    /// </summary>
    /// <param name="era">The era the request overlaps.</param>
    /// <param name="endDate">The inclusive end of the request.</param>
    /// <returns>The earlier of <paramref name="endDate" /> and the era's last day.</returns>
    private static DateOnly ThroughWithin(RbaEraWorkbook era, DateOnly endDate) =>
        era.End is { } eraEnd && eraEnd < endDate ? eraEnd : endDate;

    /// <summary>
    /// Loads an era for a request that needs it through a given day, unless the era as loaded still serves that day.
    /// </summary>
    /// <param name="era">The era to load.</param>
    /// <param name="through">The last day of the era the request needs.</param>
    /// <param name="publishAfter">A load that must finish, successfully or not, before this era is published.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the era has been loaded.</returns>
    private Task LoadEraThroughAsync(RbaEraWorkbook era, DateOnly through, Task publishAfter, CancellationToken cancellationToken)
    {
        // Coalesce concurrent loads of the same era so only one download is in flight; joiners await that shared task.
        // The shared fetch runs under a token decoupled from any caller, so one caller's cancellation cannot fault the
        // others; cancellationToken only abandons this caller's wait.
        // publishAfter is the load this caller started before this one, and the provider uses no JoinableTaskFactory,
        // so the foreign-task deadlock VSTHRD003 guards against cannot arise.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        return LoadCoalescedAsync(era.Label, () => IsEraCurrent(era, through), ct => LoadEraCoreAsync(era, through, publishAfter, ct), cancellationToken);
#pragma warning restore VSTHRD003
    }

    /// <summary>
    /// Downloads an era's table, then waits for a load to finish, so the era is published after it.
    /// </summary>
    /// <param name="era">The era to download.</param>
    /// <param name="publishAfter">
    /// The load to wait for, whether it succeeds or fails, before handing the table on.
    /// </param>
    /// <param name="cancellationToken">A token to observe while awaiting the download.</param>
    /// <returns>A task that yields the downloaded table.</returns>
    private async ValueTask<RbaRateTable> FetchEraAsync(RbaEraWorkbook era, Task publishAfter, CancellationToken cancellationToken)
    {
        RbaRateTable table = await _source.GetTableAsync(era, cancellationToken).ConfigureAwait(false);

        // publishAfter is the load this caller started before this one, and the provider uses no JoinableTaskFactory,
        // so the foreign-task deadlock VSTHRD003 guards against cannot arise.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        await publishAfter.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#pragma warning restore VSTHRD003

        return table;
    }

    /// <summary>
    /// Reports whether an era has been loaded and, as loaded, still serves a request that needs it through a given day.
    /// The caller must hold <see cref="WebRateProvider.SyncRoot" />.
    /// </summary>
    /// <param name="era">The era to test.</param>
    /// <param name="through">The last day of the era the request needs.</param>
    /// <returns>
    /// <see langword="true" /> when the era serves the request as loaded; otherwise <see langword="false" />.
    /// </returns>
    private bool IsEraCurrent(RbaEraWorkbook era, DateOnly through) =>
        _loadedEras.TryGetValue(era.Label, out LoadedUnit loaded) && loaded.Covers(through, TimeProvider.GetUtcNow());

    /// <summary>
    /// Downloads and stores a single era, re-checking the loaded set inside the single-flight section so a joiner that
    /// arrives after a prior load completes does no redundant work.
    /// </summary>
    /// <param name="era">The era to load.</param>
    /// <param name="through">The last day of the era the request needs.</param>
    /// <param name="publishAfter">A load that must finish, successfully or not, before this era is published.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the era has been loaded.</returns>
    private Task LoadEraCoreAsync(RbaEraWorkbook era, DateOnly through, Task publishAfter, CancellationToken cancellationToken) =>
        LoadFileFeedAsync(
            alreadyLoaded: () => IsEraCurrent(era, through),
            logStarting: () => Log.EraLoadStarting(_logger, _options.DownloadStartingLogLevel, era.Label),
            fetchAsync: ct => FetchEraAsync(era, publishAfter, ct),
            logExpectedFailure: ex => Log.EraLoadFailed(_logger, _options.DownloadFailedLogLevel, era.Label, ex),
            logUnexpectedFailure: ex => Log.EraLoadUnexpectedError(_logger, era.Label, ex),
            publish: (table, loadedAt) =>
            {
                // An era served from the disk cache was fetched when its entry was written.
                DateTimeOffset fetchedAt = table.CachedAtUtc ?? loadedAt;

                if (IsEraCurrent(era, through))
                    return null;

                foreach (RbaSeriesInfo info in table.GetSeriesInfo())
                    _series[info.Pair] = info;

                // Record the era as loaded only once its observations are in, so a publish that fails part-way leaves
                // it unloaded and the next request downloads it again.
                int count = AddObservations(table.EnumerateRates(), fetchedAt);
                _loadedEras[era.Label] = LoadedUnit.Create(era.End, table.GetLastDate(), fetchedAt, _options.RefreshInterval);
                return count;
            },
            logCompleted: count => Log.EraLoaded(_logger, _options.DownloadCompletedLogLevel, era.Label, count),
            cancellationToken: cancellationToken);
}
