// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowingDistributedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection;

/// <summary>
/// An <see cref="IDistributedCache" /> whose every operation throws, standing in for an unreachable backing store so the
/// startup storage probe can be exercised without a live distributed cache.
/// </summary>
internal sealed class ThrowingDistributedCache
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
