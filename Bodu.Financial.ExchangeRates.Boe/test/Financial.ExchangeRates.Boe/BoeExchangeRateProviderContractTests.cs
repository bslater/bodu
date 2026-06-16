// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Verifies that <see cref="BoeExchangeRateProvider" /> satisfies the shared dated-provider contract, so the BoE source
/// is indistinguishable in shape from any other <see cref="IDatedExchangeRateProvider" />. The provider is seeded
/// offline from the embedded spot-rate fixture through <see cref="FixtureBoeExchangeRateTableSource" />.
/// </summary>
[TestClass]
public sealed class BoeExchangeRateProviderContractTests
    : DatedExchangeRateProviderContractTests<BoeExchangeRateProvider>
{
    /// <summary>
    /// A date present in the embedded fixture (GBP/USD = 1.2065).
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override ExchangeRatePair CanonicalPair => new("GBP", "USD");

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2020, 1, 1);

    /// <inheritdoc />
    protected override BoeExchangeRateProvider CreateProvider()
    {
        BoeExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = true, EnableDiskCache = false };
        FixtureBoeExchangeRateTableSource source = new(options);

        return new BoeExchangeRateProvider(source, options);
    }
}
