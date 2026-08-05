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
    /// For amounts that are <em>computed</em> rather than quoted, prefer accumulating in <see cref="CalculatedMoney" />
    /// and settling once through <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" /> with
    /// <see cref="ScalePolicy.Custom" /> — that path defers rounding to a single, explicit settlement decision and
    /// produces the same explicit-scale value.
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
    /// Returns this value re-expressed at the supplied minor-unit scale, rounding the amount when the new scale is
    /// coarser and padding the reported precision when it is finer.
    /// </summary>
    /// <param name="minorUnits">The number of fractional digits the result reports as its minor units.</param>
    /// <param name="rounding">
    /// The midpoint-rounding rule applied when normalising to <paramref name="minorUnits" />.
    /// </param>
    /// <returns>The re-scaled monetary value.</returns>
    /// <exception cref="InvalidOperationException">
    /// This value is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minorUnits" /> is outside the range 0 to 28.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Rescaling to a coarser precision drops sub-scale digits under <paramref name="rounding" /> — rescaling a
    /// six-place unit price to a currency's two registered minor units is a plain settlement of that single value.
    /// Rescaling finer is lossless: the amount is unchanged and only the reported precision (and therefore formatting
    /// and the serialized <c>scale</c>) widens. The equivalent operation is <c>transformScale</c> in dinero.js and
    /// <c>withScale</c> on Joda's <c>BigMoney</c>.
    /// </para>
    /// <para>
    /// For policy-aware settlement — cash-rounding increments, a <see cref="ScalePolicy" />, or a configured rounding
    /// strategy — settle through <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" /> instead; this method
    /// applies only the supplied midpoint rule.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using Bodu.Financial;
    ///
    /// Money price = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);
    ///
    /// price.Rescale(2);   // USD 145.68  (MinorUnits = 2)
    /// price.Rescale(8);   // USD 145.67891200 (MinorUnits = 8, amount unchanged)
    ///]]>
    /// </code>
    /// </example>
    public Money Rescale(int minorUnits, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        EnsureHasCurrency();

        return FromExplicitScale(_amount, _code, minorUnits, rounding);
    }

    /// <summary>
    /// Returns this value with trailing-zero precision removed: the reported scale is reduced to the smallest scale
    /// that still represents the amount exactly, but never below the currency's registered minor units.
    /// </summary>
    /// <returns>
    /// The trimmed monetary value, or this value unchanged when no trailing-zero scale can be removed.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// This value is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <remarks>
    /// Trimming never changes the numeric amount — only the reported precision. A six-place <c>12.500000 USD</c> trims
    /// to the registered two places (<c>12.50</c>), a six-place <c>12.340010</c> trims to five, and a value whose
    /// finest digits are significant (<c>12.345678</c>) is returned unchanged. The registered minor units are the
    /// floor, so ordinary money is always a no-op. The equivalent operation is <c>trimScale</c> in dinero.js.
    /// </remarks>
    public Money TrimScale()
    {
        EnsureHasCurrency();

        int registry = CurrencyResolution.TryGet(IsoCodeOrEmpty, out CurrencyInfo? info) ? info!.MinorUnits : 0;

        // Walk the reported scale down while the dropped digit is a trailing zero (numeric decimal equality makes the
        // check exact), stopping at the currency's registered floor. The loop is bounded by decimal's 28-digit scale.
        int scale = MinorUnits;
        while (scale > registry && decimal.Round(_amount, scale - 1) == _amount)
            scale--;

        return scale == MinorUnits ? this : FromExplicitScale(_amount, _code, scale);
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
