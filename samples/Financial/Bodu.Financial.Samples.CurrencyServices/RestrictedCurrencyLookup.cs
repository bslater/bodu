// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RestrictedCurrencyLookup.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Samples.CurrencyServices;

/// <summary>
/// An <see cref="ICurrencyLookup" /> decorator that only resolves an allow-listed set of ISO codes.
/// Systems that settle in a fixed set of currencies install a lookup like this so that everything
/// flowing through the ambient <see cref="CurrencyResolution" /> seam — parsing, formatting,
/// minor-unit resolution — rejects currencies the system does not support.
/// </summary>
public sealed class RestrictedCurrencyLookup : ICurrencyLookup
{
    private readonly ICurrencyLookup _inner;
    private readonly HashSet<string> _allowed;

    /// <summary>
    /// Initializes a new instance allowing only <paramref name="allowedIsoCodes" />.
    /// </summary>
    /// <param name="inner">The lookup that supplies the underlying metadata.</param>
    /// <param name="allowedIsoCodes">The ISO 4217 codes this lookup resolves.</param>
    public RestrictedCurrencyLookup(ICurrencyLookup inner, params string[] allowedIsoCodes)
    {
        _inner = inner;
        _allowed = new HashSet<string>(allowedIsoCodes, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool TryByIsoCode(string isoCode, out CurrencyInfo currency)
    {
        if (_allowed.Contains(isoCode))
            return _inner.TryByIsoCode(isoCode, out currency);

        currency = default!;
        return false;
    }

    /// <inheritdoc />
    public bool TryByCurrencyCode(CurrencyCode code, out CurrencyInfo currency)
    {
        if (_allowed.Contains(code.ToString()))
            return _inner.TryByCurrencyCode(code, out currency);

        currency = default!;
        return false;
    }

    /// <inheritdoc />
    public bool TryByNumericCode(int numericCode, out CurrencyInfo currency)
    {
        if (_inner.TryByNumericCode(numericCode, out currency) && _allowed.Contains(currency.IsoCode))
            return true;

        currency = default!;
        return false;
    }

    /// <inheritdoc />
    public bool TryBySymbol(string symbol, out IReadOnlyList<CurrencyInfo> matches)
    {
        if (_inner.TryBySymbol(symbol, out IReadOnlyList<CurrencyInfo> all))
        {
            matches = all.Where(c => _allowed.Contains(c.IsoCode)).ToArray();
            return matches.Count > 0;
        }

        matches = Array.Empty<CurrencyInfo>();
        return false;
    }

    /// <inheritdoc />
    public bool TryByRegion(string regionCode, out IReadOnlyList<CurrencyInfo> matches)
    {
        if (_inner.TryByRegion(regionCode, out IReadOnlyList<CurrencyInfo> all))
        {
            matches = all.Where(c => _allowed.Contains(c.IsoCode)).ToArray();
            return matches.Count > 0;
        }

        matches = Array.Empty<CurrencyInfo>();
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<CurrencyInfo> ByCulture(CultureInfo culture) =>
        _inner.ByCulture(culture).Where(c => _allowed.Contains(c.IsoCode)).ToArray();
}
