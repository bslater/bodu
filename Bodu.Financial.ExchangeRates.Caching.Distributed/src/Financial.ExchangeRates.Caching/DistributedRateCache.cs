// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Bodu.Financial.Currencies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateCache" /> that persists a single provider's rates and fetch-coverage windows in an injected
/// <see cref="IDistributedCache" /> — for example a Redis cache — expiring them through the same freshness mechanism as
/// the in-memory, TOML, and SQLite caches.
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
/// time. The freshness, validity, merge, and coverage rules are delegated to the shared <see cref="RateCacheRules" />
/// so this backend stays behaviourally identical to the in-memory, file, and SQLite caches; this class contributes only
/// its blob storage and locking. The two halves of a pair's state are written independently through
/// <see cref="Store" /> and <see cref="RecordCoverage" /> — storing rates never drops recorded coverage, and recording
/// coverage never drops cached rows — by reading the existing blob, replacing only the affected half, and writing the
/// merged blob back. <see cref="StoreFetchedRange" /> instead replaces both halves in one blob write.
/// </para>
/// <para>
/// The cache is best-effort. An <see cref="IDistributedCache" /> offers no atomic read-modify-write, so the per-pair
/// blob is read, modified, and written back: same-process races are prevented by a per-pair in-process lock guarding
/// <see cref="Store" />, <see cref="RecordCoverage" />, and <see cref="StoreFetchedRange" />, but <em>cross-process</em>
/// concurrent writes to the same pair are last-write-wins, consistent with the documented best-effort nature of the
/// contract. Because both halves of a fetched range travel in one blob, a single <see cref="StoreFetchedRange" /> set
/// is all-or-nothing: the reader never observes coverage without its rows even across processes. As required by
/// <see cref="IRateCache" />, a backing-store failure surfaces as an empty read or a skipped write rather than an
/// exception: <see cref="IDistributedCache" /> faults and JSON (de)serialization faults degrade gracefully, while
/// argument validation still throws.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // A distributed caching provider backed by a Redis IDistributedCache resolved from DI.
/// var options = new DistributedRateCacheOptions { Provider = "RBA" };
/// var cache = new DistributedRateCache(redisDistributedCache, options);
/// IDatedRateProvider cached = new CachingRateProvider(rba, cache, new CachingRateOptions());
///]]>
/// </code>
/// </example>
public sealed class DistributedRateCache
    : IRateCache
{
    /// <summary>The serializer options used for every read and write so the wire format is stable and culture-independent.</summary>
    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The fixed currency pair used only to build a sentinel key for the startup probe under <see cref="RateCacheOptions.ValidateStorageOnStart" />; it is read (and the result discarded), never written, so its value is irrelevant.</summary>
    private static readonly CurrencyPair s_probePair = new(CurrencyCode.USD, CurrencyCode.USD);

    /// <summary>The backing distributed cache the per-pair blobs are read from and written to.</summary>
    private readonly IDistributedCache _cache;

    /// <summary>The validated options carrying the bound provider and the optional key prefix.</summary>
    private readonly DistributedRateCacheOptions _options;

    /// <summary>The striped per-pair locks guarding the read-modify-write sequences in <see cref="Store" />, <see cref="RecordCoverage" />, and <see cref="StoreFetchedRange" />. One lock object is created per pair on first use and reused thereafter.</summary>
    private readonly ConcurrentDictionary<CurrencyPair, object> _pairLocks = new();

    /// <summary>The logger that receives the best-effort degradation warnings; never <see langword="null" />.</summary>
    private readonly ILogger _logger;

    /// <summary>The time source the warning rate-limiting is measured against, so the cooldown is deterministic under test.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The minimum interval between swallowed-failure warnings; failures inside the window only increment the suppressed count.</summary>
    private static readonly TimeSpan s_warnCooldown = TimeSpan.FromMinutes(1);

    /// <summary>The UTC tick timestamp of the last emitted warning, or <see cref="long.MinValue" /> when none has been emitted.</summary>
    private long _lastWarnUtcTicks = long.MinValue;

    /// <summary>The number of swallowed failures rate-limited away since the last emitted warning.</summary>
    private int _suppressedSinceLastWarn;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedRateCache" /> class.
    /// </summary>
    /// <param name="cache">The distributed cache the per-pair blobs are persisted in.</param>
    /// <param name="options">The options carrying the bound provider and the optional key prefix.</param>
    /// <param name="timeProvider">
    /// The time source the swallowed-failure warning rate-limiting is measured against, or <see langword="null" /> to
    /// use <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a rate-limited warning when a best-effort storage failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public DistributedRateCache(IDistributedCache cache, DistributedRateCacheOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(cache);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _cache = cache;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger.Instance;

        // Eagerly probe the backing store so an unreachable or misconfigured distributed cache surfaces here rather than
        // on the first read or write. A connectivity or configuration fault propagates from the constructor; a missing
        // probe key simply reads back null.
        if (options.ValidateStorageOnStart)
            _ = _cache.Get(_options.BuildKey(s_probePair));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedRateCache" /> class bound to a provider over the
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
    public DistributedRateCache(IDistributedCache cache, string provider)
        : this(cache, new DistributedRateCacheOptions { Provider = provider })
    {
    }

    /// <inheritdoc />
    public string Provider => _options.Provider;

    /// <summary>
    /// Gets a value indicating whether a caught backing-store or serialization fault should degrade to a best-effort
    /// fallback rather than propagate. Used as the exception filter on the read and write paths so a strict cache fails
    /// fast.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="RateCacheOptions.ThrowOnStorageFailure" /> is not set; otherwise
    /// <see langword="false" />, so the failure propagates.
    /// </value>
    private bool ShouldSwallowStorageFailure => !_options.ThrowOnStorageFailure;

    /// <inheritdoc />
    public IReadOnlyList<CachedRate> GetRates(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        IReadOnlyList<CachedRate> entries = ReadEntry(pair).Entries;
        if (entries.Count == 0)
            return Array.Empty<CachedRate>();

        return RateCacheRules.SelectFresh(entries, duration, asOf);
    }

    /// <inheritdoc />
    public void Store(CurrencyPair pair, IReadOnlyList<CachedRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        lock (LockFor(pair))
        {
            PairState state = ReadEntry(pair);

            List<CachedRate> ordered = RateCacheRules.MergeRows(state.Entries, rates, duration, asOf);

            // Preserve the existing coverage half: storing rows must never drop recorded coverage.
            WriteEntry(pair, new PairState(ordered, state.Coverage));
        }
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf) =>
        RateCacheRules.BuildCoverage(ReadEntry(pair).Coverage, duration, asOf);

    /// <inheritdoc />
    public void RecordCoverage(CurrencyPair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            PairState state = ReadEntry(pair);

            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                RateCacheRules.MergeCoverage(state.Coverage, start, end, duration, asOf);

            // Preserve the existing entries half: recording coverage must never drop cached rows.
            WriteEntry(pair, new PairState(state.Entries, windows));
        }
    }

    /// <inheritdoc />
    public RateCacheWriteStatus StoreFetchedRange(
        CurrencyPair pair,
        IReadOnlyList<CachedRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rows);
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            PairState state = ReadEntry(pair);

            // Merge both halves, then write them in one blob so a reader never observes coverage without its rows; the
            // single Set is all-or-nothing.
            List<CachedRate> ordered = RateCacheRules.MergeRows(state.Entries, rows, duration, asOf);
            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                RateCacheRules.MergeCoverage(state.Coverage, start, end, duration, asOf);

            return WriteEntry(pair, new PairState(ordered, windows))
                ? RateCacheWriteStatus.Stored
                : RateCacheWriteStatus.Failed;
        }
    }

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
        List<CachedRate> rows = new(entry.Rates.Count);
        foreach (DistributedCacheRate rate in entry.Rates)
        {
            try
            {
                // A legacy blob, or a row whose source never supplied a fetch instant, omits ObservedAtUtc and reads
                // back as a null upstream fetch instant.
                DateTimeOffset? observedAt = rate.ObservedAtUtc is { } s ? ParseInstant(s) : (DateTimeOffset?)null;
                rows.Add(new CachedRate(ParseDate(rate.Date), ParseRate(rate.Rate), ParseInstant(rate.CachedAtUtc), observedAt));
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                // Skip a single malformed row rather than failing the whole read. An out-of-range decimal rate
                // parses as an OverflowException, which must be swallowed alongside FormatException so a poisoned
                // value cannot break the documented best-effort read contract.
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
    /// Reports a swallowed best-effort storage failure to the logger at <see cref="LogLevel.Warning" />, rate-limited
    /// so at most one warning is emitted per <see cref="s_warnCooldown" /> window.
    /// </summary>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    /// <param name="exception">The swallowed storage exception.</param>
    /// <remarks>
    /// The first failure after construction, and the first after each cooldown elapses, is logged immediately and
    /// carries the count of failures suppressed since the previous warning; failures inside the window only increment
    /// that count. A single warning slot is claimed with
    /// <see cref="Interlocked.CompareExchange(ref long, long, long)" /> so that under concurrent swallows exactly one
    /// caller logs per window. The cooldown is measured against the injected <see cref="TimeProvider" /> so the
    /// rate-limiting is deterministic under test.
    /// </remarks>
    private void OnStorageFailureSwallowed(string operation, Exception exception)
    {
        long now = _timeProvider.GetUtcNow().UtcTicks;
        long last = Interlocked.Read(ref _lastWarnUtcTicks);

        // Emit when no warning has been logged yet, or the cooldown has elapsed since the last one. The MinValue sentinel
        // is checked before the subtraction so a never-warned instance does not underflow the elapsed comparison.
        bool due = last == long.MinValue || (now - last) >= s_warnCooldown.Ticks;
        if (due && Interlocked.CompareExchange(ref _lastWarnUtcTicks, now, last) == last)
        {
            int suppressed = Interlocked.Exchange(ref _suppressedSinceLastWarn, 0);
            Log.StorageFailureSwallowed(_logger, _options.Provider, operation, suppressed, exception);
        }
        else
        {
            Interlocked.Increment(ref _suppressedSinceLastWarn);
        }
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
    private PairState ReadEntry(CurrencyPair pair)
    {
        byte[]? payload;
        try
        {
            payload = _cache.Get(_options.BuildKey(pair));
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ShouldSwallowStorageFailure)
        {
            // Best-effort cache: a backing-store fault degrades to an empty read rather than breaking rate retrieval.
            // Cancellation (and fatal exceptions surfaced as OperationCanceledException) propagates rather than being
            // masked as an empty read.
            OnStorageFailureSwallowed("read", ex);
            return PairState.Empty;
        }

        if (payload is null || payload.Length == 0)
            return PairState.Empty;

        try
        {
            DistributedCacheEntry? entry = JsonSerializer.Deserialize<DistributedCacheEntry>(payload, s_serializerOptions);
            return entry is null ? PairState.Empty : Project(entry);
        }
        catch (JsonException ex) when (ShouldSwallowStorageFailure)
        {
            // A corrupt or incompatible blob degrades to an empty read rather than breaking rate retrieval.
            OnStorageFailureSwallowed("deserialize", ex);
            return PairState.Empty;
        }
    }

    /// <summary>
    /// Serializes and writes the supplied state for a pair, replacing the entire blob, or removing the key when the
    /// state is empty.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="state">The state to persist.</param>
    /// <returns>
    /// <see langword="true" /> when the blob was written or removed; <see langword="false" /> when a backing-store or
    /// serialization fault was swallowed and nothing was persisted.
    /// </returns>
    /// <remarks>
    /// A backing-store fault or a serialization fault is swallowed so a failed write does not break rate retrieval, and
    /// the failure is reported so <see cref="StoreFetchedRange" /> can signal that nothing was persisted. When both
    /// halves are empty the key is removed so the entry self-cleans rather than persisting an empty blob.
    /// </remarks>
    private bool WriteEntry(CurrencyPair pair, PairState state)
    {
        string key = _options.BuildKey(pair);

        try
        {
            if (state.Entries.Count == 0 && state.Coverage.Count == 0)
            {
                _cache.Remove(key);
                return true;
            }

            var entry = new DistributedCacheEntry();
            foreach (CachedRate row in state.Entries)
            {
                entry.Rates.Add(new DistributedCacheRate
                {
                    Date = FormatDate(row.Date),
                    Rate = FormatRate(row.Rate),
                    CachedAtUtc = FormatInstant(row.CachedAtUtc),
                    ObservedAtUtc = row.ObservedAtUtc is { } o ? FormatInstant(o) : null,
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

            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(entry, s_serializerOptions);
            _cache.Set(key, payload);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ShouldSwallowStorageFailure)
        {
            // Best-effort cache: a fault from an arbitrary IDistributedCache implementation (a network error, a
            // timeout, a disposed or misconfigured cache) or a serialization fault must degrade to a skipped write
            // rather than break rate retrieval, as the IRateCache contract requires. The exception is
            // deliberately swallowed and reported as a failure; argument validation runs before this block and still
            // throws. Cancellation (and fatal exceptions surfaced as OperationCanceledException) propagates rather than
            // being masked as a failed write.
            OnStorageFailureSwallowed("store", ex);
            return false;
        }
    }

    /// <summary>
    /// Returns the lock object guarding writes for the supplied pair, creating it on first use.
    /// </summary>
    /// <param name="pair">The currency pair whose write lock is required.</param>
    /// <returns>The per-pair lock object.</returns>
    private object LockFor(CurrencyPair pair) =>
        _pairLocks.GetOrAdd(pair, static _ => new object());
}
