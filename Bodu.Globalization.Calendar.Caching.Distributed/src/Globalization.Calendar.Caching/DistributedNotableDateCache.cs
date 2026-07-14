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
/// territory's state is one get and one set. The read-merge-write mechanism, per-territory in-process locking, and the
/// freshness, validity, version-matching, and merge rules are all inherited from
/// <see cref="NotableDateCacheBase{TOptions}" />; this class contributes only the blob storage. Because
/// <see cref="IDistributedCache" /> offers no atomic read-modify-write, concurrent writes from separate processes are
/// last-write-wins, which is acceptable for a best-effort cache.
/// </para>
/// <para>
/// As required by <see cref="INotableDateCache" />, a storage failure surfaces as an empty read or a skipped write
/// rather than an exception; cancellation is allowed to propagate. Each swallowed failure is logged at
/// <see cref="LogLevel.Warning" /> rate-limited to at most one warning per minute. Because a distributed store cannot be
/// enumerated through <see cref="IDistributedCache" />, <see cref="Clear" /> removes only the keys this instance has
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
    /// <value><see langword="true" /> when <see cref="NotableDateCacheOptions.ThrowOnStorageFailure" /> is not set.</value>
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
    protected internal override bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries)
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
            _cache.Set(cacheKey, bytes);
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
