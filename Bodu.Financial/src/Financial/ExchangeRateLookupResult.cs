// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupResult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Represents the outcome of a successful exchange-rate lookup, carrying both the resolved rate and the metadata that
/// describes how it was selected.
/// </summary>
/// <param name="Rate">The resolved exchange rate, including its source provider and inversion flag.</param>
/// <param name="RequestedDate">The calendar date the caller originally requested.</param>
/// <param name="Resolution">The fallback policy that was applied during the lookup.</param>
/// <param name="OffsetDays">
/// The absolute distance, in days, between <see cref="RequestedDate" /> and the date carried by <see cref="Rate" />.
/// </param>
/// <remarks>
/// <para>
/// Returning this metadata alongside the rate gives the caller everything required to explain which observed value was
/// selected (provider, date, inversion direction, distance from the requested date, and policy applied) without having
/// to re-query the underlying table.
/// </para>
/// </remarks>
public readonly record struct ExchangeRateLookupResult(
    ExchangeRate Rate,
    DateOnly RequestedDate,
    ExchangeRateDateResolution Resolution,
    int OffsetDays)
{
    /// <summary>
    /// Gets a value indicating whether the resolved rate is observed on the requested date.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="OffsetDays" /> is zero; otherwise <see langword="false" />.
    /// </returns>
    public bool IsExactDate => OffsetDays == 0;
}
