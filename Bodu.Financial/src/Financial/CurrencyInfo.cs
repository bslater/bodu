// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
/// <remarks>
/// This record is the runtime counterpart of an <see cref="ICurrency" /> tag type. The runtime-tagged <c>MoneyValue</c>
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
    string EnglishName = "");
