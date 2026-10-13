// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairWebRateProviderTests.LoadSequence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Time;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class PairWebRateProviderTests
{
    /// <summary>The event id of the <c>PairLoadStarting</c> diagnostic.</summary>
    private const int PairLoadStartingEventId = 4401;

    /// <summary>The event id of the <c>PairLoaded</c> diagnostic.</summary>
    private const int PairLoadedEventId = 4402;

    /// <summary>
    /// Verifies that a load logs its start at the download-starting level and its completion, with the number of
    /// observations it added, at the download-completed level, once each.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenLoaded_ShouldLogStartAndCompletionOnce()
    {
        StubPairSource source = new(new RateObservation(Known, 0.6828m), new RateObservation(Known.AddDays(1), 0.6830m));
        CapturingLogger logger = new();
        using TestPairWebRateProvider provider = new(source, new TestWebRateProviderOptions(), logger);

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        Assert.AreEqual(1, logger.Entries.Count(entry => entry.EventId.Id == PairLoadStartingEventId && entry.Level == LogLevel.Debug));
        (LogLevel Level, EventId EventId, string Message, Exception? Exception) completed =
            logger.Entries.Single(entry => entry.EventId.Id == PairLoadedEventId);
        Assert.AreEqual(LogLevel.Information, completed.Level);
        Assert.Contains("AUD/USD", completed.Message);
        Assert.Contains(" 2 ", completed.Message);
    }

    /// <summary>
    /// Verifies that a pair already covered logs nothing and makes no fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWindowAlreadyCovered_ShouldLogNothing()
    {
        StubPairSource source = new(new RateObservation(Known, 0.6828m));
        CapturingLogger logger = new();
        using TestPairWebRateProvider provider = new(source, new TestWebRateProviderOptions(), logger);
        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);
        int entries = logger.Entries.Count;

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        Assert.AreEqual(entries, logger.Entries.Count);
        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that every rate a load adds is stamped with the instant the fetch completed, read from the provider's
    /// time source.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenLoaded_ShouldStampTheFetchInstant()
    {
        DateTimeOffset now = new(2023, 2, 1, 8, 30, 0, TimeSpan.Zero);
        StubPairSource source = new(new RateObservation(Known, 0.6828m));
        using TestPairWebRateProvider provider = new(source, new TestWebRateProviderOptions(), timeProvider: new MutableTimeProvider(now));

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        Assert.AreEqual(now, provider.GetLoadedBook().EnumerateSeries().Single().FetchedAtUtc);
        Assert.AreEqual(now, provider.GetRate("AUD", "USD", Known, RateLookupOptions.Exact).Rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that a fetch that fails records no coverage, so the next request fetches the window again and the
    /// failure is surfaced unchanged to its caller.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenFetchFails_ShouldFetchAgainOnTheNextRequest()
    {
        StubPairSource source = new(new RateObservation(Known, 0.6828m)) { FailNext = new HttpRequestException("boom") };
        using TestPairWebRateProvider provider = new(source, new TestWebRateProviderOptions());

        HttpRequestException ex = await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);
        });
        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        Assert.AreEqual("boom", ex.Message);
        Assert.AreEqual(2, source.CallCount);
        Assert.AreEqual(0.6828m, provider.GetRate("AUD", "USD", Known, RateLookupOptions.Exact).Rate.Rate);
    }

    /// <summary>
    /// Verifies that the series metadata a load returns is published for discovery, keyed by its pair.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenLoaded_ShouldPublishSeriesMetadataAndLoadedPair()
    {
        StubPairSource source = new(new RateObservation(Known, 0.6828m));
        using TestPairWebRateProvider provider = new(source, new TestWebRateProviderOptions());

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        Assert.AreEqual("AUD/USD", provider.GetAvailablePairs().Single());
        Assert.AreEqual(new CurrencyPair(Currencies.CurrencyCode.AUD, Currencies.CurrencyCode.USD), provider.GetLoadedPairs().Single());
    }
}
