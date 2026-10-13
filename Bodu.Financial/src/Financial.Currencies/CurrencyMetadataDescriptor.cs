// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyMetadataDescriptor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

/// <summary>
/// Snapshotted validated metadata for a currency tag type.
/// </summary>
/// <param name="IsoCode">The validated ISO 4217 alphabetic code.</param>
/// <param name="Code">
/// The stored <see cref="CurrencyCode" /> matching <paramref name="IsoCode" />, or <see cref="CurrencyCode.None" />
/// when the ISO code is outside the shipped ISO 4217 catalog.
/// </param>
/// <param name="MinorUnits">The validated minor-unit precision.</param>
/// <param name="MinorUnitFactor">Pre-computed <c>10 ^ MinorUnits</c> for use in allocation and conversion.</param>
/// <param name="CashRoundingIncrement">The validated cash-rounding increment, or <c>0m</c> when none.</param>
/// <param name="IsHistoric">Whether the currency has been demonetized.</param>
/// <param name="DemonetizedOn">The demonetization date, when known.</param>
/// <param name="SuccessorIsoCode">The replacement currency's ISO code, when applicable.</param>
/// <param name="EnglishName">
/// The currency's English-language name in singular Title Case, or an empty string when the <see cref="ICurrency" />
/// tag did not supply one.
/// </param>
internal readonly record struct CurrencyMetadataDescriptor(
    string IsoCode,
    CurrencyCode Code,
    int MinorUnits,
    decimal MinorUnitFactor,
    decimal CashRoundingIncrement,
    bool IsHistoric,
    DateOnly? DemonetizedOn,
    string? SuccessorIsoCode,
    string EnglishName);
