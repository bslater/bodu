// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IExchangeRatePairSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Fetches and parses the dated observations for a single currency pair and date range. This is the per-source seam
/// between a <see cref="PairWebExchangeRateProvider{TSeries}" /> and the network: a concrete implementation builds the
/// feed-specific request, issues it, and parses the response into a <see cref="PairRateData{TSeries}" />, while tests
/// substitute a fixture-backed implementation.
/// </summary>
/// <typeparam name="TSeries">The source-specific series-metadata type carried on the result.</typeparam>
public interface IExchangeRatePairSource<TSeries>
{
    /// <summary>
    /// Fetches and parses the observations for the supplied request.
    /// </summary>
    /// <param name="request">The request describing the pair and inclusive date range to fetch.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the fetch.</param>
    /// <returns>A task that yields the parsed, range-restricted observations and series metadata.</returns>
    ValueTask<PairRateData<TSeries>> GetPairAsync(ExchangeRatePairRequest request, CancellationToken cancellationToken = default);
}
