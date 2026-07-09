// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="YahooRateProvider" /> satisfies the shared dated-provider contract, so the Yahoo
/// source is indistinguishable in shape from any other <see cref="IDatedRateProvider" />. The provider is seeded
/// offline from the embedded AUD/USD chart fixture through <see cref="FixtureYahooRateSource" />.
/// </summary>
[TestClass]
public sealed class YahooRateProviderContractTests
    : PairWebRateProviderContractTests<YahooRateProvider, YahooSeriesInfo>
{
    /// <summary>
    /// A date present in the embedded January 2023 chart fixture (AUD/USD = 0.6828).
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
        RateHistoryAvailability.Since(new DateOnly(2003, 12, 1));

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override YahooRateProvider CreateProvider()
    {
        YahooRateProviderOptions options = new() { AllowSynchronousNetworkAccess = true };
        FixtureYahooRateSource source = new(options);

        return new YahooRateProvider(source, options);
    }
}
