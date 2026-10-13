// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.Currencies;

/// <summary>
/// Carries the runtime metadata of a currency: ISO 4217 code, minor-unit precision, cash-rounding increment, and
/// historicity / successor information.
/// </summary>
/// <param name="IsoCode">The ISO 4217 three-letter alphabetic code.</param>
/// <param name="MinorUnits">The number of fractional digits in the currency's minor unit.</param>
/// <param name="CashRoundingIncrement">
/// The smallest physical cash denomination in the major unit, or <c>0m</c> when no special cash rounding is required
/// beyond <paramref name="MinorUnits" />.
/// </param>
/// <param name="IsHistoric">Whether the currency has been demonetized.</param>
/// <param name="DemonetizedOn">The date the currency was withdrawn from circulation, when known.</param>
/// <param name="SuccessorIsoCode">The ISO 4217 code of the currency that replaced this one, when defined.</param>
/// <param name="EnglishName">
/// The currency's English-language name in singular Title Case (for example, <c>"US Dollar"</c>), or an empty string
/// when no name is supplied.
/// </param>
/// <param name="NumericCode">
/// The ISO 4217 three-digit numeric code (for example, <c>840</c> for <c>USD</c>, <c>36</c> for <c>AUD</c>), or
/// <c>0</c> when the currency is custom or the numeric code is unknown.
/// </param>
/// <exception cref="ArgumentOutOfRangeException">
/// <paramref name="MinorUnits" /> is less than 0 or greater than 28, or <paramref name="CashRoundingIncrement" /> is
/// negative or is not a whole number of minor units at <paramref name="MinorUnits" />.
/// </exception>
/// <remarks>
/// This record is the runtime counterpart of an <see cref="ICurrency" /> tag type. The runtime-tagged <c>Money</c> and
/// the <c>MoneyBag</c> aggregate operate against <see cref="CurrencyRegistry" /> entries of this shape so they can
/// handle currencies the consumer learns about at runtime (for example, from a deserialised JSON payload).
/// </remarks>
public sealed record CurrencyInfo(
    string IsoCode,
    int MinorUnits,
    decimal CashRoundingIncrement,
    bool IsHistoric,
    DateOnly? DemonetizedOn,
    string? SuccessorIsoCode,
    string EnglishName = "",
    int NumericCode = 0)
{
    /// <summary>The validated number of fractional digits in the currency's minor unit.</summary>
    private readonly int _minorUnits = ValidateMinorUnits(MinorUnits);

    /// <summary>The validated smallest physical cash denomination, in the major unit.</summary>
    private readonly decimal _cashRoundingIncrement = ValidateCashRoundingIncrement(CashRoundingIncrement, MinorUnits);

    /// <summary>
    /// Gets the number of fractional digits in the currency's minor unit.
    /// </summary>
    /// <value>The minor-unit precision, from 0 to 28.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is less than 0 or greater than 28, or <see cref="CashRoundingIncrement" /> is not a whole number of
    /// minor units at the value; the exception then names <see cref="CashRoundingIncrement" />.
    /// </exception>
    /// <remarks>
    /// The constructor, an object initializer and a <c>with</c> expression all apply the checks, so every
    /// <see cref="CurrencyInfo" /> carries a scale that <see cref="decimal" /> can represent and a cash-rounding
    /// increment that scale can hold. An initializer that sets both this property and
    /// <see cref="CashRoundingIncrement" /> checks each against the other's value when it is set, so set the increment
    /// first when the new minor units cannot hold the current one.
    /// </remarks>
    public int MinorUnits
    {
        get => _minorUnits;
        init
        {
            int minorUnits = ValidateMinorUnits(value);
            _ = ValidateCashRoundingIncrement(_cashRoundingIncrement, minorUnits);

            _minorUnits = minorUnits;
        }
    }

    /// <summary>
    /// Gets the smallest physical cash denomination in the major unit, or <c>0m</c> when no special cash rounding is
    /// required beyond <see cref="MinorUnits" />.
    /// </summary>
    /// <value>The cash-rounding increment: zero, or a positive whole number of minor units.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is negative, or is not a whole number of minor units at <see cref="MinorUnits" />.
    /// </exception>
    /// <remarks>
    /// The constructor, an object initializer and a <c>with</c> expression all apply the check, which is the rule
    /// <see cref="ICurrency.CashRoundingIncrement" /> follows for a currency tag type.
    /// </remarks>
    public decimal CashRoundingIncrement
    {
        get => _cashRoundingIncrement;
        init => _cashRoundingIncrement = ValidateCashRoundingIncrement(value, _minorUnits);
    }

    /// <summary>
    /// Gets the currency's primary symbol (for example, <c>"$"</c> or <c>"€"</c>), or an empty string when none is
    /// supplied.
    /// </summary>
    /// <value>The display symbol associated with the currency.</value>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Gets the currency's internationally disambiguated symbol (for example, <c>"US$"</c> or <c>"R$"</c>), or an empty
    /// string when none is supplied.
    /// </summary>
    /// <value>The unambiguous symbol used where the bare <see cref="Symbol" /> would be ambiguous.</value>
    public string InternationalSymbol { get; init; } = string.Empty;

    /// <summary>
    /// Gets the currency's name in its primary native language, or an empty string when none is supplied.
    /// </summary>
    /// <value>The localized currency name.</value>
    public string NativeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the ISO 3166 region codes in which the currency is used.
    /// </summary>
    /// <value>The regions associated with the currency.</value>
    public IReadOnlyList<string> RegionCodes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets alternative symbols that may also denote the currency.
    /// </summary>
    /// <value>The additional symbols recognised for the currency.</value>
    public IReadOnlyList<string> AlternativeSymbols { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Resolves a <see cref="CurrencyCode" /> enum value to its registered <see cref="CurrencyInfo" />.
    /// </summary>
    /// <param name="code">The ISO 4217 currency code to resolve.</param>
    /// <returns>The registry entry corresponding to <paramref name="code" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="code" /> is <see cref="CurrencyCode.None" /> or is not a defined <see cref="CurrencyCode" />
    /// member.
    /// </exception>
    public static CurrencyInfo FromCurrencyCode(CurrencyCode code)
    {
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(code);

        return CurrencyRegistry.Get(code.ToString());
    }

    /// <summary>
    /// Attempts to resolve an ISO 4217 alphabetic code to the matching <see cref="CurrencyCode" /> enum member.
    /// </summary>
    /// <param name="isoCode">The three-letter uppercase ISO code.</param>
    /// <param name="code">When this method returns <see langword="true" />, the matching enum value.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="isoCode" /> matches a currency in the enum - active or historic;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The match is case-sensitive: only the canonical three-uppercase-letter ISO form resolves successfully. The
    /// <see cref="CurrencyCode.None" /> sentinel and any code outside the shipped catalogue surface here as
    /// <see langword="false" />.
    /// </remarks>
    public static bool TryGetCurrencyCode(string isoCode, out CurrencyCode code)
    {
        code = CurrencyCode.None;

        // Enum.TryParse also accepts the underlying numeric value (for example "840" for USD), which would bypass the
        // documented three-letter ISO shape and silently resolve to a currency. Require the alphabetic shape first.
        return FinancialThrowHelper.IsValidIsoCodeShape(isoCode)
            && Enum.TryParse(isoCode, ignoreCase: false, out code)
            && code != CurrencyCode.None
            && Enum.IsDefined(code);
    }

    /// <summary>
    /// Resolves an ISO 4217 alphabetic code to the matching <see cref="CurrencyCode" /> enum member.
    /// </summary>
    /// <param name="isoCode">The three-letter uppercase ISO code.</param>
    /// <returns>The <see cref="CurrencyCode" /> member matching <paramref name="isoCode" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> does not match any active or historic currency in <see cref="CurrencyCode" />.
    /// </exception>
    /// <remarks>
    /// The throwing counterpart of <see cref="TryGetCurrencyCode(string, out CurrencyCode)" />, and the canonical
    /// boundary helper for turning an externally supplied ISO string into the stored enum representation.
    /// </remarks>
    public static CurrencyCode ParseCurrencyCode(string isoCode)
    {
        ThrowHelper.ThrowIfNull(isoCode);

        if (!TryGetCurrencyCode(isoCode, out CurrencyCode code))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_Invalid_UnknownCurrencyRejected, isoCode),
                nameof(isoCode));
        }

        return code;
    }

    /// <summary>
    /// Returns the <see cref="CurrencyCode" /> enum value corresponding to this <see cref="CurrencyInfo" />.
    /// </summary>
    /// <returns>The matching enum member.</returns>
    /// <exception cref="InvalidOperationException">
    /// The currency's <see cref="IsoCode" /> does not correspond to any <see cref="CurrencyCode" /> member (for
    /// example, a currency outside the shipped ISO 4217 catalogue).
    /// </exception>
    public CurrencyCode ToCurrencyCode()
    {
        if (!TryGetCurrencyCode(IsoCode, out CurrencyCode code))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Op_Invalid_NoCurrencyCodeForIsoCode,
                    IsoCode));
        }

        return code;
    }

    /// <summary>
    /// Returns <paramref name="minorUnits" /> after checking that it lies in the range 0 to 28.
    /// </summary>
    /// <param name="minorUnits">The candidate minor-unit count.</param>
    /// <returns>The minor-unit count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minorUnits" /> is less than 0 or greater than 28.
    /// </exception>
    private static int ValidateMinorUnits(int minorUnits)
    {
        ThrowHelper.ThrowIfOutOfRange(minorUnits, 0, MoneyMath.MaxMinorUnits, paramName: nameof(MinorUnits));

        return minorUnits;
    }

    /// <summary>
    /// Returns <paramref name="increment" /> after checking that it is not negative and is a whole number of minor
    /// units at <paramref name="minorUnits" />.
    /// </summary>
    /// <param name="increment">The candidate cash-rounding increment.</param>
    /// <param name="minorUnits">The minor-unit count the increment must be a whole number of.</param>
    /// <returns>The cash-rounding increment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="increment" /> is negative, or is not a whole number of minor units at
    /// <paramref name="minorUnits" />.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "The invalid value is the record's CashRoundingIncrement, which the constructor parameter, the init accessor and a with expression all set; the parameter name identifies that member.")]
    private static decimal ValidateCashRoundingIncrement(decimal increment, int minorUnits)
    {
        if (increment < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CashRoundingIncrement),
                increment,
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_CashRoundingIncrementNegative, increment));
        }

        if (!MoneyMath.IsWholeNumberOfMinorUnits(increment, minorUnits))
        {
            throw new ArgumentOutOfRangeException(
                nameof(CashRoundingIncrement),
                increment,
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_CashRoundingIncrementTooFine, increment, minorUnits));
        }

        return increment;
    }
}
