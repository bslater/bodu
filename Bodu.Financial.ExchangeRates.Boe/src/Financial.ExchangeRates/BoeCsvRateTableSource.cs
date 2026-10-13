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

    /// <summary>The provider options supplying the endpoint, series catalog, and refresh interval.</summary>
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
        (byte[] bytes, DateTimeOffset? cachedAtUtc) = await CachedDownload.GetOrDownloadAsync(
            _cache,
            (startDate, endDate),
            _options.RefreshInterval,
            _httpClient,
            () => _options.BuildRequestUrl(_options.Series.Select(static series => series.SeriesCode).ToArray(), startDate, endDate),
            cancellationToken).ConfigureAwait(false);

        BoeRateTable table = BoeRateCsvParser.Parse(bytes, _options);
        table.CachedAtUtc = cachedAtUtc;
        return table;
    }
}
