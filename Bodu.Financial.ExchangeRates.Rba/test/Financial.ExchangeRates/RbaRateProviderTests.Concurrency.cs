// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.Concurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>
    /// Verifies that a range spanning several eras starts downloading all of them at once, rather than waiting for each
    /// download before starting the next.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenRangeSpansSeveralEras_ShouldDownloadThemConcurrently()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        GatedRbaRateTableSource source = new(options);
        using RbaRateProvider provider = new(source, options);

        Task load = provider.LoadRangeAsync(new DateOnly(2014, 1, 1), new DateOnly(2023, 6, 30));
        int started = source.CallCount;
        source.Release();
        await load;

        Assert.AreEqual(3, started);
    }

    /// <summary>
    /// Verifies that preloading every era downloads at most four at once.
    /// </summary>
    [TestMethod]
    public async Task PreloadAsync_WhenCatalogueHasMoreErasThanTheLimit_ShouldDownloadFourAtOnce()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        GatedRbaRateTableSource source = new(options);
        using RbaRateProvider provider = new(source, options);

        Task load = provider.PreloadAsync();
        int started = source.CallCount;
        source.Release();
        await load;

        Assert.AreEqual(4, started);
        Assert.AreEqual(RbaEraWorkbook.Default.Count, source.CallCount);
    }

    /// <summary>
    /// Verifies that eras downloaded at once are still published in date order, so a later era's rates never land
    /// ahead of an earlier era's in a series, even when the later era's download finishes first.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenALaterEraDownloadsFirst_ShouldPublishErasInDateOrder()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        HeldRbaRateTableSource source = new(options);
        StateCapturingLogger logger = new();
        using RbaRateProvider provider = new(source, options, logger);

        Task load = provider.LoadRangeAsync(new DateOnly(2014, 1, 1), new DateOnly(2023, 6, 30));
        source.ReleaseLatestFirst();
        await load;

        (int ingested, int outOfOrder) = CountIngestedOutOfDateOrder(logger);
        Assert.IsGreaterThan(0, ingested);
        Assert.AreEqual(0, outOfOrder);
    }

    /// <summary>
    /// Counts the observations a provider logged as ingested, and those ingested ahead of a later date already
    /// ingested for the same pair.
    /// </summary>
    /// <param name="logger">The logger the provider wrote to.</param>
    /// <returns>The number of observations ingested, and the number ingested out of date order within their pair.</returns>
    private static (int Ingested, int OutOfOrder) CountIngestedOutOfDateOrder(StateCapturingLogger logger)
    {
        Dictionary<(string From, string To), DateOnly> latest = [];
        var ingested = 0;
        var outOfOrder = 0;

        foreach ((EventId eventId, IReadOnlyDictionary<string, object?> values) in logger.Entries)
        {
            if (eventId.Id != ObservationIngestedEventId)
                continue;

            var pair = ((string)values["fromIsoCode"]!, (string)values["toIsoCode"]!);
            var date = (DateOnly)values["date"]!;
            ingested++;
            if (latest.TryGetValue(pair, out DateOnly previous) && date < previous)
                outOfOrder++;
            else
                latest[pair] = date;
        }

        return (ingested, outOfOrder);
    }
}
