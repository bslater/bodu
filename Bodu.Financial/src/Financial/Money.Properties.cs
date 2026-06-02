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
    /// Gets a value indicating whether this value is a default-initialised, currency-less <see cref="Money" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when this value was produced by <c>default(Money)</c> and therefore carries no
    /// currency; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// A default-initialised <see cref="Money" /> is the unavoidable zero value of the struct, not a valid financial
    /// zero. It carries no ISO code and is rejected by every arithmetic, allocation, and conversion operation; use
    /// <see cref="Zero(string)" /> to obtain a usable zero for a specific currency. Equality, hashing, and formatting
    /// remain safe to call so diagnostic surfaces do not throw.
    /// </remarks>
    public bool IsDefault =>
        _isoCode is null;

    /// <summary>
    /// Gets a value indicating whether this value carries a currency and can participate in financial operations.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when this value carries an ISO code; otherwise <see langword="false" /> for a
    /// default-initialised value.
    /// </returns>
    public bool HasCurrency =>
        _isoCode is not null;

    /// <summary>
    /// Gets the minor-unit precision of this value.
    /// </summary>
    /// <returns>
    /// The explicit scale supplied at construction when this value carries one; otherwise the currency's minor-unit
    /// precision as reported by <see cref="CurrencyRegistry" />, or zero when the currency is unknown to the registry.
    /// </returns>
    /// <remarks>
    /// A value constructed for an unregistered currency through
    /// <see cref="Money.FromUnchecked(decimal, string, int, MidpointRounding)" /> reports the caller-supplied scale,
    /// keeping the stored precision and the reported minor units self-consistent. Ordinary construction always resolves
    /// the precision from the registry.
    /// </remarks>
    public int MinorUnits =>
        _explicitScalePlusOne > 0
            ? _explicitScalePlusOne - 1
            : CurrencyRegistry.TryGet(IsoCode, out CurrencyInfo? info) ? info!.MinorUnits : 0;

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
    /// <exception cref="InvalidOperationException">This value is a default-initialised, currency-less <see cref="Money" />.</exception>
    public Money Abs
    {
        get
        {
            EnsureHasCurrency();
            return WithAmount(Math.Abs(_amount));
        }
    }

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
