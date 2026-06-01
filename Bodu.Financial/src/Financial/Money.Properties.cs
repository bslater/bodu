// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct Money
{
    /// <summary>
    /// Gets the rounded monetary amount in the major unit of the currency.
    /// </summary>
    /// <returns>The amount stored by this instance.</returns>
    public decimal Amount =>
        _amount;

    /// <summary>
    /// Gets the ISO 4217 alphabetic code identifying the currency, or an empty string for a default-initialised value.
    /// </summary>
    /// <returns>The currency's ISO code.</returns>
    public string IsoCode =>
        _isoCode ?? string.Empty;

    /// <summary>
    /// Gets the minor-unit precision of the currency, as reported by <see cref="CurrencyRegistry" />.
    /// </summary>
    /// <returns>
    /// The number of fractional digits of the currency's minor unit, or zero when the currency is unknown to the
    /// registry.
    /// </returns>
    public int MinorUnits =>
        CurrencyRegistry.TryGet(IsoCode, out CurrencyInfo? info) ? info!.MinorUnits : 0;

    /// <summary>
    /// Gets a value indicating whether this amount is zero.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="Amount" /> is zero; otherwise <see langword="false" />.
    /// </returns>
    public bool IsZero =>
        _amount == 0m;

    /// <summary>
    /// Gets a value indicating whether this amount is strictly greater than zero.
    /// </summary>
    /// <returns><see langword="true" /> when positive; otherwise <see langword="false" />.</returns>
    public bool IsPositive =>
        _amount > 0m;

    /// <summary>
    /// Gets a value indicating whether this amount is strictly less than zero.
    /// </summary>
    /// <returns><see langword="true" /> when negative; otherwise <see langword="false" />.</returns>
    public bool IsNegative =>
        _amount < 0m;

    /// <summary>
    /// Gets the sign of this amount.
    /// </summary>
    /// <returns><c>-1</c>, <c>0</c>, or <c>1</c>.</returns>
    public int Sign =>
        Math.Sign(_amount);

    /// <summary>
    /// Gets the absolute value of this amount.
    /// </summary>
    /// <returns>A <see cref="Money" /> with the same ISO code and a non-negative amount.</returns>
    public Money Abs =>
        FromNormalized(Math.Abs(_amount), IsoCode);

    /// <summary>
    /// Returns a <see cref="Money" /> representing zero of the specified currency.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 code.</param>
    /// <returns>A zero <see cref="Money" /> in <paramref name="isoCode" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="isoCode" /> is empty or whitespace.</exception>
    public static Money Zero(string isoCode) =>
        new(0m, isoCode);
}
