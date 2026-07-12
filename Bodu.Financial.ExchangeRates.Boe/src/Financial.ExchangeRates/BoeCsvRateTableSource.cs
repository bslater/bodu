// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeCsvRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains a Bank of England date range's rate table by querying the IADB CSV endpoint (via a response cache) and
/// parsing the result.
/// </summary>
internal sealed class BoeCsvRateTableSource
    : IBoeRateTableSource
{
    /// <summary>The HTTP client used to download range responses.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the endpoint, series catalogue, and refresh interval.</summary>
    private readonly BoeRateProviderOptions _options;

    /// <summary>The response byte cache, keyed by the inclusive date range.</summary>
    private readonly IByteCache<(DateOnly StartDate, DateOnly EndDate)> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeCsvRateTableSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to download range responses.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="cache">The response byte cache.</param>
    internal BoeCsvRateTableSource(HttpClient httpClient, BoeRateProviderOptions options, IByteCache<(DateOnly StartDate, DateOnly EndDate)> cache)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(cache);

        _httpClient = httpClient;
        _options = options;
        _cache = cache;
    }

    /// <inheritdoc />
    public async ValueTask<BoeRateTable> GetTableAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        byte[] bytes = await GetResponseBytesAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);

        using MemoryStream stream = new(bytes, writable: false);
        return BoeRateCsvParser.Parse(stream, _options);
    }

    /// <summary>
    /// Returns the range's response bytes from the cache, downloading and caching them on a miss.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the download.</param>
    /// <returns>A task that yields the response bytes.</returns>
    private async ValueTask<byte[]> GetResponseBytesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        if (_cache.TryGet((startDate, endDate), _options.RefreshInterval, out byte[]? cached))
            return cached;

        string[] codes = _options.Series.Select(static series => series.SeriesCode).ToArray();
        Uri url = _options.Endpoint.BuildRequestUrl(codes, startDate, endDate);
        byte[] bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
        _cache.Store((startDate, endDate), bytes);

        return bytes;
    }
}
