// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="OfxRateProvider" /> satisfies the shared dated-provider contract, so the OFX source
/// is indistinguishable in shape from any other <see cref="IDatedRateProvider" />. The provider is seeded
/// offline from the embedded AUD/USD spot-rate-history fixture through <see cref="FixtureOfxRateSource" />.
/// </summary>
[TestClass]
public sealed class OfxRateProviderContractTests
    : PairWebRateProviderContractTests<OfxRateProvider, OfxSeriesInfo>
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
        RateHistoryAvailability.Unbounded;

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override OfxRateProvider CreateProvider()
    {
        OfxRateProviderOptions options = new() { AllowSynchronousNetworkAccess = true };
        FixtureOfxRateSource source = new(options);

        return new OfxRateProvider(source, options);
    }
}
