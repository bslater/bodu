// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateObservation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents one parsed cell of an IMF Representative Exchange Rates report: the rate quoting a single currency
/// against the US dollar on a single business day, already normalized to units of the quote currency per one US dollar.
/// </summary>
/// <param name="Date">The observation date.</param>
/// <param name="CurrencyCode">The resolved quote-currency ISO code.</param>
/// <param name="Rate">The number of units of the quote currency per one US dollar.</param>
internal readonly record struct ImfRateObservation(DateOnly Date, string CurrencyCode, decimal Rate);
