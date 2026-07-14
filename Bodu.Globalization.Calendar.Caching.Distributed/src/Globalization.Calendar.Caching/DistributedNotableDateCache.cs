// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text.Json;
using Bodu.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateCache" /> that persists computed years in any <see cref="IDistributedCache" /> — Redis,
/// SQL Server, or an in-memory distributed cache — as one JSON blob per territory, expiring them through the same
/// freshness and version mechanism as the other backends.
/// </summary>
/// <remarks>
/// <para>
/// A territory's cached years are stored as a single JSON blob under a per-territory key, so a read-modify-write of a
/// territory's state is one get and one set. The read-merge-write mechanism, per-territory in-process locking, and the
/// freshness, validity, version-matching, and merge rules are all inherited from
/// <see cref="NotableDateCacheBase{TOptions}" />; this class contributes only the blob storage. Because
/// <see cref="IDistributedCache" /> offers no atomic read-modify-write, concurrent writes from separate processes are
/// last-write-wins, which is acceptable for a best-effort cache.
/// </para>
/// <para>
/// As required by <see cref="INotableDateCache" />, a storage failure surfaces as an empty read or a skipped write
/// rather than an exception; cancellation is allowed to propagate. Each swallowed failure is logged at
/// <see cref="LogLevel.Warning" /> rate-limited to at most one warning per minute. Because a distributed store cannot
/// be enumerated through <see cref="IDistributedCache" />, <see cref="Clear" /> removes only the keys this instance has
/// written.
/// </para>
/// </remarks>
public sealed class DistributedNotableDateCache
    : NotableDateCacheBase<DistributedNotableDateCacheOptions>
{
    /// <summary>The backing distributed store.</summary>
    private readonly IDistributedCache _cache;

    /// <summary>The logger that receives the rate-limited degradation warnings.</summary>
    private readonly ILogger _logger;

    /// <summary>Rate-limits the degradation warning to at most one emission per cooldown window.</summary>
    private readonly RateLimitedWarningGate _warnGate;

    /// <summary>The keys this instance has written, so <see cref="Clear" /> can remove them.</summary>
    private readonly ConcurrentDictionary<string, byte> _writtenKeys = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedNotableDateCache" /> class.
    /// </summary>
    /// <param name="cache">The backing distributed store.</param>
    /// <param name="options">The options carrying the key prefix.</param>
    /// <param name="timeProvider">
    /// The time source the degradation-warning cooldown is measured against, or <see langword="null" /> to use
    /// <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">
    /// The logger that receives the rate-limited best-effort degradation warnings, or <see langword="null" /> to leave
    /// the degradation unreported.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public DistributedNotableDateCache(IDistributedCache cache, DistributedNotableDateCacheOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
        : base(options)
    {
        ThrowHelper.ThrowIfNull(cache);

        _cache = cache;
        _warnGate = new RateLimitedWarningGate(timeProvider, RateLimitedWarningGate.DefaultCooldown);
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Gets a value indicating whether a caught storage failure should degrade to a best-effort fallback rather than
    /// propagate.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="NotableDateCacheOptions.ThrowOnStorageFailure" /> is not set.
    /// </value>
    private bool ShouldSwallowStorageFailure => !Options.ThrowOnStorageFailure;

    /// <inheritdoc />
    public override void Clear()
    {
        foreach (string cacheKey in _writtenKeys.Keys)
        {
            try
            {
                _cache.Remove(cacheKey);
                _writtenKeys.TryRemove(cacheKey, out _);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ShouldSwallowStorageFailure)
            {
                OnStorageFailureSwallowed("clear", ex);
            }
        }
    }

    /// <inheritdoc />
    protected internal override IReadOnlyList<NotableDateCacheEntry> ReadEntries(string territory)
    {
        string cacheKey = Options.BuildKey(territory);

        try
        {
            byte[]? bytes = _cache.Get(cacheKey);
            if (bytes is null || bytes.Length == 0)
                return Array.Empty<NotableDateCacheEntry>();

            NotableDateCacheFile? file = JsonSerializer.Deserialize<NotableDateCacheFile>(bytes, NotableDateCacheFileConverter.JsonOptions);
            return file is null ? Array.Empty<NotableDateCacheEntry>() : NotableDateCacheFileConverter.ToEntries(file);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException || ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("read", ex);
            return Array.Empty<NotableDateCacheEntry>();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Writes without a server-side expiration; reached only when a caller bypasses the time-to-live-aware overload.
    /// The standard <c>StoreYear</c> path flows through <see cref="WriteEntries(string,
    /// IReadOnlyList{NotableDateCacheEntry}, TimeSpan, DateTimeOffset)" />, which stamps the blob's absolute
    /// expiration.
    /// </remarks>
    protected internal override bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries) =>
        WriteCore(territory, entries, entryOptions: null);

    /// <inheritdoc />
    /// <remarks>
    /// Stamps each written territory blob with a server-side lifetime of <paramref name="ttl" /> plus
    /// <see cref="DistributedNotableDateCacheOptions.EntryExpirationMargin" />, so a key whose territory stops being
    /// queried self-evicts from the backing store; any entry evicted at that point would already be stale on read, so
    /// served results are unchanged. The lifetime is expressed relative to the store's own clock
    /// (<see cref="DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow" />) rather than as an
    /// application-clock absolute instant, so clock skew between the application and the store — or a test-supplied
    /// synthetic clock — cannot evict entries prematurely. A <see langword="null" /> margin disables the server-side
    /// expiration entirely.
    /// </remarks>
    protected internal override bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries, TimeSpan ttl, DateTimeOffset asOf) =>
        WriteCore(
            territory,
            entries,
            Options.EntryExpirationMargin is { } margin
                ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl + margin }
                : null);

    /// <summary>
    /// Serializes and writes a territory's entries as one blob, or removes the key when the entry list is empty so the
    /// blob self-cleans. A backing-store fault or a serialization fault is swallowed and reported as an unpersisted
    /// write.
    /// </summary>
    /// <param name="territory">The normalized territory key.</param>
    /// <param name="entries">The entries to persist.</param>
    /// <param name="entryOptions">
    /// The server-side entry options carrying the absolute expiration, or <see langword="null" /> to write without
    /// one.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the blob was persisted (or the key removed); <see langword="false" /> when a
    /// storage failure was swallowed and nothing was persisted.
    /// </returns>
    private bool WriteCore(string territory, IReadOnlyList<NotableDateCacheEntry> entries, DistributedCacheEntryOptions? entryOptions)
    {
        string cacheKey = Options.BuildKey(territory);

        try
        {
            if (entries.Count == 0)
            {
                _cache.Remove(cacheKey);
                _writtenKeys.TryRemove(cacheKey, out _);
                return true;
            }

            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
                NotableDateCacheFileConverter.ToFile(territory, entries),
                NotableDateCacheFileConverter.JsonOptions);
            if (entryOptions is null)
                _cache.Set(cacheKey, bytes);
            else
                _cache.Set(cacheKey, bytes, entryOptions);
            _writtenKeys[cacheKey] = 0;
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException || ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("store", ex);
            return false;
        }
    }

    /// <summary>
    /// Reports a swallowed best-effort storage failure to the logger, rate-limited to at most one warning per cooldown
    /// window.
    /// </summary>
    /// <param name="operation">The storage operation that failed.</param>
    /// <param name="exception">The swallowed storage exception.</param>
    private void OnStorageFailureSwallowed(string operation, Exception exception)
    {
        if (_warnGate.TryClaimWarning(out int suppressed))
            Log.StorageFailureSwallowed(_logger, operation, suppressed, exception);
    }
}
