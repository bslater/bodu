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
    /// Creates a runtime-tagged <see cref="Money" /> from the supplied amount and ISO 4217 code, applying the given
    /// <paramref name="policy" /> when the code is structurally valid but not registered in
    /// <see cref="CurrencyRegistry" />.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="isoCode">The ISO 4217 three-letter alphabetic code identifying the currency.</param>
    /// <param name="policy">The behaviour applied when the currency is unknown to the registry.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <returns>The constructed monetary value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> is not exactly three uppercase ASCII letters; or it is unregistered and
    /// <paramref name="policy" /> is <see cref="UnknownCurrencyPolicy.Reject" />; or it is unregistered and
    /// <paramref name="policy" /> is <see cref="UnknownCurrencyPolicy.AllowWithExplicitScale" /> (which requires a
    /// scale, supplied through <see cref="FromUnchecked(decimal, string, int, MidpointRounding)" />).
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="UnknownCurrencyPolicy" /> member.
    /// </exception>
    /// <remarks>
    /// Registered currencies are rounded to the registry's minor units regardless of <paramref name="policy" />. The
    /// policy governs only the unregistered case: <see cref="UnknownCurrencyPolicy.AllowUnscaled" /> stores the amount
    /// at its source precision (reporting zero minor units), and
    /// <see cref="UnknownCurrencyPolicy.AllowWithExplicitScale" /> is rejected here because it needs an explicit scale
    /// — call <see cref="FromUnchecked(decimal, string, int, MidpointRounding)" /> for that path.
    /// </remarks>
    public static Money From(
        decimal amount,
        string isoCode,
        UnknownCurrencyPolicy policy,
        MidpointRounding rounding = MidpointRounding.ToEven)
    {
        ThrowHelper.ThrowIfNull(isoCode);
        FinancialThrowHelper.ThrowIfUnknownCurrencyPolicyUndefined(policy);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(isoCode);

        if (CurrencyRegistry.TryGet(isoCode, out CurrencyInfo? info) && info is not null)
            return new Money(decimal.Round(amount, info.MinorUnits, rounding), isoCode, (byte)0);

        return policy switch
        {
            UnknownCurrencyPolicy.AllowUnscaled => new Money(amount, isoCode, (byte)0),
            UnknownCurrencyPolicy.AllowWithExplicitScale => throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_Invalid_UnknownCurrencyRequiresScale, isoCode),
                nameof(policy)),
            _ => throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_Invalid_UnknownCurrencyRejected, isoCode),
                nameof(isoCode)),
        };
    }

    /// <summary>
    /// Creates a runtime-tagged <see cref="Money" /> for a currency that need not be registered, rounding the amount to
    /// the caller-supplied minor-unit scale so the result reports that scale from <see cref="Money.MinorUnits" />.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="isoCode">The ISO 4217 three-letter alphabetic code identifying the currency.</param>
    /// <param name="minorUnits">
    /// The number of fractional digits to round to and report as the value's minor units.
    /// </param>
    /// <param name="rounding">
    /// The midpoint-rounding rule applied when normalising to <paramref name="minorUnits" />.
    /// </param>
    /// <returns>The constructed monetary value carrying an explicit scale.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> is not exactly three uppercase ASCII letters.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minorUnits" /> is outside the range 0 to 28.
    /// </exception>
    /// <remarks>
    /// This is the explicit escape hatch for staging or custom currencies that are intentionally absent from
    /// <see cref="CurrencyRegistry" />. Because the scale is supplied and stored, the resulting value never exhibits
    /// the "source precision retained but zero minor units reported" inconsistency that ordinary unregistered
    /// construction would otherwise produce.
    /// </remarks>
    public static Money FromUnchecked(decimal amount, string isoCode, int minorUnits, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(isoCode);
        FinancialThrowHelper.ThrowIfMinorUnitsOutOfRange(minorUnits, isoCode);

        return new Money(decimal.Round(amount, minorUnits, rounding), isoCode, (byte)(minorUnits + 1));
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
    /// <param name="code">The active ISO 4217 currency code.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <returns>The constructed monetary value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="code" /> is not a defined <see cref="CurrencyCode" /> member.
    /// </exception>
    public static Money From(decimal amount, CurrencyCode code, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        if (!Enum.IsDefined(code))
        {
            throw new ArgumentOutOfRangeException(
                nameof(code),
                code,
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Arg_Invalid_CurrencyCodeNotMapped,
                    code,
                    (int)code));
        }

        return new(amount, code.ToString(), rounding);
    }

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
