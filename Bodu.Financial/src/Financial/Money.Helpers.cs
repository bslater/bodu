// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Helpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public readonly partial struct Money
{
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
    /// <paramref name="code" /> is not a defined currency, or <paramref name="minorUnits" /> is outside the range 0 to
    /// 28.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the direct construction route for values whose precision is known up front and differs from the
    /// currency's registered minor units — typically unit prices such as a six-decimal-place share price in a
    /// two-decimal currency. The supplied scale is stored with the value: <see cref="Money.MinorUnits" /> reports it,
    /// formatting pads to it, arithmetic and allocation round intermediate results to it rather than to the registry
    /// precision, and the JSON converters persist it so a round-trip restores the same precision.
    /// </para>
    /// <para>
    /// For amounts that are <em>computed</em> rather than quoted, prefer accumulating in
    /// <see cref="CalculatedMoney" /> and settling once through
    /// <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" /> with <see cref="ScalePolicy.Custom" /> — that
    /// path defers rounding to a single, explicit settlement decision and produces the same explicit-scale value.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using Bodu.Financial;
    ///
    /// // A share price quoted to six decimal places in two-decimal USD.
    /// Money price = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);
    ///
    /// price.MinorUnits;      // 6
    /// price.ToString("R");   // "USD 145.678912"
    ///]]>
    /// </code>
    /// </example>
    public static Money FromExplicitScale(decimal amount, CurrencyCode code, int minorUnits, MidpointRounding rounding = MidpointRounding.ToEven)
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
    /// <exception cref="InvalidOperationException">
    /// <paramref name="currency" />'s ISO code does not correspond to any <see cref="CurrencyCode" /> member.
    /// </exception>
    public static Money From(decimal amount, CurrencyInfo currency, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        ThrowHelper.ThrowIfNull(currency);
        return new(amount, currency.ToCurrencyCode(), rounding);
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
