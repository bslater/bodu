// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagConversionAudit{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Bundles the aggregated total produced by a dated bag conversion with the full per-line audit trail.
/// </summary>
/// <typeparam name="TTarget">The destination currency type.</typeparam>
/// <param name="Total">The aggregated total in the destination currency.</param>
/// <param name="Lines">One line per source currency in the bag, in ISO-lexicographic order.</param>
public readonly record struct MoneyBagConversionAudit<TTarget>(
    Money<TTarget> Total,
    IReadOnlyList<MoneyBagConversionLine> Lines)
    where TTarget : ICurrency;
