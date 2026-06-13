// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateObservation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Represents one parsed cell of a Bank of England IADB response: the daily spot rate quoted against the pound for a
/// single currency on a single date.
/// </summary>
/// <param name="Date">The observation date.</param>
/// <param name="CurrencyCode">The resolved quote-currency ISO code.</param>
/// <param name="Rate">The number of units of the quote currency per one pound sterling.</param>
internal readonly record struct BoeExchangeRateObservation(DateOnly Date, string CurrencyCode, decimal Rate);
