// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingExchangeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="AggregatingExchangeRateProvider" /> satisfies the shared dated-provider contract when
/// grouping a single source, so the aggregating entry point is indistinguishable in shape from the provider it fronts.
/// </summary>
[TestClass]
public sealed class AggregatingExchangeRateProviderContractTests
    : DatedExchangeRateProviderContractTests<AggregatingExchangeRateProvider>
{
    /// <summary>
    /// The name of the single grouped child.
    /// </summary>
    private const string ProviderName = "Test";

    /// <summary>
    /// The date the grouped child is seeded to resolve.
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override ExchangeRatePair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2024, 6, 17);

    /// <inheritdoc />
    protected override AggregatingExchangeRateProvider CreateProvider()
    {
        FixedDatedExchangeRateProvider child = new(new[] { new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, s_seeded, 0.6828m, ProviderName) });

        return new AggregatingExchangeRateProvider(new[] { new NamedDatedExchangeRateProvider(ProviderName, child) });
    }
}
