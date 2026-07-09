// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairRateData{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents the normalized result an <see cref="IPairRateSource{TSeries}" /> produces for one fetch: the resolved
/// currency pair, its dated observations, and the source-specific series metadata.
/// </summary>
/// <typeparam name="TSeries">The source-specific series-metadata type describing the fetched series.</typeparam>
/// <param name="Pair">The resolved currency pair the observations quote.</param>
/// <param name="Observations">
/// The dated observations for the pair, already restricted to the requested range. The consuming provider stamps each
/// with its own provider identifier when materializing <see cref="ExchangeRate" /> values, so the source need not.
/// </param>
/// <param name="Series">
/// The source-specific metadata describing the fetched series, surfaced for pair discovery.
/// </param>
public sealed record PairRateData<TSeries>(
    CurrencyPair Pair,
    IReadOnlyList<RateObservation> Observations,
    TSeries Series);
