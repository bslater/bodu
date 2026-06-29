// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RoutedHttpMessageHandler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test message handler that answers each request from a caller-supplied responder, recording the requests it
/// received so a test can assert request order, URLs, and headers.
/// </summary>
internal sealed class RoutedHttpMessageHandler
    : HttpMessageHandler
{
    /// <summary>The responder that maps a request to its response.</summary>
    private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _responder;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutedHttpMessageHandler" /> class.
    /// </summary>
    /// <param name="responder">
    /// The responder invoked for each request, receiving the request and the zero-based request index.
    /// </param>
    public RoutedHttpMessageHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    /// <summary>
    /// Gets the requests this handler has received, in order.
    /// </summary>
    /// <value>The recorded requests.</value>
    public List<HttpRequestMessage> Requests { get; } = new();

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int index = Requests.Count;
        Requests.Add(request);

        return Task.FromResult(_responder(request, index));
    }
}
