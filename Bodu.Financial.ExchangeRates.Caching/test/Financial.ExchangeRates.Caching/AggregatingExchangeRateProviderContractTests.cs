// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingExchangeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
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
    private static readonly DateOnly Seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override ExchangeRatePair CanonicalPair => new("AUD", "USD");

    /// <inheritdoc />
    protected override DateOnly KnownDate => Seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2024, 6, 17);

    /// <inheritdoc />
    protected override AggregatingExchangeRateProvider CreateProvider()
    {
        FixedDatedExchangeRateProvider child = new(new[] { new ExchangeRate("AUD", "USD", Seeded, 0.6828m, ProviderName) });

        return new AggregatingExchangeRateProvider(new[] { new NamedDatedExchangeRateProvider(ProviderName, child) });
    }
}
