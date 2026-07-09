// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeChartingRatesSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains an XE.com charting-rates series by issuing an authorized GET against the configured endpoint and parsing the
/// JSON response.
/// </summary>
/// <remarks>
/// <para>
/// The <c>Authorization: Basic</c> token the endpoint requires is obtained per request from an
/// <see cref="IXeAuthTokenProvider" /> and attached to the outgoing request, so a token shared across a pooled
/// <see cref="HttpClient" /> is never baked into the client's default headers. When the endpoint rejects the cached
/// token with <see cref="HttpStatusCode.Unauthorized" /> or <see cref="HttpStatusCode.Forbidden" />, the token is
/// refreshed and the request is retried once.
/// </para>
/// <para>
/// XE returns a server-determined window rather than honouring an explicit date range, so the parser restricts the
/// parsed observations to the request's inclusive range.
/// </para>
/// </remarks>
internal sealed class XeChartingRatesSource
    : IPairRateSource<XeSeriesInfo>
{
    /// <summary>The HTTP client used to issue charting-rates requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, charting-rates path, and currency aliases.</summary>
    private readonly XeRateProviderOptions _options;

    /// <summary>The token provider supplying the <c>Authorization: Basic</c> credential.</summary>
    private readonly IXeAuthTokenProvider _authTokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="XeChartingRatesSource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue charting-rates requests.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="authTokenProvider">The token provider supplying the authorization credential.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" />, <paramref name="options" />, or
    /// <paramref name="authTokenProvider" /> is <see langword="null" />.
    /// </exception>
    internal XeChartingRatesSource(HttpClient httpClient, XeRateProviderOptions options, IXeAuthTokenProvider authTokenProvider)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(authTokenProvider);

        _httpClient = httpClient;
        _options = options;
        _authTokenProvider = authTokenProvider;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<XeSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        Uri url = BuildRequestUri(request);
        byte[] json = await SendAuthorizedAsync(url, cancellationToken).ConfigureAwait(false);

        return XeChartingRatesResponseParser.Parse(json, request, _options);
    }

    /// <summary>
    /// Issues the authorized request, refreshing the token and retrying once when the endpoint rejects a cached token.
    /// </summary>
    /// <param name="url">The absolute request URI.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the request.</param>
    /// <returns>A task that yields the UTF-8 JSON response bytes.</returns>
    /// <exception cref="HttpRequestException">Thrown when the request ultimately fails.</exception>
    private async Task<byte[]> SendAuthorizedAsync(Uri url, CancellationToken cancellationToken)
    {
        for (int attempt = 0; ; attempt++)
        {
            string token = await _authTokenProvider.GetTokenAsync(forceRefresh: attempt > 0, cancellationToken).ConfigureAwait(false);

            using HttpRequestMessage message = new(HttpMethod.Get, url);
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);

            using HttpResponseMessage response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

            // A rejected token is the one failure worth retrying: refresh it and re-issue the request exactly once.
            if (attempt == 0 && (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
                continue;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Builds the absolute charting-rates request URI from the options and request.
    /// </summary>
    /// <param name="request">The pair request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(CurrencyPairRequest request)
    {
        string from = _options.MapCurrencyCode(request.Pair.From.ToString());
        string to = _options.MapCurrencyCode(request.Pair.To.ToString());

        UriBuilder builder = new(new Uri(_options.BaseAddress, _options.ChartingRatesPath))
        {
            Query = string.Format(
                CultureInfo.InvariantCulture,
                "fromCurrency={0}&toCurrency={1}&isExtended=true",
                Uri.EscapeDataString(from),
                Uri.EscapeDataString(to)),
        };

        return builder.Uri;
    }
}
