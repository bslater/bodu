// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConversionRoundingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies when a <see cref="MonetaryContext" /> rounds the result of a currency conversion.
/// </summary>
public enum ConversionRoundingPolicy
{
    /// <summary>
    /// Round the converted amount to the target currency's scale immediately.
    /// </summary>
    RoundAtTarget = 0,

    /// <summary>
    /// Defer rounding, preserving the full-precision converted amount for further calculation.
    /// </summary>
    Defer = 1,
}
