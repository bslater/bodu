// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
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
/// The provider derives from <see cref="WebExchangeRateProvider" />, which supplies the in-memory accumulator, the
/// immutable snapshot, the full synchronous and asynchronous lookup matrix, and ownership of the
/// <see cref="HttpClient" /> when this provider creates one. Loading is era-based: each era is a published <c>.xls</c>
/// file covering a span of dates, and a range load fetches every era overlapping the requested window. Use
/// <see cref="PreloadAsync" />, <see cref="LoadEraAsync" />, or <see cref="LoadRangeAsync" /> to warm the store.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured from <see cref="RbaExchangeRateOptions.UserAgent" /> and
/// <see cref="RbaExchangeRateOptions.HttpTimeout" />, disposing it with the provider. The constructor that takes an
/// <see cref="HttpClient" /> uses the caller-supplied client as-is; this is the path the dependency-injection package
/// uses.
/// </para>
/// <para>
/// <strong>Logging.</strong> When an <see cref="ILogger" /> is supplied (directly or through the dependency-injection
/// package) the provider records: the start of an era download (<see cref="LogLevel.Debug" />), a completed download
/// with its observation count (<see cref="LogLevel.Information" />), each ingested observation (
/// <see cref="LogLevel.Trace" />), and a failed download (<see cref="LogLevel.Warning" />, then re-thrown). Every level
/// is configurable through the corresponding <c>*LogLevel</c> property on <see cref="RbaExchangeRateOptions" />;
/// omitting the logger selects <see cref="NullLogger.Instance" />, so logging is opt-in and free when unused.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var rba = new RbaExchangeRateProvider(new RbaExchangeRateOptions());
/// await rba.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));
///
/// RateLookupResult aud = rba.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
/// // aud.Rate.Provider == RbaExchangeRateProvider.ProviderName; the reverse direction (USD->AUD) is inverted.
///]]>
/// </code>
/// </example>
public sealed class RbaExchangeRateProvider
    : WebExchangeRateProvider
{
    /// <summary>The provider identifier stamped on every rate this provider produces.</summary>
    public const string ProviderName = "RBA";

    /// <summary>The base currency the RBA quotes against.</summary>
    public const CurrencyCode BaseCurrency = CurrencyCode.AUD;

    /// <summary>The source that downloads and parses era files.</summary>
    private readonly IRbaExchangeRateTableSource _source;

    /// <summary>The provider options.</summary>
    private readonly RbaExchangeRateOptions _options;

    /// <summary>The logger that records era downloads and on-demand network fetches.</summary>
    private readonly ILogger _logger;

    /// <summary>The labels of eras whose data has been loaded.</summary>
    private readonly HashSet<string> _loadedEras = new(StringComparer.Ordinal);

    /// <summary>The discovered currency series, keyed by pair.</summary>
    private readonly Dictionary<CurrencyPair, RbaSeriesInfo> _series = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateProvider" /> class backed by an
    /// <see cref="HttpClient" /> the provider creates and owns, configured from the supplied options.
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
    public RbaExchangeRateProvider(RbaExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateProvider" /> class backed by the RBA <c>.xls</c>
    /// files, downloaded with the caller-supplied HTTP client. The caller owns the client's configuration and lifetime.
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
    public RbaExchangeRateProvider(HttpClient httpClient, RbaExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateProvider" /> class backed by an explicit table
    /// source, used for testing.
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
    internal RbaExchangeRateProvider(IRbaExchangeRateTableSource source, RbaExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateProvider" /> class from an owned client, building
    /// the table source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private RbaExchangeRateProvider(RbaExchangeRateOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(CreateSource(ownedHttpClient, options), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateProvider" /> class, the shared core all public and
    /// internal constructors funnel through.
    /// </summary>
    /// <param name="source">The table source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private RbaExchangeRateProvider(
        IRbaExchangeRateTableSource source,
        RbaExchangeRateOptions options,
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
    /// Computed from the configured <see cref="RbaExchangeRateOptions.Eras" />: the earliest era start bounds how far
    /// back the historical workbook catalogue reaches — 1 January 1983 for the default <see cref="RbaEra.Default" />
    /// catalogue.
    /// </remarks>
    public override RateHistoryAvailability HistoryAvailability
    {
        get
        {
            DateOnly earliest = _options.Eras[0].Start;
            foreach (RbaEra era in _options.Eras)
            {
                if (era.Start < earliest)
                    earliest = era.Start;
            }

            return RateHistoryAvailability.Since(earliest);
        }
    }

    /// <inheritdoc />
    protected override TimeSpan DefaultLookback => TimeSpan.Zero;

    /// <summary>
    /// Downloads and loads every era in the configured catalogue.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while awaiting the loads.</param>
    /// <returns>A task that completes when every era has been loaded.</returns>
    public async Task PreloadAsync(CancellationToken cancellationToken = default)
    {
        foreach (RbaEra era in _options.Eras)
            await LoadEraAsync(era, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads and loads a single era, if it has not already been loaded.
    /// </summary>
    /// <param name="era">The era to load.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the era has been loaded.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="era" /> is <see langword="null" />.
    /// </exception>
    public Task LoadEraAsync(RbaEra era, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(era);

        lock (SyncRoot)
        {
            if (_loadedEras.Contains(era.Label))
                return Task.CompletedTask;
        }

        // Coalesce concurrent loads of the same era so only one download is in flight; joiners await that shared task.
        // The shared fetch runs under a token decoupled from any caller, so one caller's cancellation cannot fault the
        // others; cancellationToken only abandons this caller's wait.
        return LoadCoalescedAsync(era.Label, ct => LoadEraCoreAsync(era, ct), cancellationToken);
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
            foreach (RbaEra era in _options.Eras)
            {
                if (era.Start <= endDate && (era.End is null || era.End.Value >= startDate) && !_loadedEras.Contains(era.Label))
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
    protected override void OnObservationIngested(ExchangeRate rate) =>
        Log.ObservationIngested(_logger, _options.ObservationIngestedLogLevel, rate.From.ToString(), rate.To.ToString(), rate.Date, rate.Rate);

    /// <inheritdoc />
    protected override string FormatRateNotFound(string fromIsoCode, string toIsoCode, DateOnly date) =>
        string.Format(CultureInfo.CurrentCulture, RbaResourceStrings.IO_KeyNotFound_RbaRate, fromIsoCode, toIsoCode, date);

    /// <summary>
    /// Builds the default <c>.xls</c> table source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download era files.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>A new table source.</returns>
    private static RbaXlsExchangeRateTableSource CreateSource(HttpClient httpClient, RbaExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        IRbaWorkbookCache cache = options.EnableDiskCache
            ? new FileSystemRbaWorkbookCache(options.CacheDirectory)
            : NullRbaWorkbookCache.Instance;

        return new RbaXlsExchangeRateTableSource(httpClient, options, cache);
    }

    /// <summary>
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the options' user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(RbaExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return ExchangeRateHttpClientFactory.Create(options.UserAgent, options.HttpTimeout);
    }

    /// <summary>
    /// Loads every era overlapping the inclusive date range, skipping eras already loaded.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the loads.</param>
    /// <returns>A task that completes when the overlapping eras have been loaded.</returns>
    private async Task LoadRangeInternalAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        foreach (RbaEra era in _options.Eras)
        {
            if (era.Start <= endDate && (era.End is null || era.End.Value >= startDate))
                await LoadEraAsync(era, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Downloads and stores a single era, re-checking the loaded set inside the single-flight section so a joiner that
    /// arrives after a prior load completes does no redundant work.
    /// </summary>
    /// <param name="era">The era to load.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the era has been loaded.</returns>
    private async Task LoadEraCoreAsync(RbaEra era, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            if (_loadedEras.Contains(era.Label))
                return;
        }

        Log.EraLoadStarting(_logger, _options.DownloadStartingLogLevel, era.Label);

        RbaExchangeRateTable table;
        try
        {
            table = await _source.GetTableAsync(era, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or FormatException)
        {
            // Only the failures a fetch is expected to produce — transport, stream, and malformed-feed errors
            // (ExchangeRateFormatException derives from FormatException) — are logged as era-load failures.
            Log.EraLoadFailed(_logger, _options.DownloadFailedLogLevel, era.Label, ex);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Anything else indicates a probable bug: log it under a distinct event id at Error so it stays
            // visible in telemetry without being relabelled as an era failure, then rethrow. Cancellation
            // (including HttpClient timeouts, which surface as TaskCanceledException) is never logged.
            Log.EraLoadUnexpectedError(_logger, era.Label, ex);
            throw;
        }

        // Capture the load instant immediately after the download completes so it stamps every rate this era produces.
        DateTimeOffset fetchedAt = TimeProvider.GetUtcNow();

        lock (SyncRoot)
        {
            if (!_loadedEras.Add(era.Label))
                return;

            foreach (RbaSeriesInfo info in table.GetSeriesInfo())
                _series[info.Pair] = info;

            int count = AddObservations(table.EnumerateRates(), fetchedAt);
            RebuildSnapshot();

            Log.EraLoaded(_logger, _options.DownloadCompletedLogLevel, era.Label, count);
        }
    }
}
