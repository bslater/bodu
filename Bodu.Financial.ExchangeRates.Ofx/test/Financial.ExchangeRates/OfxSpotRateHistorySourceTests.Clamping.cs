// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxSpotRateHistorySourceTests.Clamping.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that the source caps a request's end bound at the current instant so OFX never receives a future
/// <c>ToDate</c>, regardless of the timezone a caller derived its dates from.
/// </summary>
public partial class OfxSpotRateHistorySourceTests
{
    /// <summary>The fixed instant tests resolve "now" against: midday UTC on 2026-07-01.</summary>
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that when the requested end date is the current UTC day, the end bound is capped at the current instant
    /// rather than inflated to the last millisecond of the day (which would be in OFX's future).
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenEndDateIsToday_ShouldCapEndBoundAtNow()
    {
        Uri uri = await CaptureRequestUri(FixedNow, new OfxExchangeRateOptions(), new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1));

        long expectedEnd = FixedNow.ToUnixTimeMilliseconds();
        Assert.IsTrue(uri.AbsolutePath.EndsWith($"/{expectedEnd}", StringComparison.Ordinal), uri.AbsolutePath);
    }

    /// <summary>
    /// Verifies that a future end date (as a caller in a timezone ahead of UTC could supply) is capped at the current
    /// instant, so OFX never sees a <c>ToDate</c> beyond the present.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenEndDateInFuture_ShouldCapEndBoundAtNow()
    {
        Uri uri = await CaptureRequestUri(FixedNow, new OfxExchangeRateOptions(), new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 2));

        long expectedEnd = FixedNow.ToUnixTimeMilliseconds();
        Assert.IsTrue(uri.AbsolutePath.EndsWith($"/{expectedEnd}", StringComparison.Ordinal), uri.AbsolutePath);
    }

    /// <summary>
    /// Verifies that a wholly past end date is left as the last millisecond of that day, so the clamp never shortens a
    /// legitimate historical window.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenEndDateInPast_ShouldUseEndOfDay()
    {
        Uri uri = await CaptureRequestUri(FixedNow, new OfxExchangeRateOptions(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        long expectedEnd = new DateTimeOffset(2026, 1, 31, 23, 59, 59, 999, TimeSpan.Zero).ToUnixTimeMilliseconds();
        Assert.IsTrue(uri.AbsolutePath.EndsWith($"/{expectedEnd}", StringComparison.Ordinal), uri.AbsolutePath);
    }

    /// <summary>
    /// Verifies that a non-zero <see cref="OfxExchangeRateOptions.FutureClampSkew" /> caps the end bound at the current
    /// instant less the skew, compensating for an OFX server clock that trails UTC.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenFutureClampSkewSet_ShouldCapEndBoundAtNowMinusSkew()
    {
        OfxExchangeRateOptions options = new() { FutureClampSkew = TimeSpan.FromHours(2) };

        Uri uri = await CaptureRequestUri(FixedNow, options, new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1));

        long expectedEnd = (FixedNow - TimeSpan.FromHours(2)).ToUnixTimeMilliseconds();
        Assert.IsTrue(uri.AbsolutePath.EndsWith($"/{expectedEnd}", StringComparison.Ordinal), uri.AbsolutePath);
    }

    /// <summary>
    /// Verifies that when the whole requested window begins after the current instant, no HTTP request is issued and the
    /// pair yields no observations.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRangeEntirelyInFuture_ShouldNotIssueRequest()
    {
        StubHttpMessageHandler handler = new(OfxFixtures.ReadBytes(OfxFixtures.AudUsd));
        using HttpClient client = new(handler);
        MutableTimeProvider time = new(FixedNow);
        OfxExchangeRateProvider provider = new(client, new OfxExchangeRateOptions(), logger: null, timeProvider: time);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 10));

        Assert.AreEqual(0, handler.RequestCount);

        ExchangeRateRangeResult range = provider.GetRates("AUD", "USD", new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 10));
        Assert.AreEqual(0, range.Count);
    }

    /// <summary>
    /// Loads a pair through a stubbed HTTP client with a fixed clock and returns the request URI the source produced.
    /// </summary>
    /// <param name="now">The instant the provider resolves "now" against.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="startDate">The inclusive range start requested.</param>
    /// <param name="endDate">The inclusive range end requested.</param>
    /// <returns>The captured absolute request URI.</returns>
    private static async Task<Uri> CaptureRequestUri(DateTimeOffset now, OfxExchangeRateOptions options, DateOnly startDate, DateOnly endDate)
    {
        StubHttpMessageHandler handler = new(OfxFixtures.ReadBytes(OfxFixtures.AudUsd));
        using HttpClient client = new(handler);
        MutableTimeProvider time = new(now);
        OfxExchangeRateProvider provider = new(client, options, logger: null, timeProvider: time);

        await provider.LoadPairAsync("AUD", "USD", startDate, endDate);

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);
        return handler.LastRequestUri!;
    }
}
