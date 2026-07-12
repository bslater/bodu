// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the loading and lookup behaviour of <see cref="ImfRateProvider" /> against the embedded April 2026 report.
/// </summary>
[TestClass]
public partial class ImfRateProviderTests
{
    /// <summary>A date present in the embedded April 2026 report.</summary>
    private static readonly DateOnly s_seeded = new(2026, 4, 1);

    /// <summary>
    /// Creates a provider seeded from the embedded report with its April 2026 month loaded.
    /// </summary>
    /// <returns>A warmed provider.</returns>
    private static async Task<ImfRateProvider> CreateLoadedProviderAsync()
    {
        ImfRateProviderOptions options = new() { EnableDiskCache = false };
        ImfRateProvider provider = new(new FixtureImfRateTableSource(options), options);

        await provider.LoadMonthAsync(new ImfReportMonth(2026, 4));

        return provider;
    }
}
