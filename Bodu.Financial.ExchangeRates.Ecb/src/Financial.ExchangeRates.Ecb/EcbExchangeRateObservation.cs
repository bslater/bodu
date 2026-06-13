// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateObservation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Represents one parsed cell of an ECB <c>eurofxref</c> feed: the euro reference rate quoted against a single currency
/// on a single date.
/// </summary>
/// <param name="Date">The observation date.</param>
/// <param name="CurrencyCode">The resolved quote-currency ISO code.</param>
/// <param name="Rate">The number of units of the quote currency per one euro.</param>
internal readonly record struct EcbExchangeRateObservation(DateOnly Date, string CurrencyCode, decimal Rate);
