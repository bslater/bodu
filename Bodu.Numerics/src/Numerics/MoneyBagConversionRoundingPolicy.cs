// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagConversionRoundingPolicy.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Selects the rounding policy used by <see cref="MoneyBag.ConvertTo{TTarget}(IExchangeRateProvider, MoneyBagConversionRoundingPolicy)" />
/// when aggregating per-currency balances into a single target-currency total.
/// </summary>
/// <remarks>
/// The two policies are deliberately spelled out because both are common — neither is wrong, and the choice
/// depends on the accounting workflow. Aggregate-then-round (the default) suits FX position totals where
/// per-line precision is irrelevant; per-line rounding suits ledger / tax workflows where each converted
/// balance must settle independently at the destination precision before aggregation.
/// </remarks>
public enum MoneyBagConversionRoundingPolicy
{
    /// <summary>
    /// Sum every converted balance as a raw <see cref="decimal" /> first, then round the aggregate once to
    /// the destination currency's minor-unit precision. This is the default — it minimises the cumulative
    /// rounding error in a portfolio FX total.
    /// </summary>
    SumRawThenRound = 0,

    /// <summary>
    /// Round each converted per-currency balance to the destination currency's minor-unit precision before
    /// adding it into the total. Use this for ledger and tax workflows where each settled line must be
    /// representable at the destination precision before aggregation.
    /// </summary>
    RoundEachCurrencyThenSum = 1,
}
