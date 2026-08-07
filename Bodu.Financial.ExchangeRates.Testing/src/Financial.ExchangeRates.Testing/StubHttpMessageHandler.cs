// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StubHttpMessageHandler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;

namespace Bodu.Financial.ExchangeRates.Testing;

/// <summary>
/// A test message handler that returns fixed content for every request and records what it received.
/// </summary>
/// <remarks>
/// <para>
/// Provider tests typically substitute a fixture-backed <c>IPairRateSource</c> that reads an embedded response and
/// calls the parser directly. That covers parsing but bypasses request construction entirely, leaving the URL the
/// provider actually builds unasserted. Driving the real source over this handler closes that gap:
/// <see cref="LastRequestUri" /> is what makes the assertion possible, and <see cref="RequestCount" /> distinguishes
/// a request that was served from one that was never issued.
/// </para>
/// </remarks>
public sealed class StubHttpMessageHandler
    : HttpMessageHandler
{
    /// <summary>The content returned for every request.</summary>
    private readonly byte[] _content;

    /// <summary>The status code returned for every request.</summary>
    private readonly HttpStatusCode _statusCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubHttpMessageHandler" /> class.
    /// </summary>
    /// <param name="content">The content returned for every request.</param>
    /// <param name="statusCode">The status code returned for every request.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content" /> is <see langword="null" />.</exception>
    public StubHttpMessageHandler(byte[] content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _statusCode = statusCode;
    }

    /// <summary>
    /// Gets the number of requests this handler has received.
    /// </summary>
    /// <value>The request count.</value>
    public int RequestCount { get; private set; }

    /// <summary>
    /// Gets the URI of the most recent request, or <see langword="null" /> when none has been received.
    /// </summary>
    /// <value>The last request URI.</value>
    public Uri? LastRequestUri { get; private set; }

    /// <summary>
    /// Gets the <c>Authorization</c> header of the most recent request, or <see langword="null" /> when none was
    /// sent.
    /// </summary>
    /// <value>The last authorization header.</value>
    public AuthenticationHeaderValue? LastAuthorization { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        LastRequestUri = request.RequestUri;
        LastAuthorization = request.Headers.Authorization;

        return Task.FromResult(new HttpResponseMessage(_statusCode) { Content = new ByteArrayContent(_content) });
    }
}
