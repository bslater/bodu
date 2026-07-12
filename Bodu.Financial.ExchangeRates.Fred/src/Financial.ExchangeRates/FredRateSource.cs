// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains a currency pair's rates from FRED by resolving the pair to a single FRED series and issuing a GET against
/// the series-observations endpoint, then parsing the JSON response.
/// </summary>
/// <remarks>
/// The pair is resolved to a FRED series identifier through <see cref="FredRateProviderOptions.SeriesMap" />. A pair
/// with no mapped series returns an empty result without issuing any request, so the base class's inverse-lookup
/// fallback can be exercised. The API key is presented as the <c>api_key</c> query parameter.
/// </remarks>
internal sealed class FredRateSource
    : IPairRateSource<FredSeriesInfo>
{
    /// <summary>The HTTP client used to issue requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, observations path, series map, and API key.</summary>
    private readonly FredRateProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FredRateSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue requests.</param>
    /// <param name="options">The provider options.</param>
    internal FredRateSource(HttpClient httpClient, FredRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<FredSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.TryGetSeriesId(request.Pair, out string seriesId))
        {
            // Unmapped pair: behave like one with no published data so inverse-fallback paths can be exercised.
            return new PairRateData<FredSeriesInfo>(
                request.Pair,
                Array.Empty<RateObservation>(),
                new FredSeriesInfo(request.Pair, string.Empty));
        }

        Uri url = BuildRequestUri(seriesId, request);
        byte[] json = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);

        return FredResponseParser.Parse(json, request, seriesId);
    }

    /// <summary>
    /// Builds the absolute request URI for a series' observations over the request's inclusive date range.
    /// </summary>
    /// <param name="seriesId">The FRED series identifier to fetch.</param>
    /// <param name="request">The pair request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(string seriesId, CurrencyPairRequest request)
    {
        string query = string.Format(
            CultureInfo.InvariantCulture,
            "series_id={0}&api_key={1}&file_type=json&observation_start={2}&observation_end={3}&sort_order=asc",
            seriesId,
            Uri.EscapeDataString(_options.ApiKey),
            request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            request.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        UriBuilder builder = new(new Uri(_options.BaseAddress, _options.ObservationsPath)) { Query = query };

        return builder.Uri;
    }
}
