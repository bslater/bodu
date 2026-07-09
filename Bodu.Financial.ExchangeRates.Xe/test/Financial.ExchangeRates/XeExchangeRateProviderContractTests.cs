// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeExchangeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="XeExchangeRateProvider" /> satisfies the shared dated-provider contract, so the XE source is
/// indistinguishable in shape from any other <see cref="IDatedExchangeRateProvider" />. The provider is seeded offline
/// from the embedded AUD/USD charting-rates fixture through <see cref="FixtureXeExchangeRateSource" />.
/// </summary>
[TestClass]
public sealed class XeExchangeRateProviderContractTests
    : PairWebExchangeRateProviderContractTests<XeExchangeRateProvider, XeSeriesInfo>
{
    /// <summary>
    /// A date present in the embedded January 2023 fixture (AUD/USD = 0.6828).
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override ExchangeRatePair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2020, 1, 1);

    /// <inheritdoc />
    protected override DateOnly RangeStart => new(2023, 1, 2);

    /// <inheritdoc />
    protected override DateOnly RangeEnd => new(2023, 1, 6);

    /// <inheritdoc />
    protected override ExchangeRateHistoryAvailability ExpectedHistoryAvailability =>
        ExchangeRateHistoryAvailability.RollingDays(3650);

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override XeExchangeRateProvider CreateProvider()
    {
        XeExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = true };
        FixtureXeExchangeRateSource source = new(options);

        return new XeExchangeRateProvider(source, options);
    }
}
