// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecordingDistributedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// An in-memory <see cref="IDistributedCache" /> test double that records the
/// <see cref="DistributedCacheEntryOptions" /> supplied with every set, so the server-side expiration a cache stamps
/// onto its blobs can be asserted.
/// </summary>
internal sealed class RecordingDistributedCache : IDistributedCache
{
    /// <summary>The stored payloads, keyed by cache key.</summary>
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    /// <summary>The recorded set calls, in order.</summary>
    private readonly List<(string Key, DistributedCacheEntryOptions? Options)> _sets = new();

    /// <summary>
    /// Gets the recorded set calls: the key and the entry options supplied, <see langword="null" /> when the
    /// options-less overload was used.
    /// </summary>
    /// <value>The recorded sets, in call order.</value>
    public IReadOnlyList<(string Key, DistributedCacheEntryOptions? Options)> Sets
    {
        get
        {
            lock (_sets)
                return _sets.ToArray();
        }
    }

    /// <inheritdoc />
    public byte[]? Get(string key) => _store.TryGetValue(key, out byte[]? value) ? value : null;

    /// <inheritdoc />
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _store[key] = value;
        lock (_sets)
            _sets.Add((key, options));
    }

    /// <inheritdoc />
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Refresh(string key)
    {
    }

    /// <inheritdoc />
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    /// <inheritdoc />
    public void Remove(string key) => _store.TryRemove(key, out _);

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }
}
