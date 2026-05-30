// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyRegistry.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Runtime catalogue of <see cref="CurrencyInfo" /> entries keyed by ISO 4217 alphabetic code. Supports lookup by code,
/// enumeration of every known currency, and registration of custom currencies that are not in the shipped catalogue.
/// </summary>
/// <remarks>
/// <para>
/// The shipped catalogue is populated from the source-generated <see cref="GeneratedCurrencyRegistration" /> list at
/// first access — no runtime reflection scans the assembly. Custom currencies can be registered via
/// <see cref="Register(CurrencyInfo)" /> or <see cref="TryRegister(CurrencyInfo)" /> and take precedence over the
/// shipped entry of the same code.
/// </para>
/// <para>
/// Lookups are thread-safe. Custom registrations are stored in a <see cref="ConcurrentDictionary{TKey, TValue}" /> so
/// concurrent readers and writers do not need external coordination.
/// </para>
/// </remarks>
public static class CurrencyRegistry
{
    /// <summary>
    /// The shipped catalogue, snapshotted into a <see cref="FrozenDictionary{TKey, TValue}" /> at static-ctor time.
    /// </summary>
    private static readonly FrozenDictionary<string, CurrencyInfo> s_shipped = BuildShipped();

    /// <summary>
    /// User-registered currencies layered on top of the shipped catalogue.
    /// </summary>
    private static readonly ConcurrentDictionary<string, CurrencyInfo> s_custom = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets a snapshot of every known currency — the shipped catalogue layered with any custom registrations, with
    /// custom registrations taking precedence on conflict.
    /// </summary>
    /// <returns>A point-in-time read-only enumeration of the registered <see cref="CurrencyInfo" /> entries.</returns>
    public static IReadOnlyCollection<CurrencyInfo> All
    {
        get
        {
            if (s_custom.IsEmpty)
                return s_shipped.Values;

            Dictionary<string, CurrencyInfo> merged = new(s_shipped.Count + s_custom.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, CurrencyInfo> entry in s_shipped)
                merged[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, CurrencyInfo> entry in s_custom)
                merged[entry.Key] = entry.Value;

            return merged.Values;
        }
    }

    /// <summary>
    /// Attempts to look up the currency identified by <paramref name="isoCode" />.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 alphabetic code to resolve.</param>
    /// <param name="info">
    /// When this method returns <see langword="true" />, the matching <see cref="CurrencyInfo" />; otherwise the
    /// default value.
    /// </param>
    /// <returns><see langword="true" /> when the currency is registered; otherwise <see langword="false" />.</returns>
    public static bool TryGet(string isoCode, out CurrencyInfo? info)
    {
        if (isoCode is null)
        {
            info = null;
            return false;
        }

        if (s_custom.TryGetValue(isoCode, out CurrencyInfo? custom))
        {
            info = custom;
            return true;
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
    public static CurrencyInfo Get(string isoCode)
    {
        ThrowHelper.ThrowIfNull(isoCode);

        return TryGet(isoCode, out CurrencyInfo? info)
            ? info!
            : throw new KeyNotFoundException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.IO_KeyNotFound_Currency, isoCode));
    }

    /// <summary>
    /// Determines whether the registry contains an entry for <paramref name="isoCode" />.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 alphabetic code to test.</param>
    /// <returns><see langword="true" /> when an entry exists; otherwise <see langword="false" />.</returns>
    public static bool Contains(string isoCode) =>
        TryGet(isoCode, out _);

    /// <summary>
    /// Registers a custom currency, taking precedence over the shipped catalogue when an entry already exists for the
    /// same ISO code.
    /// </summary>
    /// <param name="info">The currency metadata to register.</param>
    /// <exception cref="ArgumentNullException"><paramref name="info" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// A custom registration already exists for <paramref name="info" />.<see cref="CurrencyInfo.IsoCode" />.
    /// </exception>
    /// <remarks>
    /// Use <see cref="TryRegister(CurrencyInfo)" /> when conflicts should be silently ignored rather than raised.
    /// </remarks>
    public static void Register(CurrencyInfo info)
    {
        ThrowHelper.ThrowIfNull(info);

        if (!s_custom.TryAdd(info.IsoCode, info))
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Op_Invalid_DuplicateCustomCurrency, info.IsoCode));
        }
    }

    /// <summary>
    /// Attempts to register a custom currency.
    /// </summary>
    /// <param name="info">The currency metadata to register.</param>
    /// <returns>
    /// <see langword="true" /> when the registration succeeded; <see langword="false" /> when <paramref name="info" />
    /// is <see langword="null" /> or a custom registration with the same ISO code already exists.
    /// </returns>
    public static bool TryRegister(CurrencyInfo info) =>
        info is not null && s_custom.TryAdd(info.IsoCode, info);

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
