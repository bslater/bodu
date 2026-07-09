// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaHistorySource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Obtains an OANDA rate history by issuing a GET against the configured data endpoint and parsing the JSON response.
/// </summary>
/// <remarks>
/// <para>
/// The OANDA Historical Currency Converter is fronted by a bot-mitigation layer that issues a short-lived session
/// cookie on the first page load and rejects the data endpoint until that cookie is present. The source therefore
/// primes the session once by requesting the site root, relying on the <see cref="HttpClient" />'s cookie container to
/// carry the cookie onto the data request, and re-primes once if a later request is challenged.
/// </para>
/// <para>
/// The endpoint serves a server-determined recent window rather than honouring an arbitrary historical range, so the
/// parser restricts the parsed observations to the request's inclusive range. The <c>User-Agent</c> the endpoint
/// requires is configured on the <see cref="HttpClient" /> (by the provider when it owns the client, or by the caller
/// when the client is supplied), not per request.
/// </para>
/// </remarks>
internal sealed class OandaHistorySource
    : IPairRateSource<OandaSeriesInfo>
{
    /// <summary>The HTTP client used to issue priming and history requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the base address, paths, and query parameters.</summary>
    private readonly OandaRateProviderOptions _options;

    /// <summary>Tracks whether the session has been primed since the last challenge.</summary>
    private volatile bool _primed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OandaHistorySource" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to issue priming and history requests.</param>
    /// <param name="options">The provider options.</param>
    internal OandaHistorySource(HttpClient httpClient, OandaRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<PairRateData<OandaSeriesInfo>> GetPairAsync(CurrencyPairRequest request, CancellationToken cancellationToken = default)
    {
        Uri url = BuildRequestUri(request);

        await EnsurePrimedAsync(cancellationToken).ConfigureAwait(false);

        using HttpResponseMessage response = await SendDataRequestAsync(url, cancellationToken).ConfigureAwait(false);

        HttpResponseMessage effective = response;
        HttpResponseMessage? retried = null;
        try
        {
            // A challenge means the session cookie was missing or stale: re-prime once and retry the data request.
            if (IsChallenge(response.StatusCode))
            {
                _primed = false;
                await EnsurePrimedAsync(cancellationToken).ConfigureAwait(false);
                retried = await SendDataRequestAsync(url, cancellationToken).ConfigureAwait(false);
                effective = retried;
            }

            effective.EnsureSuccessStatusCode();
            byte[] json = await effective.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

            return OandaHistoryResponseParser.Parse(json, request, _options);
        }
        finally
        {
            retried?.Dispose();
        }
    }

    /// <summary>
    /// Reports whether a status code indicates a bot-mitigation challenge that a re-prime may clear.
    /// </summary>
    /// <param name="statusCode">The response status code.</param>
    /// <returns><see langword="true" /> when the status is a challenge; otherwise <see langword="false" />.</returns>
    private static bool IsChallenge(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Forbidden or HttpStatusCode.ServiceUnavailable;

    /// <summary>
    /// Builds the absolute history request URI from the options and request.
    /// </summary>
    /// <param name="request">The pair request.</param>
    /// <returns>The absolute request URI.</returns>
    private Uri BuildRequestUri(CurrencyPairRequest request)
    {
        string query = _options.BuildQuery(
            request.Pair.From.ToString(),
            request.Pair.To.ToString(),
            request.StartDate,
            request.EndDate);

        UriBuilder builder = new(new Uri(_options.BaseAddress, _options.UpdatePath))
        {
            Query = query,
        };

        return builder.Uri;
    }

    /// <summary>
    /// Issues the data request with the headers the endpoint expects.
    /// </summary>
    /// <param name="url">The absolute data request URI.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the request.</param>
    /// <returns>The HTTP response.</returns>
    private Task<HttpResponseMessage> SendDataRequestAsync(Uri url, CancellationToken cancellationToken)
    {
        HttpRequestMessage message = new(HttpMethod.Get, url);
        message.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        message.Headers.Referrer = _options.BaseAddress;
        message.Headers.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");

        return _httpClient.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Primes the session by requesting the site root so the bot-mitigation cookie is captured by the client's cookie
    /// container before a data request is issued.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while awaiting the priming request.</param>
    /// <returns>A task that completes when the session has been primed.</returns>
    /// <remarks>
    /// Priming is best-effort and idempotent: the <see cref="_primed" /> flag short-circuits it after the first
    /// success, and the rare case of two first-callers each issuing a root request is harmless, so no lock is taken.
    /// </remarks>
    private async Task EnsurePrimedAsync(CancellationToken cancellationToken)
    {
        if (_primed)
            return;

        Uri primeUri = new(_options.BaseAddress, _options.PrimePath);

        // Read only the headers: the cookie is recorded as the response is received, and the body is not needed.
        using HttpRequestMessage message = new(HttpMethod.Get, primeUri);
        using HttpResponseMessage response = await _httpClient
            .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        // Even a non-success page load may still set the cookie, and the data request reports the authoritative outcome.
        _primed = true;
    }
}
