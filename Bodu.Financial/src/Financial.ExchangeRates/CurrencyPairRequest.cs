// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyPairRequest.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes a single pair-based fetch request issued to an <c>IPairRateSource&lt;TSeries&gt;</c>: the currency pair to
/// load and the inclusive date range to cover.
/// </summary>
/// <param name="Pair">The currency pair to fetch.</param>
/// <param name="StartDate">The inclusive start of the range to fetch.</param>
/// <param name="EndDate">The inclusive end of the range to fetch.</param>
/// <remarks>
/// A source maps the request to the wire form its feed requires (a ticker symbol, a path, query parameters) and is free
/// to over-fetch and range-filter the result; the request expresses only the caller's intent, not the feed's transport.
/// </remarks>
public readonly record struct CurrencyPairRequest(CurrencyPair Pair, DateOnly StartDate, DateOnly EndDate);
