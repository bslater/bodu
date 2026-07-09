// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="OandaRateProvider" /> satisfies the shared dated-provider contract, so the OANDA
/// source is indistinguishable in shape from any other <see cref="IDatedRateProvider" />. The provider is
/// seeded offline from the embedded AUD/USD rate-history fixture through <see cref="FixtureOandaRateSource" />.
/// </summary>
[TestClass]
public sealed class OandaRateProviderContractTests
    : PairWebRateProviderContractTests<OandaRateProvider, OandaSeriesInfo>
{
    /// <summary>
    /// A date present in the embedded January 2023 fixture (AUD/USD = 0.6828).
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2020, 1, 1);

    /// <inheritdoc />
    protected override RateHistoryAvailability ExpectedHistoryAvailability =>
        RateHistoryAvailability.RollingDays(180);

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override OandaRateProvider CreateProvider()
    {
        OandaRateProviderOptions options = new() { AllowSynchronousNetworkAccess = true };
        FixtureOandaRateSource source = new(options);

        return new OandaRateProvider(source, options);
    }
}
