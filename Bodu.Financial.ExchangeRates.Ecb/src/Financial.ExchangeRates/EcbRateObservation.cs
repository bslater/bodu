// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateObservation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents one parsed cell of an ECB <c>eurofxref</c> feed: the euro reference rate quoted against a single currency
/// on a single date.
/// </summary>
/// <param name="Date">The observation date.</param>
/// <param name="CurrencyCode">The resolved quote-currency ISO code.</param>
/// <param name="Rate">The number of units of the quote currency per one euro.</param>
internal readonly record struct EcbRateObservation(DateOnly Date, string CurrencyCode, decimal Rate);
