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
/// <param name="Provenance">
/// The lineage of the resolved rate: where it came from and, for a cache serve, how old it is.
/// </param>
/// <remarks>
/// <para>
/// Returning this metadata alongside the rate gives the caller everything required to explain which observed value was
/// selected (provider, date, inversion direction, distance from the requested date, and policy applied) without having
/// to re-query the underlying table.
/// </para>
/// <para>
/// <see cref="Provenance" /> is always populated. A rate resolved directly by a provider carries
/// <see cref="ExchangeRateOrigin.Live" /> with a <see langword="null" /> <see cref="ExchangeRateProvenance.Backend" />,
/// <see cref="ExchangeRateProvenance.CachedAtUtc" />, and <see cref="ExchangeRateProvenance.Age" />. A rate served from
/// a cache carries <see cref="ExchangeRateOrigin.Cache" /> with the serving backend, the instant the served data was
/// cached, and the age it had at the lookup instant.
/// </para>
/// </remarks>
public readonly record struct ExchangeRateLookupResult(
    ExchangeRate Rate,
    DateOnly RequestedDate,
    ExchangeRateDateResolution Resolution,
    int OffsetDays,
    ExchangeRateProvenance Provenance);
