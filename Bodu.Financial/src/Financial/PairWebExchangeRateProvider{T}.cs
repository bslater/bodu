// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairWebExchangeRateProvider{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

using Bodu.Financial.Currencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Provides the shared machinery for a <see cref="WebExchangeRateProvider" /> that fetches one currency pair per
/// request from a remote feed — per-pair coverage tracking, single-flight request coalescing, the fetch-and-accumulate
/// orchestration, and the diagnostic logging — leaving a derived type to supply only the feed identity and the
/// feed-specific exception text. The actual fetch and parse are delegated to an
/// <see cref="IExchangeRatePairSource{TSeries}" />.
/// </summary>
/// <typeparam name="TSeries">
/// The source-specific series-metadata type surfaced through <see cref="GetAvailablePairs" />.
/// </typeparam>
/// <remarks>
/// <para>
/// This base sits between <see cref="WebExchangeRateProvider" /> (which owns the in-memory accumulator, the immutable
/// snapshot, and the full lookup matrix) and a concrete pair-based source such as Yahoo Finance or OFX. It implements
/// <see cref="WebExchangeRateProvider.EnsureLoadedAsync" /> and <see cref="WebExchangeRateProvider.IsLoaded" /> with a
/// gap-respecting per-pair <see cref="DateRangeCoverage" /> set so a request that straddles an unfetched interior gap
/// is correctly treated as uncovered and re-fetched, and coalesces concurrent fetches of the same pair-and-window so
/// only one request is in flight.
/// </para>
/// <para>
/// A derived type supplies <see cref="WebExchangeRateProvider.ProviderId" /> and may override
/// <see cref="WebExchangeRateProvider.CreateRangeInvertedException" />,
/// <see cref="WebExchangeRateProvider.FormatRateNotFound" />, and <see cref="FormatPairForLog" /> to present
/// feed-specific exception types, messages, and log labels.
/// </para>
/// </remarks>
public abstract class PairWebExchangeRateProvider<TSeries>
    : WebExchangeRateProvider
{
    /// <summary>The source that fetches and parses a pair's observations.</summary>
    private readonly IExchangeRatePairSource<TSeries> _source;

    /// <summary>The provider options.</summary>
    private readonly WebExchangeRateProviderOptions _options;

    /// <summary>The logger that records pair downloads and on-demand network fetches.</summary>
    private readonly ILogger _logger;

    /// <summary>The set of inclusive date ranges fetched so far for each pair. A gap-respecting coverage set is used rather than a single <c>(min, max)</c> envelope so a request that straddles an unfetched interior gap is correctly treated as uncovered and re-fetched.</summary>
    private readonly Dictionary<ExchangeRatePair, DateRangeCoverage> _coverage = new();

    /// <summary>The discovered series, keyed by pair.</summary>
    private readonly Dictionary<ExchangeRatePair, TSeries> _series = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PairWebExchangeRateProvider{TSeries}" /> class.
    /// </summary>
    /// <param name="source">The source that fetches and parses a pair's observations.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">
    /// The logger that records pair downloads and on-demand network fetches. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <param name="ownedHttpClient">
    /// The HTTP client this provider should own and dispose, or <see langword="null" /> when the client is
    /// caller-supplied.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for the undated surfaces. <see langword="null" /> selects
    /// <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected PairWebExchangeRateProvider(
        IExchangeRatePairSource<TSeries> source,
        WebExchangeRateProviderOptions options,
        ILogger? logger,
        HttpClient? ownedHttpClient,
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
    protected sealed override bool AllowSynchronousNetworkAccess => _options.AllowSynchronousNetworkAccess;

    /// <inheritdoc />
    protected sealed override TimeSpan DefaultLookback => _options.DefaultLookback;

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
    /// <exception cref="ArgumentException">Thrown when an ISO code is invalid or the range is inverted.</exception>
    public Task LoadPairAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(fromIsoCode);
        ThrowHelper.ThrowIfNull(toIsoCode);
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);

        ExchangeRatePair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        return EnsureLoadedAsync(pair, startDate, endDate, cancellationToken).AsTask();
    }

    /// <summary>
    /// Gets the currency pairs fetched so far.
    /// </summary>
    /// <returns>A snapshot of the discovered series, one per currency pair.</returns>
    public IReadOnlyCollection<TSeries> GetAvailablePairs()
    {
        lock (SyncRoot)
        {
            return _series.Values.ToArray();
        }
    }

    /// <inheritdoc />
    protected sealed override ValueTask EnsureLoadedAsync(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            if (IsRangeCovered(pair, startDate, endDate))
                return ValueTask.CompletedTask;
        }

        // Coalesce concurrent fetches of the same pair-and-window so only one request is in flight. The shared fetch
        // runs under a token decoupled from any caller, so one caller's cancellation cannot fault the others;
        // cancellationToken only abandons this caller's wait.
        return new ValueTask(LoadCoalescedAsync(
            $"{pair.From}/{pair.To}:{startDate.DayNumber}-{endDate.DayNumber}",
            ct => LoadPairCoreAsync(pair, startDate, endDate, ct),
            cancellationToken));
    }

    /// <inheritdoc />
    protected sealed override bool IsLoaded(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate)
    {
        lock (SyncRoot)
        {
            return IsRangeCovered(pair, startDate, endDate);
        }
    }

    /// <inheritdoc />
    protected sealed override void OnObservationIngested(ExchangeRate rate) =>
        WebExchangeRateProviderLog.ObservationIngested(_logger, _options.ObservationIngestedLogLevel, rate.From.ToString(), rate.To.ToString(), rate.Date, rate.Rate);

    /// <inheritdoc />
    protected sealed override void OnSynchronousNetworkFetch(DateOnly date) =>
        WebExchangeRateProviderLog.SynchronousNetworkFetch(_logger, _options.SynchronousNetworkFetchLogLevel, date);

    /// <summary>
    /// Formats the label used for a pair in the download log messages. The default returns the <c>FROM/TO</c> ISO-code
    /// pair; a derived type may override it to log a feed-specific identifier such as a ticker.
    /// </summary>
    /// <param name="pair">The pair being logged.</param>
    /// <returns>The label to log.</returns>
    protected virtual string FormatPairForLog(ExchangeRatePair pair) =>
        $"{pair.From}/{pair.To}";

    /// <summary>
    /// Fetches and stores a pair's observations for the inclusive window, re-checking coverage inside the single-flight
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

        string label = FormatPairForLog(pair);
        ExchangeRatePairRequest request = new(pair, startDate, endDate);

        WebExchangeRateProviderLog.PairLoadStarting(_logger, _options.DownloadStartingLogLevel, label);

        PairRateData<TSeries> data;
        try
        {
            data = await _source.GetPairAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            WebExchangeRateProviderLog.PairLoadFailed(_logger, _options.DownloadFailedLogLevel, label, ex);
            throw;
        }

        // Capture the load instant immediately after the download completes so it stamps every rate this pair produces.
        DateTimeOffset fetchedAt = TimeProvider.GetUtcNow();

        lock (SyncRoot)
        {
            _series[data.Pair] = data.Series;

            int count = AddObservations(EnumerateRates(data), fetchedAt);
            ExtendCoveredRange(pair, startDate, endDate);
            RebuildSnapshot();

            WebExchangeRateProviderLog.PairLoaded(_logger, _options.DownloadCompletedLogLevel, label, count);
        }
    }

    /// <summary>
    /// Enumerates the parsed observations as <see cref="ExchangeRate" /> values stamped with this provider's
    /// identifier.
    /// </summary>
    /// <param name="data">The parsed pair data.</param>
    /// <returns>One <see cref="ExchangeRate" /> per observation.</returns>
    private IEnumerable<ExchangeRate> EnumerateRates(PairRateData<TSeries> data)
    {
        foreach (ExchangeRateObservation observation in data.Observations)
        {
            yield return new ExchangeRate(
                data.Pair.From,
                data.Pair.To,
                observation.Date,
                observation.Rate,
                ProviderId);
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
