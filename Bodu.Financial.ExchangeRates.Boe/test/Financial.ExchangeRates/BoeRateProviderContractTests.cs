// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="BoeRateProvider" /> satisfies the shared dated-provider contract, so the BoE source
/// is indistinguishable in shape from any other <see cref="IDatedRateProvider" />. The provider is seeded
/// offline from the embedded spot-rate fixture through <see cref="FixtureBoeRateTableSource" />.
/// </summary>
[TestClass]
public sealed class BoeRateProviderContractTests
    : DatedRateProviderContractTests<BoeRateProvider>
{
    /// <summary>
    /// A date present in the embedded fixture (GBP/USD = 1.2065).
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.GBP, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2020, 1, 1);

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override BoeRateProvider CreateProvider()
    {
        BoeRateProviderOptions options = new() { AllowSynchronousNetworkAccess = true, EnableDiskCache = false };
        FixtureBoeRateTableSource source = new(options);

        return new BoeRateProvider(source, options);
    }
}
