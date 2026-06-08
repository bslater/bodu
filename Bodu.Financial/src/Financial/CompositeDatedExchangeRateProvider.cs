// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeDatedExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
public sealed class CompositeDatedExchangeRateProvider
    : IDatedExchangeRateProvider
{
    /// <summary>
    /// The ordered providers consulted on every lookup.
    /// </summary>
    private readonly IDatedExchangeRateProvider[] _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeDatedExchangeRateProvider" /> class with the default
    /// <see cref="ExchangeRateProviderSelectionPolicy.ProviderPriorityFirst" /> selection policy.
    /// </summary>
    /// <param name="providers">The providers to consult, in priority order.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="providers" /> or any element of <paramref name="providers" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="providers" /> is empty.</exception>
    public CompositeDatedExchangeRateProvider(IEnumerable<IDatedExchangeRateProvider> providers)
        : this(providers, ExchangeRateProviderSelectionPolicy.ProviderPriorityFirst)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeDatedExchangeRateProvider" /> class with the supplied
    /// selection policy.
    /// </summary>
    /// <param name="providers">The providers to consult, in priority order.</param>
    /// <param name="selectionPolicy">
    /// The policy that controls how the composite picks between providers when more than one can satisfy the requested
    /// pair. Only <see cref="ExchangeRateProviderSelectionPolicy.ProviderPriorityFirst" /> is implemented in v1.0;
    /// other defined values throw <see cref="NotSupportedException" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="providers" /> or any element of <paramref name="providers" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="providers" /> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="selectionPolicy" /> is not a defined
    /// <see cref="ExchangeRateProviderSelectionPolicy" /> member.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if <paramref name="selectionPolicy" /> is a defined value other than
    /// <see cref="ExchangeRateProviderSelectionPolicy.ProviderPriorityFirst" />.
    /// </exception>
    public CompositeDatedExchangeRateProvider(
        IEnumerable<IDatedExchangeRateProvider> providers,
        ExchangeRateProviderSelectionPolicy selectionPolicy)
    {
        ThrowHelper.ThrowIfNull(providers);
        ThrowHelper.ThrowIfEnumValueIsUndefined(selectionPolicy);

        if (selectionPolicy != ExchangeRateProviderSelectionPolicy.ProviderPriorityFirst)
            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Op_NotSupported_SelectionPolicy,
                    selectionPolicy));

        IDatedExchangeRateProvider[] snapshot = [.. providers];

        if (snapshot.Length == 0)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_CompositeNoProviders, nameof(providers));

        for (var i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] is null)
                throw new ArgumentNullException(
                    nameof(providers),
                    string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_Null_ProviderAtIndex, i));
        }

        _providers = snapshot;
        SelectionPolicy = selectionPolicy;
    }

    /// <summary>
    /// Gets the selection policy applied by this composite.
    /// </summary>
    /// <returns>The configured <see cref="ExchangeRateProviderSelectionPolicy" />.</returns>
    public ExchangeRateProviderSelectionPolicy SelectionPolicy { get; }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
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
        ExchangeRateLookupOptions? options,
        out ExchangeRateLookupResult result)
    {
        options ??= ExchangeRateLookupOptions.Exact;
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
