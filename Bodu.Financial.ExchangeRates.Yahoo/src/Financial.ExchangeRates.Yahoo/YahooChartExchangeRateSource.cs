// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooChartExchangeRateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Obtains a Yahoo Finance chart by issuing a GET against the configured chart endpoint and parsing the JSON response.
/// </summary>
/// <remarks>
/// The bar interval is fixed at one day; the date range is taken from the request and expressed as the <c>period1</c>/
/// <c>period2</c> Unix-second query parameters. Each request carries the configured
/// <see cref="YahooExchangeRateOptions.UserAgent" />, set per request so the shared client's headers are not mutated;
/// the Yahoo Finance endpoint answers requests that omit a recognizable user agent with <c>429 Too Many Requests</c>.
/// Connection reuse is the responsibility of the supplied <see cref="HttpClient" /> (typically one created by
/// <c>IHttpClientFactory</c>).
/// </remarks>
internal sealed class YahooChartExchangeRateSource
    : IYahooExchangeRateChartSource
{
    /// <summary>
    /// The fixed daily bar interval requested from the chart endpoint.
    /// </summary>
    private const string DailyInterval = "1d";

    /// <summary>
    /// The HTTP client used to issue chart requests.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// The provider options supplying the base address, chart path, and symbol format.
    /// </summary>
    private readonly YahooExchangeRateOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooChartExchangeRateSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue chart requests.</param>
    /// <param name="options">The provider options.</param>
    internal YahooChartExchangeRateSource(HttpClient httpClient, YahooExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<YahooExchangeRateChart> GetChartAsync(YahooChartRequest request, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(request);

        Uri url = BuildRequestUri(request);

        using HttpRequestMessage message = new(HttpMethod.Get, url);
        ApplyHeaders(message);

        using HttpResponseMessage response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        return YahooChartResponseParser.Parse(json, request, _options);
    }

    /// <summary>
    /// Applies the configured request headers — notably the <c>User-Agent</c> the Yahoo Finance endpoint requires — to
    /// an outgoing chart request.
    /// </summary>
    /// <param name="message">The request message to populate.</param>
    /// <remarks>
    /// The header is set per request rather than on the shared <see cref="HttpClient" />, so the provider presents a
    /// recognizable user agent whether it is constructed directly or resolved from dependency injection, and the
    /// shared client's headers are never mutated. Yahoo Finance answers requests that omit a recognizable user agent
    /// with <c>429 Too Many Requests</c>.
    /// </remarks>
    private void ApplyHeaders(HttpRequestMessage message)
    {
        if (!string.IsNullOrWhiteSpace(_options.UserAgent))
            message.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
    }

    /// <summary>
    /// Builds the absolute chart request URI from the options and request.
    /// </summary>
    /// <param name="request">The chart request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(YahooChartRequest request)
    {
        // The ticker is built from validated ISO letters plus a safe suffix, so it is substituted into the path
        // segment directly; escaping the '=' would change the resource Yahoo serves.
        var path = _options.ChartPath.Replace(
            YahooExchangeRateOptions.SymbolPlaceholder,
            request.Symbol,
            StringComparison.Ordinal);

        var period1 = ToUnixSeconds(request.StartDate);

        // period2 is exclusive at the source, so add a day to keep the requested end date inclusive.
        var period2 = ToUnixSeconds(request.EndDate.AddDays(1));

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

    /// <summary>
    /// Converts a date to Unix seconds at midnight UTC.
    /// </summary>
    /// <param name="date">The date to convert.</param>
    /// <returns>The Unix-second timestamp.</returns>
    private static long ToUnixSeconds(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeSeconds();
}
