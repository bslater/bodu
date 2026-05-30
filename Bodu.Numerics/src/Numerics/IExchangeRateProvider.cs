// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Defines the minimum contract for a timeless exchange-rate provider that returns a single multiplier per currency
/// pair without resolution metadata.
/// </summary>
/// <remarks>
/// <para>
/// This is the simple/compatibility surface intended for static rate tables, demonstrations, and consumers that do not
/// require date-aware lookups. For dated, auditable lookups with provider attribution and fallback metadata, use
/// <see cref="IDatedExchangeRateProvider" /> instead — or wrap a dated provider via
/// <see cref="DatedExchangeRateProviderAdapter" /> when both surfaces are needed.
/// </para>
/// </remarks>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Returns the exchange rate that converts an amount in <paramref name="fromIsoCode" /> to
    /// <paramref name="toIsoCode" />.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO-style code.</param>
    /// <param name="toIsoCode">The destination-currency ISO-style code.</param>
    /// <returns>A strictly positive <see cref="decimal" /> multiplier.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="fromIsoCode" /> or <paramref name="toIsoCode" /> is not a three-character uppercase
    /// ASCII code.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown if no rate is available for the requested pair.</exception>
    decimal GetRate(string fromIsoCode, string toIsoCode);
}
