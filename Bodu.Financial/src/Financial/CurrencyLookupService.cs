// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyLookupService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Default <see cref="ICurrencyLookup" /> backed by <see cref="CurrencyRegistry" />. Reverse indexes for numeric code,
/// symbol, and region are built lazily from a snapshot of the registry taken on first use.
/// </summary>
/// <remarks>
/// The reverse indexes reflect the registry contents at the time of first access. Construct a new instance after
/// registering custom currencies that must participate in symbol, numeric, or region lookups.
/// </remarks>
public sealed class CurrencyLookupService
    : ICurrencyLookup
{
    /// <summary>Lazily built numeric-code index; non-historic entries win on a numeric-code collision.</summary>
    private readonly Lazy<IReadOnlyDictionary<int, CurrencyInfo>> _byNumericCode;

    /// <summary>Lazily built symbol index, keyed by primary and alternative symbols.</summary>
    private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<CurrencyInfo>>> _bySymbol;

    /// <summary>Lazily built region index, keyed by ISO 3166 region code.</summary>
    private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<CurrencyInfo>>> _byRegion;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyLookupService" /> class.
    /// </summary>
    public CurrencyLookupService()
    {
        _byNumericCode = new Lazy<IReadOnlyDictionary<int, CurrencyInfo>>(BuildNumericIndex);
        _bySymbol = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<CurrencyInfo>>>(BuildSymbolIndex);
        _byRegion = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<CurrencyInfo>>>(BuildRegionIndex);
    }

    /// <inheritdoc />
    public bool TryByIsoCode(string isoCode, out CurrencyInfo currency)
    {
        if (CurrencyRegistry.TryGet(isoCode, out CurrencyInfo? info) && info is not null)
        {
            currency = info;
            return true;
        }

        currency = null!;
        return false;
    }

    /// <inheritdoc />
    public bool TryByCurrencyCode(CurrencyCode code, out CurrencyInfo currency)
    {
        if (Enum.IsDefined(code) && CurrencyRegistry.TryGet(code.ToString(), out CurrencyInfo? info) && info is not null)
        {
            currency = info;
            return true;
        }

        currency = null!;
        return false;
    }

    /// <inheritdoc />
    public bool TryByNumericCode(int numericCode, out CurrencyInfo currency)
    {
        if (_byNumericCode.Value.TryGetValue(numericCode, out CurrencyInfo? info))
        {
            currency = info;
            return true;
        }

        currency = null!;
        return false;
    }

    /// <inheritdoc />
    public bool TryBySymbol(string symbol, out IReadOnlyList<CurrencyInfo> matches)
    {
        ThrowHelper.ThrowIfNull(symbol);

        if (_bySymbol.Value.TryGetValue(symbol, out IReadOnlyList<CurrencyInfo>? found))
        {
            matches = found;
            return true;
        }

        matches = Array.Empty<CurrencyInfo>();
        return false;
    }

    /// <inheritdoc />
    public bool TryByRegion(string regionCode, out IReadOnlyList<CurrencyInfo> matches)
    {
        ThrowHelper.ThrowIfNull(regionCode);

        if (_byRegion.Value.TryGetValue(regionCode, out IReadOnlyList<CurrencyInfo>? found))
        {
            matches = found;
            return true;
        }

        matches = Array.Empty<CurrencyInfo>();
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<CurrencyInfo> ByCulture(CultureInfo culture)
    {
        ThrowHelper.ThrowIfNull(culture);

        if (culture.IsNeutralCulture || string.IsNullOrEmpty(culture.Name))
            return Array.Empty<CurrencyInfo>();

        var region = new RegionInfo(culture.Name);
        return TryByIsoCode(region.ISOCurrencySymbol, out CurrencyInfo currency)
            ? [currency]
            : Array.Empty<CurrencyInfo>();
    }

    /// <summary>
    /// Builds the numeric-code index, preferring non-historic entries on a collision.
    /// </summary>
    /// <returns>The numeric-code index.</returns>
    private static IReadOnlyDictionary<int, CurrencyInfo> BuildNumericIndex()
    {
        Dictionary<int, CurrencyInfo> index = new();
        foreach (CurrencyInfo info in CurrencyRegistry.All)
        {
            if (info.NumericCode == 0)
                continue;

            if (!index.TryGetValue(info.NumericCode, out CurrencyInfo? existing) || (existing.IsHistoric && !info.IsHistoric))
                index[info.NumericCode] = info;
        }

        return index;
    }

    /// <summary>
    /// Builds the symbol index from primary and alternative symbols.
    /// </summary>
    /// <returns>The symbol index.</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<CurrencyInfo>> BuildSymbolIndex()
    {
        Dictionary<string, List<CurrencyInfo>> index = new(StringComparer.Ordinal);
        foreach (CurrencyInfo info in CurrencyRegistry.All)
        {
            if (!string.IsNullOrEmpty(info.Symbol))
                Append(index, info.Symbol, info);
            if (!string.IsNullOrEmpty(info.InternationalSymbol))
                Append(index, info.InternationalSymbol, info);
            foreach (string symbol in info.AlternativeSymbols)
            {
                if (!string.IsNullOrEmpty(symbol))
                    Append(index, symbol, info);
            }
        }

        return Freeze(index);
    }

    /// <summary>
    /// Builds the region index from each currency's region codes.
    /// </summary>
    /// <returns>The region index.</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<CurrencyInfo>> BuildRegionIndex()
    {
        Dictionary<string, List<CurrencyInfo>> index = new(StringComparer.Ordinal);
        foreach (CurrencyInfo info in CurrencyRegistry.All)
        {
            foreach (string region in info.RegionCodes)
            {
                if (!string.IsNullOrEmpty(region))
                    Append(index, region, info);
            }
        }

        return Freeze(index);
    }

    /// <summary>
    /// Appends <paramref name="info" /> to the list stored under <paramref name="key" />, deduplicating repeated keys.
    /// </summary>
    /// <param name="index">The index being built.</param>
    /// <param name="key">The lookup key.</param>
    /// <param name="info">The currency to append.</param>
    private static void Append(Dictionary<string, List<CurrencyInfo>> index, string key, CurrencyInfo info)
    {
        if (!index.TryGetValue(key, out List<CurrencyInfo>? list))
        {
            list = new List<CurrencyInfo>();
            index[key] = list;
        }

        if (!list.Contains(info))
            list.Add(info);
    }

    /// <summary>
    /// Converts the mutable build index into a read-only projection.
    /// </summary>
    /// <param name="index">The build index.</param>
    /// <returns>The read-only index.</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<CurrencyInfo>> Freeze(Dictionary<string, List<CurrencyInfo>> index)
    {
        Dictionary<string, IReadOnlyList<CurrencyInfo>> frozen = new(index.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, List<CurrencyInfo>> entry in index)
            frozen[entry.Key] = entry.Value;
        return frozen;
    }
}
