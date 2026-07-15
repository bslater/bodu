// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderBase.Warm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <content> The warm-up surface: pre-populates the cache for a set of pairs over a date window through the normal
/// read-through path, so a deployment can pay the origin fetches up front instead of on the first user request.
/// </content>
public abstract partial class CachingRateProviderBase
{
    /// <summary>The number of pairs warmed concurrently.</summary>
    private const int WarmConcurrency = 4;

    /// <summary>
    /// Warms the cache for a set of currency pairs over a date window, fetching each pair's window through the normal
    /// range read-through path so misses populate rows and coverage while already covered pairs cost only a cache read.
    /// </summary>
    /// <param name="pairs">The currency pairs to warm, each a from/to ISO-code tuple.</param>
    /// <param name="startDate">The inclusive first date of the window to warm.</param>
    /// <param name="endDate">The inclusive last date of the window to warm.</param>
    /// <param name="cancellationToken">Cancels the warm-up.</param>
    /// <returns>The number of pairs warmed successfully.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pairs" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="endDate" /> precedes <paramref name="startDate" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is cancelled.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Up to four pairs are fetched concurrently; the shipped origin providers already single-flight their downloads,
    /// so the bounded parallelism only overlaps fetches of <em>different</em> pairs. A pair whose fetch fails is logged
    /// at <see cref="Microsoft.Extensions.Logging.LogLevel.Warning" /> and skipped — the remaining pairs still warm —
    /// and is excluded from the returned count. Cancellation is the one failure that is not swallowed: a cancelled
    /// <paramref name="cancellationToken" /> aborts the warm-up and propagates.
    /// </para>
    /// <para>
    /// Each pair goes through <see cref="GetRatesAsync" />, so a warmed window is recorded as covered exactly as a
    /// user-triggered range fetch would be — including for windows the source returns no rows for — and later range
    /// lookups of the window are cache hits until normal expiry.
    /// </para>
    /// </remarks>
    public async ValueTask<int> WarmAsync(
        IEnumerable<(string From, string To)> pairs,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowHelper.ThrowIfNull(pairs);
        if (endDate < startDate) throw new ArgumentException(CachingResourceStrings.Arg_Invalid_RangeInverted, nameof(endDate));

        (string From, string To)[] targets = pairs as (string From, string To)[] ?? pairs.ToArray();
        if (targets.Length == 0)
            return 0;

        using var throttle = new SemaphoreSlim(WarmConcurrency);
        bool[] outcomes = await Task.WhenAll(
            targets.Select(pair => WarmPairAsync(throttle, pair.From, pair.To, startDate, endDate, cancellationToken))).ConfigureAwait(false);

        return outcomes.Count(static warmed => warmed);
    }

    /// <summary>
    /// Warms one pair's window under the shared concurrency throttle, reporting whether the fetch succeeded.
    /// </summary>
    /// <param name="throttle">The semaphore bounding concurrent pair fetches.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive first date of the window to warm.</param>
    /// <param name="endDate">The inclusive last date of the window to warm.</param>
    /// <param name="cancellationToken">Cancels the fetch.</param>
    /// <returns>
    /// <see langword="true" /> when the pair warmed successfully; <see langword="false" /> when its failure was
    /// swallowed.
    /// </returns>
    private async Task<bool> WarmPairAsync(SemaphoreSlim throttle, string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _ = await GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller's cancellation aborts the warm-up as a whole rather than reading as one failed pair.
            throw;
        }
        catch (Exception ex)
        {
            Log.WarmPairFailed(_logger, _cache.Provider, fromIsoCode, toIsoCode, ex);
            return false;
        }
        finally
        {
            _ = throttle.Release();
        }
    }
}
