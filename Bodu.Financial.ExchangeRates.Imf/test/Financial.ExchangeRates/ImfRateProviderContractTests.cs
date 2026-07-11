// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="ImfRateProvider" /> satisfies the shared dated-provider contract, so the IMF source is
/// indistinguishable in shape from any other <see cref="IDatedRateProvider" />. The provider is seeded offline from the
/// embedded USD/GBP daily SDMX-JSON fixture through <see cref="FixtureImfRateSource" />.
/// </summary>
[TestClass]
public sealed class ImfRateProviderContractTests
    : PairWebRateProviderContractTests<ImfRateProvider, ImfSeriesInfo>
{
    /// <inheritdoc />
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.USD, CurrencyCode.GBP);

    /// <inheritdoc />
    protected override DateOnly KnownDate => new(2023, 1, 3);

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2019, 1, 1);

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
    protected override ImfRateProvider CreateProvider()
    {
        ImfRateProviderOptions options = new() { AllowSynchronousNetworkAccess = true };
        FixtureImfRateSource source = new(options);

        return new ImfRateProvider(source, options);
    }
}
