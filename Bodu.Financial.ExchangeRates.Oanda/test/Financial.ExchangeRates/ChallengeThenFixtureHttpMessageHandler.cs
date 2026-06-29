// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChallengeThenFixtureHttpMessageHandler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A test message handler that answers the data endpoint with a bot-mitigation challenge for an initial run of requests
/// and then with the fixture, while always answering priming (non-data) requests successfully, so the source's
/// re-prime-and-retry path can be observed.
/// </summary>
internal sealed class ChallengeThenFixtureHttpMessageHandler
    : HttpMessageHandler
{
    /// <summary>The fixture content returned once the data endpoint stops challenging.</summary>
    private readonly byte[] _content;

    /// <summary>The number of leading data requests answered with a challenge status.</summary>
    private readonly int _challengesBeforeSuccess;

    /// <summary>The marker that identifies a data-endpoint request by its path.</summary>
    private readonly string _dataPathMarker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChallengeThenFixtureHttpMessageHandler" /> class.
    /// </summary>
    /// <param name="content">The fixture content returned for a successful data request.</param>
    /// <param name="challengesBeforeSuccess">The number of leading data requests answered with a challenge.</param>
    /// <param name="dataPathMarker">The path fragment that identifies a data-endpoint request.</param>
    public ChallengeThenFixtureHttpMessageHandler(byte[] content, int challengesBeforeSuccess, string dataPathMarker = "update")
    {
        _content = content;
        _challengesBeforeSuccess = challengesBeforeSuccess;
        _dataPathMarker = dataPathMarker;
    }

    /// <summary>
    /// Gets the number of data-endpoint requests this handler has received.
    /// </summary>
    /// <value>The data request count.</value>
    public int DataRequestCount { get; private set; }

    /// <summary>
    /// Gets the number of priming (non-data) requests this handler has received.
    /// </summary>
    /// <value>The priming request count.</value>
    public int PrimeRequestCount { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        bool isData = request.RequestUri is not null
            && request.RequestUri.AbsolutePath.Contains(_dataPathMarker, StringComparison.Ordinal);

        if (!isData)
        {
            PrimeRequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) });
        }

        DataRequestCount++;
        if (DataRequestCount <= _challengesBeforeSuccess)
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new ByteArrayContent([]) });

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(_content) });
    }
}
