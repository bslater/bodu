// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

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
/// The type-level <see cref="JsonConverterAttribute" /> always serializes the canonical
/// <see cref="Serialization.FinancialJsonPolicy.Strict" /> object shape (
/// <c>{ "amount": &lt;number&gt;, "currency": "&lt;ISO&gt;" }</c>). The compact and lenient shapes are opt-in only and
/// must be selected through
/// <see cref="Serialization.FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters(System.Text.Json.JsonSerializerOptions, Serialization.FinancialJsonPolicy)" />
/// .
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(MoneyJsonConverter))]
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
    /// <c>0</c>..<c>28</c>, set only by the internal explicit-scale settlement path
    /// (<see cref="FromExplicitScale(decimal, CurrencyCode, int, MidpointRounding)" />) used by
    /// <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" />.
    /// </remarks>
    private readonly byte _explicitScalePlusOne;

    /// <summary>
    /// Initializes a new instance of the <see cref="Money" /> struct from an amount and ISO 4217 code, rounding the
    /// amount to the currency's minor-unit precision using banker's rounding.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="isoCode">The ISO 4217 three-letter alphabetic code identifying the currency.</param>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> is not exactly three uppercase ASCII letters, or it is not registered in
    /// <see cref="CurrencyRegistry" />.
    /// </exception>
    public Money(decimal amount, string isoCode)
        : this(amount, isoCode, MidpointRounding.ToEven)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money" /> struct from an amount and ISO 4217 code, rounding the
    /// amount to the currency's minor-unit precision using the supplied rule.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="isoCode">The ISO 4217 three-letter alphabetic code identifying the currency.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> is not exactly three uppercase ASCII letters — empty, whitespace, the wrong length,
    /// lowercase, or contains non-letter characters — or it is structurally valid but not registered in
    /// <see cref="CurrencyRegistry" />.
    /// </exception>
    /// <remarks>
    /// The ISO code is validated to ISO 4217's three-uppercase-ASCII-letters shape and must resolve to a registered
    /// <see cref="CurrencyInfo" />; the amount is then rounded to the registry's <c>MinorUnits</c>. A structurally
    /// valid code that is not a known currency is rejected so a value can never store a precision it cannot describe.
    /// </remarks>
    public Money(decimal amount, string isoCode, MidpointRounding rounding)
    {
        ThrowHelper.ThrowIfNull(isoCode);
        ValidateIsoCode(isoCode);

        if (!CurrencyInfo.TryGetCurrencyCode(isoCode, out CurrencyCode code))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_Invalid_UnknownCurrencyRejected, isoCode),
                nameof(isoCode));
        }

        _amount = MoneyMath.Round(amount, CurrencyInfo.FromCurrencyCode(code).MinorUnits, rounding);
        _code = code;
        _explicitScalePlusOne = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money" /> struct from an amount and currency, rounding the amount to
    /// the currency's minor-unit precision using the supplied rule.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="code">The currency identifying this value.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="code" /> is <see cref="CurrencyCode.None" /> or is not a defined <see cref="CurrencyCode" /> member.
    /// </exception>
    public Money(decimal amount, CurrencyCode code, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(code);

        _amount = MoneyMath.Round(amount, CurrencyInfo.FromCurrencyCode(code).MinorUnits, rounding);
        _code = code;
        _explicitScalePlusOne = 0;
    }

    /// <summary>
    /// Validates that <paramref name="isoCode" /> is exactly three uppercase ASCII letters.
    /// </summary>
    /// <param name="isoCode">The candidate ISO code.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> is not exactly three uppercase ASCII letters.
    /// </exception>
    private static void ValidateIsoCode(string isoCode)
    {
        if (isoCode.Length != 3)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Arg_Invalid_IsoCodeLength,
                    isoCode,
                    isoCode.Length),
                nameof(isoCode));
        }

        for (int i = 0; i < 3; i++)
        {
            char c = isoCode[i];
            if (c is < 'A' or > 'Z')
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        FinancialResourceStrings.Arg_Invalid_IsoCodeNonAscii,
                        isoCode,
                        c),
                    nameof(isoCode));
            }
        }
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
    /// Transitional string overload of <see cref="FromNormalized(decimal, CurrencyCode)" />.
    /// </summary>
    /// <param name="amount">The normalised amount.</param>
    /// <param name="isoCode">The ISO 4217 code.</param>
    /// <returns>The wrapped <see cref="Money" />.</returns>
    internal static Money FromNormalized(decimal amount, string isoCode) =>
        new(amount, CurrencyInfo.ParseCurrencyCode(isoCode), (byte)0);

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
