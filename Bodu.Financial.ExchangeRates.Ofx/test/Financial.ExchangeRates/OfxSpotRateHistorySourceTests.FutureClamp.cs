// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxSpotRateHistorySourceTests.FutureClamp.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Financial.ExchangeRates.Testing;
using Bodu.Test.Time;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that the OFX history source never asks the endpoint for a window extending past the current instant, which
/// the endpoint rejects with <c>PS:spotratehistoryrange:0005 "ToDate can not be in the future"</c>.
/// </summary>
public partial class OfxSpotRateHistorySourceTests
{
    /// <summary>The fixed instant the clamp tests resolve "now" against — mid-morning UTC, so a same-day request has a clampable remainder.</summary>
    private static readonly DateTimeOffset ClampNow = new(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a request whose end date is the current day caps the outgoing end bound at the current instant
    /// rather than the last millisecond of that date, which would lie in the endpoint's future.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenEndDateIsToday_ShouldClampEndBoundToNow()
    {
        StubHttpMessageHandler handler = new(OfxFixtures.ReadBytes(OfxFixtures.AudUsd));
        using HttpClient client = new(handler);
        OfxRateProvider provider = new(client, new OfxRateProviderOptions(), logger: null, timeProvider: new MutableTimeProvider(ClampNow));

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 15));

        long expectedEnd = ClampNow.ToUnixTimeMilliseconds();
        long unclampedEnd = new DateTimeOffset(2026, 6, 15, 23, 59, 59, 999, TimeSpan.Zero).ToUnixTimeMilliseconds();

        Assert.IsNotNull(handler.LastRequestUri);
        Assert.IsTrue(
            handler.LastRequestUri!.AbsolutePath.EndsWith($"/{expectedEnd}", StringComparison.Ordinal),
            $"end bound must be clamped to {expectedEnd}, was {handler.LastRequestUri.AbsolutePath}");
        Assert.IsFalse(
            handler.LastRequestUri.AbsolutePath.Contains(unclampedEnd.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal),
            "the end of the requested day must never be sent: the endpoint rejects a future ToDate");
    }

    /// <summary>
    /// Verifies that a request whose window lies entirely in the future issues no HTTP request at all and reports no
    /// data, rather than succeeding with an empty result — a success would let the provider mark the future range as
    /// covered and keep serving nothing once those dates became real.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWindowIsEntirelyInTheFuture_ShouldNotIssueRequestAndReportNoData()
    {
        StubHttpMessageHandler handler = new(OfxFixtures.ReadBytes(OfxFixtures.AudUsd));
        using HttpClient client = new(handler);
        OfxRateProvider provider = new(client, new OfxRateProviderOptions(), logger: null, timeProvider: new MutableTimeProvider(ClampNow));

        await Assert.ThrowsExactlyAsync<ExchangeRateFormatException>(async () =>
        {
            await provider.LoadPairAsync("AUD", "USD", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));
        });

        Assert.AreEqual(0, handler.RequestCount, "a wholly future window must not reach the endpoint");
    }

    /// <summary>
    /// Verifies that a request entirely in the past is sent unchanged, so the clamp costs recency only where the window
    /// actually reaches the present.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenEndDateIsInThePast_ShouldSendTheFullDay()
    {
        StubHttpMessageHandler handler = new(OfxFixtures.ReadBytes(OfxFixtures.AudUsd));
        using HttpClient client = new(handler);
        OfxRateProvider provider = new(client, new OfxRateProviderOptions(), logger: null, timeProvider: new MutableTimeProvider(ClampNow));

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

        long expectedEnd = new DateTimeOffset(2026, 5, 31, 23, 59, 59, 999, TimeSpan.Zero).ToUnixTimeMilliseconds();

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.IsTrue(
            handler.LastRequestUri!.AbsolutePath.EndsWith($"/{expectedEnd}", StringComparison.Ordinal),
            handler.LastRequestUri.AbsolutePath);
    }

    /// <summary>
    /// Verifies that a configured <see cref="OfxRateProviderOptions.FutureClampSkew" /> is subtracted from the current
    /// instant when capping the end bound, compensating for an endpoint clock that trails UTC.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenFutureClampSkewIsConfigured_ShouldSubtractItFromTheEndBound()
    {
        StubHttpMessageHandler handler = new(OfxFixtures.ReadBytes(OfxFixtures.AudUsd));
        using HttpClient client = new(handler);
        OfxRateProviderOptions options = new() { FutureClampSkew = TimeSpan.FromMinutes(5) };
        OfxRateProvider provider = new(client, options, logger: null, timeProvider: new MutableTimeProvider(ClampNow));

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 15));

        long expectedEnd = ClampNow.AddMinutes(-5).ToUnixTimeMilliseconds();

        Assert.IsNotNull(handler.LastRequestUri);
        Assert.IsTrue(
            handler.LastRequestUri!.AbsolutePath.EndsWith($"/{expectedEnd}", StringComparison.Ordinal),
            handler.LastRequestUri.AbsolutePath);
    }
}
