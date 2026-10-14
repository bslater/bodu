// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooChartRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Extensions;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains a currency pair's Yahoo Finance chart by mapping the pair to its <c>{FROM}{TO}=X</c> ticker, issuing a GET
/// against the configured chart endpoint, and parsing the JSON response.
/// </summary>
/// <remarks>
/// The bar interval is fixed at one day; the date range is taken from the request and expressed as the <c>period1</c>/
/// <c>period2</c> Unix-second query parameters. The window opens at midnight UTC on the day before the start date,
/// because a daily bar is stamped at the start of its trading day in the exchange's time zone, which for an exchange
/// east of UTC falls on the evening before in UTC; the parser dates each bar in the exchange's time zone and drops the
/// bars outside the requested range. The <c>User-Agent</c> the Yahoo Finance endpoint requires is configured on the
/// <see cref="HttpClient" /> (by the provider when it owns the client, or by the caller when the client is supplied),
/// not per request. Connection reuse is the responsibility of the supplied <see cref="HttpClient" /> (typically one
/// created by <c>IHttpClientFactory</c>).
/// </remarks>
internal sealed class YahooChartRateSource
    : IPairRateSource<YahooSeriesInfo>
{
    /// <summary>The fixed daily bar interval requested from the chart endpoint.</summary>
    private const string DailyInterval = "1d";

    /// <summary>The number of seconds in a day.</summary>
    private const long SecondsPerDay = 24 * 60 * 60;

    /// <summary>The HTTP client used to issue chart requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, chart path, and symbol format.</summary>
    private readonly YahooRateProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooChartRateSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue chart requests.</param>
    /// <param name="options">The provider options.</param>
    internal YahooChartRateSource(HttpClient httpClient, YahooRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<YahooSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        string symbol = _options.BuildSymbol(request.Pair.From.ToString(), request.Pair.To.ToString());

        Uri url = BuildRequestUri(symbol, request);
        byte[] json = await _httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);

        return YahooChartResponseParser.Parse(json, request, symbol, _options);
    }

    /// <summary>
    /// Builds the absolute chart request URI from the options, ticker, and request.
    /// </summary>
    /// <param name="symbol">The <c>{FROM}{TO}=X</c> ticker addressing the pair's chart.</param>
    /// <param name="request">The pair request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(string symbol, CurrencyPairRequest request)
    {
        // The ticker is built from validated ISO letters plus a safe suffix, so it is substituted into the path
        // segment directly; escaping the '=' would change the resource Yahoo serves.
        string path = _options.ChartPath.Replace(
            YahooRateProviderOptions.SymbolPlaceholder,
            symbol,
            StringComparison.Ordinal);

        // A daily bar is stamped at the start of its trading day in the exchange's time zone: for London in summer that
        // is 23:00 UTC on the day before, before midnight UTC on the start date. Opening the window a day early keeps the
        // start date's bar; the parser dates bars in exchange time and drops the extra day. Subtracting seconds rather
        // than a calendar day keeps DateOnly.MinValue representable.
        long period1 = request.StartDate.ToUnixTimeSeconds() - SecondsPerDay;

        // period2 is exclusive at the source, so add a day to keep the requested end date inclusive.
        long period2 = request.EndDate.AddDays(1).ToUnixTimeSeconds();

        UriBuilder builder = new(new Uri(_options.BaseAddress, path))
        {
            Query = string.Format(
                CultureInfo.InvariantCulture,
                "period1={0}&period2={1}&interval={2}&includePrePost=false",
                period1,
                period2,
                DailyInterval),
        };

        return builder.Uri;
    }
}
