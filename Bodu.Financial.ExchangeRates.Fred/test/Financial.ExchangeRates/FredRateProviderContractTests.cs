// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="FredRateProvider" /> satisfies the shared dated-provider contract, so the FRED source is
/// indistinguishable in shape from any other <see cref="IDatedRateProvider" />. The provider is seeded offline from the
/// embedded DEXUSEU (EUR/USD) observations fixture through <see cref="FixtureFredRateSource" />.
/// </summary>
[TestClass]
public sealed class FredRateProviderContractTests
    : PairWebRateProviderContractTests<FredRateProvider, FredSeriesInfo>
{
    /// <summary>A date present in the embedded January 2023 observations fixture (EUR/USD = 1.0546).</summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.EUR, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2020, 1, 1);

    /// <inheritdoc />
    protected override DateOnly RangeStart => new(2023, 1, 2);

    /// <inheritdoc />
    protected override DateOnly RangeEnd => new(2023, 1, 6);

    /// <inheritdoc />
    protected override RateHistoryAvailability ExpectedHistoryAvailability =>
        RateHistoryAvailability.Unbounded;

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override FredRateProvider CreateProvider()
    {
        FredRateProviderOptions options = new() { ApiKey = "test-key", AllowSynchronousNetworkAccess = true };
        FixtureFredRateSource source = new(options);

        return new FredRateProvider(source, options);
    }
}
