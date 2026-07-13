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
/// An <see cref="INotableDateCache" /> that persists computed years in any
/// <see cref="IDistributedCache" /> — Redis, SQL Server, or an in-memory distributed cache — as one JSON blob per
/// territory, expiring them through the same freshness and version mechanism as the other backends.
/// </summary>
/// <remarks>
/// <para>
/// A territory's cached years are stored as a single JSON blob under a per-territory key, so a read-modify-write of a
/// territory's state is one get and one set. Because <see cref="IDistributedCache" /> offers no atomic read-modify-write,
/// each write runs under a per-territory in-process lock; concurrent writes from separate processes are last-write-wins,
/// which is acceptable for a best-effort cache. The freshness, validity, version-matching, and merge rules are delegated
/// to the shared <see cref="NotableDateCacheRules" /> so this backend stays behaviourally identical to the others.
/// </para>
/// <para>
/// As required by <see cref="INotableDateCache" />, a storage failure surfaces as an empty read or a skipped write rather
/// than an exception; cancellation is allowed to propagate. Each swallowed failure is logged at
/// <see cref="LogLevel.Warning" /> rate-limited to at most one warning per minute. Because a distributed store cannot be
/// enumerated through <see cref="IDistributedCache" />, <see cref="Clear" /> removes only the keys this instance has
/// written.
/// </para>
/// </remarks>
public sealed class DistributedNotableDateCache
    : INotableDateCache
{
    /// <summary>The minimum interval between two emitted degradation warnings.</summary>
    private static readonly TimeSpan s_warnCooldown = TimeSpan.FromMinutes(1);

    /// <summary>The serializer options for the per-territory blob.</summary>
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    /// <summary>The backing distributed store.</summary>
    private readonly IDistributedCache _cache;

    /// <summary>The validated options carrying the key prefix.</summary>
    private readonly DistributedNotableDateCacheOptions _options;

    /// <summary>The logger that receives the rate-limited degradation warnings.</summary>
    private readonly ILogger _logger;

    /// <summary>Rate-limits the degradation warning to at most one emission per cooldown window.</summary>
    private readonly RateLimitedWarningGate _warnGate;

    /// <summary>The striped per-territory locks guarding the read-modify-write of a territory's blob.</summary>
    private readonly ConcurrentDictionary<string, object> _territoryLocks = new(StringComparer.Ordinal);

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
    {
        ThrowHelper.ThrowIfNull(cache);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _cache = cache;
        _options = options;
        _warnGate = new RateLimitedWarningGate(timeProvider, s_warnCooldown);
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Gets a value indicating whether a caught storage failure should degrade to a best-effort fallback rather than
    /// propagate.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="NotableDateCacheOptions.ThrowOnStorageFailure" /> is not set.</value>
    private bool ShouldSwallowStorageFailure => !_options.ThrowOnStorageFailure;

    /// <inheritdoc />
    public NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(resourceVersion);

        string key = NotableDateCacheRules.NormalizeTerritory(territory);

        IReadOnlyList<NotableDateCacheEntry> entries = ReadTerritory(key);
        return entries.Count == 0 ? null : NotableDateCacheRules.SelectFresh(entries, year, resourceVersion, ttl, asOf);
    }

    /// <inheritdoc />
    public NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(entry);

        string key = NotableDateCacheRules.NormalizeTerritory(entry.Territory);
        var normalized = entry with { Territory = key };

        lock (LockFor(key))
        {
            List<NotableDateCacheEntry> merged = NotableDateCacheRules.Merge(ReadTerritory(key), normalized, ttl, asOf);
            return WriteTerritory(key, merged) ? NotableDateCacheWriteStatus.Stored : NotableDateCacheWriteStatus.Failed;
        }
    }

    /// <inheritdoc />
    public void Clear()
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

    /// <summary>
    /// Reads every cached year for a territory, returning an empty list when the key is absent or the read fails.
    /// </summary>
    /// <param name="territory">The normalized territory.</param>
    /// <returns>The stored entries, unfiltered, or an empty list on failure.</returns>
    private IReadOnlyList<NotableDateCacheEntry> ReadTerritory(string territory)
    {
        string cacheKey = _options.BuildKey(territory);

        try
        {
            byte[]? bytes = _cache.Get(cacheKey);
            if (bytes is null || bytes.Length == 0)
                return Array.Empty<NotableDateCacheEntry>();

            NotableDateCacheFile? file = JsonSerializer.Deserialize<NotableDateCacheFile>(bytes, s_json);
            return file is null ? Array.Empty<NotableDateCacheEntry>() : NotableDateCacheFileConverter.ToState(file).Entries;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when ((ex is JsonException || ShouldSwallowStorageFailure))
        {
            OnStorageFailureSwallowed("read", ex);
            return Array.Empty<NotableDateCacheEntry>();
        }
    }

    /// <summary>
    /// Replaces a territory's blob with the merged entries, removing the key when nothing remains.
    /// </summary>
    /// <param name="territory">The normalized territory whose blob is replaced.</param>
    /// <param name="entries">The entries to persist.</param>
    /// <returns>
    /// <see langword="true" /> when the write succeeded; <see langword="false" /> when a storage error was swallowed and
    /// nothing was persisted.
    /// </returns>
    private bool WriteTerritory(string territory, List<NotableDateCacheEntry> entries)
    {
        string cacheKey = _options.BuildKey(territory);

        try
        {
            if (entries.Count == 0)
            {
                _cache.Remove(cacheKey);
                _writtenKeys.TryRemove(cacheKey, out _);
                return true;
            }

            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(NotableDateCacheFileConverter.ToFile(new TerritoryCacheState(entries)), s_json);
            _cache.Set(cacheKey, bytes);
            _writtenKeys[cacheKey] = 0;
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when ((ex is JsonException || ShouldSwallowStorageFailure))
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

    /// <summary>
    /// Returns the lock object guarding writes for the supplied normalized territory, creating it on first use.
    /// </summary>
    /// <param name="territory">The normalized territory whose write lock is required.</param>
    /// <returns>The per-territory lock object.</returns>
    private object LockFor(string territory) =>
        _territoryLocks.GetOrAdd(territory, static _ => new object());
}
