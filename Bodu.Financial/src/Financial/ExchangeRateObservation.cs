// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateObservation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Represents a single observed exchange rate on a specific calendar date for a series.
/// </summary>
/// <param name="Date">The calendar date on which the rate was observed.</param>
/// <param name="Rate">
/// The observed rate. Must be strictly positive when consumed by an <see cref="ExchangeRateSeries" /> or
/// <see cref="ExchangeRateSeriesBuilder" />; the type itself does not validate the value because it is also used
/// as a transport record in scenarios that produce default instances.
/// </param>
/// <remarks>
/// <para>
/// This type is a lightweight value carrier used by the series API in preference to repeated
/// <c>(DateOnly, decimal)</c> tuples. The surrounding series enforces the &quot;strictly positive rate&quot; and
/// &quot;unique date&quot; invariants; values constructed in isolation (including
/// <see langword="default" /><c>(ExchangeRateObservation)</c>) carry no validation guarantees.
/// </para>
/// </remarks>
public readonly record struct ExchangeRateObservation(DateOnly Date, decimal Rate);
