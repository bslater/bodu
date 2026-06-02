// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Conversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

public readonly partial struct Money
{
    /// <summary>
<<<<<<< HEAD
    /// Converts this <see cref="Money" /> to a strongly-typed <see cref="Money{TCurrency}" /> when the runtime currency
    /// matches <typeparamref name="TCurrency" />.
=======
    /// Returns a high-precision <see cref="CalculatedMoney" /> in the same currency, suitable for deferred-rounding
    /// calculation chains.
    /// </summary>
    /// <returns>A <see cref="CalculatedMoney" /> carrying this value's amount and ISO code.</returns>
    /// <exception cref="InvalidOperationException">This value carries no ISO code (default-initialised).</exception>
    public CalculatedMoney ToCalculated()
    {
        if (string.IsNullOrEmpty(IsoCode))
            throw new InvalidOperationException(FinancialResourceStrings.Op_Invalid_MoneyRequiresCurrency);

        return new CalculatedMoney(_amount, IsoCode);
    }

    /// <summary>
    /// Converts this <see cref="Money" /> to a strongly-typed <see cref="Money{TCurrency}" /> when the runtime
    /// currency matches <typeparamref name="TCurrency" />.
>>>>>>> 066692f232b4d4e9e85dd58b3fc6144c4d8d78ff
    /// </summary>
    /// <typeparam name="TCurrency">The target currency type.</typeparam>
    /// <returns>The strongly-typed monetary value.</returns>
    /// <exception cref="InvalidOperationException">
    /// The instance's <see cref="IsoCode" /> does not match the ISO code of <typeparamref name="TCurrency" />.
    /// </exception>
    public Money<TCurrency> As<TCurrency>()
        where TCurrency : ICurrency
    {
        return !string.Equals(IsoCode, CurrencyMetadata<TCurrency>.Value.IsoCode, StringComparison.Ordinal)
            ? throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_CurrencyMismatchInAs,
                    IsoCode,
                    typeof(TCurrency).Name,
                    CurrencyMetadata<TCurrency>.Value.IsoCode))
            : new Money<TCurrency>(_amount);
    }

    /// <summary>
    /// Attempts to convert this <see cref="Money" /> to a strongly-typed <see cref="Money{TCurrency}" />.
    /// </summary>
    /// <typeparam name="TCurrency">The target currency type.</typeparam>
    /// <param name="result">When this method returns <see langword="true" />, the strongly-typed value.</param>
    /// <returns><see langword="true" /> when the currencies match; otherwise <see langword="false" />.</returns>
    public bool TryAs<TCurrency>(out Money<TCurrency> result)
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
    /// Converts this amount to a different currency at the supplied exchange rate, rounding to the target currency's
    /// minor-unit precision.
    /// </summary>
    /// <param name="targetIsoCode">The ISO 4217 code of the destination currency.</param>
    /// <param name="exchangeRate">The rate, expressed as units of the destination per unit of the source.</param>
    /// <returns>The converted <see cref="Money" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="targetIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="targetIsoCode" /> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="exchangeRate" /> is negative.</exception>
    public Money Convert(string targetIsoCode, decimal exchangeRate) =>
        Convert(targetIsoCode, exchangeRate, MidpointRounding.ToEven);

    /// <summary>
    /// Converts this amount using an auditable <see cref="ExchangeRate" /> quote, rounding to the target currency under
    /// <paramref name="context" />.
    /// </summary>
    /// <param name="rate">The exchange-rate quote whose source currency must match this value's currency.</param>
    /// <param name="context">
    /// The monetary context governing rounding; <see langword="null" /> selects <see cref="MonetaryContext.Default" />.
    /// </param>
    /// <returns>The converted <see cref="Money" /> in the rate's target currency.</returns>
    /// <exception cref="InvalidOperationException">
    /// The rate's source currency does not match this value's <see cref="IsoCode" />.
    /// </exception>
    /// <remarks>
    /// Prefer this quote-first overload over the raw-decimal <see cref="Convert(string, decimal, MidpointRounding)" />
    /// surface: it preserves the rate's direction, date, provider, and inversion metadata for downstream audit, and the
    /// companion <see cref="ConvertWithResult(ExchangeRate, MonetaryContext?)" /> returns that metadata alongside the
    /// rounding adjustment.
    /// </remarks>
    public Money Convert(ExchangeRate rate, MonetaryContext? context = null) =>
        ConvertWithResult(rate, context).Target;

    /// <summary>
    /// Converts this amount using an auditable <see cref="ExchangeRate" /> quote and returns the full conversion
    /// result, including the rate, context, and rounding adjustment.
    /// </summary>
    /// <param name="rate">The exchange-rate quote whose source currency must match this value's currency.</param>
    /// <param name="context">
    /// The monetary context governing rounding; <see langword="null" /> selects <see cref="MonetaryContext.Default" />.
    /// </param>
    /// <returns>The <see cref="MoneyConversionResult" /> describing the conversion.</returns>
    /// <exception cref="InvalidOperationException">
    /// The rate's source currency does not match this value's <see cref="IsoCode" />.
    /// </exception>
    public MoneyConversionResult ConvertWithResult(ExchangeRate rate, MonetaryContext? context = null)
    {
        if (!string.Equals(rate.FromIsoCode, IsoCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_ConversionRateDirectionMismatch,
                    IsoCode,
                    rate.FromIsoCode,
                    rate.ToIsoCode));
        }

        MonetaryContext effective = context ?? MonetaryContext.Default;
        var raw = rate.Convert(_amount);
        Money target = new CalculatedMoney(raw, rate.ToIsoCode).RoundToMoney(effective);

        return new MoneyConversionResult(this, target, rate, effective, target.Amount - raw);
    }

    /// <summary>
    /// Converts this amount to a different currency at the supplied exchange rate, using the specified rounding rule.
    /// </summary>
    /// <param name="targetIsoCode">The ISO 4217 code of the destination currency.</param>
    /// <param name="exchangeRate">The rate, expressed as units of the destination per unit of the source.</param>
    /// <param name="rounding">The midpoint-rounding rule applied at the target precision.</param>
    /// <returns>The converted <see cref="Money" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="targetIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="targetIsoCode" /> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="exchangeRate" /> is negative.</exception>
    public Money Convert(string targetIsoCode, decimal exchangeRate, MidpointRounding rounding)
    {
        FinancialThrowHelper.ThrowIfExchangeRateNotPositive(exchangeRate);
        return new Money(_amount * exchangeRate, targetIsoCode, rounding);
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
