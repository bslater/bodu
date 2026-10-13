// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.Publication.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>The event id of the <c>ObservationIngested</c> diagnostic, logged once per observation while a load publishes.</summary>
    private const int ObservationIngestedEventId = 4705;

    /// <summary>
    /// Verifies that a report month whose publication fails part-way is not recorded as loaded, so the next request
    /// downloads and publishes it again.
    /// </summary>
    [TestMethod]
    public async Task LoadMonthAsync_WhenPublishFails_ShouldLoadTheMonthAgainOnTheNextRequest()
    {
        ImfRateProviderOptions options = new() { EnableDiskCache = false };
        FixtureImfRateTableSource source = new(options);
        FaultingLogger logger = new(ObservationIngestedEventId);
        using ImfRateProvider provider = new(source, options, logger);
        ImfReportMonth month = new(2026, 4);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await provider.LoadMonthAsync(month);
        });

        logger.IsFaulting = false;
        await provider.LoadMonthAsync(month);

        Assert.AreEqual(2, source.GetTableCallCount);
        Assert.AreEqual(158.84m, provider.GetRate("USD", "JPY", s_seeded, RateLookupOptions.Exact).Rate.Rate);
    }
}
