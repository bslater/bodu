// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConversionRoundingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies when the result of a currency conversion is rounded.
/// </summary>
/// <remarks>
/// <see cref="MonetaryContext.ConversionRounding" /> carries this value, but no conversion consults it yet: every
/// conversion rounds at the target, as <see cref="RoundAtTarget" /> describes, whichever value the context holds.
/// </remarks>
public enum ConversionRoundingPolicy
{
    /// <summary>
    /// Round the converted amount to the target currency's scale immediately.
    /// </summary>
    RoundAtTarget = 0,

    /// <summary>
    /// Reserved for deferring rounding, preserving the full-precision converted amount for further calculation. No
    /// conversion honours it yet.
    /// </summary>
    Defer = 1,
}
