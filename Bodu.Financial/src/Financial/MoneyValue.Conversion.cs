// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValue.Conversion.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

public readonly partial struct MoneyValue
{
    /// <summary>
    /// Converts this <see cref="MoneyValue" /> to a strongly-typed <see cref="Money{TCurrency}" /> when the runtime
    /// currency matches <typeparamref name="TCurrency" />.
    /// </summary>
    /// <typeparam name="TCurrency">The target currency type.</typeparam>
    /// <returns>The strongly-typed monetary value.</returns>
    /// <exception cref="InvalidOperationException">
    /// The instance's <see cref="IsoCode" /> does not match the ISO code of <typeparamref name="TCurrency" />.
    /// </exception>
    public Money<TCurrency> ToTyped<TCurrency>()
        where TCurrency : ICurrency
    {
        if (!string.Equals(IsoCode, CurrencyMetadata<TCurrency>.Value.IsoCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_CannotConvertMoneyValueToTyped,
                    IsoCode,
                    typeof(TCurrency).Name,
                    CurrencyMetadata<TCurrency>.Value.IsoCode));
        }

        return new Money<TCurrency>(_amount);
    }

    /// <summary>
    /// Attempts to convert this <see cref="MoneyValue" /> to a strongly-typed <see cref="Money{TCurrency}" />.
    /// </summary>
    /// <typeparam name="TCurrency">The target currency type.</typeparam>
    /// <param name="result">When this method returns <see langword="true" />, the strongly-typed value.</param>
    /// <returns><see langword="true" /> when the currencies match; otherwise <see langword="false" />.</returns>
    public bool TryToTyped<TCurrency>(out Money<TCurrency> result)
        where TCurrency : ICurrency
    {
        if (string.Equals(IsoCode, CurrencyMetadata<TCurrency>.Value.IsoCode, StringComparison.Ordinal))
        {
            result = new Money<TCurrency>(_amount);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Creates a <see cref="MoneyValue" /> from a strongly-typed <see cref="Money{TCurrency}" />.
    /// </summary>
    /// <typeparam name="TCurrency">The source currency type.</typeparam>
    /// <param name="money">The strongly-typed monetary value.</param>
    /// <returns>The runtime-tagged equivalent.</returns>
    public static MoneyValue FromTyped<TCurrency>(Money<TCurrency> money)
        where TCurrency : ICurrency =>
        FromNormalized(money.Amount, CurrencyMetadata<TCurrency>.Value.IsoCode);

    /// <summary>
    /// Converts this amount to a different currency at the supplied exchange rate, rounding to the target currency's
    /// minor-unit precision.
    /// </summary>
    /// <param name="targetIsoCode">The ISO 4217 code of the destination currency.</param>
    /// <param name="exchangeRate">The rate, expressed as units of the destination per unit of the source.</param>
    /// <returns>The converted <see cref="MoneyValue" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="targetIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="targetIsoCode" /> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="exchangeRate" /> is negative.</exception>
    public MoneyValue Convert(string targetIsoCode, decimal exchangeRate) =>
        Convert(targetIsoCode, exchangeRate, MidpointRounding.ToEven);

    /// <summary>
    /// Converts this amount to a different currency at the supplied exchange rate, using the specified rounding rule.
    /// </summary>
    /// <param name="targetIsoCode">The ISO 4217 code of the destination currency.</param>
    /// <param name="exchangeRate">The rate, expressed as units of the destination per unit of the source.</param>
    /// <param name="rounding">The midpoint-rounding rule applied at the target precision.</param>
    /// <returns>The converted <see cref="MoneyValue" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="targetIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="targetIsoCode" /> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="exchangeRate" /> is negative.</exception>
    public MoneyValue Convert(string targetIsoCode, decimal exchangeRate, MidpointRounding rounding)
    {
        FinancialThrowHelper.ThrowIfExchangeRateNotPositive(exchangeRate);
        return new MoneyValue(_amount * exchangeRate, targetIsoCode, rounding);
    }

    /// <summary>
    /// Converts this amount to a strongly-typed <see cref="Money{TTarget}" /> at the supplied exchange rate.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="exchangeRate">The rate, expressed as units of the destination per unit of the source.</param>
    /// <returns>The converted typed monetary value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="exchangeRate" /> is negative.</exception>
    public Money<TTarget> Convert<TTarget>(decimal exchangeRate)
        where TTarget : ICurrency =>
        Convert<TTarget>(exchangeRate, MidpointRounding.ToEven);

    /// <summary>
    /// Converts this amount to a strongly-typed <see cref="Money{TTarget}" /> at the supplied exchange rate, using the
    /// specified rounding rule.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="exchangeRate">The rate, expressed as units of the destination per unit of the source.</param>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The converted typed monetary value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="exchangeRate" /> is negative.</exception>
    public Money<TTarget> Convert<TTarget>(decimal exchangeRate, MidpointRounding rounding)
        where TTarget : ICurrency
    {
        FinancialThrowHelper.ThrowIfExchangeRateNotPositive(exchangeRate);
        return new Money<TTarget>(_amount * exchangeRate, rounding);
    }
}
