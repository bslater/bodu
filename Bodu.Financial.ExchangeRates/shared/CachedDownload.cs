// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachedDownload.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Returns a file feed's bytes for a download unit from a byte cache, downloading and caching them on a miss: the
/// lookup, request and store each file-feed table source would otherwise write out itself.
/// </summary>
/// <remarks>
/// This type is compiled into each file-feed provider.
/// </remarks>
internal static class CachedDownload
{
    /// <summary>
    /// Returns the bytes for a download unit from the cache while they are fresh, or downloads and caches them.
    /// </summary>
    /// <typeparam name="TKey">The type identifying a download unit.</typeparam>
    /// <param name="cache">The byte cache to consult and fill.</param>
    /// <param name="key">The download unit.</param>
    /// <param name="refreshInterval">The maximum age at which a cached entry is still fresh.</param>
    /// <param name="httpClient">The client that downloads the unit on a miss.</param>
    /// <param name="url">Builds the unit's address; called only on a miss.</param>
    /// <param name="cancellationToken">A token to observe while downloading.</param>
    /// <returns>
    /// A task that yields the bytes, with the instant the cache stored them when it served them, or
    /// <see langword="null" /> when they were just downloaded.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache" />, <paramref name="httpClient" /> or <paramref name="url" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// A failed download propagates unchanged (for example as <see cref="HttpRequestException" />) and caches nothing.
    /// </remarks>
    internal static ValueTask<(byte[] Bytes, DateTimeOffset? CachedAtUtc)> GetOrDownloadAsync<TKey>(
        IByteCache<TKey> cache,
        TKey key,
        TimeSpan refreshInterval,
        HttpClient httpClient,
        Func<Uri> url,
        CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfNull(cache);
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(url);

        return cache.TryGet(key, refreshInterval, out byte[]? cached, out DateTimeOffset? cachedAtUtc)
            ? ValueTask.FromResult<(byte[] Bytes, DateTimeOffset? CachedAtUtc)>((cached, cachedAtUtc))
            : DownloadAsync(cache, key, httpClient, url(), cancellationToken);
    }

    /// <summary>
    /// Downloads a unit's bytes and caches them.
    /// </summary>
    /// <typeparam name="TKey">The type identifying a download unit.</typeparam>
    /// <param name="cache">The byte cache to fill.</param>
    /// <param name="key">The download unit.</param>
    /// <param name="httpClient">The client that downloads the unit.</param>
    /// <param name="url">The unit's address.</param>
    /// <param name="cancellationToken">A token to observe while downloading.</param>
    /// <returns>A task that yields the downloaded bytes, with no cached time.</returns>
    private static async ValueTask<(byte[] Bytes, DateTimeOffset? CachedAtUtc)> DownloadAsync<TKey>(
        IByteCache<TKey> cache,
        TKey key,
        HttpClient httpClient,
        Uri url,
        CancellationToken cancellationToken)
    {
        byte[] bytes = await httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
        cache.Store(key, bytes);

        return (bytes, null);
    }
}
