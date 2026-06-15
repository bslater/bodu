// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FailingDistributedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// An <see cref="IDistributedCache" /> whose every operation throws, standing in for a backing store that is down or
/// misbehaving (a dropped Redis connection, a timeout) so the cache's graceful degradation can be exercised without a
/// real distributed cache.
/// </summary>
/// <remarks>
/// Each member throws an <see cref="InvalidOperationException" /> to model an arbitrary backing-store fault; the cache
/// under test must catch it and degrade to an empty read or a skipped write rather than letting it surface.
/// </remarks>
internal sealed class FailingDistributedCache
    : IDistributedCache
{
    /// <inheritdoc />
    public byte[]? Get(string key) =>
        throw new InvalidOperationException("The distributed cache is unavailable.");

    /// <inheritdoc />
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        throw new InvalidOperationException("The distributed cache is unavailable.");

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        throw new InvalidOperationException("The distributed cache is unavailable.");

    /// <inheritdoc />
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        throw new InvalidOperationException("The distributed cache is unavailable.");

    /// <inheritdoc />
    public void Refresh(string key) =>
        throw new InvalidOperationException("The distributed cache is unavailable.");

    /// <inheritdoc />
    public Task RefreshAsync(string key, CancellationToken token = default) =>
        throw new InvalidOperationException("The distributed cache is unavailable.");

    /// <inheritdoc />
    public void Remove(string key) =>
        throw new InvalidOperationException("The distributed cache is unavailable.");

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken token = default) =>
        throw new InvalidOperationException("The distributed cache is unavailable.");
}
