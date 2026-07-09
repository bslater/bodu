// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="CachingRateProvider" /> satisfies the shared dated-provider contract, so a
/// cache-fronted provider is indistinguishable in shape from a direct one — including reporting identical results and
/// provenance across its synchronous and asynchronous surfaces.
/// </summary>
[TestClass]
public sealed class CachingRateProviderContractTests
    : DatedRateProviderContractTests<CachingRateProvider>
{
    /// <summary>
    /// The provider name the inner source and cache are bound to.
    /// </summary>
    private const string ProviderName = "Test";

    /// <summary>
    /// The date the inner source is seeded to resolve.
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2024, 6, 17);

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override CachingRateProvider CreateProvider()
    {
        FixedDatedRateProvider inner = new(new[] { new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, s_seeded, 0.6828m, ProviderName) });
        InMemoryRateCache cache = new(ProviderName);
        CachingRateOptions options = new() { DefaultExpiry = TimeSpan.FromHours(24) };

        return new CachingRateProvider(inner, cache, options);
    }
}
