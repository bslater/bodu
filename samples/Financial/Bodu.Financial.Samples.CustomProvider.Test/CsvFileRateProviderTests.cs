// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CsvFileRateProviderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.Samples.CustomProvider;

/// <summary>
/// Verifies that the sample <see cref="CsvFileRateProvider" /> satisfies the shared dated-provider
/// contract by deriving the <see cref="DatedRateProviderContractTests{TProvider}" /> base shipped in
/// <c>Bodu.Financial.ExchangeRates.Testing</c> — the same base every built-in provider passes. This is
/// the pattern for validating any consumer-written <see cref="IDatedRateProvider" />: supply the
/// seeded provider and its known/unknown dates, and the base exercises the full lookup surface
/// (sync/async equivalence, date resolution, inverse and identity fallbacks, provenance, misses).
/// </summary>
[TestClass]
public sealed class CsvFileRateProviderTests
    : DatedRateProviderContractTests<CsvFileRateProvider>
{
    // The overrides below describe the committed Data/custom-feed.csv fixture: the contract base derives
    // every probe (hits, misses, inverse lookups, the range sweep) from these seed values.

    /// <inheritdoc />
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <inheritdoc />
    protected override DateOnly KnownDate => new(2024, 1, 15);

    /// <inheritdoc />
    protected override DateOnly UnknownDate => new(2024, 6, 17);

    /// <inheritdoc />
    protected override DateOnly RangeStart => new(2024, 1, 2);

    /// <inheritdoc />
    protected override DateOnly RangeEnd => new(2024, 1, 31);

    /// <inheritdoc />
    protected override CsvFileRateProvider CreateProvider() =>
        new(Path.Combine(AppContext.BaseDirectory, "Data", "custom-feed.csv"), "CustomFeed");
}
