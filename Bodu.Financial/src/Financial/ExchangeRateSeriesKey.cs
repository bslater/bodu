// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Identifies a single rate series within an <see cref="ExchangeRateTable" /> by its currency pair and provider.
/// </summary>
/// <param name="Pair">The currency pair the series describes.</param>
/// <param name="Provider">The non-empty identifier of the publishing source.</param>
public readonly record struct ExchangeRateSeriesKey(ExchangeRatePair Pair, string Provider);
