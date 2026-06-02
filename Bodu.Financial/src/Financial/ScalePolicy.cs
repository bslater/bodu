// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScalePolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies the fractional-digit scale a <see cref="MonetaryContext" /> rounds to at an operation boundary.
/// </summary>
public enum ScalePolicy
{
    /// <summary>
    /// Round to the currency's declared minor-unit precision.
    /// </summary>
    CurrencyMinorUnits = 0,

    /// <summary>
    /// Do not round; preserve the full precision of the computed amount.
    /// </summary>
    Unrounded = 1,

    /// <summary>
    /// Round to the caller-supplied <see cref="MonetaryContext.CustomScale" />.
    /// </summary>
    Custom = 2,
}
