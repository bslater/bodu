// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial;

/// <summary>
/// Verifies that <see cref="FixedDatedExchangeRateProvider" /> satisfies the shared dated-provider contract, so the
/// in-memory snapshot provider is indistinguishable in shape from any other <see cref="IDatedExchangeRateProvider" />.
/// </summary>
[TestClass]
public sealed class FixedDatedExchangeRateProviderContractTests
    : DatedExchangeRateProviderContractTests<FixedDatedExchangeRateProvider>
{
    /// <summary>
    /// The provider name stamped on the seeded observation.
    /// </summary>
    private const string ProviderName = "Test";

    /// <summary>
    /// The date the provider is seeded to resolve.
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override ExchangeRatePair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2024, 6, 17);

    /// <inheritdoc />
    protected override FixedDatedExchangeRateProvider CreateProvider() =>
        new(new[] { new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, s_seeded, 0.6828m, ProviderName) });
}
