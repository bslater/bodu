// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Helpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public readonly partial struct Money
{
    /// <summary>
    /// Creates a runtime-tagged <see cref="Money" /> from the supplied amount and ISO 4217 code, rounding to the
    /// currency's minor-unit precision.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="isoCode">The ISO 4217 three-letter alphabetic code identifying the currency.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <returns>The constructed monetary value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> is not exactly three uppercase ASCII letters.
    /// </exception>
    public static Money From(decimal amount, string isoCode, MidpointRounding rounding = MidpointRounding.ToEven) =>
        new(amount, isoCode, rounding);

    /// <summary>
    /// Creates a <see cref="Money" /> that carries an explicit minor-unit scale, rounding the amount to that scale and
    /// reporting it from <see cref="Money.MinorUnits" />.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="code">The currency identifying this value.</param>
    /// <param name="minorUnits">
    /// The number of fractional digits to round to and report as the value's minor units.
    /// </param>
    /// <param name="rounding">
    /// The midpoint-rounding rule applied when normalising to <paramref name="minorUnits" />.
    /// </param>
    /// <returns>The constructed monetary value carrying an explicit scale.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="code" /> is not a defined currency, or <paramref name="minorUnits" /> is outside the range 0 to 28.
    /// </exception>
    /// <remarks>
    /// Settlement helper used by <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" /> when a monetary context
    /// requests a precision other than the currency's registered minor units. Because the scale is supplied and stored,
    /// the value reports the resolved precision rather than the registry's default.
    /// </remarks>
    internal static Money FromExplicitScale(decimal amount, CurrencyCode code, int minorUnits, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(code);
        FinancialThrowHelper.ThrowIfMinorUnitsOutOfRange(minorUnits, code.ToString());

        return new Money(MoneyMath.Round(amount, minorUnits, rounding), code, (byte)(minorUnits + 1));
    }

    /// <summary>
    /// Creates a runtime-tagged <see cref="Money" /> from the supplied amount and currency metadata.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="currency">The currency metadata identifying the result's currency.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <returns>The constructed monetary value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency" /> is <see langword="null" />.</exception>
    public static Money From(decimal amount, CurrencyInfo currency, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        ThrowHelper.ThrowIfNull(currency);
        return new(amount, currency.IsoCode, rounding);
    }

    /// <summary>
    /// Creates a runtime-tagged <see cref="Money" /> from the supplied amount and ISO 4217 enum value.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="code">The ISO 4217 currency code.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <returns>The constructed monetary value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="code" /> is <see cref="CurrencyCode.None" /> or is not a defined <see cref="CurrencyCode" />
    /// member.
    /// </exception>
    public static Money From(decimal amount, CurrencyCode code, MidpointRounding rounding = MidpointRounding.ToEven) =>
        new(amount, code, rounding);

    /// <summary>
    /// Creates a <see cref="Money{TCurrency}" /> from the supplied amount, rounding to the currency's minor-unit
    /// precision using banker's rounding.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type identifier.</typeparam>
    /// <param name="amount">The monetary amount in the major unit of <typeparamref name="TCurrency" />.</param>
    /// <returns>The constructed monetary value.</returns>
    public static Money<TCurrency> Of<TCurrency>(decimal amount)
        where TCurrency : ICurrency =>
        new(amount);

    /// <summary>
    /// Creates a <see cref="Money{TCurrency}" /> from the supplied amount and rounding rule.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type identifier.</typeparam>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalizing to the minor-unit precision.</param>
    /// <returns>The constructed monetary value.</returns>
    public static Money<TCurrency> Of<TCurrency>(decimal amount, MidpointRounding rounding)
        where TCurrency : ICurrency =>
        new(amount, rounding);

    /// <summary>
    /// Returns the zero value of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type identifier.</typeparam>
    /// <returns>The zero monetary amount.</returns>
    public static Money<TCurrency> Zero<TCurrency>()
        where TCurrency : ICurrency =>
        default;
}
