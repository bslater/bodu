// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.GetRateAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that a single-date lookup early in a month, whose nearest earlier rate is in the previous month's
    /// report, loads every report the default seven-day <see cref="WebRateProviderOptions.DefaultLookback" /> window
    /// reaches, so the previous month's rate is found: Saturday 2 May 2026 resolves to Thursday 30 April, the fixture's
    /// May report holding no earlier rate.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenDefaultLookbackReachesThePreviousMonth_ShouldLoadThatMonthsReport()
    {
        ImfRateProviderOptions options = new() { EnableDiskCache = false };
        FixtureImfRateTableSource source = new(options);
        using ImfRateProvider provider = new(source, options);

        RateLookupResult result = await provider.GetRateAsync("USD", "JPY", new DateOnly(2026, 5, 2), RateLookupOptions.PreviousWithin(7));

        Assert.AreEqual(new DateOnly(2026, 4, 30), result.Rate.Date);
        Assert.AreEqual(160.27m, result.Rate.Rate);
        CollectionAssert.AreEquivalent(new[] { "2026-04", "2026-05" }, source.RequestedMonths.ToArray());
    }

    /// <summary>
    /// Verifies that a single-date lookup whose <see cref="WebRateProviderOptions.DefaultLookback" /> window lies
    /// within one month loads only that month's report.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenDefaultLookbackStaysWithinTheMonth_ShouldLoadOnlyThatMonth()
    {
        ImfRateProviderOptions options = new() { EnableDiskCache = false, DefaultLookback = TimeSpan.FromDays(1) };
        FixtureImfRateTableSource source = new(options);
        using ImfRateProvider provider = new(source, options);

        RateLookupResult result = await provider.GetRateAsync("USD", "JPY", new DateOnly(2026, 4, 15));

        Assert.AreEqual(158.87m, result.Rate.Rate);
        CollectionAssert.AreEqual(new[] { "2026-04" }, source.RequestedMonths.ToArray());
    }
}
