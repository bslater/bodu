// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyDisplay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies how <see cref="MoneyFormatter" /> renders the currency designator alongside the amount.
/// </summary>
public enum CurrencyDisplay
{
    /// <summary>
    /// Render the ISO 4217 code (for example, <c>"USD 1,234.56"</c>).
    /// </summary>
    IsoCode = 0,

    /// <summary>
    /// Render the culture's native currency symbol where it matches the amount's currency.
    /// </summary>
    Symbol = 1,

    /// <summary>
    /// Render the currency's English-language name (for example, <c>"1,234.56 US Dollar"</c>).
    /// </summary>
    EnglishName = 2,

    /// <summary>
    /// Render the bare numeric amount with no currency designator.
    /// </summary>
    None = 3,
}
