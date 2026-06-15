// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// An <see cref="IExchangeRateCache" /> that persists a single provider's rates and fetch-coverage windows in an
/// injected <see cref="IDistributedCache" /> — for example a Redis cache — expiring them through the same freshness
/// mechanism as the in-memory, TOML, and SQLite caches.
/// </summary>
/// <remarks>
/// <para>
/// The full per-pair state is stored as a single JSON blob under one stable, collision-free key,
/// <c>{prefix}{provider}:{from}{to}</c>. The blob carries both the cached rate rows and the recorded coverage windows.
/// Decimal rates are serialized as invariant strings and all dates and instants as invariant ISO text (a
/// <see cref="DateOnly" /> as <c>yyyy-MM-dd</c>, a <see cref="DateTimeOffset" /> in round-trip <c>"O"</c> form) so the
/// full precision and scale round-trips losslessly, mirroring the TOML and SQLite caches' string-decimal choice.
/// </para>
/// <para>
/// Expiry is by caching duration rather than by storage: stale and semantically invalid rows are filtered on read and
/// pruned on write, and stale coverage windows are pruned when coverage is recorded, so the entry self-cleans over
/// time. The two halves of a pair's state are written independently — storing rates never drops recorded coverage, and
/// recording coverage never drops cached rows — by reading the existing blob, replacing only the affected half, and
/// writing the merged blob back.
/// </para>
/// <para>
/// The cache is best-effort. An <see cref="IDistributedCache" /> offers no atomic read-modify-write, so the per-pair
/// blob is read, modified, and written back: same-process races are prevented by a per-pair in-process lock guarding
/// <see cref="Store" /> and <see cref="RecordCoverage" />, but <em>cross-process</em> concurrent writes to the same
/// pair are last-write-wins, consistent with the documented best-effort nature of the contract. As required by
/// <see cref="IExchangeRateCache" />, a backing-store failure surfaces as an empty read or a skipped write rather than
/// an exception: <see cref="IDistributedCache" /> faults and JSON (de)serialization faults degrade gracefully, while
/// argument validation still throws.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // A distributed caching provider backed by a Redis IDistributedCache resolved from DI.
/// var options = new DistributedExchangeRateCacheOptions { Provider = "RBA" };
/// var cache = new DistributedExchangeRateCache(redisDistributedCache, options);
/// IDatedExchangeRateProvider cached = new CachingExchangeRateProvider(rba, cache, new CachingExchangeRateOptions());
///]]>
/// </code>
/// </example>
public sealed class DistributedExchangeRateCache
    : IExchangeRateCache
{
    /// <summary>
    /// The clock-skew tolerance applied when validating a row's caching instant: a row stamped more than this far in
    /// the future of the evaluation instant is treated as invalid rather than fresh.
    /// </summary>
    private static readonly TimeSpan s_clockSkewTolerance = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The serializer options used for every read and write so the wire format is stable and culture-independent.
    /// </summary>
    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The backing distributed cache the per-pair blobs are read from and written to.
    /// </summary>
    private readonly IDistributedCache _cache;

    /// <summary>
    /// The validated options carrying the bound provider and the optional key prefix.
    /// </summary>
    private readonly DistributedExchangeRateCacheOptions _options;

    /// <summary>
    /// The striped per-pair locks guarding the read-modify-write sequences in <see cref="Store" /> and
    /// <see cref="RecordCoverage" />. One lock object is created per pair on first use and reused thereafter.
    /// </summary>
    private readonly ConcurrentDictionary<ExchangeRatePair, object> _pairLocks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedExchangeRateCache" /> class.
    /// </summary>
    /// <param name="cache">The distributed cache the per-pair blobs are persisted in.</param>
    /// <param name="options">The options carrying the bound provider and the optional key prefix.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public DistributedExchangeRateCache(IDistributedCache cache, DistributedExchangeRateCacheOptions options)
    {
        ThrowHelper.ThrowIfNull(cache);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _cache = cache;
        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedExchangeRateCache" /> class bound to a provider over the
    /// supplied distributed cache.
    /// </summary>
    /// <param name="cache">The distributed cache the per-pair blobs are persisted in.</param>
    /// <param name="provider">The provider the cache stores rates for.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache" /> is <see langword="null" />, or when <paramref name="provider" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="provider" /> is empty or white space.
    /// </exception>
    public DistributedExchangeRateCache(IDistributedCache cache, string provider)
        : this(cache, new DistributedExchangeRateCacheOptions { Provider = provider })
    {
    }

    /// <inheritdoc />
    public string Provider => _options.Provider;

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        IReadOnlyList<CachedExchangeRate> entries = ReadEntry(pair).Entries;
        if (entries.Count == 0)
            return Array.Empty<CachedExchangeRate>();

        List<CachedExchangeRate> fresh = new(entries.Count);
        foreach (CachedExchangeRate entry in entries)
        {
            if (IsValid(entry, asOf) && entry.IsFresh(asOf, duration))
                fresh.Add(entry);
        }

        fresh.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return fresh;
    }

    /// <inheritdoc />
    public void Store(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        lock (LockFor(pair))
        {
            PairState state = ReadEntry(pair);

            // Merge with any existing entry so the most recently cached rate wins per date.
            Dictionary<DateOnly, CachedExchangeRate> merged = new();
            foreach (CachedExchangeRate existing in state.Entries)
                merged[existing.Date] = existing;

            foreach (CachedExchangeRate rate in rates)
            {
                if (!IsValid(rate, asOf))
                    continue;

                if (!merged.TryGetValue(rate.Date, out CachedExchangeRate current) || rate.CachedAtUtc >= current.CachedAtUtc)
                    merged[rate.Date] = rate;
            }

            // Prune rows that are no longer fresh or are semantically invalid, then order by date so the entry is stable
            // and self-cleaning.
            List<CachedExchangeRate> ordered = new(merged.Count);
            foreach (CachedExchangeRate entry in merged.Values)
            {
                if (IsValid(entry, asOf) && entry.IsFresh(asOf, duration))
                    ordered.Add(entry);
            }

            ordered.Sort(static (left, right) => left.Date.CompareTo(right.Date));

            // Preserve the existing coverage half: storing rows must never drop recorded coverage.
            WriteEntry(pair, new PairState(ordered, state.Coverage));
        }
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        DateRangeCoverage coverage = new();
        foreach ((DateOnly start, DateOnly end, DateTimeOffset fetchedAt) in ReadEntry(pair).Coverage)
        {
            if (asOf - fetchedAt < duration)
                coverage.Add(start, end);
        }

        return coverage;
    }

    /// <inheritdoc />
    public void RecordCoverage(ExchangeRatePair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            PairState state = ReadEntry(pair);

            // Keep the still-fresh windows, drop the rest, then append the newly fetched window so the entry self-cleans.
            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> windows = new(state.Coverage.Count + 1);
            foreach ((DateOnly windowStart, DateOnly windowEnd, DateTimeOffset fetchedAt) in state.Coverage)
            {
                if (asOf - fetchedAt < duration)
                    windows.Add((windowStart, windowEnd, fetchedAt));
            }

            windows.Add((start, end, asOf));

            // Preserve the existing entries half: recording coverage must never drop cached rows.
            WriteEntry(pair, new PairState(state.Entries, windows));
        }
    }

    /// <summary>
    /// Reports whether a cached row is semantically valid against the evaluation instant.
    /// </summary>
    /// <param name="entry">The cached row to validate.</param>
    /// <param name="asOf">
    /// The instant against which the caching instant is checked for implausible future stamps.
    /// </param>
    /// <returns>
    /// <see langword="false" /> when the row carries a non-positive rate, a default (unset) date, or a caching instant
    /// implausibly far in the future of <paramref name="asOf" />; otherwise <see langword="true" />.
    /// </returns>
    /// <remarks>
    /// Invalid rows are silently skipped on both write (rejecting bad incoming data) and read (rejecting persisted or
    /// tampered rows) so a malformed cache never surfaces a nonsensical rate. A small clock-skew tolerance is allowed
    /// so a row stamped marginally ahead of the evaluating clock is not discarded.
    /// </remarks>
    private static bool IsValid(CachedExchangeRate entry, DateTimeOffset asOf) =>
        entry.Rate > 0m
            && entry.Date != default
            && entry.CachedAtUtc <= asOf + s_clockSkewTolerance;

    /// <summary>
    /// Formats a <see cref="DateOnly" /> as invariant <c>yyyy-MM-dd</c> text for storage.
    /// </summary>
    /// <param name="value">The date to format.</param>
    /// <returns>The invariant ISO date text.</returns>
    private static string FormatDate(DateOnly value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses invariant <c>yyyy-MM-dd</c> text back into a <see cref="DateOnly" />.
    /// </summary>
    /// <param name="text">The stored date text.</param>
    /// <returns>The parsed date.</returns>
    private static DateOnly ParseDate(string text) =>
        DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a <see cref="DateTimeOffset" /> as invariant round-trip (<c>"O"</c>) text for storage.
    /// </summary>
    /// <param name="value">The instant to format.</param>
    /// <returns>The invariant round-trip text.</returns>
    private static string FormatInstant(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses invariant round-trip (<c>"O"</c>) text back into a <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="text">The stored instant text.</param>
    /// <returns>The parsed instant.</returns>
    private static DateTimeOffset ParseInstant(string text) =>
        DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    /// <summary>
    /// Formats a decimal rate as invariant text for storage so its scale and precision round-trips losslessly.
    /// </summary>
    /// <param name="value">The rate to format.</param>
    /// <returns>The invariant decimal text.</returns>
    private static string FormatRate(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses invariant decimal text back into a rate.
    /// </summary>
    /// <param name="text">The stored decimal text.</param>
    /// <returns>The parsed rate.</returns>
    private static decimal ParseRate(string text) =>
        decimal.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>
    /// Projects the persisted JSON blob into the in-memory rows and windows used by the public surface, skipping any
    /// individual row or window that cannot be parsed.
    /// </summary>
    /// <param name="entry">The deserialized blob.</param>
    /// <returns>The parsed, unfiltered state.</returns>
    /// <remarks>
    /// Returns the raw stored rows and windows without freshness filtering; the freshness policy is applied by the
    /// public surface. A malformed row or window that cannot be parsed is skipped so a single corrupt value never fails
    /// the whole read.
    /// </remarks>
    private static PairState Project(DistributedCacheEntry entry)
    {
        List<CachedExchangeRate> rows = new(entry.Rates.Count);
        foreach (DistributedCacheRate rate in entry.Rates)
        {
            try
            {
                rows.Add(new CachedExchangeRate(ParseDate(rate.Date), ParseRate(rate.Rate), ParseInstant(rate.CachedAtUtc)));
            }
            catch (FormatException)
            {
                // Skip a single malformed row rather than failing the whole read.
            }
        }

        List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> windows = new(entry.Coverage.Count);
        foreach (DistributedCacheCoverage window in entry.Coverage)
        {
            try
            {
                windows.Add((ParseDate(window.Start), ParseDate(window.End), ParseInstant(window.FetchedAtUtc)));
            }
            catch (FormatException)
            {
                // Skip a single malformed window rather than failing the whole read.
            }
        }

        return new PairState(rows, windows);
    }

    /// <summary>
    /// Reads and deserializes the persisted blob for a pair, returning empty state when none exists or the read fails.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The stored state, or <see cref="PairState.Empty" /> when none is available or the read fails.</returns>
    /// <remarks>
    /// A backing-store fault or a corrupt, undeserializable blob degrades to empty state rather than throwing, as the
    /// best-effort contract requires.
    /// </remarks>
    private PairState ReadEntry(ExchangeRatePair pair)
    {
        byte[]? payload;
        try
        {
            payload = _cache.Get(_options.BuildKey(pair));
        }
        catch (Exception)
        {
            // Best-effort cache: a backing-store fault degrades to an empty read rather than breaking rate retrieval.
            return PairState.Empty;
        }

        if (payload is null || payload.Length == 0)
            return PairState.Empty;

        try
        {
            DistributedCacheEntry? entry = JsonSerializer.Deserialize<DistributedCacheEntry>(payload, s_serializerOptions);
            return entry is null ? PairState.Empty : Project(entry);
        }
        catch (JsonException)
        {
            // A corrupt or incompatible blob degrades to an empty read rather than breaking rate retrieval.
            return PairState.Empty;
        }
    }

    /// <summary>
    /// Serializes and writes the supplied state for a pair, replacing the entire blob, or removing the key when the
    /// state is empty.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="state">The state to persist.</param>
    /// <remarks>
    /// A backing-store fault or a serialization fault is swallowed so a failed write does not break rate retrieval.
    /// When both halves are empty the key is removed so the entry self-cleans rather than persisting an empty blob.
    /// </remarks>
    private void WriteEntry(ExchangeRatePair pair, PairState state)
    {
        var key = _options.BuildKey(pair);

        try
        {
            if (state.Entries.Count == 0 && state.Coverage.Count == 0)
            {
                _cache.Remove(key);
                return;
            }

            var entry = new DistributedCacheEntry();
            foreach (CachedExchangeRate row in state.Entries)
            {
                entry.Rates.Add(new DistributedCacheRate
                {
                    Date = FormatDate(row.Date),
                    Rate = FormatRate(row.Rate),
                    CachedAtUtc = FormatInstant(row.CachedAtUtc),
                });
            }

            foreach ((DateOnly windowStart, DateOnly windowEnd, DateTimeOffset fetchedAt) in state.Coverage)
            {
                entry.Coverage.Add(new DistributedCacheCoverage
                {
                    Start = FormatDate(windowStart),
                    End = FormatDate(windowEnd),
                    FetchedAtUtc = FormatInstant(fetchedAt),
                });
            }

            var payload = JsonSerializer.SerializeToUtf8Bytes(entry, s_serializerOptions);
            _cache.Set(key, payload);
        }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception
        catch (Exception)
        {
            // Best-effort cache: a fault from an arbitrary IDistributedCache implementation (a network error, a
            // timeout, a disposed or misconfigured cache) or a serialization fault must degrade to a skipped write
            // rather than break rate retrieval, as the IExchangeRateCache contract requires. The exception is
            // deliberately swallowed; argument validation runs before this block and still throws.
        }
#pragma warning restore RCS1075
    }

    /// <summary>
    /// Returns the lock object guarding writes for the supplied pair, creating it on first use.
    /// </summary>
    /// <param name="pair">The currency pair whose write lock is required.</param>
    /// <returns>The per-pair lock object.</returns>
    private object LockFor(ExchangeRatePair pair) =>
        _pairLocks.GetOrAdd(pair, static _ => new object());
}
