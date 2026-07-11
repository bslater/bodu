// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixerRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains a currency pair's rates from Fixer by issuing a GET against the time-series endpoint (for multi-day ranges)
/// or the single-date endpoint (for one-day requests) and parsing the JSON response.
/// </summary>
/// <remarks>
/// The base currency is taken from the request's source currency and the quote symbol from its destination currency,
/// both mapped through the configured currency aliases. The access key is presented as the <c>access_key</c> query
/// parameter. Whether the account's plan permits the requested base currency or endpoint is determined by Fixer and
/// surfaces through the parser as a fetch failure.
/// </remarks>
internal sealed class FixerRateSource
    : IPairRateSource<FixerSeriesInfo>
{
    /// <summary>The HTTP client used to issue requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, endpoint paths, and access key.</summary>
    private readonly FixerRateProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixerRateSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue requests.</param>
    /// <param name="options">The provider options.</param>
    internal FixerRateSource(HttpClient httpClient, FixerRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<FixerSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        string baseSymbol = _options.MapSymbol(request.Pair.From.ToString());
        string quoteSymbol = _options.MapSymbol(request.Pair.To.ToString());

        Uri url = BuildRequestUri(baseSymbol, quoteSymbol, request);
        byte[] json = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);

        return FixerResponseParser.Parse(json, request, quoteSymbol, _options);
    }

    /// <summary>
    /// Builds the absolute request URI, selecting the single-date endpoint for a one-day request and the time-series
    /// endpoint otherwise.
    /// </summary>
    /// <param name="baseSymbol">The mapped base-currency symbol.</param>
    /// <param name="quoteSymbol">The mapped quote-currency symbol.</param>
    /// <param name="request">The pair request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(string baseSymbol, string quoteSymbol, CurrencyPairRequest request)
    {
        bool singleDay = request.StartDate == request.EndDate;

        string path = singleDay
            ? _options.HistoricalPath.Replace(
                FixerRateProviderOptions.DatePlaceholder,
                request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            : _options.TimeSeriesPath;

        string query = singleDay
            ? string.Format(
                CultureInfo.InvariantCulture,
                "access_key={0}&base={1}&symbols={2}",
                Uri.EscapeDataString(_options.ApiKey),
                baseSymbol,
                quoteSymbol)
            : string.Format(
                CultureInfo.InvariantCulture,
                "access_key={0}&base={1}&symbols={2}&start_date={3}&end_date={4}",
                Uri.EscapeDataString(_options.ApiKey),
                baseSymbol,
                quoteSymbol,
                request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                request.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        UriBuilder builder = new(new Uri(_options.BaseAddress, path)) { Query = query };

        return builder.Uri;
    }
}
