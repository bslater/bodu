// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CashRoundingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies whether a <see cref="MonetaryContext" /> snaps results to the currency's physical cash denomination.
/// </summary>
public enum CashRoundingPolicy
{
    /// <summary>
    /// Do not apply cash rounding; results are expressed at the resolved minor-unit scale.
    /// </summary>
    None = 0,

    /// <summary>
    /// Snap results to the currency's cash-rounding increment (for example, the Swiss 5-rappen increment).
    /// </summary>
    CurrencyCashIncrement = 1,
}
