// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StubXeAuthTokenProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IXeAuthTokenProvider" /> that returns a canned token without contacting the network, recording how
/// many times it was asked and how many of those asks requested a refresh.
/// </summary>
internal sealed class StubXeAuthTokenProvider
    : IXeAuthTokenProvider
{
    /// <summary>The token returned for every request.</summary>
    private readonly string _token;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubXeAuthTokenProvider" /> class.
    /// </summary>
    /// <param name="token">The token returned for every request.</param>
    public StubXeAuthTokenProvider(string token = "c3R1Yi10b2tlbg==")
    {
        _token = token;
    }

    /// <summary>
    /// Gets the number of times a token was requested.
    /// </summary>
    /// <value>The total request count.</value>
    public int CallCount { get; private set; }

    /// <summary>
    /// Gets the number of times a refresh was requested.
    /// </summary>
    /// <value>The refresh request count.</value>
    public int RefreshCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<string> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken = default)
    {
        CallCount++;
        if (forceRefresh)
            RefreshCount++;

        return ValueTask.FromResult(_token);
    }
}
