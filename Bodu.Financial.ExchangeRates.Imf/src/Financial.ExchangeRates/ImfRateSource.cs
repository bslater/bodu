// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Net.Http.Headers;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains a currency pair's daily rates from the IMF SDMX Data API by resolving the pair to an SDMX series key,
/// issuing a GET over the requested date range, and parsing the SDMX-JSON response.
/// </summary>
/// <remarks>
/// The series key is resolved through <see cref="ImfRateProviderOptions.SeriesMap" />. A pair that has no mapped series
/// key is treated as one with no published data — an empty result is returned without issuing a request — so the
/// provider's inverse-lookup fallback can be exercised. The request addresses the daily series over the inclusive range
/// through <c>startPeriod</c> / <c>endPeriod</c> <c>YYYY-MM-DD</c> parameters, and asks for SDMX-JSON through the
/// <c>Accept</c> header.
/// </remarks>
internal sealed class ImfRateSource
    : IPairRateSource<ImfSeriesInfo>
{
    /// <summary>The media type requested for the SDMX-JSON data message.</summary>
    private const string SdmxJsonMediaType = "application/vnd.sdmx.data+json";

    /// <summary>The HTTP client used to issue requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, endpoint paths, and series map.</summary>
    private readonly ImfRateProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue requests.</param>
    /// <param name="options">The provider options.</param>
    internal ImfRateSource(HttpClient httpClient, ImfRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<ImfSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.TryGetSeriesKey(request.Pair, out string seriesKey))
        {
            // Unmapped pair: behave like one with no published data so inverse-fallback paths can be exercised.
            return new PairRateData<ImfSeriesInfo>(
                request.Pair,
                Array.Empty<RateObservation>(),
                new ImfSeriesInfo(request.Pair, seriesKey));
        }

        Uri url = BuildRequestUri(seriesKey, request);

        using HttpRequestMessage message = new(HttpMethod.Get, url);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(SdmxJsonMediaType));

        using HttpResponseMessage response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        byte[] json = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        return ImfResponseParser.Parse(json, request, seriesKey);
    }

    /// <summary>
    /// Builds the absolute request URI addressing the SDMX daily data resource for a series over the requested range.
    /// </summary>
    /// <param name="seriesKey">The resolved SDMX series key.</param>
    /// <param name="request">The pair request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(string seriesKey, CurrencyPairRequest request)
    {
        string path = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1}/{2}/{3}",
            _options.DataPath,
            _options.Dataflow,
            _options.DataVersion,
            seriesKey);

        string query = string.Format(
            CultureInfo.InvariantCulture,
            "startPeriod={0}&endPeriod={1}&dimensionAtObservation=TIME_PERIOD",
            request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            request.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        UriBuilder builder = new(new Uri(_options.BaseAddress, path)) { Query = query };

        return builder.Uri;
    }
}
