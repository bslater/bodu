// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public readonly partial struct CalculatedMoney
{
    /// <summary>
    /// Gets the unrounded monetary amount in the major unit of the currency.
    /// </summary>
    /// <value>The full-precision amount stored by this instance.</value>
    public decimal Amount =>
        _amount;

    /// <summary>
    /// Gets the currency identifying this value.
    /// </summary>
    /// <value>
    /// The stored <see cref="CurrencyCode" />, or <see cref="CurrencyCode.None" /> for a default-initialised value.
    /// </value>
    public CurrencyCode Code =>
        _code;

    /// <summary>
    /// Gets the ISO 4217 alphabetic code identifying the currency, or an empty string for a default-initialised value.
    /// </summary>
    /// <value>The currency's ISO code, used by the settlement and diagnostic paths that need its string form.</value>
    internal string IsoCodeOrEmpty =>
        _code == CurrencyCode.None ? string.Empty : _code.ToString();

    /// <summary>
    /// Gets a value indicating whether this amount is zero.
    /// </summary>
    /// <value><see langword="true" /> when the amount is zero; otherwise <see langword="false" />.</value>
    public bool IsZero =>
        _amount == 0m;

    /// <summary>
    /// Gets the sign of this amount.
    /// </summary>
    /// <value><c>-1</c>, <c>0</c>, or <c>1</c>.</value>
    public int Sign =>
        Math.Sign(_amount);
}
