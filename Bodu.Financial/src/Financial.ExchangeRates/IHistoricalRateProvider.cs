// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IHistoricalRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Identifies an exchange-rate provider that advertises how far back it can serve historical rates, so composing layers
/// can avoid asking it for dates it has declared unavailable.
/// </summary>
/// <remarks>
/// <para>
/// This is an optional capability interface layered over <see cref="IDatedRateProvider" />, following the same
/// probe-at-runtime idiom as <c>IPairRateLoader</c>: consumers test a provider with <see langword="is" /> and treat one
/// that does not implement the interface as <see cref="RateHistoryAvailability.Unbounded" /> — never skipped and never
/// clamped. Implementing the interface therefore only ever removes doomed upstream calls; it cannot hide data a
/// provider could have served.
/// </para>
/// <para>
/// The advertised value is a declaration about the upstream source, not a per-request guarantee: a date inside the
/// advertised window can still miss (weekends, holidays, unpublished series), and consumers must retain their normal
/// miss handling. Availability describes the dated history surface only; the undated <see cref="IRateProvider" />
/// latest-rate surface is unaffected.
/// </para>
/// </remarks>
public interface IHistoricalRateProvider
{
    /// <summary>
    /// Gets the history depth this provider advertises: how far back it can serve rates.
    /// </summary>
    /// <value>
    /// The advertised availability. <see cref="RateHistoryAvailability.Unbounded" /> declares no known floor.
    /// </value>
    RateHistoryAvailability HistoryAvailability { get; }
}
