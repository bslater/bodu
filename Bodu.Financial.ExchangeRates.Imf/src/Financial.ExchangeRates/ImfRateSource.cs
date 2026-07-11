// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains a currency pair's rates from the IMF SDMX-JSON CompactData service by resolving the pair to an SDMX series
/// key, issuing a GET for the covered months, and parsing the JSON response.
/// </summary>
/// <remarks>
/// The series key is resolved through <see cref="ImfRateProviderOptions.SeriesMap" />. A pair that has no mapped series
/// key is treated as one with no published data — an empty result is returned without issuing a request — so the
/// provider's inverse-lookup fallback can be exercised. IMF observations are monthly, so the request's date range is
/// projected to the enclosing <c>YYYY-MM</c> <c>startPeriod</c> and <c>endPeriod</c> parameters.
/// </remarks>
internal sealed class ImfRateSource
    : IPairRateSource<ImfSeriesInfo>
{
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
        byte[] json = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);

        return ImfResponseParser.Parse(json, request, seriesKey);
    }

    /// <summary>
    /// Builds the absolute request URI addressing the CompactData resource for a series over the covered months.
    /// </summary>
    /// <param name="seriesKey">The resolved SDMX series key.</param>
    /// <param name="request">The pair request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(string seriesKey, CurrencyPairRequest request)
    {
        string path = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1}/{2}",
            _options.CompactDataPath,
            _options.Dataflow,
            seriesKey);

        string query = string.Format(
            CultureInfo.InvariantCulture,
            "startPeriod={0}&endPeriod={1}",
            request.StartDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            request.EndDate.ToString("yyyy-MM", CultureInfo.InvariantCulture));

        UriBuilder builder = new(new Uri(_options.BaseAddress, path)) { Query = query };

        return builder.Uri;
    }
}
