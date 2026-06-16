// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbXmlExchangeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Obtains an ECB feed's rate table by downloading its <c>eurofxref</c> XML file (via a feed cache) and parsing it.
/// </summary>
internal sealed class EcbXmlExchangeRateTableSource
    : IEcbExchangeRateTableSource
{
    /// <summary>
    /// The HTTP client used to download feed files.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// The provider options supplying the base URL, refresh interval, and alias map.
    /// </summary>
    private readonly EcbExchangeRateOptions _options;

    /// <summary>
    /// The feed byte cache.
    /// </summary>
    private readonly IEcbFeedCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbXmlExchangeRateTableSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download feed files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="cache">The feed byte cache.</param>
    internal EcbXmlExchangeRateTableSource(HttpClient httpClient, EcbExchangeRateOptions options, IEcbFeedCache cache)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(cache);

        _httpClient = httpClient;
        _options = options;
        _cache = cache;
    }

    /// <inheritdoc />
    public async ValueTask<EcbExchangeRateTable> GetTableAsync(EcbExchangeRateFeed feed, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(feed);

        byte[] bytes = await GetFeedBytesAsync(feed, cancellationToken).ConfigureAwait(false);

        using MemoryStream stream = new(bytes, writable: false);
        return EcbExchangeRateXmlParser.Parse(stream, _options);
    }

    /// <summary>
    /// Returns the feed's bytes from the cache, downloading and caching them on a miss.
    /// </summary>
    /// <param name="feed">The feed whose file is required.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the download.</param>
    /// <returns>A task that yields the feed bytes.</returns>
    private async ValueTask<byte[]> GetFeedBytesAsync(EcbExchangeRateFeed feed, CancellationToken cancellationToken)
    {
        if (_cache.TryGet(feed, _options.RefreshInterval, out byte[]? cached))
            return cached;

        Uri url = _options.Endpoint.ResolveFeedUrl(feed);
        byte[] bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
        _cache.Store(feed, bytes);

        return bytes;
    }
}
