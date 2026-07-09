// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagConversionLine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial;

/// <summary>
/// Captures the per-line audit metadata produced by
/// <see cref="MoneyBag.ConvertToWithAudit{TTarget}(IDatedRateProvider, DateOnly, RateLookupOptions?, MoneyBagConversionRoundingPolicy)" />
/// for a single source currency in the bag.
/// </summary>
/// <param name="SourceIsoCode">The source-currency ISO code for this line.</param>
/// <param name="SourceAmount">The bag's raw decimal balance for that source.</param>
/// <param name="Rate">
/// The exchange-rate lookup result used for the conversion, or <see langword="null" /> when the source already matches
/// the target currency (identity pass-through).
/// </param>
/// <param name="RawConvertedAmount">
/// The unrounded contribution to the aggregated total — <c>SourceAmount × Rate</c> for cross-currency lines or
/// <c>SourceAmount</c> for the identity pass-through.
/// </param>
public readonly record struct MoneyBagConversionLine(
    string SourceIsoCode,
    decimal SourceAmount,
    RateLookupResult? Rate,
    decimal RawConvertedAmount);
