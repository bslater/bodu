// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

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
    /// Gets the currency identifying this value.
    /// </summary>
    /// <returns>The stored <see cref="CurrencyCode" />, or <see cref="CurrencyCode.None" /> for a default-initialised value.</returns>
    public CurrencyCode Code =>
        _code;

    /// <summary>
    /// Gets the ISO 4217 alphabetic code identifying the currency, or an empty string for a default-initialised value.
    /// </summary>
    /// <returns>The currency's ISO code.</returns>
    public string IsoCode =>
        _code == CurrencyCode.None ? string.Empty : _code.ToString();

    /// <summary>
    /// Gets a value indicating whether this value is a default-initialised, currency-less <see cref="Money" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when this value was produced by <c>default(Money)</c> and therefore carries no currency;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// A default-initialised <see cref="Money" /> is the unavoidable zero value of the struct, not a valid financial
    /// zero. It carries no ISO code and is rejected by every arithmetic, allocation, and conversion operation; use
    /// <see cref="Zero(string)" /> to obtain a usable zero for a specific currency. Equality, hashing, and formatting
    /// remain safe to call so diagnostic surfaces do not throw.
    /// </remarks>
    public bool IsDefault =>
        _code == CurrencyCode.None;

    /// <summary>
    /// Gets a value indicating whether this value carries a currency and can participate in financial operations.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when this value carries an ISO code; otherwise <see langword="false" /> for a
    /// default-initialised value.
    /// </returns>
    public bool HasCurrency =>
        _code != CurrencyCode.None;

    /// <summary>
    /// Gets the minor-unit precision of this value.
    /// </summary>
    /// <returns>
    /// The explicit scale supplied at construction when this value carries one; otherwise the currency's minor-unit
    /// precision as reported by <see cref="CurrencyRegistry" />, or zero when the currency is unknown to the registry.
    /// </returns>
    /// <remarks>
    /// A value materialised by the settlement path
    /// (<see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" />) at a precision other than the currency's
    /// registered minor units reports that explicit scale, keeping the stored precision and the reported minor units
    /// self-consistent. Ordinary construction always resolves the precision from the registry.
    /// </remarks>
    public int MinorUnits =>
        _explicitScalePlusOne > 0
            ? _explicitScalePlusOne - 1
            : CurrencyResolution.TryGet(IsoCode, out CurrencyInfo? info) ? info!.MinorUnits : 0;

    /// <summary>
    /// Returns a <see cref="Money" /> representing zero of the specified currency.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 code.</param>
    /// <returns>A zero <see cref="Money" /> in <paramref name="isoCode" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> is not exactly three uppercase ASCII letters, or is structurally valid but not
    /// registered in <see cref="CurrencyRegistry" />.
    /// </exception>
    public static Money Zero(string isoCode) =>
        new(0m, isoCode);
}
