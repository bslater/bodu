// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StubHttpMessageHandler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test message handler that returns fixed content for every request and records the requests it received.
/// </summary>
internal sealed class StubHttpMessageHandler
    : HttpMessageHandler
{
    /// <summary>The content returned for every request.</summary>
    private readonly byte[] _content;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubHttpMessageHandler" /> class.
    /// </summary>
    /// <param name="content">The content returned for every request.</param>
    public StubHttpMessageHandler(byte[] content)
    {
        _content = content;
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

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        LastRequestUri = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(_content) });
    }
}
