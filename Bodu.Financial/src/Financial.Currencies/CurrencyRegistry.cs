// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyRegistry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Globalization;

namespace Bodu.Financial.Currencies;

/// <summary>
/// Read-only catalogue of <see cref="CurrencyInfo" /> entries keyed by ISO 4217 alphabetic code. Supports lookup by
/// code and enumeration of every shipped currency.
/// </summary>
/// <remarks>
/// <para>
/// The catalogue is populated from the source-generated <see cref="GeneratedCurrencyRegistration" /> list at first
/// access — no runtime reflection scans the assembly. It covers the full active and historic ISO 4217 set; every entry
/// has a corresponding <see cref="CurrencyCode" /> member.
/// </para>
/// <para>
/// Lookups are thread-safe because the backing <see cref="FrozenDictionary{TKey, TValue}" /> is immutable.
/// </para>
/// </remarks>
public static class CurrencyRegistry
{
    /// <summary>The shipped catalogue, snapshotted into a <see cref="FrozenDictionary{TKey, TValue}" /> at static-ctor time.</summary>
    private static readonly FrozenDictionary<string, CurrencyInfo> s_shipped = BuildShipped();

    /// <summary>
    /// Gets every shipped currency.
    /// </summary>
    /// <value>A read-only enumeration of the registered <see cref="CurrencyInfo" /> entries.</value>
    public static IReadOnlyCollection<CurrencyInfo> All => s_shipped.Values;

    /// <summary>
    /// Attempts to look up the currency identified by <paramref name="isoCode" />.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 alphabetic code to resolve.</param>
    /// <param name="info">
    /// When this method returns <see langword="true" />, the matching <see cref="CurrencyInfo" />; otherwise the
    /// default value.
    /// </param>
    /// <returns><see langword="true" /> when the currency is registered; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// Lookup is case-sensitive and matches canonical uppercase ISO 4217 codes only; a lower- or mixed-case code is
    /// treated as unknown and returns <see langword="false" />. This mirrors the uppercase-only ISO-code contract the
    /// exchange-rate providers enforce (a mixed-case code is rejected as malformed there), keeping code resolution
    /// uniform across the library.
    /// </remarks>
    public static bool TryGet(string isoCode, out CurrencyInfo? info)
    {
        if (isoCode is null)
        {
            info = null;
            return false;
        }

        if (s_shipped.TryGetValue(isoCode, out CurrencyInfo? shipped))
        {
            info = shipped;
            return true;
        }

        info = null;
        return false;
    }

    /// <summary>
    /// Returns the <see cref="CurrencyInfo" /> for <paramref name="isoCode" /> or throws when no entry exists.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 alphabetic code to resolve.</param>
    /// <returns>The matching <see cref="CurrencyInfo" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">No currency is registered under <paramref name="isoCode" />.</exception>
    /// <remarks>
    /// Resolution is case-sensitive and matches canonical uppercase ISO 4217 codes only; a lower- or mixed-case code is
    /// treated as unknown and throws <see cref="KeyNotFoundException" />.
    /// </remarks>
    public static CurrencyInfo Get(string isoCode)
    {
        ThrowHelper.ThrowIfNull(isoCode);

        return TryGet(isoCode, out CurrencyInfo? info)
            ? info!
            : throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.IO_KeyNotFound_Currency, isoCode));
    }

    /// <summary>
    /// Determines whether the registry contains an entry for <paramref name="isoCode" />.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 alphabetic code to test.</param>
    /// <returns><see langword="true" /> when an entry exists; otherwise <see langword="false" />.</returns>
    public static bool Contains(string isoCode) =>
        TryGet(isoCode, out _);

    /// <summary>
    /// Builds the frozen shipped catalogue from the source-generated registration list.
    /// </summary>
    /// <returns>An immutable mapping of ISO code to <see cref="CurrencyInfo" />.</returns>
    private static FrozenDictionary<string, CurrencyInfo> BuildShipped()
    {
        Dictionary<string, CurrencyInfo> dict = new(StringComparer.Ordinal);
        foreach (CurrencyInfo info in GeneratedCurrencyRegistration.All())
            dict[info.IsoCode] = info;
        return dict.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
