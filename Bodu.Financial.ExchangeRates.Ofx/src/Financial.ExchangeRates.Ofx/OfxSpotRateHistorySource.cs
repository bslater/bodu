// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxSpotRateHistorySource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Ofx;

/// <summary>
/// Obtains an OFX spot-rate history by issuing a GET against the configured history endpoint and parsing the JSON
/// response.
/// </summary>
/// <remarks>
/// OFX returns a server-determined historical window for the requested reporting interval rather than honouring an
/// explicit date range, so the parser restricts the parsed observations to the request's inclusive range. The
/// <c>User-Agent</c> the OFX endpoint requires is configured on the <see cref="HttpClient" /> (by the provider when it
/// owns the client, or by the caller when the client is supplied), not per request.
/// </remarks>
internal sealed class OfxSpotRateHistorySource
    : IExchangeRatePairSource<OfxSeriesInfo>
{
    /// <summary>The HTTP client used to issue history requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, history path, and query parameters.</summary>
    private readonly OfxExchangeRateOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfxSpotRateHistorySource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue history requests.</param>
    /// <param name="options">The provider options.</param>
    internal OfxSpotRateHistorySource(HttpClient httpClient, OfxExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<OfxSeriesInfo>> GetPairAsync(ExchangeRatePairRequest request, CancellationToken cancellationToken = default)
    {
        Uri url = BuildRequestUri(request);
        byte[] json = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);

        return OfxSpotRateHistoryResponseParser.Parse(json, request, _options);
    }

    /// <summary>
    /// Builds the absolute history request URI from the options and request.
    /// </summary>
    /// <param name="request">The pair request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(ExchangeRatePairRequest request)
    {
        // The path is built from validated ISO letters substituted into a fixed template, so it is composed directly.
        string path = _options.BuildPath(request.Pair.From.ToString(), request.Pair.To.ToString());

        UriBuilder builder = new(new Uri(_options.BaseAddress, path))
        {
            Query = string.Format(
                CultureInfo.InvariantCulture,
                "DecimalPlaces={0}&ReportingInterval={1}&format=json",
                _options.DecimalPlaces,
                _options.ReportingInterval),
        };

        return builder.Uri;
    }
}
