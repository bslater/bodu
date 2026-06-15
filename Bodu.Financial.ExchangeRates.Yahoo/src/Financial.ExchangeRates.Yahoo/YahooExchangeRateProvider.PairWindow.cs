// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateProvider.PairWindow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

public sealed partial class YahooExchangeRateProvider
{
    /// <summary>
    /// Identifies a single chart fetch by its currency pair and inclusive date window, used as the single-flight key so
    /// concurrent callers requesting the same pair over the same window coalesce onto one fetch while distinct windows
    /// fetch independently.
    /// </summary>
    /// <param name="Pair">The currency pair being fetched.</param>
    /// <param name="Start">The inclusive start of the fetched window.</param>
    /// <param name="End">The inclusive end of the fetched window.</param>
    private readonly record struct PairWindow(ExchangeRatePair Pair, DateOnly Start, DateOnly End);
}
