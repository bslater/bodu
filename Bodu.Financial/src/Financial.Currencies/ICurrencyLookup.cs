// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICurrencyLookup.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.Currencies;

/// <summary>
/// Resolves <see cref="CurrencyInfo" /> metadata by ISO code, numeric code, symbol, region, or culture, providing
/// NodaMoney-style lookup ergonomics over the runtime currency catalogue without moving currency identity into culture.
/// </summary>
public interface ICurrencyLookup
{
    /// <summary>
    /// Attempts to resolve a currency by its ISO 4217 alphabetic code.
    /// </summary>
    /// <param name="isoCode">The three-letter uppercase ISO code.</param>
    /// <param name="currency">When this method returns <see langword="true" />, the matching currency.</param>
    /// <returns><see langword="true" /> when a currency was found; otherwise <see langword="false" />.</returns>
    bool TryByIsoCode(string isoCode, out CurrencyInfo currency);

    /// <summary>
    /// Attempts to resolve a currency by its <see cref="CurrencyCode" /> enum value.
    /// </summary>
    /// <param name="code">The active ISO 4217 currency code.</param>
    /// <param name="currency">When this method returns <see langword="true" />, the matching currency.</param>
    /// <returns><see langword="true" /> when a currency was found; otherwise <see langword="false" />.</returns>
    bool TryByCurrencyCode(CurrencyCode code, out CurrencyInfo currency);

    /// <summary>
    /// Attempts to resolve a currency by its ISO 4217 numeric code.
    /// </summary>
    /// <param name="numericCode">The three-digit numeric code.</param>
    /// <param name="currency">When this method returns <see langword="true" />, the matching currency.</param>
    /// <returns><see langword="true" /> when a currency was found; otherwise <see langword="false" />.</returns>
    bool TryByNumericCode(int numericCode, out CurrencyInfo currency);

    /// <summary>
    /// Attempts to resolve the currencies that use <paramref name="symbol" /> as a primary or alternative symbol.
    /// </summary>
    /// <param name="symbol">The currency symbol.</param>
    /// <param name="matches">When this method returns <see langword="true" />, the matching currencies.</param>
    /// <returns>
    /// <see langword="true" /> when at least one currency was found; otherwise <see langword="false" />.
    /// </returns>
    bool TryBySymbol(string symbol, out IReadOnlyList<CurrencyInfo> matches);

    /// <summary>
    /// Attempts to resolve the currencies used in the region identified by <paramref name="regionCode" />.
    /// </summary>
    /// <param name="regionCode">The ISO 3166 region code.</param>
    /// <param name="matches">When this method returns <see langword="true" />, the matching currencies.</param>
    /// <returns>
    /// <see langword="true" /> when at least one currency was found; otherwise <see langword="false" />.
    /// </returns>
    bool TryByRegion(string regionCode, out IReadOnlyList<CurrencyInfo> matches);

    /// <summary>
    /// Resolves the currencies associated with <paramref name="culture" />'s region.
    /// </summary>
    /// <param name="culture">The culture whose region currency is resolved.</param>
    /// <returns>The matching currencies, or an empty list when none are found.</returns>
    IReadOnlyList<CurrencyInfo> ByCulture(CultureInfo culture);
}
