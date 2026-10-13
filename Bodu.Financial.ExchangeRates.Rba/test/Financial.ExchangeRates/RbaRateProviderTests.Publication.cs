// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.Publication.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>The event id of the <c>ObservationIngested</c> diagnostic, logged once per observation while a load publishes.</summary>
    private const int ObservationIngestedEventId = 4305;

    /// <summary>
    /// Verifies that an era whose publication fails part-way is not recorded as loaded, so the next request downloads
    /// and publishes it again.
    /// </summary>
    [TestMethod]
    public async Task LoadEraAsync_WhenPublishFails_ShouldLoadTheEraAgainOnTheNextRequest()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        FixtureRbaRateTableSource source = new(options);
        FaultingLogger logger = new(ObservationIngestedEventId);
        using RbaRateProvider provider = new(source, options, logger);
        RbaEraWorkbook era = RbaEraWorkbook.Default[^1];

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await provider.LoadEraAsync(era);
        });

        logger.IsFaulting = false;
        await provider.LoadEraAsync(era);

        Assert.AreEqual(2, source.GetTableCallCount);
        Assert.AreEqual(0.6828m, provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3)).Rate.Rate);
    }
}
