// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

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
/// The currency's English-language name in singular Title Case (for example, <c>"United States Dollar"</c>), or an
/// empty string when no name is supplied.
/// </param>
/// <param name="NumericCode">
/// The ISO 4217 three-digit numeric code (for example, <c>840</c> for <c>USD</c>, <c>36</c> for <c>AUD</c>), or
/// <c>0</c> when the currency is custom or the numeric code is unknown.
/// </param>
/// <remarks>
/// This record is the runtime counterpart of an <see cref="ICurrency" /> tag type. The runtime-tagged <c>Money</c>
/// and the <c>MoneyBag</c> aggregate operate against <see cref="CurrencyRegistry" /> entries of this shape so they can
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
    /// <summary>
    /// Resolves a <see cref="CurrencyCode" /> enum value to its registered <see cref="CurrencyInfo" />.
    /// </summary>
    /// <param name="code">The active ISO 4217 currency code.</param>
    /// <returns>The registry entry corresponding to <paramref name="code" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="code" /> is not a defined <see cref="CurrencyCode" /> member.
    /// </exception>
    public static CurrencyInfo FromCurrencyCode(CurrencyCode code)
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

        return CurrencyRegistry.Get(code.ToString());
    }

    /// <summary>
    /// Attempts to resolve an ISO 4217 alphabetic code to the matching <see cref="CurrencyCode" /> enum member.
    /// </summary>
    /// <param name="isoCode">The three-letter uppercase ISO code.</param>
    /// <param name="code">When this method returns <see langword="true" />, the matching enum value.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="isoCode" /> matches an active currency in the enum; otherwise
    /// <see langword="false" /> (including for historic currencies that are not in the enum).
    /// </returns>
    /// <remarks>
    /// The match is case-sensitive: only the canonical three-uppercase-letter ISO form resolves successfully. Custom or
    /// historic currencies are absent from <see cref="CurrencyCode" /> for enum stability and surface here as
    /// <see langword="false" />.
    /// </remarks>
    public static bool TryGetCurrencyCode(string isoCode, out CurrencyCode code) =>
        Enum.TryParse(isoCode, ignoreCase: false, out code) && Enum.IsDefined(code);

    /// <summary>
    /// Returns the <see cref="CurrencyCode" /> enum value corresponding to this <see cref="CurrencyInfo" />.
    /// </summary>
    /// <returns>The matching enum member.</returns>
    /// <exception cref="InvalidOperationException">
    /// The currency's <see cref="IsoCode" /> is not an active ISO 4217 currency (historic / demonetized currencies are
    /// intentionally excluded from <see cref="CurrencyCode" />).
    /// </exception>
    public CurrencyCode ToCurrencyCode()
    {
        if (!TryGetCurrencyCode(IsoCode, out CurrencyCode code))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_NoCurrencyCodeForIsoCode,
                    IsoCode));
        }

        return code;
    }
}
