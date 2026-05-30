// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Financial.Kat;

/// <summary>
/// Represents a known-answer test row exercising full <see cref="ExchangeRateSeries" /> construction followed by a
/// single <see cref="ExchangeRateSeries.TryGetRate" /> call.
/// </summary>
/// <param name="Name">The short label identifying the scenario.</param>
/// <param name="Pair">The currency pair carried by the constructed series.</param>
/// <param name="Provider">The provider name carried by the constructed series.</param>
/// <param name="Rates">The date/rate observations used to build the series.</param>
/// <param name="RequestedDate">The date the caller asks about.</param>
/// <param name="Options">The lookup options supplied to <see cref="ExchangeRateSeries.TryGetRate" />.</param>
/// <param name="ExpectedSuccess"><see langword="true" /> when the lookup should succeed; otherwise <see langword="false" />.</param>
/// <param name="ExpectedResolvedDate">The expected resolved date when <paramref name="ExpectedSuccess" /> is <see langword="true" />.</param>
/// <param name="ExpectedRate">The expected rate value when <paramref name="ExpectedSuccess" /> is <see langword="true" />.</param>
public sealed record ExchangeRateSeriesKat(
    string Name,
    ExchangeRatePair Pair,
    string Provider,
    (DateOnly Date, decimal Rate)[] Rates,
    DateOnly RequestedDate,
    ExchangeRateLookupOptions Options,
    bool ExpectedSuccess,
    DateOnly? ExpectedResolvedDate,
    decimal? ExpectedRate) : IKat;
