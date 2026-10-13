// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.Snapshot.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that loading a month whose report holds no usable observation leaves the provider's snapshot as it
    /// was, rather than rebuilding it from everything already held.
    /// </summary>
    [TestMethod]
    public async Task LoadMonthAsync_WhenMonthAddsNothing_ShouldKeepTheSnapshot()
    {
        ImfRateProviderOptions options = new() { EnableDiskCache = false };
        FixtureImfRateTableSource source = new(options);
        using ImfRateProvider provider = new(source, options);
        await provider.LoadMonthAsync(new ImfReportMonth(2026, 4));
        FixedDatedRateProvider before = provider.GetLoadedSnapshot();

        await provider.LoadMonthAsync(new ImfReportMonth(2026, 3));

        Assert.AreEqual(2, source.GetTableCallCount);
        Assert.AreSame(before, provider.GetLoadedSnapshot());
    }
}
