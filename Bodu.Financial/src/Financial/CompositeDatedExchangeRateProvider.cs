// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeDatedExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Resolves exchange-rate lookups across an ordered set of <see cref="IDatedExchangeRateProvider" /> instances using a
/// deterministic first-available strategy.
/// </summary>
/// <remarks>
/// <para>
/// On every lookup the composite consults its providers in construction order and returns the first successful result.
/// This keeps fallback behaviour explicit and auditable. More elaborate cross-provider policies (such as preferring an
/// exact-date hit from any provider before any fallback) are intentionally deferred until a concrete consumer requires
/// them.
/// </para>
/// </remarks>
public sealed class CompositeDatedExchangeRateProvider : IDatedExchangeRateProvider
{
    /// <summary>
    /// The ordered providers consulted on every lookup.
    /// </summary>
    private readonly IDatedExchangeRateProvider[] _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeDatedExchangeRateProvider" /> class.
    /// </summary>
    /// <param name="providers">The providers to consult, in priority order.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="providers" /> or any element of <paramref name="providers" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="providers" /> is empty.</exception>
    public CompositeDatedExchangeRateProvider(IEnumerable<IDatedExchangeRateProvider> providers)
    {
        ThrowHelper.ThrowIfNull(providers);

        IDatedExchangeRateProvider[] snapshot = [.. providers];

        if (snapshot.Length == 0)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_CompositeNoProviders, nameof(providers));

        for (var i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] is null)
                throw new ArgumentNullException(
                    nameof(providers),
                    string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_Null_ProviderAtIndex, i));
        }

        _providers = snapshot;
    }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions options)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.IO_KeyNotFound_CompositeExchangeRate,
                    fromIsoCode,
                    toIsoCode,
                    date,
                    _providers.Length));
    }

    /// <inheritdoc />
    public bool TryGetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions options,
        out ExchangeRateLookupResult result)
    {
        IDatedExchangeRateProvider[] providers = _providers;

        for (var i = 0; i < providers.Length; i++)
        {
            if (providers[i].TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
                return true;
        }

        result = default;
        return false;
    }
}
