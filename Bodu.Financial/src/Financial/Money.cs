// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Represents an immutable monetary amount whose currency is identified at runtime by ISO 4217 code, in contrast to
/// <see cref="Money{TCurrency}" /> where the currency is a type parameter.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Money" /> is the runtime-tagged counterpart of <see cref="Money{TCurrency}" />. Use it when the currency
/// is data rather than part of the type — for example, when deserialising payloads that carry the currency code, or
/// when modelling a generic invoicing engine that processes arbitrary currencies. The trade-off is that cross-currency
/// arithmetic and comparison surface as <see cref="InvalidOperationException" /> at runtime instead of as compile
/// errors.
/// </para>
/// <para>
/// The amount is rounded on construction to the minor-unit precision reported by <see cref="CurrencyRegistry" /> for
/// the supplied ISO code, using banker's rounding by default. A structurally valid ISO code that is not a known
/// currency is rejected.
/// </para>
/// <para>
/// JSON serialization ships in the companion <c>Bodu.Financial.Serialization.Json</c> package; the type carries no
/// <c>[JsonConverter]</c> attribute, so register the financial converters on the target <c>JsonSerializerOptions</c>
/// via its <c>AddFinancialJsonConverters</c> extension.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Construct from an amount + ISO code; the amount is rounded to the currency's minor units on creation.
/// var price = new Money(19.999m, CurrencyCode.USD);   // 20.00 USD (banker's rounding)
/// var tax = new Money(1.60m, CurrencyCode.USD);
///
/// // Same-currency arithmetic and comparison.
/// Money total = price + tax;                          // 21.60 USD
/// bool isDearer = total > price;                      // true
///
/// // Scaling by a scalar re-rounds the product to the minor unit.
/// Money tripled = price * 3m;                         // 60.00 USD
///
/// // Mixing currencies throws at runtime - use Money<TCurrency> for compile-time safety.
/// var euros = new Money(5m, CurrencyCode.EUR);
/// // _ = total + euros;                               // throws InvalidOperationException
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Money
{
    /// <summary>The rounded amount in the major unit of the currency identified by <see cref="_code" />.</summary>
    private readonly decimal _amount;

    /// <summary>The currency identifying this value, or <see cref="CurrencyCode.None" /> for a default-initialised value.</summary>
    private readonly CurrencyCode _code;

    /// <summary>The explicit minor-unit scale plus one, or <c>0</c> when no explicit scale is associated and the precision is derived from <see cref="CurrencyRegistry" />.</summary>
    /// <remarks>
    /// The "+1" bias lets a default-initialised <see cref="Money" /> (all-zero fields) mean "use the registry" rather
    /// than "explicit scale 0". A value of <c>n + 1</c> denotes an explicit minor-unit scale of <c>n</c> in the range
    /// <c>0</c>..<c>28</c>, set only by the internal explicit-scale settlement path (<see cref="FromExplicitScale(decimal, CurrencyCode, int, MidpointRounding)" />)
    /// used by <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" />.
    /// </remarks>
    private readonly byte _explicitScalePlusOne;

    /// <summary>
    /// Initializes a new instance of the <see cref="Money" /> struct from an amount and currency, rounding the amount
    /// to the currency's minor-unit precision using the supplied rule.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="code">The currency identifying this value.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="code" /> is <see cref="CurrencyCode.None" /> or is not a defined <see cref="CurrencyCode" />
    /// member.
    /// </exception>
    public Money(decimal amount, CurrencyCode code, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(code);

        _amount = MoneyMath.Round(amount, CurrencyInfo.FromCurrencyCode(code).MinorUnits, rounding);
        _code = code;
        _explicitScalePlusOne = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money" /> struct from pre-computed field values, bypassing
    /// validation and rounding.
    /// </summary>
    /// <param name="amount">The stored amount.</param>
    /// <param name="code">The currency, or <see cref="CurrencyCode.None" /> for a currency-less value.</param>
    /// <param name="explicitScalePlusOne">
    /// The explicit minor-unit scale plus one, or <c>0</c> to derive precision from <see cref="CurrencyRegistry" />.
    /// </param>
    private Money(decimal amount, CurrencyCode code, byte explicitScalePlusOne)
    {
        _amount = amount;
        _code = code;
        _explicitScalePlusOne = explicitScalePlusOne;
    }

    /// <summary>
    /// Creates a <see cref="Money" /> from an amount and currency already at the currency's minor-unit precision.
    /// </summary>
    /// <param name="amount">The normalised amount.</param>
    /// <param name="code">The currency.</param>
    /// <returns>The wrapped <see cref="Money" />.</returns>
    internal static Money FromNormalized(decimal amount, CurrencyCode code) =>
        new(amount, code, (byte)0);

    /// <summary>
    /// Returns a copy of this value with a different amount, preserving the currency and any explicit scale.
    /// </summary>
    /// <param name="amount">The replacement amount, assumed to already be at this value's minor-unit precision.</param>
    /// <returns>The updated <see cref="Money" />.</returns>
    private Money WithAmount(decimal amount) =>
        new(amount, _code, _explicitScalePlusOne);

    /// <summary>
    /// Returns a copy of this value with <paramref name="amount" /> rounded to this value's minor-unit precision,
    /// preserving the ISO code and any explicit scale.
    /// </summary>
    /// <param name="amount">The raw amount to round.</param>
    /// <returns>The updated <see cref="Money" />.</returns>
    private Money WithRoundedAmount(decimal amount) =>
        new(MoneyMath.Round(amount, MinorUnits, MidpointRounding.ToEven), _code, _explicitScalePlusOne);

    /// <summary>
    /// Returns a copy of this value with <paramref name="amount" /> rounded to this value's minor-unit precision using
    /// the supplied <paramref name="rounding" /> rule, preserving the ISO code and any explicit scale.
    /// </summary>
    /// <param name="amount">The raw amount to round.</param>
    /// <param name="rounding">The midpoint-rounding rule applied to <paramref name="amount" />.</param>
    /// <returns>The updated <see cref="Money" />.</returns>
    private Money WithRoundedAmount(decimal amount, MidpointRounding rounding) =>
        new(MoneyMath.Round(amount, MinorUnits, rounding), _code, _explicitScalePlusOne);

    /// <summary>
    /// Throws when this value is a default-initialised, currency-less <see cref="Money" /> that must not participate in
    /// a financial operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This value carries no ISO code (a default-initialised <see cref="Money" />).
    /// </exception>
    private void EnsureHasCurrency()
    {
        if (_code == CurrencyCode.None)
        {
            throw new InvalidOperationException(
                FinancialResourceStrings.Op_Invalid_MoneyRequiresCurrency);
        }
    }
}
