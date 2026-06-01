// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyConversionResult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Captures the audit trail of a single runtime-tagged currency conversion: the source and settled target amounts, the
/// exchange rate applied, the monetary context that governed rounding, and how much rounding moved the result.
/// </summary>
/// <param name="Source">The original source amount.</param>
/// <param name="Target">The converted amount, rounded according to <paramref name="Context" />.</param>
/// <param name="Rate">The exchange rate applied to produce <paramref name="Target" />.</param>
/// <param name="Context">The monetary context that governed rounding.</param>
/// <param name="RoundingAdjustment">
/// The signed difference between the rounded target amount and the exact (unrounded) converted amount, or
/// <see langword="null" /> when rounding was deferred.
/// </param>
public readonly record struct MoneyConversionResult(
    Money Source,
    Money Target,
    ExchangeRate Rate,
    MonetaryContext Context,
    decimal? RoundingAdjustment);
