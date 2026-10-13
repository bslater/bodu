// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeCsvRateTableSourceTests.GetTableAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class BoeCsvRateTableSourceTests
{
    /// <summary>
    /// Verifies that a range not cached is requested for every configured series, cached once, and looked up with the
    /// configured refresh interval.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenNotCached_ShouldRequestTheRangeAndCacheIt()
    {
        BoeRateProviderOptions options = new() { RefreshInterval = TimeSpan.FromHours(5) };
        BoeCsvRateTableSource source = CreateSource(out StubHttpMessageHandler handler, out RecordingCache cache, options);

        BoeRateTable table = await source.GetTableAsync(s_from, s_to);

        string[] codes = options.Series.Select(static series => series.SeriesCode).ToArray();
        Assert.AreEqual(options.Endpoint.BuildRequestUrl(codes, s_from, s_to), handler.LastRequestUri);
        Assert.AreEqual(1, cache.StoreCount);
        Assert.AreEqual(TimeSpan.FromHours(5), cache.LastRefreshInterval);
        Assert.IsNotEmpty(table.Observations);
    }

    /// <summary>
    /// Verifies that a cached range is parsed without a request and without storing it again.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenCached_ShouldNotIssueARequest()
    {
        BoeCsvRateTableSource source = CreateSource(out StubHttpMessageHandler handler, out RecordingCache cache);
        cache.Seed((s_from, s_to), BoeFixtures.ReadBytes(BoeFixtures.Sample));

        BoeRateTable table = await source.GetTableAsync(s_from, s_to);

        Assert.AreEqual(0, handler.RequestCount);
        Assert.AreEqual(0, cache.StoreCount);
        Assert.IsNotEmpty(table.Observations);
    }

    /// <summary>
    /// Verifies that a failed download propagates and leaves nothing in the cache.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenDownloadFails_ShouldCacheNothing()
    {
        RecordingCache cache = new();
        using HttpClient client = new(new StubHttpMessageHandler([], HttpStatusCode.ServiceUnavailable));
        BoeCsvRateTableSource source = new(client, new BoeRateProviderOptions(), cache);

        _ = await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            _ = await source.GetTableAsync(s_from, s_to);
        });

        Assert.AreEqual(0, cache.StoreCount);
    }
}
