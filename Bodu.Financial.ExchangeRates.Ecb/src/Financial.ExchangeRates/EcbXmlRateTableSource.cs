// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbXmlRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains an ECB feed's rate table by downloading its <c>eurofxref</c> XML file (via a feed cache) and parsing it.
/// </summary>
internal sealed class EcbXmlRateTableSource
    : IEcbRateTableSource
{
    /// <summary>The HTTP client used to download feed files.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base URL, refresh interval, and alias map.</summary>
    private readonly EcbRateProviderOptions _options;

    /// <summary>The feed byte cache.</summary>
    private readonly IByteCache<EcbRateFeed> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbXmlRateTableSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download feed files.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="cache">The feed byte cache.</param>
    internal EcbXmlRateTableSource(HttpClient httpClient, EcbRateProviderOptions options, IByteCache<EcbRateFeed> cache)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(cache);

        _httpClient = httpClient;
        _options = options;
        _cache = cache;
    }

    /// <inheritdoc />
    public async ValueTask<EcbRateTable> GetTableAsync(EcbRateFeed feed, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(feed);

        (byte[] bytes, DateTimeOffset? cachedAtUtc) = await CachedDownload.GetOrDownloadAsync(
            _cache,
            feed,
            _options.RefreshInterval,
            _httpClient,
            () => _options.Endpoint.ResolveFeedUrl(feed),
            cancellationToken).ConfigureAwait(false);

        using MemoryStream stream = new(bytes, writable: false);
        EcbRateTable table = EcbRateXmlParser.Parse(stream, _options);
        table.CachedAtUtc = cachedAtUtc;
        return table;
    }
}
