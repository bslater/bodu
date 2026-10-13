// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.Concurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Logging;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that a range spanning several months starts downloading all of them at once, rather than waiting for
    /// each report before requesting the next.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenRangeSpansSeveralMonths_ShouldDownloadThemConcurrently()
    {
        ImfRateProviderOptions options = new() { EnableDiskCache = false };
        GatedImfRateTableSource source = new();
        using ImfRateProvider provider = new(source, options);

        Task load = provider.LoadRangeAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));
        int started = source.CallCount;
        source.Release();
        await load;

        Assert.AreEqual(3, started);
    }

    /// <summary>
    /// Verifies that a range spanning a year downloads at most four months at once.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenRangeSpansMoreMonthsThanTheLimit_ShouldDownloadFourAtOnce()
    {
        ImfRateProviderOptions options = new() { EnableDiskCache = false };
        GatedImfRateTableSource source = new();
        using ImfRateProvider provider = new(source, options);

        Task load = provider.LoadRangeAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        int started = source.CallCount;
        source.Release();
        await load;

        Assert.AreEqual(4, started);
        Assert.AreEqual(12, source.CallCount);
    }

    /// <summary>
    /// Verifies that months downloaded at once are still published in date order, so a later month's rates never land
    /// ahead of an earlier month's in a series, even when the later month's report arrives first.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenALaterMonthDownloadsFirst_ShouldPublishMonthsInDateOrder()
    {
        ImfRateProviderOptions options = new() { EnableDiskCache = false };
        HeldImfRateTableSource source = new();
        StateCapturingLogger logger = new();
        using ImfRateProvider provider = new(source, options, logger);

        Task load = provider.LoadRangeAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));
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
