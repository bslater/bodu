// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEcbExchangeRateTableSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the parsed rate table for an ECB feed, abstracting over how the underlying file is obtained and decoded.
/// </summary>
/// <remarks>
/// The shipped implementation downloads and parses the ECB <c>eurofxref</c> XML files. The abstraction leaves room for
/// an alternative source (for example, an SDMX or CSV feed) to produce the same normalized table without changing the
/// provider.
/// </remarks>
internal interface IEcbExchangeRateTableSource
{
    /// <summary>
    /// Obtains and parses the rate table for the specified feed.
    /// </summary>
    /// <param name="feed">The feed to obtain.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
    /// <returns>A task that yields the parsed <see cref="EcbExchangeRateTable" />.</returns>
    ValueTask<EcbExchangeRateTable> GetTableAsync(EcbExchangeRateFeed feed, CancellationToken cancellationToken = default);
}
