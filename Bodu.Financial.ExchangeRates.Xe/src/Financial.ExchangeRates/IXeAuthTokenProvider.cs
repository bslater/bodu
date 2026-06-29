// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IXeAuthTokenProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Supplies the <c>Authorization: Basic</c> token the XE.com charting-rates endpoint requires. This is the seam between
/// the source and the token-acquisition strategy: the default implementation discovers the token from the XE website,
/// while tests substitute a canned-token implementation.
/// </summary>
internal interface IXeAuthTokenProvider
{
    /// <summary>
    /// Gets the base64 credential to send as the <c>Authorization: Basic</c> value, acquiring and caching it on first
    /// use.
    /// </summary>
    /// <param name="forceRefresh">
    /// <see langword="true" /> to discard any cached token and re-acquire one, used after the endpoint rejects a cached
    /// token; <see langword="false" /> to reuse a cached token when present.
    /// </param>
    /// <param name="cancellationToken">A token to observe while awaiting acquisition.</param>
    /// <returns>A task that yields the base64 credential.</returns>
    ValueTask<string> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken = default);
}
