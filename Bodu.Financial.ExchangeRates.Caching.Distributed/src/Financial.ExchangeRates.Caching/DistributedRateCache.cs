// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Caching;
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
/// Because a pair's whole state travels as one blob, this backend fits the whole-state seam of
/// <see cref="RateCacheBase{TOptions}" /> exactly: the read-merge-write mechanism, per-pair locking, snapshot read, and
/// the shared <see cref="RateCacheRules" /> policy are all inherited, and this class contributes only the blob
/// serialization. A single <see cref="RateCacheBase{TOptions}.StoreFetchedRange" /> set is all-or-nothing — the reader
/// never observes coverage without its rows even across processes — while <em>cross-process</em> concurrent writes to
/// the same pair remain last-write-wins, consistent with the documented best-effort nature of the contract.
/// </para>
/// <para>
/// As required by <see cref="IRateCache" />, a backing-store failure surfaces as an empty read or a skipped write
/// rather than an exception: <see cref="IDistributedCache" /> faults and JSON (de)serialization faults degrade
/// gracefully, while argument validation still throws.
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
    : RateCacheBase<DistributedRateCacheOptions>
{
    /// <summary>The serializer options used for every read and write so the wire format is stable and culture-independent.</summary>
    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The fixed currency pair used only to build a sentinel key for the startup probe under <see cref="RateCacheOptions.ValidateStorageOnStart" />; it is read (and the result discarded), never written, so its value is irrelevant.</summary>
    private static readonly CurrencyPair s_probePair = new(CurrencyCode.USD, CurrencyCode.USD);

    /// <summary>The backing distributed cache the per-pair blobs are read from and written to.</summary>
    private readonly IDistributedCache _cache;

    /// <summary>The logger that receives the best-effort degradation warnings; never <see langword="null" />.</summary>
    private readonly ILogger _logger;

    /// <summary>Rate-limits the swallowed-failure warning to at most one emission per <see cref="RateLimitedWarningGate.DefaultCooldown" /> window.</summary>
    private readonly RateLimitedWarningGate _warnGate;

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
        : base(options)
    {
        ThrowHelper.ThrowIfNull(cache);
        ThrowHelper.ThrowIfNull(options);

        _cache = cache;
        _warnGate = new RateLimitedWarningGate(timeProvider, RateLimitedWarningGate.DefaultCooldown);
        _logger = logger ?? NullLogger.Instance;

        // Eagerly probe the backing store so an unreachable or misconfigured distributed cache surfaces here rather than
        // on the first read or write. A connectivity or configuration fault propagates from the constructor; a missing
        // probe key simply reads back null.
        if (options.ValidateStorageOnStart)
            _ = _cache.Get(Options.BuildKey(s_probePair));
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

    /// <summary>
    /// Gets a value indicating whether a caught backing-store or serialization fault should degrade to a best-effort
    /// fallback rather than propagate. Used as the exception filter on the read and write paths so a strict cache fails
    /// fast.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="RateCacheOptions.ThrowOnStorageFailure" /> is not set; otherwise
    /// <see langword="false" />, so the failure propagates.
    /// </value>
    private bool ShouldSwallowStorageFailure => !Options.ThrowOnStorageFailure;

    /// <inheritdoc />
    /// <remarks>
    /// A backing-store fault or a corrupt, undeserializable blob degrades to empty state rather than throwing, as the
    /// best-effort contract requires. A malformed individual row or window is skipped so a single corrupt value never
    /// fails the whole read.
    /// </remarks>
    internal override CachePairState ReadState(CurrencyPair pair)
    {
        byte[]? payload;
        try
        {
            payload = _cache.Get(Options.BuildKey(pair));
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ShouldSwallowStorageFailure)
        {
            // Best-effort cache: a backing-store fault degrades to an empty read rather than breaking rate retrieval.
            // Cancellation (and fatal exceptions surfaced as OperationCanceledException) propagates rather than being
            // masked as an empty read.
            OnStorageFailureSwallowed("read", ex);
            return CachePairState.Empty;
        }

        return DeserializePayload(payload);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads the blob through the backing store's asynchronous API so a network-backed read never blocks a thread-pool
    /// thread; policy, degradation, and deserialization are identical to the synchronous read.
    /// </remarks>
    internal override async ValueTask<CachePairState> ReadStateAsync(CurrencyPair pair, CancellationToken cancellationToken)
    {
        byte[]? payload;
        try
        {
            payload = await _cache.GetAsync(Options.BuildKey(pair), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ShouldSwallowStorageFailure)
        {
            // Best-effort cache: identical degradation to the synchronous read; cancellation propagates.
            OnStorageFailureSwallowed("read", ex);
            return CachePairState.Empty;
        }

        return DeserializePayload(payload);
    }

    /// <summary>
    /// Deserializes a read blob into the pair state, degrading a corrupt or incompatible blob to empty state.
    /// </summary>
    /// <param name="payload">The raw blob, or <see langword="null" /> when the key is absent.</param>
    /// <returns>The parsed state, or <see cref="CachePairState.Empty" />.</returns>
    private CachePairState DeserializePayload(byte[]? payload)
    {
        if (payload is null || payload.Length == 0)
            return CachePairState.Empty;

        try
        {
            DistributedCacheEntry? entry = JsonSerializer.Deserialize<DistributedCacheEntry>(payload, s_serializerOptions);
            return entry is null ? CachePairState.Empty : Project(entry);
        }
        catch (JsonException ex) when (ShouldSwallowStorageFailure)
        {
            // A corrupt or incompatible blob degrades to an empty read rather than breaking rate retrieval.
            OnStorageFailureSwallowed("deserialize", ex);
            return CachePairState.Empty;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Writes without a server-side expiration; reached only when a caller bypasses the duration-aware overload. The
    /// standard write paths flow through <see cref="WriteState(CurrencyPair, CachePairState, TimeSpan,
    /// DateTimeOffset)" />, which stamps the blob's absolute expiration.
    /// </remarks>
    internal override bool WriteState(CurrencyPair pair, CachePairState state) =>
        WriteCore(pair, state, entryOptions: null);

    /// <inheritdoc />
    /// <remarks>
    /// Stamps each written blob with a server-side lifetime of <paramref name="duration" /> plus
    /// <see cref="DistributedRateCacheOptions.EntryExpirationMargin" />, so a key whose pair stops being queried
    /// self-evicts from the backing store; any entry evicted at that point would already be stale on read, so served
    /// results are unchanged. The lifetime is expressed relative to the store's own clock (<see cref="DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow" />)
    /// rather than as an application-clock absolute instant, so clock skew between the application and the store — or a
    /// test-supplied synthetic clock — cannot evict entries prematurely. A <see langword="null" /> margin disables the
    /// server-side expiration entirely.
    /// </remarks>
    internal override bool WriteState(CurrencyPair pair, CachePairState state, TimeSpan duration, DateTimeOffset asOf)
    {
        DistributedCacheEntryOptions? entryOptions = Options.EntryExpirationMargin is { } margin
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration + margin }
            : null;

        return WriteCore(pair, state, entryOptions);
    }

    /// <summary>
    /// Serializes and writes a pair's whole state as one blob — all-or-nothing, so a reader never observes coverage
    /// without its rows — or removes the key when the state is empty so the entry self-cleans. A backing-store fault or
    /// a serialization fault is swallowed and reported as an unpersisted write.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="state">The state to persist.</param>
    /// <param name="entryOptions">
    /// The server-side entry options carrying the absolute expiration, or <see langword="null" /> to write without one.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the blob was persisted (or the key removed); <see langword="false" /> when a
    /// storage failure was swallowed and nothing was persisted.
    /// </returns>
    private bool WriteCore(CurrencyPair pair, CachePairState state, DistributedCacheEntryOptions? entryOptions)
    {
        string key = Options.BuildKey(pair);

        try
        {
            if (state.Entries.Count == 0 && state.Coverage.Count == 0)
            {
                _cache.Remove(key);
                return true;
            }

            byte[] payload = SerializePayload(state);
            if (entryOptions is null)
                _cache.Set(key, payload);
            else
                _cache.Set(key, payload, entryOptions);
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

    /// <inheritdoc />
    /// <remarks>
    /// Writes the blob through the backing store's asynchronous API so a network-backed write never blocks a
    /// thread-pool thread; policy, degradation, and the stamped server-side lifetime are identical to the synchronous
    /// write.
    /// </remarks>
    internal override async ValueTask<bool> WriteStateAsync(CurrencyPair pair, CachePairState state, TimeSpan duration, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        string key = Options.BuildKey(pair);

        try
        {
            if (state.Entries.Count == 0 && state.Coverage.Count == 0)
            {
                await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                return true;
            }

            byte[] payload = SerializePayload(state);
            DistributedCacheEntryOptions entryOptions = Options.EntryExpirationMargin is { } margin
                ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration + margin }
                : new DistributedCacheEntryOptions();

            await _cache.SetAsync(key, payload, entryOptions, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ShouldSwallowStorageFailure)
        {
            // Best-effort cache: identical degradation to the synchronous write; cancellation propagates.
            OnStorageFailureSwallowed("store", ex);
            return false;
        }
    }

    /// <summary>
    /// Serializes a pair's state into the stable JSON blob format shared by both write surfaces.
    /// </summary>
    /// <param name="state">The state to serialize.</param>
    /// <returns>The UTF-8 JSON payload.</returns>
    private static byte[] SerializePayload(CachePairState state)
    {
        var entry = new DistributedCacheEntry();
        foreach (CachedRate row in state.Entries)
        {
            entry.Rates.Add(new DistributedCacheRate
            {
                Date = InvariantCacheText.FormatDate(row.Date),
                Rate = InvariantCacheText.FormatDecimal(row.Rate),
                CachedAtUtc = InvariantCacheText.FormatInstant(row.CachedAtUtc),
                ObservedAtUtc = row.ObservedAtUtc is { } o ? InvariantCacheText.FormatInstant(o) : null,
            });
        }

        foreach (CoverageWindow window in state.Coverage)
        {
            entry.Coverage.Add(new DistributedCacheCoverage
            {
                Start = InvariantCacheText.FormatDate(window.Start),
                End = InvariantCacheText.FormatDate(window.End),
                FetchedAtUtc = InvariantCacheText.FormatInstant(window.FetchedAtUtc),
            });
        }

        return JsonSerializer.SerializeToUtf8Bytes(entry, s_serializerOptions);
    }

    /// <summary>
    /// Projects the persisted JSON blob into the in-memory rows and windows the base mechanism operates on, skipping
    /// any individual row or window that cannot be parsed.
    /// </summary>
    /// <param name="entry">The deserialized blob.</param>
    /// <returns>The parsed, unfiltered state.</returns>
    private static CachePairState Project(DistributedCacheEntry entry)
    {
        List<CachedRate> rows = new(entry.Rates.Count);
        foreach (DistributedCacheRate rate in entry.Rates)
        {
            try
            {
                // A legacy blob, or a row whose source never supplied a fetch instant, omits ObservedAtUtc and reads
                // back as a null upstream fetch instant.
                DateTimeOffset? observedAt = rate.ObservedAtUtc is { } s ? InvariantCacheText.ParseInstant(s) : (DateTimeOffset?)null;
                rows.Add(new CachedRate(
                    InvariantCacheText.ParseDate(rate.Date),
                    InvariantCacheText.ParseDecimal(rate.Rate),
                    InvariantCacheText.ParseInstant(rate.CachedAtUtc),
                    observedAt));
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                // Skip a single malformed row rather than failing the whole read. An out-of-range decimal rate
                // parses as an OverflowException, which must be swallowed alongside FormatException so a poisoned
                // value cannot break the documented best-effort read contract.
            }
        }

        List<CoverageWindow> windows = new(entry.Coverage.Count);
        foreach (DistributedCacheCoverage window in entry.Coverage)
        {
            try
            {
                windows.Add(new CoverageWindow(
                    InvariantCacheText.ParseDate(window.Start),
                    InvariantCacheText.ParseDate(window.End),
                    InvariantCacheText.ParseInstant(window.FetchedAtUtc)));
            }
            catch (FormatException)
            {
                // Skip a single malformed window rather than failing the whole read.
            }
        }

        return new CachePairState(rows, windows);
    }

    /// <summary>
    /// Reports a swallowed best-effort storage failure to the logger at <see cref="LogLevel.Warning" />, rate-limited
    /// so at most one warning is emitted per <see cref="RateLimitedWarningGate.DefaultCooldown" /> window.
    /// </summary>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    /// <param name="exception">The swallowed storage exception.</param>
    private void OnStorageFailureSwallowed(string operation, Exception exception)
    {
        // The counter increments on every swallow; only the log volume is rate-limited by the gate below.
        CachingDistributedMeter.StorageFailure(Options.Provider, operation);

        if (_warnGate.TryClaimWarning(out int suppressed))
            CachingDistributedLog.StorageFailureSwallowed(_logger, Options.Provider, operation, suppressed, exception);
    }
}
