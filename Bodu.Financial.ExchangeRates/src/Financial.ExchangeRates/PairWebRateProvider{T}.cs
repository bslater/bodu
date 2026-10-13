// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairWebRateProvider{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Provides the shared machinery for a <see cref="WebRateProvider" /> that fetches one currency pair per request from a
/// remote feed - per-pair coverage tracking, single-flight request coalescing, the fetch-and-accumulate orchestration,
/// and the diagnostic logging - leaving a derived type to supply only the feed identity and the feed-specific exception
/// text. The actual fetch and parse are delegated to an <see cref="IPairRateSource{TSeries}" />.
/// </summary>
/// <typeparam name="TSeries">
/// The source-specific series-metadata type surfaced through <see cref="GetAvailablePairs" />.
/// </typeparam>
/// <remarks>
/// <para>
/// This base sits between <see cref="WebRateProvider" /> (which owns the in-memory accumulator, the immutable snapshot,
/// and the full lookup matrix) and a concrete pair-based source such as Yahoo Finance or OFX. It implements
/// <see cref="WebRateProvider.EnsureLoadedAsync" /> and <see cref="WebRateProvider.IsLoaded" /> with a gap-respecting
/// per-pair record of the days fetched, so a request that straddles an unfetched interior gap is correctly treated as
/// uncovered and re-fetched, and coalesces concurrent fetches of the same pair-and-window so only one request is in
/// flight.
/// </para>
/// <para>
/// <strong>Refresh.</strong> A fetched day is held for good once it is final: once it has passed (any day before the
/// UTC date of the fetch) or once the source has returned a rate for it. The other days of a fetch, today and any later
/// day the source returned no rate for, are held only for <see cref="WebRateProviderOptions.RefreshInterval" />; the
/// next request that reaches one of them after that fetches it again, so a rate the source publishes later the same day
/// is served rather than missed for the life of the provider.
/// </para>
/// <para>
/// A derived type supplies <see cref="WebRateProvider.ProviderId" /> and may override
/// <see cref="WebRateProvider.CreateRangeInvertedException" />, <see cref="WebRateProvider.FormatRateNotFound" />, and
/// <see cref="FormatPairForLog" /> to present feed-specific exception types, messages, and log labels.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial.ExchangeRates;
///
/// // A concrete pair-based source: the fetch/parse lives in an IPairRateSource<TSeries>;
/// // this type contributes only the feed identity. Coverage tracking, single-flight
/// // coalescing, and the full lookup surface come from the base classes.
/// public sealed class MyFeedRateProvider : PairWebRateProvider<MyFeedSeriesInfo>
/// {
///     public MyFeedRateProvider(MyFeedRateProviderOptions options, HttpClient httpClient)
///         : base(new MyFeedSource(httpClient, options), options, logger: null, ownedHttpClient: null, timeProvider: null)
///     {
///     }
///
///     protected override string ProviderId => "MyFeed";
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public abstract class PairWebRateProvider<TSeries>
    : WebRateProvider
{
    /// <summary>The source that fetches and parses a pair's observations.</summary>
    private readonly IPairRateSource<TSeries> _source;

    /// <summary>The provider options.</summary>
    private readonly WebRateProviderOptions _options;

    /// <summary>The logger that records pair downloads and on-demand network fetches.</summary>
    private readonly ILogger _logger;

    /// <summary>The days fetched so far for each pair: final days for good, open days until the refresh interval passes. A gap-respecting record is used rather than a single <c>(min, max)</c> envelope so a request that straddles an unfetched interior gap is correctly treated as uncovered and re-fetched.</summary>
    private readonly Dictionary<CurrencyPair, LoadedRangeCoverage> _coverage = new();

    /// <summary>The discovered series, keyed by pair.</summary>
    private readonly Dictionary<CurrencyPair, TSeries> _series = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PairWebRateProvider{TSeries}" /> class.
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
    protected PairWebRateProvider(
        IPairRateSource<TSeries> source,
        WebRateProviderOptions options,
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
    public override RateHistoryAvailability HistoryAvailability => _options.HistoryAvailability;

    /// <inheritdoc />
    protected sealed override bool AllowSynchronousNetworkAccess => _options.AllowSynchronousNetworkAccess;

    /// <inheritdoc />
    protected sealed override TimeSpan DefaultLookback => _options.DefaultLookback;

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
    protected sealed override ValueTask EnsureLoadedAsync(CurrencyPair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        // Fetch only the span of the window not yet covered for the pair, so a window that extends one already loaded (a
        // sliding on-demand look-back window, for example) fetches its new days rather than the whole window again.
        DateOnly fetchStart;
        DateOnly fetchEnd;
        lock (SyncRoot)
        {
            if (!TryGetUncoveredSpan(pair, startDate, endDate, out fetchStart, out fetchEnd))
                return ValueTask.CompletedTask;
        }

        // Coalesce concurrent fetches of the same pair-and-span so only one request is in flight. The shared fetch runs
        // under a token decoupled from any caller, so one caller's cancellation cannot fault the others;
        // cancellationToken only abandons this caller's wait.
        return new ValueTask(LoadCoalescedAsync(
            $"{pair.From}/{pair.To}:{fetchStart.DayNumber}-{fetchEnd.DayNumber}",
            () => IsRangeCovered(pair, fetchStart, fetchEnd),
            ct => LoadPairCoreAsync(pair, fetchStart, fetchEnd, ct),
            cancellationToken));
    }

    /// <inheritdoc />
    protected sealed override bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate)
    {
        lock (SyncRoot)
        {
            return IsRangeCovered(pair, startDate, endDate);
        }
    }

    /// <inheritdoc />
    protected sealed override void OnObservationIngested(ExchangeRate rate)
    {
        if (_logger.IsEnabled(_options.ObservationIngestedLogLevel))
        {
            string from = rate.From.ToString();
            string to = rate.To.ToString();
            WebRateProviderLog.ObservationIngested(_logger, _options.ObservationIngestedLogLevel, from, to, rate.Date, rate.Rate);
        }
    }

    /// <inheritdoc />
    protected sealed override void OnSynchronousNetworkFetch(DateOnly date) =>
        WebRateProviderLog.SynchronousNetworkFetch(_logger, _options.SynchronousNetworkFetchLogLevel, date);

    /// <summary>
    /// Formats the label used for a pair in the download log messages. The default returns the <c>FROM/TO</c> ISO-code
    /// pair; a derived type may override it to log a feed-specific identifier such as a ticker.
    /// </summary>
    /// <param name="pair">The pair being logged.</param>
    /// <returns>The label to log.</returns>
    protected virtual string FormatPairForLog(CurrencyPair pair) =>
        $"{pair.From}/{pair.To}";

    /// <summary>
    /// Returns the latest date a fetch returned a rate for.
    /// </summary>
    /// <param name="data">The parsed pair data.</param>
    /// <returns>
    /// The latest observation date in <paramref name="data" />, or <see langword="null" /> when it holds none.
    /// </returns>
    private static DateOnly? GetLastDate(PairRateData<TSeries> data)
    {
        DateOnly? last = null;
        foreach (RateObservation observation in data.Observations)
        {
            if (last is null || observation.Date > last.Value)
                last = observation.Date;
        }

        return last;
    }

    /// <summary>
    /// Fetches and stores a pair's observations for the inclusive window through the shared
    /// <see cref="WebRateProvider.LoadFileFeedAsync{TTable}" /> sequence, which re-checks coverage inside the
    /// single-flight section so a joiner that arrives after a prior fetch completes does no redundant work.
    /// </summary>
    /// <param name="pair">The currency pair to fetch.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the fetch.</param>
    /// <returns>A task that completes when the pair's window has been loaded.</returns>
    private Task LoadPairCoreAsync(CurrencyPair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        // The pair's label is formatted once, as the load starts, and every later entry of the load reuses it.
        string label = string.Empty;

        return LoadFileFeedAsync(
            alreadyLoaded: () => IsRangeCovered(pair, startDate, endDate),
            logStarting: () =>
            {
                label = FormatPairForLog(pair);
                WebRateProviderLog.PairLoadStarting(_logger, _options.DownloadStartingLogLevel, label);
            },
            fetchAsync: ct => _source.GetPairAsync(new CurrencyPairRequest(pair, startDate, endDate), ct),
            logExpectedFailure: ex => WebRateProviderLog.PairLoadFailed(_logger, _options.DownloadFailedLogLevel, label, ex),
            logUnexpectedFailure: ex => WebRateProviderLog.PairLoadUnexpectedError(_logger, label, ex),
            publish: (data, fetchedAt) =>
            {
                _series[data.Pair] = data.Series;

                // Days the source can still publish (today, and any later day it returned no rate for) are held only
                // until the refresh interval has passed, so a rate published after this fetch is fetched on a later
                // request.
                int count = AddObservations(EnumerateRates(data), fetchedAt);
                RecordFetchedRange(pair, startDate, endDate, GetLastDate(data), fetchedAt);
                return count;
            },
            logCompleted: count => WebRateProviderLog.PairLoaded(_logger, _options.DownloadCompletedLogLevel, label, count),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Enumerates the parsed observations as <see cref="ExchangeRate" /> values stamped with this provider's
    /// identifier.
    /// </summary>
    /// <param name="data">The parsed pair data.</param>
    /// <returns>One <see cref="ExchangeRate" /> per observation.</returns>
    private IEnumerable<ExchangeRate> EnumerateRates(PairRateData<TSeries> data)
    {
        foreach (RateObservation observation in data.Observations)
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
    /// Reports whether every day in the inclusive range is held for a pair, either for good or provisionally and not
    /// yet expired. The caller must hold <see cref="WebRateProvider.SyncRoot" />.
    /// </summary>
    /// <param name="pair">The pair to check.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns><see langword="true" /> when the range is fully covered; otherwise <see langword="false" />.</returns>
    private bool IsRangeCovered(CurrencyPair pair, DateOnly startDate, DateOnly endDate) =>
        _coverage.TryGetValue(pair, out LoadedRangeCoverage? coverage)
        && coverage.Contains(startDate, endDate, TimeProvider.GetUtcNow());

    /// <summary>
    /// Finds the span of the inclusive range not held for a pair, counting provisionally held days as held until they
    /// expire. The caller must hold <see cref="WebRateProvider.SyncRoot" />.
    /// </summary>
    /// <param name="pair">The pair to check.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="fetchStart">
    /// When this method returns <see langword="true" />, the first day not yet fetched.
    /// </param>
    /// <param name="fetchEnd">When this method returns <see langword="true" />, the last day not yet fetched.</param>
    /// <returns>
    /// <see langword="true" /> when some day of the range has not been fetched; otherwise <see langword="false" />.
    /// </returns>
    private bool TryGetUncoveredSpan(CurrencyPair pair, DateOnly startDate, DateOnly endDate, out DateOnly fetchStart, out DateOnly fetchEnd)
    {
        if (_coverage.TryGetValue(pair, out LoadedRangeCoverage? coverage))
            return coverage.TryGetUncoveredSpan(startDate, endDate, TimeProvider.GetUtcNow(), out fetchStart, out fetchEnd);

        fetchStart = startDate;
        fetchEnd = endDate;
        return true;
    }

    /// <summary>
    /// Records a fetch of the inclusive range for a pair: its final days for good, and the rest until
    /// <see cref="WebRateProviderOptions.RefreshInterval" /> has passed. The caller must hold
    /// <see cref="WebRateProvider.SyncRoot" />.
    /// </summary>
    /// <param name="pair">The pair fetched.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="lastReturnedDate">
    /// The latest date the fetch returned a rate for, or <see langword="null" /> when it returned none.
    /// </param>
    /// <param name="fetchedAtUtc">The instant the fetch completed.</param>
    private void RecordFetchedRange(CurrencyPair pair, DateOnly startDate, DateOnly endDate, DateOnly? lastReturnedDate, DateTimeOffset fetchedAtUtc)
    {
        if (!_coverage.TryGetValue(pair, out LoadedRangeCoverage? coverage))
        {
            coverage = new LoadedRangeCoverage();
            _coverage[pair] = coverage;
        }

        coverage.Add(startDate, endDate, lastReturnedDate, fetchedAtUtc, _options.RefreshInterval);
    }
}
