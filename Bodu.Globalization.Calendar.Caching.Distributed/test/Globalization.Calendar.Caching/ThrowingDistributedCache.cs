// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowingDistributedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="IDistributedCache" /> whose every operation throws, standing in for an unreachable backing store so
/// the startup storage probe can be exercised without a live distributed cache.
/// </summary>
internal sealed class ThrowingDistributedCache
    : IDistributedCache
{
    /// <summary>The message carried by every fault this cache raises.</summary>
    private const string Unavailable = "The distributed cache is unavailable.";

    /// <inheritdoc />
    public byte[]? Get(string key) =>
        throw new InvalidOperationException(Unavailable);

    /// <inheritdoc />
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        throw new InvalidOperationException(Unavailable);

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        throw new InvalidOperationException(Unavailable);

    /// <inheritdoc />
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        throw new InvalidOperationException(Unavailable);

    /// <inheritdoc />
    public void Refresh(string key) =>
        throw new InvalidOperationException(Unavailable);

    /// <inheritdoc />
    public Task RefreshAsync(string key, CancellationToken token = default) =>
        throw new InvalidOperationException(Unavailable);

    /// <inheritdoc />
    public void Remove(string key) =>
        throw new InvalidOperationException(Unavailable);

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken token = default) =>
        throw new InvalidOperationException(Unavailable);
}
