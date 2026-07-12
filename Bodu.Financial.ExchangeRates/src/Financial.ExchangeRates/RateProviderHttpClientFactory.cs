// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateProviderHttpClientFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Creates and configures the <see cref="HttpClient" /> a web-backed exchange-rate provider owns when the caller does
/// not supply one, applying the HTTP contract (user agent and request timeout) the remote endpoint requires.
/// </summary>
/// <remarks>
/// <para>
/// A provider that builds its own client owns the HTTP contract with the endpoint: it presents a recognizable
/// <c>User-Agent</c> (several feeds — Yahoo Finance in particular — answer requests without one with
/// <c>429 Too Many Requests</c>) and applies the configured request timeout. When the caller supplies a client instead,
/// provisioning it correctly is the caller's responsibility, so this factory is not involved.
/// </para>
/// <para>
/// The returned client is owned by the provider that requested it and is disposed when that provider is disposed.
/// </para>
/// </remarks>
public static class RateProviderHttpClientFactory
{
    /// <summary>
    /// The default cap, in bytes, applied to <see cref="HttpClient.MaxResponseContentBufferSize" /> for a
    /// provider-owned client (64 MiB).
    /// </summary>
    /// <remarks>
    /// The exchange-rate feeds these clients address return small payloads (a CSV, XML, or JSON document), so a
    /// finite ceiling well above any legitimate response prevents a compromised or man-in-the-middle endpoint from
    /// driving unbounded memory use, while never affecting a well-formed response.
    /// </remarks>
    public const long DefaultMaxResponseContentBufferSize = 64L * 1024 * 1024;

    /// <summary>
    /// Creates a new <see cref="HttpClient" /> configured with the supplied user agent, request timeout, and
    /// response-content buffer cap.
    /// </summary>
    /// <param name="userAgent">
    /// The <c>User-Agent</c> header applied to every request. A <see langword="null" />, empty, or white-space value
    /// suppresses the header.
    /// </param>
    /// <param name="httpTimeout">
    /// The request timeout applied to the client. Values not greater than zero are ignored.
    /// </param>
    /// <param name="maxResponseContentBufferSize">
    /// The maximum number of bytes to buffer when reading a response. Values not greater than zero are ignored,
    /// leaving the framework default; the default caps the client at
    /// <see cref="DefaultMaxResponseContentBufferSize" />.
    /// </param>
    /// <returns>A new, configured <see cref="HttpClient" />.</returns>
    /// <remarks>
    /// The timeout is applied to <see cref="HttpClient.Timeout" /> directly because a provider-owned client has no
    /// resilience handler enforcing a per-attempt timeout; a dependency-injection registration that adds its own
    /// resilience pipeline supplies its client to the provider instead and does not use this factory. The buffer
    /// cap bounds the memory a single response can consume, so an oversized body fails rather than exhausting memory.
    /// </remarks>
    public static HttpClient Create(
        string? userAgent,
        TimeSpan httpTimeout,
        long maxResponseContentBufferSize = DefaultMaxResponseContentBufferSize)
    {
        var client = new HttpClient();

        if (!string.IsNullOrWhiteSpace(userAgent))
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

        if (httpTimeout > TimeSpan.Zero)
            client.Timeout = httpTimeout;

        if (maxResponseContentBufferSize > 0)
            client.MaxResponseContentBufferSize = maxResponseContentBufferSize;

        return client;
    }
}
