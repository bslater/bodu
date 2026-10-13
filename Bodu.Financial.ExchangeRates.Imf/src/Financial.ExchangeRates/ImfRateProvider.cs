// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Serves IMF (International Monetary Fund) Representative Exchange Rates as <see cref="ExchangeRate" /> values,
/// implementing the Bodu.Financial provider contracts over the IMF's published monthly tab-separated report.
/// </summary>
/// <remarks>
/// <para>
/// The provider derives from <see cref="WebRateProvider" />, which supplies the in-memory accumulator, the immutable
/// snapshot, the full synchronous and asynchronous lookup matrix, and ownership of the <see cref="HttpClient" /> when
/// this provider creates one. Loading is month-based: one download covers every reported currency across each business
/// day of a month, so a request for any date in a month loads the whole month. Use <see cref="PreloadAsync" />,
/// <see cref="LoadMonthAsync" />, or <see cref="LoadRangeAsync" /> to warm the store.
/// </para>
/// <para>
/// <strong>USD-anchored, keyless.</strong> Every rate the report publishes quotes a currency against the US dollar, so
/// only <c>USD/X</c> and <c>X/USD</c> pairs are serviceable; a cross-currency pair is rejected. Direction is normalized
/// on ingest - labels the IMF quotes in US dollars per currency unit are inverted - so the stored rate is always units
/// of the quote currency per one US dollar. The report requires no API key.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured from <see cref="WebRateProviderOptions.UserAgent" /> and
/// <see cref="WebRateProviderOptions.HttpTimeout" />, disposing it with the provider. The constructor that takes an
/// <see cref="HttpClient" /> uses the caller-supplied client as-is; this is the path the dependency-injection package
/// uses.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var imf = new ImfRateProvider(new ImfRateProviderOptions());
/// await imf.LoadRangeAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));
///
/// RateLookupResult jpy = imf.GetRate("USD", "JPY", new DateOnly(2026, 4, 1));
/// RateLookupResult usd = imf.GetRate("JPY", "USD", new DateOnly(2026, 4, 1)); // inverted
///]]>
/// </code>
/// </example>
public sealed class ImfRateProvider
    : WebRateProvider
{
    /// <summary>The provider identifier stamped on every rate this provider produces.</summary>
    public const string ProviderName = "IMF";

    /// <summary>The base currency the IMF report quotes against.</summary>
    public const CurrencyCode BaseCurrency = CurrencyCode.USD;

    /// <summary>The source that downloads and parses report files.</summary>
    private readonly IImfRateTableSource _source;

    /// <summary>The provider options.</summary>
    private readonly ImfRateProviderOptions _options;

    /// <summary>The logger that records report downloads and on-demand network fetches.</summary>
    private readonly ILogger _logger;

    /// <summary>The report months whose data has been loaded, each with the days it can no longer revise and when its open days expire.</summary>
    private readonly Dictionary<(int Year, int Month), LoadedUnit> _loadedMonths = new();

    /// <summary>The discovered currency series, keyed by pair.</summary>
    private readonly Dictionary<CurrencyPair, ImfSeriesInfo> _series = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class backed by an <see cref="HttpClient" />
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
    public ImfRateProvider(ImfRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class backed by the IMF report, downloaded with
    /// the caller-supplied HTTP client. The caller owns the client's configuration and lifetime.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download report files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">
    /// The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public ImfRateProvider(HttpClient httpClient, ImfRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(CreateSource(httpClient, options, logger), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class backed by an explicit table source, used
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
    internal ImfRateProvider(IImfRateTableSource source, ImfRateProviderOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class from an owned client, building the table
    /// source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private ImfRateProvider(ImfRateProviderOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(CreateSource(ownedHttpClient, options, logger), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProvider" /> class, the shared core all public and internal
    /// constructors funnel through.
    /// </summary>
    /// <param name="source">The table source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private ImfRateProvider(
        IImfRateTableSource source,
        ImfRateProviderOptions options,
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
    /// The look-back is zero because the report is loaded a whole month at a time: any date in a requested window is
    /// covered by that month's single download, so no extra look-back window is needed.
    /// </remarks>
    protected override TimeSpan DefaultLookback => TimeSpan.Zero;

    /// <inheritdoc />
    public override RateHistoryAvailability HistoryAvailability => _options.HistoryAvailability;

    /// <summary>
    /// Downloads and loads the report for the current month, warming the store with its published business days.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the current month has been loaded.</returns>
    public Task PreloadAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);
        return LoadMonthAsync(new ImfReportMonth(today.Year, today.Month), cancellationToken);
    }

    /// <summary>
    /// Downloads and loads a single month's report, if it has not already been loaded.
    /// </summary>
    /// <param name="month">The report month to load.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the month has been loaded.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="month" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// A closed month's report never changes and is loaded once. A month loaded before the IMF published its last day's
    /// rates is downloaded again once <see cref="ImfRateProviderOptions.RefreshInterval" /> has passed since it was
    /// loaded.
    /// </remarks>
    public Task LoadMonthAsync(ImfReportMonth month, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(month);

        return LoadMonthThroughAsync(month, month.SelectDate, Task.CompletedTask, cancellationToken);
    }

    /// <summary>
    /// Downloads and loads every month spanning an inclusive date range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when every covering month has been loaded.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    public Task LoadRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        // The inverted range is reported through the returned task, as it was when this method awaited each month.
        if (endDate < startDate)
            return Task.FromException(CreateRangeInvertedException(startDate, endDate));

        return LoadMonthsThroughAsync(startDate, endDate, cancellationToken);
    }

    /// <summary>
    /// Gets the currency pairs discovered across the months loaded so far.
    /// </summary>
    /// <returns>A snapshot of the discovered series, one per currency pair.</returns>
    public IReadOnlyCollection<ImfSeriesInfo> GetAvailablePairs()
    {
        lock (SyncRoot)
        {
            return _series.Values.ToArray();
        }
    }

    /// <inheritdoc />
    protected override ValueTask EnsureLoadedAsync(CurrencyPair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        new(LoadMonthsThroughAsync(startDate, endDate, cancellationToken));

    /// <inheritdoc />
    protected override bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate)
    {
        lock (SyncRoot)
        {
            for (var month = new DateOnly(startDate.Year, startDate.Month, 1); month <= endDate; month = month.AddMonths(1))
            {
                ImfReportMonth reportMonth = new(month.Year, month.Month);
                if (!IsMonthCurrent(reportMonth, ThroughWithin(reportMonth, endDate)))
                    return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    protected override void ValidateRangeRequest(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        string baseIso = BaseCurrency.ToString();
        if (!string.Equals(fromIsoCode, baseIso, StringComparison.Ordinal) &&
            !string.Equals(toIsoCode, baseIso, StringComparison.Ordinal))
        {
            throw new RateSeriesNotFoundException(
                string.Format(CultureInfo.CurrentCulture, ImfResourceStrings.IO_KeyNotFound_ImfSeries, fromIsoCode, toIsoCode));
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
        string.Format(CultureInfo.CurrentCulture, ImfResourceStrings.IO_KeyNotFound_ImfRate, fromIsoCode, toIsoCode, date);

    /// <summary>
    /// Builds the default report table source from the supplied client and options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download report files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The provider's logger, forwarded to the on-disk report cache for degradation warnings.
    /// </param>
    /// <returns>A new table source.</returns>
    private static ImfReportTableSource CreateSource(HttpClient httpClient, ImfRateProviderOptions options, ILogger? logger)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        IImfReportCache cache = options.EnableDiskCache
            ? new FileSystemImfReportCache(options.CacheDirectory, logger)
            : NullImfReportCache.Instance;

        return new ImfReportTableSource(httpClient, options, cache);
    }

    /// <summary>
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the options' user agent, timeout, and
    /// response buffer cap.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    internal static HttpClient CreateOwnedClient(ImfRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return RateProviderHttpClientFactory.Create(options.UserAgent, options.HttpTimeout, options.MaxResponseContentBufferSize);
    }

    /// <summary>
    /// Returns the last day of a report month a request that ends on a given day needs.
    /// </summary>
    /// <param name="month">The report month the request overlaps.</param>
    /// <param name="endDate">The inclusive end of the request.</param>
    /// <returns>The earlier of <paramref name="endDate" /> and the month's last day.</returns>
    private static DateOnly ThroughWithin(ImfReportMonth month, DateOnly endDate) =>
        month.SelectDate < endDate ? month.SelectDate : endDate;

    /// <summary>
    /// Loads every month spanning an inclusive date range, each for the days of it the range needs, downloading up to
    /// four at once but publishing each only after the month before it, so a later month's rates never land ahead of an
    /// earlier month's in a series.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the loads.</param>
    /// <returns>A task that completes when every covering month has been loaded.</returns>
    private Task LoadMonthsThroughAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        List<ImfReportMonth> months = [];
        for (var month = new DateOnly(startDate.Year, startDate.Month, 1); month <= endDate; month = month.AddMonths(1))
            months.Add(new ImfReportMonth(month.Year, month.Month));

        // ForEachAsync starts one load at a time, in order, so each load is handed the load started before it.
        Task previous = Task.CompletedTask;
        return BoundedConcurrency.ForEachAsync(
            months,
            BoundedConcurrency.DefaultLimit,
            month =>
            {
                previous = LoadMonthThroughAsync(month, ThroughWithin(month, endDate), previous, cancellationToken);
                return previous;
            });
    }

    /// <summary>
    /// Loads a report month for a request that needs it through a given day, unless the month as loaded still serves
    /// that day.
    /// </summary>
    /// <param name="month">The report month to load.</param>
    /// <param name="through">The last day of the month the request needs.</param>
    /// <param name="publishAfter">A load that must finish, successfully or not, before this month is published.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the month has been loaded.</returns>
    private Task LoadMonthThroughAsync(ImfReportMonth month, DateOnly through, Task publishAfter, CancellationToken cancellationToken)
    {
        // Coalesce concurrent loads of the same month so only one download is in flight; joiners await that shared task.
        // The shared fetch runs under a token decoupled from any caller, so one caller's cancellation cannot fault the
        // others; cancellationToken only abandons this caller's wait.
        // publishAfter is the load this caller started before this one, and the provider uses no JoinableTaskFactory,
        // so the foreign-task deadlock VSTHRD003 guards against cannot arise.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        return LoadCoalescedAsync(month.Key, () => IsMonthCurrent(month, through), ct => LoadMonthCoreAsync(month, through, publishAfter, ct), cancellationToken);
#pragma warning restore VSTHRD003
    }

    /// <summary>
    /// Downloads a month's report table, then waits for a load to finish, so the month is published after it.
    /// </summary>
    /// <param name="month">The report month to download.</param>
    /// <param name="publishAfter">
    /// The load to wait for, whether it succeeds or fails, before handing the table on.
    /// </param>
    /// <param name="cancellationToken">A token to observe while awaiting the download.</param>
    /// <returns>A task that yields the downloaded table.</returns>
    private async ValueTask<ImfRateTable> FetchMonthAsync(ImfReportMonth month, Task publishAfter, CancellationToken cancellationToken)
    {
        ImfRateTable table = await _source.GetTableAsync(month, cancellationToken).ConfigureAwait(false);

        // publishAfter is the load this caller started before this one, and the provider uses no JoinableTaskFactory,
        // so the foreign-task deadlock VSTHRD003 guards against cannot arise.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        await publishAfter.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#pragma warning restore VSTHRD003

        return table;
    }

    /// <summary>
    /// Reports whether a report month has been loaded and, as loaded, still serves a request that needs it through a
    /// given day. The caller must hold <see cref="WebRateProvider.SyncRoot" />.
    /// </summary>
    /// <param name="month">The report month to test.</param>
    /// <param name="through">The last day of the month the request needs.</param>
    /// <returns>
    /// <see langword="true" /> when the month serves the request as loaded; otherwise <see langword="false" />.
    /// </returns>
    private bool IsMonthCurrent(ImfReportMonth month, DateOnly through) =>
        _loadedMonths.TryGetValue((month.Year, month.Month), out LoadedUnit loaded) && loaded.Covers(through, TimeProvider.GetUtcNow());

    /// <summary>
    /// Downloads and stores a single month's report, re-checking the loaded set inside the single-flight section so a
    /// joiner that arrives after a prior load completes does no redundant work.
    /// </summary>
    /// <param name="month">The report month to load.</param>
    /// <param name="through">The last day of the month the request needs.</param>
    /// <param name="publishAfter">A load that must finish, successfully or not, before this month is published.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the load.</param>
    /// <returns>A task that completes when the month has been loaded.</returns>
    private Task LoadMonthCoreAsync(ImfReportMonth month, DateOnly through, Task publishAfter, CancellationToken cancellationToken) =>
        LoadFileFeedAsync(
            alreadyLoaded: () => IsMonthCurrent(month, through),
            logStarting: () => Log.ReportLoadStarting(_logger, _options.DownloadStartingLogLevel, month.Key),
            fetchAsync: ct => FetchMonthAsync(month, publishAfter, ct),
            logExpectedFailure: ex => Log.ReportLoadFailed(_logger, _options.DownloadFailedLogLevel, month.Key, ex),
            logUnexpectedFailure: ex => Log.ReportLoadUnexpectedError(_logger, month.Key, ex),
            publish: (table, loadedAt) =>
            {
                // A report served from the disk cache was fetched when its entry was written.
                DateTimeOffset fetchedAt = table.CachedAtUtc ?? loadedAt;

                if (IsMonthCurrent(month, through))
                    return null;

                foreach (ImfSeriesInfo info in table.GetSeriesInfo())
                    _series[info.Pair] = info;

                // Record the month as loaded only once its observations are in, so a publish that fails part-way leaves
                // it unloaded and the next request downloads it again.
                int count = AddObservations(table.EnumerateRates(), fetchedAt);
                _loadedMonths[(month.Year, month.Month)] = LoadedUnit.Create(month.SelectDate, table.GetLastDate(), fetchedAt, _options.RefreshInterval);
                return count;
            },
            logCompleted: count => Log.ReportLoaded(_logger, _options.DownloadCompletedLogLevel, month.Key, count),
            cancellationToken: cancellationToken);
}
