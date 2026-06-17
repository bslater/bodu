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
/// The provider derives from <see cref="WebExchangeRateProvider" />, which supplies the in-memory accumulator, the
/// immutable snapshot, the full synchronous and asynchronous lookup matrix, and ownership of the
/// <see cref="HttpClient" /> when this provider creates one. Yahoo serves arbitrary pairs through the
/// <c>{FROM}{TO}=X</c> ticker convention, so any pair of ISO codes can be requested directly. Use
/// <see cref="LoadPairAsync" /> to warm a pair's in-memory store.
/// </para>
/// <para>
/// <strong>HttpClient ownership.</strong> The constructor that takes only options builds and owns an
/// <see cref="HttpClient" /> configured with the options' <see cref="YahooExchangeRateOptions.UserAgent" /> and
/// <see cref="YahooExchangeRateOptions.HttpTimeout" /> (the Yahoo endpoint answers requests without a recognizable user
/// agent with <c>429 Too Many Requests</c>), disposing it with the provider. The constructor that takes an
/// <see cref="HttpClient" /> uses the caller-supplied client as-is, leaving its configuration and lifetime to the
/// caller; this is the path the dependency-injection package uses.
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
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var yahoo = new YahooExchangeRateProvider(new YahooExchangeRateOptions());
/// await yahoo.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
///
/// ExchangeRateLookupResult aud = yahoo.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
///]]>
/// </code>
/// </example>
public sealed class YahooExchangeRateProvider
    : WebExchangeRateProvider
{
    /// <summary>The provider identifier stamped on every rate this provider produces.</summary>
    public const string ProviderName = "Yahoo";

    /// <summary>The source that fetches and parses charts.</summary>
    private readonly IYahooExchangeRateChartSource _source;

    /// <summary>The provider options.</summary>
    private readonly YahooExchangeRateOptions _options;

    /// <summary>The logger that records chart downloads and on-demand network fetches.</summary>
    private readonly ILogger _logger;

    /// <summary>The set of inclusive date ranges fetched so far for each pair. A gap-respecting coverage set is used rather than a single <c>(min, max)</c> envelope so a request that straddles an unfetched interior gap is correctly treated as uncovered and re-fetched.</summary>
    private readonly Dictionary<ExchangeRatePair, DateRangeCoverage> _coverage = new();

    /// <summary>The discovered currency series, keyed by pair.</summary>
    private readonly Dictionary<ExchangeRatePair, YahooSeriesInfo> _series = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateProvider" /> class backed by an
    /// <see cref="HttpClient" /> the provider creates and owns, configured from the supplied options.
    /// </summary>
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
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public YahooExchangeRateProvider(YahooExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(options, CreateOwnedClient(options), logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateProvider" /> class backed by the Yahoo Finance
    /// chart endpoint, queried with the caller-supplied HTTP client. The caller owns the client's configuration and
    /// lifetime.
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
        : this(CreateSource(httpClient, options), options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateProvider" /> class backed by an explicit chart
    /// source, used for testing.
    /// </summary>
    /// <param name="source">The chart source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger. <see langword="null" /> selects <see cref="NullLogger.Instance" />.</param>
    /// <param name="timeProvider">
    /// The time source. <see langword="null" /> selects <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    internal YahooExchangeRateProvider(IYahooExchangeRateChartSource source, YahooExchangeRateOptions options, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(source, options, ownedHttpClient: null, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateProvider" /> class from an owned client, building
    /// the chart source over it before forwarding to the core constructor.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The HTTP client this provider creates and owns.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private YahooExchangeRateProvider(YahooExchangeRateOptions options, HttpClient ownedHttpClient, ILogger? logger, TimeProvider? timeProvider)
        : this(new YahooChartExchangeRateSource(ownedHttpClient, options), options, ownedHttpClient, logger, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateProvider" /> class, the shared core all public and
    /// internal constructors funnel through.
    /// </summary>
    /// <param name="source">The chart source.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="ownedHttpClient">The owned client to dispose with the provider, or <see langword="null" />.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time source.</param>
    private YahooExchangeRateProvider(
        IYahooExchangeRateChartSource source,
        YahooExchangeRateOptions options,
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
    protected override TimeSpan DefaultLookback => _options.DefaultLookback;

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
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);

        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);
        return EnsureLoadedAsync(pair, startDate, endDate, cancellationToken).AsTask();
    }

    /// <summary>
    /// Gets the currency pairs fetched so far.
    /// </summary>
    /// <returns>A snapshot of the discovered series, one per currency pair.</returns>
    public IReadOnlyCollection<YahooSeriesInfo> GetAvailablePairs()
    {
        lock (SyncRoot)
        {
            return _series.Values.ToArray();
        }
    }

    /// <inheritdoc />
    protected override ValueTask EnsureLoadedAsync(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            if (IsRangeCovered(pair, startDate, endDate))
                return ValueTask.CompletedTask;
        }

        // Coalesce concurrent fetches of the same pair-and-window so only one chart request is in flight. The shared
        // fetch runs under a token decoupled from any caller, so one caller's cancellation cannot fault the others;
        // cancellationToken only abandons this caller's wait.
        return new ValueTask(LoadCoalescedAsync(
            $"{pair.FromIsoCode}/{pair.ToIsoCode}:{startDate.DayNumber}-{endDate.DayNumber}",
            ct => LoadPairCoreAsync(pair, startDate, endDate, ct),
            cancellationToken));
    }

    /// <inheritdoc />
    protected override bool IsLoaded(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate)
    {
        lock (SyncRoot)
        {
            return IsRangeCovered(pair, startDate, endDate);
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
        new YahooExchangeRateDateRangeException(
            string.Format(CultureInfo.CurrentCulture, YahooResourceStrings.Arg_OutOfRange_YahooDateRange, startDate, endDate));

    /// <inheritdoc />
    protected override string FormatRateNotFound(string fromIsoCode, string toIsoCode, DateOnly date) =>
        string.Format(CultureInfo.CurrentCulture, YahooResourceStrings.IO_KeyNotFound_YahooRate, fromIsoCode, toIsoCode, date);

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
    /// Builds the <see cref="HttpClient" /> this provider owns, configured with the options' user agent and timeout.
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <returns>A new, configured client owned by the provider.</returns>
    private static HttpClient CreateOwnedClient(YahooExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        return ExchangeRateHttpClientFactory.Create(options.UserAgent, options.HttpTimeout);
    }

    /// <summary>
    /// Fetches and stores a pair's chart for the inclusive window, re-checking coverage inside the single-flight
    /// section so a joiner that arrives after a prior fetch completes does no redundant work.
    /// </summary>
    /// <param name="pair">The currency pair to fetch.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the fetch.</param>
    /// <returns>A task that completes when the pair's window has been loaded.</returns>
    private async Task LoadPairCoreAsync(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            if (IsRangeCovered(pair, startDate, endDate))
                return;
        }

        string symbol = _options.BuildSymbol(pair.FromIsoCode, pair.ToIsoCode);
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

        // Capture the load instant immediately after the download completes so it stamps every rate this pair produces.
        var fetchedAt = TimeProvider.GetUtcNow();

        lock (SyncRoot)
        {
            YahooSeriesInfo info = chart.GetSeriesInfo();
            _series[info.Pair] = info;

            int count = AddObservations(chart.EnumerateRates(), fetchedAt);
            ExtendCoveredRange(pair, startDate, endDate);
            RebuildSnapshot();

            Log.PairLoaded(_logger, _options.DownloadCompletedLogLevel, symbol, count);
        }
    }

    /// <summary>
    /// Reports whether every day in the inclusive range has already been fetched for a pair. The caller must hold
    /// <see cref="WebExchangeRateProvider.SyncRoot" />.
    /// </summary>
    /// <param name="pair">The pair to check.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns><see langword="true" /> when the range is fully covered; otherwise <see langword="false" />.</returns>
    private bool IsRangeCovered(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate) =>
        _coverage.TryGetValue(pair, out DateRangeCoverage? coverage) && coverage.Contains(startDate, endDate);

    /// <summary>
    /// Records the inclusive range as fetched for a pair, merging it into the pair's coverage set. The caller must hold
    /// <see cref="WebExchangeRateProvider.SyncRoot" />.
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
}
