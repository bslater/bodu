// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct CalculatedMoney
{
    /// <summary>
    /// Gets the unrounded monetary amount in the major unit of the currency.
    /// </summary>
    /// <returns>The full-precision amount stored by this instance.</returns>
    public decimal Amount =>
        _amount;

    /// <summary>
    /// Gets the ISO 4217 alphabetic code identifying the currency, or an empty string for a default-initialised value.
    /// </summary>
    /// <returns>The currency's ISO code.</returns>
    public string IsoCode =>
        _isoCode ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether this amount is zero.
    /// </summary>
    /// <returns><see langword="true" /> when the amount is zero; otherwise <see langword="false" />.</returns>
    public bool IsZero =>
        _amount == 0m;

    /// <summary>
    /// Gets the sign of this amount.
    /// </summary>
    /// <returns><c>-1</c>, <c>0</c>, or <c>1</c>.</returns>
    public int Sign =>
        Math.Sign(_amount);
}
