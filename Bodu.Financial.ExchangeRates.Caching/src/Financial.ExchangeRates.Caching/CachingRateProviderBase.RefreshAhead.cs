// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderBase.RefreshAhead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <content> The refresh-ahead (stale-while-revalidate) machinery: an aged cache hit is still served immediately, but
/// when the served data is older than <see cref="CachingRateOptions.RefreshAheadFraction" /> of its effective expiry,
/// at most one background refresh per pair and window is scheduled so the data is refetched before it expires and no
/// caller ever absorbs the refetch latency. </content>
public abstract partial class CachingRateProviderBase
{
    /// <summary>The pending background refreshes, keyed by lookup shape, pair, and window, so concurrent aged hits for the same key join the one in-flight refresh rather than duplicating it. Entries are removed when their task completes.</summary>
    private readonly ConcurrentDictionary<RefreshKey, Task> _pendingRefreshAhead = new();

    /// <summary>
    /// Schedules a background refresh of a single-date serve when refresh-ahead is enabled and the served data has aged
    /// past the configured fraction of the pair's effective expiry.
    /// </summary>
    /// <param name="duration">The configured caching duration in effect for the serving lookup.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The served date.</param>
    /// <param name="options">The lookup rules the serving lookup applied, reused by the refresh.</param>
    /// <param name="now">The serving lookup's instant.</param>
    /// <param name="servedCachedAtUtc">
    /// The cache instant of the served data, or <see langword="null" /> when no cached row backs the serve — in which
    /// case there is no age to evaluate and nothing is scheduled.
    /// </param>
    /// <remarks>
    /// The threshold is evaluated against the requested pair's effective (post-jitter) expiry even when the serve was
    /// resolved from the inverse pair's rows — a deliberate approximation that keeps the trigger cheap; the refresh
    /// itself fetches and stores the requested orientation, exactly as a miss would.
    /// </remarks>
    private void MaybeScheduleRefreshAhead(TimeSpan duration, string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, DateTimeOffset now, DateTimeOffset? servedCachedAtUtc)
    {
        if (_options.RefreshAheadFraction <= 0 || servedCachedAtUtc is not { } servedAt || _disposed)
            return;

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        if (!IsPastRefreshThreshold(pair, duration, servedAt, now))
            return;

        ScheduleRefreshAhead(
            new RefreshKey(IsRange: false, pair, date, date),
            fromIsoCode,
            toIsoCode,
            () => RefreshSingleAsync(duration, fromIsoCode, toIsoCode, date, options));
    }

    /// <summary>
    /// Schedules a background refresh of a covered range serve when refresh-ahead is enabled and the oldest served row
    /// has aged past the configured fraction of the pair's effective expiry.
    /// </summary>
    /// <param name="duration">The configured caching duration in effect for the serving lookup.</param>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the served window.</param>
    /// <param name="endDate">The inclusive end of the served window.</param>
    /// <param name="now">The serving lookup's instant.</param>
    /// <param name="oldestCachedAtUtc">
    /// The oldest cache instant among the served rows, or <see langword="null" /> for a covered-but-empty window —
    /// which carries no age and schedules nothing.
    /// </param>
    private void MaybeScheduleRangeRefreshAhead(TimeSpan duration, CurrencyPair pair, string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, DateTimeOffset now, DateTimeOffset? oldestCachedAtUtc)
    {
        if (_options.RefreshAheadFraction <= 0 || oldestCachedAtUtc is not { } servedAt || _disposed)
            return;

        if (!IsPastRefreshThreshold(pair, duration, servedAt, now))
            return;

        ScheduleRefreshAhead(
            new RefreshKey(IsRange: true, pair, startDate, endDate),
            fromIsoCode,
            toIsoCode,
            () => RefreshRangeAsync(duration, pair, fromIsoCode, toIsoCode, startDate, endDate));
    }

    /// <summary>
    /// Determines whether served data has aged past the refresh-ahead threshold: the configured fraction of the pair's
    /// effective (post-jitter) expiry.
    /// </summary>
    /// <param name="pair">The pair whose effective expiry governs the threshold.</param>
    /// <param name="duration">The configured caching duration.</param>
    /// <param name="servedCachedAtUtc">The cache instant of the served data.</param>
    /// <param name="now">The serving lookup's instant.</param>
    /// <returns><see langword="true" /> when the served data's age has reached the threshold.</returns>
    /// <remarks>
    /// Because <see cref="CachingRateOptions.RefreshAheadFraction" /> is below one, the threshold always precedes the
    /// effective expiry, so a continuously hot pair refreshes before it can ever miss.
    /// </remarks>
    private bool IsPastRefreshThreshold(CurrencyPair pair, TimeSpan duration, DateTimeOffset servedCachedAtUtc, DateTimeOffset now)
    {
        TimeSpan effective = EffectiveExpiry(pair, duration);
        var threshold = TimeSpan.FromTicks((long)(effective.Ticks * _options.RefreshAheadFraction));
        return now - servedCachedAtUtc >= threshold;
    }

