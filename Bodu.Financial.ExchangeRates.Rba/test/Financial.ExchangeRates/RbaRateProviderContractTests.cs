// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="RbaRateProvider" /> satisfies the shared dated-provider contract, so the RBA source
/// is indistinguishable in shape from any other <see cref="IDatedRateProvider" />. The provider loads each era
/// offline from its synthetic workbook through <see cref="FixtureRbaRateTableSource" />.
/// </summary>
[TestClass]
public sealed class RbaRateProviderContractTests
    : DatedRateProviderContractTests<RbaRateProvider>
{
    /// <summary>
    /// A date of the 2023-to-current era's workbook, which holds AUD/USD 0.6828 on it.
    /// </summary>
    private static readonly DateOnly s_seeded = new(2023, 1, 3);

    /// <inheritdoc />
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => s_seeded;

    /// <inheritdoc />
    /// <remarks>New Year's Day, for which the 2018-2022 era's workbook, like the RBA's, has no row.</remarks>
    protected override DateOnly UnknownDate => new(2020, 1, 1);

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override RbaRateProvider CreateProvider()
    {
        RbaRateProviderOptions options = new() { AllowSynchronousNetworkAccess = true, EnableDiskCache = false };
        FixtureRbaRateTableSource source = new(options);

        return new RbaRateProvider(source, options);
    }
}
