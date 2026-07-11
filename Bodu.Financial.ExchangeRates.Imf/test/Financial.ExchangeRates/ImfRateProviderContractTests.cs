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
/// embedded April 2026 Representative Exchange Rates report through <see cref="FixtureImfRateTableSource" />.
/// </summary>
[TestClass]
public sealed class ImfRateProviderContractTests
    : DatedRateProviderContractTests<ImfRateProvider>
{
    /// <inheritdoc />
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.USD, CurrencyCode.JPY);

    /// <inheritdoc />
    protected override DateOnly KnownDate => new(2026, 4, 1);

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2019, 1, 1);

    /// <inheritdoc />
    protected override DateOnly RangeStart => new(2026, 4, 1);

    /// <inheritdoc />
    protected override DateOnly RangeEnd => new(2026, 4, 16);

    /// <inheritdoc />
    protected override bool SupportsDisposalGuard => true;

    /// <inheritdoc />
    protected override ImfRateProvider CreateProvider()
    {
        ImfRateProviderOptions options = new() { AllowSynchronousNetworkAccess = true, EnableDiskCache = false };
        FixtureImfRateTableSource source = new(options);

        return new ImfRateProvider(source, options);
    }
}
