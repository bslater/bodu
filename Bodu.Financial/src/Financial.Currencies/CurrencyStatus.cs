// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyStatus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

/// <summary>
/// Describes the ISO 4217 lifecycle status of a <see cref="CurrencyCode" /> member.
/// </summary>
/// <remarks>
/// The values are powers of two so the classification can be extended with additional, combinable statuses without
/// invalidating existing annotations.
/// </remarks>
[Flags]
public enum CurrencyStatus
{
    /// <summary>
    /// No status; carried by the <see cref="CurrencyCode.None" /> sentinel.
    /// </summary>
    None = 0,

    /// <summary>
    /// An active ISO 4217 currency in current circulation.
    /// </summary>
    Active = 1,

    /// <summary>
    /// A historic ISO 4217 currency that has been demonetized.
    /// </summary>
    Historic = 2,
}
