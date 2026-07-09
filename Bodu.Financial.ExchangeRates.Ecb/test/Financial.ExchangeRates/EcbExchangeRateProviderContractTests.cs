// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="EcbExchangeRateProvider" /> satisfies the shared dated-provider contract, so the ECB source
/// is indistinguishable in shape from any other <see cref="IDatedRateProvider" />. The provider is seeded
/// offline from the embedded <c>eurofxref</c> fixture through <see cref="FixtureEcbExchangeRateTableSource" />.
/// </summary>
[TestClass]
public sealed class EcbExchangeRateProviderContractTests
    : DatedExchangeRateProviderContractTests<EcbExchangeRateProvider>
{
    /// <summary>
    /// A date present in the embedded fixture (EUR/USD = 1.0545).
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.EUR, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2020, 1, 1);

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override EcbExchangeRateProvider CreateProvider()
    {
        EcbExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = true, EnableDiskCache = false };
        FixtureEcbExchangeRateTableSource source = new(options);

        return new EcbExchangeRateProvider(source, options);
    }
}