    /// <summary>
    /// Registers and starts the background refresh for a key, unless one is already pending for it.
    /// </summary>
    /// <param name="key">The lookup shape, pair, and window identifying the refresh.</param>
    /// <param name="fromIsoCode">The source-currency ISO code, for diagnostics.</param>
    /// <param name="toIsoCode">The destination-currency ISO code, for diagnostics.</param>
    /// <param name="refresh">The refresh body to run in the background.</param>
    /// <remarks>
    /// The pending registration is published <em>before</em> the task starts — a completion source's task is added
    /// under the key first, and only a successful add spawns the worker — so a concurrent aged hit can never slip in
    /// between a worker finishing and its registration appearing, and the finally-removal can never race a fresh add of
    /// the same attempt. Failures are swallowed after logging: the hit that triggered the refresh was already served,
    /// and the next aged hit schedules a fresh attempt. Disposal is re-checked when the worker starts; pending
    /// refreshes are abandoned, not drained, by <see cref="Dispose()" />.
    /// </remarks>
    private void ScheduleRefreshAhead(RefreshKey key, string fromIsoCode, string toIsoCode, Func<ValueTask> refresh)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRefreshAhead.TryAdd(key, completion.Task))
            return;

        Log.RefreshAheadScheduled(_logger, _options.CacheHitLogLevel, _cache.Provider, fromIsoCode, toIsoCode, key.Start, key.End);

        // Fire-and-forget by design: the task is observed through the pending registry (and the test hook), and its
        // body routes every outcome to the meter and log rather than throwing.
        _ = Task.Run(async () =>
        {
            try
            {
                if (_disposed)
                    return;

                await refresh().ConfigureAwait(false);
                CachingMeter.RefreshAhead(_cache.Provider, "success");
            }
            catch (Exception ex)
            {
                CachingMeter.RefreshAhead(_cache.Provider, "failed");
                Log.RefreshAheadFailed(_logger, _cache.Provider, fromIsoCode, toIsoCode, key.Start, key.End, ex);
            }
            finally
            {
                _pendingRefreshAhead.TryRemove(key, out _);
                completion.SetResult();
            }
        });
    }

    /// <summary>
    /// Refetches a single date from the inner provider and stores the result, mirroring the single-date miss path.
    /// </summary>
    /// <param name="duration">The configured caching duration the stored row's expiry derives from.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The date to refresh.</param>
    /// <param name="options">The lookup rules the serving lookup applied.</param>
    /// <returns>A task representing the refresh.</returns>
    /// <remarks>
    /// The advertised-history guard is re-evaluated against a fresh instant: a date that has meanwhile fallen outside
    /// the inner's advertised history is a successful no-op rather than a doomed fetch.
    /// </remarks>
    private async ValueTask RefreshSingleAsync(TimeSpan duration, string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (IsOutsideAdvertisedHistory(date, options, now, out _))
            return;

        RateLookupResult result = await Inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, CancellationToken.None).ConfigureAwait(false);
        await StoreResultOnEitherSeamAsync(duration, fromIsoCode, toIsoCode, result, now, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Refetches a range window from the inner provider and stores the rows and coverage atomically, mirroring the
    /// range miss path including the advertised-history clamp.
    /// </summary>
    /// <param name="duration">The configured caching duration the stored rows' expiry derives from.</param>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the window to refresh.</param>
    /// <param name="endDate">The inclusive end of the window to refresh.</param>
    /// <returns>A task representing the refresh.</returns>
    private async ValueTask RefreshRangeAsync(TimeSpan duration, CurrencyPair pair, string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (!TryClampFetchStart(startDate, endDate, now, out DateOnly fetchStart, out _))
        {
            // The whole window has meanwhile fallen outside the advertised history: refresh the coverage record
            // (empty, like the miss path's skip) instead of issuing a doomed fetch.
            _ = await StoreFetchedRangeOnEitherSeamAsync(duration, pair, [], startDate, endDate, now, CancellationToken.None).ConfigureAwait(false);
            return;
        }

        IReadOnlyList<ExchangeRate> fetched =
            await Inner.GetRatesAsync(fromIsoCode, toIsoCode, fetchStart, endDate, CancellationToken.None).ConfigureAwait(false);

        _ = await StoreFetchedRangeOnEitherSeamAsync(duration, pair, fetched, startDate, endDate, now, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for every currently pending background refresh to complete, so tests can deterministically observe
    /// refresh-ahead side effects. Refreshes are registered synchronously inside the serving call, so a pending refresh
    /// scheduled by a completed lookup is always visible here.
    /// </summary>
    /// <returns>A task that completes when every refresh pending at the call instant has finished.</returns>
    internal Task WaitForRefreshAheadForTestingAsync() =>
        Task.WhenAll(_pendingRefreshAhead.Values.ToArray());

    /// <summary>
    /// Identifies one schedulable refresh: the lookup shape, the requested pair, and the served window (a single-date
    /// serve uses the date as both bounds).
    /// </summary>
    /// <param name="IsRange">Whether the refresh replays a range lookup rather than a single-date lookup.</param>
    /// <param name="Pair">The requested currency pair.</param>
    /// <param name="Start">The inclusive first date of the served window.</param>
    /// <param name="End">The inclusive last date of the served window.</param>
    private readonly record struct RefreshKey(bool IsRange, CurrencyPair Pair, DateOnly Start, DateOnly End);
}
