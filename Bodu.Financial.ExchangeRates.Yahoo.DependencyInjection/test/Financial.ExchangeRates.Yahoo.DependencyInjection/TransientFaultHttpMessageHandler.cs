// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TransientFaultHttpMessageHandler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;

namespace Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection;

/// <summary>
/// A test message handler that returns a configured transient failure status for an initial run of requests and then a
/// success status, recording how many requests it received so a retry can be observed.
/// </summary>
internal sealed class TransientFaultHttpMessageHandler
    : HttpMessageHandler
{
    /// <summary>
    /// The number of leading requests answered with <see cref="_failureStatus" />.
    /// </summary>
    private readonly int _failuresBeforeSuccess;

    /// <summary>
    /// The status returned for the leading failing requests.
    /// </summary>
    private readonly HttpStatusCode _failureStatus;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransientFaultHttpMessageHandler" /> class.
    /// </summary>
    /// <param name="failuresBeforeSuccess">The number of leading requests answered with a failure status.</param>
    /// <param name="failureStatus">The status returned while failing. Defaults to <see cref="HttpStatusCode.ServiceUnavailable" />.</param>
    public TransientFaultHttpMessageHandler(int failuresBeforeSuccess, HttpStatusCode failureStatus = HttpStatusCode.ServiceUnavailable)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
        _failureStatus = failureStatus;
    }

    /// <summary>
    /// Gets the number of requests this handler has received.
    /// </summary>
    /// <returns>The request count.</returns>
    public int RequestCount { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        HttpStatusCode status = RequestCount <= _failuresBeforeSuccess ? _failureStatus : HttpStatusCode.OK;
        return Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent([]) });
    }
}
