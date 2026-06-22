// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="RbaExchangeRateProvider" /> satisfies the shared dated-provider contract, so the RBA source
/// is indistinguishable in shape from any other <see cref="IDatedExchangeRateProvider" />. The provider is seeded
/// offline from the embedded sample workbook through <see cref="FixtureRbaExchangeRateTableSource" />.
/// </summary>
[TestClass]
public sealed class RbaExchangeRateProviderContractTests
    : DatedExchangeRateProviderContractTests<RbaExchangeRateProvider>
{
    /// <summary>
    /// A date present in the embedded sample workbook (AUD/USD = 0.6828).
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override ExchangeRatePair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2020, 1, 1);

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override RbaExchangeRateProvider CreateProvider()
    {
        RbaExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = true, EnableDiskCache = false };
        FixtureRbaExchangeRateTableSource source = new(options);

        return new RbaExchangeRateProvider(source, options);
    }
}
